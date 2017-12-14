using System;
using System.Collections.Generic;
using System.IO;
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

        public WebAsset WithNewContent(WebAssetContent content)
            => new WebAsset(Path, 
                Metadata
                    .WithContentLength(null) // contentLength could be changed
                    .WithLastModifiedAt(DateTime.UtcNow) // as well as content itself
                , content);
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

        WebAssetContent()
        {
        }

        public WebAssetContent Buffered() => FromBuffer(Buffer());

        public static WebAssetContent FromBuffer(byte[] content)
            => new WebAssetContent { content = content };

        public static WebAssetContent FromStream(Stream stream)
            => new WebAssetContent { stream = stream };

        public Stream Stream() => stream ?? new MemoryStream(content);

        public byte[] Buffer()
            => content ?? ReadStream();

        public bool IsBuffered => content != null;

        public string GetString(Encoding encoding = null)
            => ResolveEncoding(encoding).GetString(Buffered().Buffer());

        public WebAssetContent Replace(IEnumerable<(string, string)> replacements, Encoding encoding = null)
        {
            var sb = new StringBuilder(GetString(encoding));
            foreach (var replacement in replacements)
            {
                sb.Replace(replacement.Item1, replacement.Item2);
            }

            return FromBuffer(ResolveEncoding(encoding).GetBytes(sb.ToString()));
        }

        public Encoding ResolveEncoding(Encoding encoding)
            => encoding ?? Encoding.UTF8;

        byte[] ReadStream()
        {
            using (stream)
            {
                return StreamUtil.ReadFully(stream);
            }
        }
    }
}
