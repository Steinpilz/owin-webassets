using System;
using System.Collections.Generic;
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

        public WebAssetsOwinHandler(
            IFileSystem fileSystem,
            IWebAssetProcessor webAssetProcessor,
            IContentTypeProvider contentTypeProvider,
            string fallbackAssetPath
            )
        {
            this.fileSystem = fileSystem;
            this.webAssetProcessor = webAssetProcessor;
            this.contentTypeProvider = contentTypeProvider;
            this.fallbackAssetPath = fallbackAssetPath;
        }

        public async Task<bool> Handle(IOwinContext context)
        {
            var asset = ResolveAsset(context.Request);
            if (asset == null)
            {
                return false;
            }

            var processedAsset = this.webAssetProcessor.Process(asset, context.Request);

            var assetContent = processedAsset.Content;
            if (processedAsset.Metadata.ContentLength == null)
                assetContent = assetContent.Buffered();

            context.Response.ContentLength = processedAsset.Metadata.ContentLength
                ?? assetContent.Buffer().Length;

            this.contentTypeProvider.TryGetContentType(processedAsset.Metadata.FileName, out string contentType);

            context.Response.ContentType = processedAsset.Metadata.ContentType ?? contentType;

            await context.Response.WriteAsync(assetContent.Buffer());
            return true;
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
