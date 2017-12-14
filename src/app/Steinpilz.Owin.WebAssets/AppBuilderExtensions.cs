using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles.ContentTypes;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steinpilz.Owin.WebAssets
{
    public static class AppBuilderExtensions
    {
        public static IAppBuilder UseWebAssets(this IAppBuilder appBuilder, Action<WebAssetsConfig> configuration = null)
        {
            var config = new WebAssetsConfig();
            configuration?.Invoke(config);

            var handler = new Lazy<WebAssetsOwinHandler>(() => new WebAssetsOwinHandler(
                    config.FileSystem,
                    new WebAssetProcessors(config.WebAssetProcessors),
                    new FileExtensionContentTypeProvider(),
                    config.FallbackAsset
                    ));

            appBuilder.Use(async (context, next) => {
                var webAssetFound = await handler.Value.Handle(context);
                if (!webAssetFound)
                    await next();
            });

            return appBuilder;
        }

        public static IAppBuilder UseWebAssets(this IAppBuilder appBuilder, string mountPath, Action<WebAssetsConfig> configuration = null)
        {
            return appBuilder.Map(PathString.FromUriComponent(mountPath), b => b.UseWebAssets(configuration));
        }
    }

    public class WebAssetsConfig
    {
        public IFileSystem FileSystem { get; private set; }
        public List<IWebAssetProcessor> WebAssetProcessors { get; private set; } = new List<IWebAssetProcessor>();
        public string FallbackAsset { get; private set; } = "/index.html";

        public WebAssetsConfig UseFileSystem(IFileSystem fileSystem)
        {
            FileSystem = fileSystem;
            return this;
        }

        public WebAssetsConfig AddWebAssetProcessor(IWebAssetProcessor webAssetProcessor)
        {
            WebAssetProcessors.Add(webAssetProcessor);
            return this;
        }

        public WebAssetsConfig WithFallbackAsset(string fallbackAsset)
        {
            FallbackAsset = fallbackAsset;
            return this;
        }
    }
}
