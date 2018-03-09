using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles.ContentTypes;

namespace Steinpilz.Owin.WebAssets
{
    class WebAssetsOwinHandler 
    {
        private readonly string owinMountPath;
        private readonly IFileSystem fileSystem;
        private readonly IWebAssetProcessor webAssetProcessor;
        private readonly IContentTypeProvider contentTypeProvider;
        private readonly string fallbackAssetPath;
        private readonly bool isStatic;

        public WebAssetsOwinHandler(
            IFileSystem fileSystem,
            IWebAssetProcessor webAssetProcessor,
            IContentTypeProvider contentTypeProvider,
            string fallbackAssetPath,
            bool isStatic
            )
        {
            this.fileSystem = fileSystem;
            this.webAssetProcessor = webAssetProcessor;
            this.contentTypeProvider = contentTypeProvider;
            this.fallbackAssetPath = fallbackAssetPath;
            this.isStatic = isStatic;
        }

        public async Task<bool> Handle(IOwinContext context)
        {
            var asset = ResolveAsset(context.Request);
            if (asset == null) return false;
            if (NotModified(context, asset)) return true;

            var processedAsset = await ProcessAsset(context, asset);

            await WriteAssetContent(context, processedAsset);

            return true;
        }

        private bool NotModified(IOwinContext context, WebAsset asset)
        {
            if (asset.Metadata.LastModifiedAt != null)
            {
                var ifModifiedSinceHeader = context.Request.Headers["If-Modified-Since"];
                if (DateTime.TryParse(ifModifiedSinceHeader ?? "", out DateTime ifModifiedSince))
                {
                    var lastModifiedNorm = NormalizeDate(asset.Metadata.LastModifiedAt.Value);
                    var ifModifiedSinceNorm = NormalizeDate(ifModifiedSince);

                    if (lastModifiedNorm <= ifModifiedSinceNorm)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                        return true;
                    }
                }
            }
            return false;
        }

        private static async Task WriteAssetContent(IOwinContext context, WebAsset processedAsset)
        {
            using (var assetStream = processedAsset.Content.Stream())
            {
                await StreamUtil.CopyAsync(assetStream, context.Response.Body);
            }
        }

        private async Task<WebAsset> ProcessAsset(IOwinContext context, WebAsset asset)
        {
            var processedAsset = await this.webAssetProcessor.ProcessAsync(asset, context.Request);

            if (processedAsset.Metadata.LastModifiedAt != null)
            {
                context.Response.Headers.Append("Last-Modified",
                    processedAsset.Metadata.LastModifiedAt.Value.ToUniversalTime().ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'")
                    );
            }

            if (processedAsset.Metadata.ContentLength != null)
                context.Response.ContentLength = processedAsset.Metadata.ContentLength;

            // set content length only for buffered
            if (context.Response.ContentLength == null && processedAsset.Content.IsBuffered)
                context.Response.ContentLength = processedAsset.Content.Buffer().Length;

            this.contentTypeProvider.TryGetContentType(processedAsset.Metadata.FileName, out string contentType);

            context.Response.ContentType = processedAsset.Metadata.ContentType ?? contentType;

            var contentEncodingHeader = ContentEncodingHeader(processedAsset.Content);
            if (contentEncodingHeader != null)
                context.Response.Headers.Append("Content-Encoding", contentEncodingHeader);

            return processedAsset;
        }

        string ContentEncodingHeader(WebAssetContent content)
        {
            switch(content.Encoding)
            {
                case ContentEncoding.Deflate: return "deflate";
                case ContentEncoding.GZip: return "gzip";

                case ContentEncoding.Raw:
                default:
                    return null;
            }
        }

        DateTime NormalizeDate(DateTime dateTime)
        {
            var utc = dateTime.ToUniversalTime();

            // ignore everything after seconds
            return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, DateTimeKind.Utc);
        }

        WebAsset ResolveAsset(IOwinRequest request)
            => ReadWebAsset(request.Path.ToString()) ?? ReadWebAsset(fallbackAssetPath);

        WebAsset ReadWebAsset(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (this.fileSystem.TryGetFileInfo(path, out IFileInfo fileInfo))
                return ToWebAsset(path, fileInfo);

            return null;
        }

        WebAsset ToWebAsset(string path, IFileInfo fileInfo)
            => new WebAsset(
                path, 
                new WebAssetMetadata(fileInfo.Name, fileInfo.Length, fileInfo.LastModified, null), 
                WebAssetContent.FromStream(fileInfo.CreateReadStream())
            );
    }
}
