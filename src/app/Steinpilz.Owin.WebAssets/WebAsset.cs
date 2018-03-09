using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steinpilz.Owin.WebAssets
{
    public class WebAsset
    {
        public string Path { get; }
        public WebAssetMetadata Metadata { get; }
        public WebAssetContent Content { get; }

        public WebAsset(
            string path,
            WebAssetMetadata metadata,
            WebAssetContent content
            )
        {
            Path = path;
            Metadata = metadata;
            Content = content;
        }

        public WebAsset WithMetadata(WebAssetMetadata metadata)
            => new WebAsset(Path, metadata, Content);

        public WebAsset WithNewContent(WebAssetContent content)
            => new WebAsset(Path,
                Metadata.WithContentLength(null), content);
    }

    public class WebAssetMetadata
    {
        public string FileName { get; }
        public long? ContentLength { get; }
        public DateTime? LastModifiedAt { get; }
        public string ContentType { get; }

        public WebAssetMetadata(
            string fileName,
            long? contentLength,
            DateTime? lastModifiedAt,
            string contentType
            )
        {
            FileName = fileName;
            ContentLength = contentLength;
            LastModifiedAt = lastModifiedAt;
            ContentType = contentType;
        }

        public WebAssetMetadata WithContentLength(long? contentLength)
            => new WebAssetMetadata(FileName, contentLength, LastModifiedAt, ContentType);

        public WebAssetMetadata WithLastModifiedAt(DateTime? lastModifiedAt)
            => new WebAssetMetadata(FileName, ContentLength, lastModifiedAt, ContentType);
    }

    public class WebAssetContent
    {
        Stream stream;
        byte[] content;
        ContentEncoding encoding;

        public ContentEncoding Encoding => encoding;
        public bool IsRawEncoding => Encoding == ContentEncoding.Raw;

        WebAssetContent()
        {
            encoding = ContentEncoding.Raw;
        }

        public WebAssetContent Buffered() => BufferedAsync().Result;
        public async Task<WebAssetContent> BufferedAsync() => FromBuffer(await BufferAsync().ConfigureAwait(false));

        public static WebAssetContent FromBuffer(byte[] content)
            => new WebAssetContent { content = content };

        public static WebAssetContent FromStream(Stream stream)
            => new WebAssetContent { stream = stream };

        WebAssetContent WithEncoding(ContentEncoding encoding)
            => new WebAssetContent { stream = stream, content = content, encoding = encoding };


        public WebAssetContent Encode(ContentEncoding encoding)
            => EncodeAsync(encoding).Result;

        public async Task<WebAssetContent> EncodeAsync(ContentEncoding encoding)
        {
            if (this.encoding == encoding)
                return this;

            if(encoding == ContentEncoding.Raw)
            {
                switch(this.encoding)
                {
                    case ContentEncoding.Deflate:
                        return FromStream(new DeflateStream(Stream(), CompressionMode.Decompress));
                    case ContentEncoding.GZip:
                        return FromStream(new GZipStream(Stream(), CompressionMode.Decompress));
                    default:
                        throw new NotImplementedException();
                }
            }

            var rawContent = IsRawEncoding ? this : await this.EncodeRawAsync().ConfigureAwait(false);

            switch(encoding)
            {
                case ContentEncoding.Deflate:
                    return FromBuffer(await CompressToDeflate(rawContent.Stream()))
                        .WithEncoding(encoding);
                case ContentEncoding.GZip:
                    return FromBuffer(await CompressToGZip(rawContent.Stream()))
                        .WithEncoding(encoding);
                default:
                    throw new NotImplementedException();
            }
        }

        public WebAssetContent EncodeRaw()
            => EncodeRawAsync().Result;

        public Task<WebAssetContent> EncodeRawAsync()
            => EncodeAsync(ContentEncoding.Raw);

        public Stream Stream() => stream ?? new MemoryStream(content);

        public byte[] Buffer() => BufferAsync().Result;

        public async Task<byte[]> BufferAsync()
            => content ?? await ReadStream().ConfigureAwait(false);

        public bool IsBuffered => content != null;

        public string GetString(Encoding encoding = null)
            => GetStringAsync(encoding).Result;

        public async Task<string> GetStringAsync(Encoding encoding = null)
            => ResolveEncoding(encoding).GetString((await BufferedAsync()).Buffer());

        public WebAssetContent Replace(IEnumerable<(string, string)> replacements, Encoding encoding = null)
            => ReplaceAsync(replacements, encoding).Result;

        public async Task<WebAssetContent> ReplaceAsync(IEnumerable<(string, string)> replacements, Encoding encoding = null)
        {
            var sb = new StringBuilder(await GetStringAsync(encoding));
            foreach (var replacement in replacements)
            {
                sb.Replace(replacement.Item1, replacement.Item2);
            }

            return FromBuffer(ResolveEncoding(encoding).GetBytes(sb.ToString()));
        }

        public Encoding ResolveEncoding(Encoding encoding)
            => encoding ?? System.Text.Encoding.UTF8;

        async Task<byte[]> ReadStream()
        {
            using (stream)
            {
                return await StreamUtil.ReadFullyAsync(stream).ConfigureAwait(false);
            }
        }

        static async Task<byte[]> CompressToDeflate(Stream input)
        {
            var output = new MemoryStream();
            using (var zippedStream = new DeflateStream(output, CompressionMode.Compress))
                await StreamUtil.CopyAsync(input, zippedStream).ConfigureAwait(false);

            return output.ToArray();
        }

        static async Task<byte[]> CompressToGZip(Stream input)
        {
            var output = new MemoryStream();
            using(input)
            using (var zippedStream = new GZipStream(output, CompressionMode.Compress))
                await StreamUtil.CopyAsync(input, zippedStream).ConfigureAwait(false);

            return output.ToArray();
        }
    }

    public enum ContentEncoding
    {
        Raw,
        GZip,
        Deflate
    }
}
