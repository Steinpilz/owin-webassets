using Microsoft.Owin;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Steinpilz.Owin.WebAssets
{
    class WebAssetCompressor : IWebAssetProcessor
    {
        private readonly Func<WebAsset, bool> compressFilter;

        public WebAssetCompressor(Func<WebAsset, bool> compressionFilter)
        {
            this.compressFilter = compressionFilter ?? throw new ArgumentNullException(nameof(compressionFilter));
        }

        static ContentEncoding?[] supportedEncodings = new ContentEncoding?[] { ContentEncoding.Deflate, ContentEncoding.GZip };
        public async Task<WebAsset> ProcessAsync(WebAsset webAsset, IOwinRequest request)
        {
            var acceptEncoding = AcceptEncoding(request);
            var encoding = supportedEncodings.FirstOrDefault(x => acceptEncoding.Contains(x.Value));
            if (encoding == null)
                return webAsset;

            if (!this.compressFilter(webAsset))
                return webAsset;

            var encodedContent = await webAsset.Content.EncodeAsync(encoding.Value).ConfigureAwait(false);

            return webAsset.WithNewContent(encodedContent);
        }

        static ContentEncoding[] empty = new ContentEncoding[0];
        ContentEncoding[] AcceptEncoding(IOwinRequest request)
        {
            var headerValue = request.Headers["Accept-Encoding"];
            if (headerValue == null) return empty;

            return headerValue.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(TryParse).Where(x => x != null).Select(x => x.Value).ToArray();
        }

        ContentEncoding? TryParse(string value)
        {
            switch (value.ToLower())
            {
                case "deflate": return ContentEncoding.Deflate;
                case "gzip": return ContentEncoding.GZip;
                default:  return null;
            }
        }
    }

    static class CompressionDefaults
    {
        static string[] extensionToCompress = new[]
        {
            ".js" ,
            ".css" ,
            ".yml"  ,
            ".json" ,
            ".svg"  ,
            ".txt"  ,
            ".html" ,
            ".map"  ,
            ".ttf"  ,
            ".otf"
        };

        public static bool ShouldCompress(WebAsset request)
            => extensionToCompress.Any(ext => request.Path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}
