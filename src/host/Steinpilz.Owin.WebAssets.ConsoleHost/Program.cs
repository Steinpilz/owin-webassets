using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using System;
using System.Threading.Tasks;

namespace Steinpilz.Owin.WebAssets.ConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Microsoft.Owin.Hosting.WebApp.Start("http://localhost:5008", appBuilder =>
            {
                appBuilder.UseWebAssets("/ui", opt => opt.UseFileSystem(
                    new EmbeddedResourceFileSystem(typeof(Program).Assembly, "Steinpilz.Owin.WebAssets.ConsoleHost.assets")
                    ).AddWebAssetProcessor(new BaseHrefProcessor()));
            }))
            {

                Console.ReadLine();
            }
        }
    }

    class BaseHrefProcessor : IWebAssetProcessor
    {
        public async Task<WebAsset> ProcessAsync(WebAsset webAsset, IOwinRequest request)
        {
            //   return webAsset;
            var newContent = await webAsset.Content.ReplaceAsync(new[] { ("{BASE_HREF}", request.PathBase.Value + "/") });
            var bufferedNewContent = await newContent.BufferedAsync();

            return webAsset
                .WithNewContent(bufferedNewContent)
                .WithMetadata(webAsset.Metadata.WithContentLength(bufferedNewContent.Buffer().Length));
        }
    }
}