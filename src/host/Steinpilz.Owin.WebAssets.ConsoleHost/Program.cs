using Microsoft.Owin;
using Microsoft.Owin.FileSystems;
using System;

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
        public WebAsset Process(WebAsset webAsset, IOwinRequest request)
        {
            return webAsset.WithNewContent(webAsset.Content.Replace(new[] { ("{BASE_HREF}", request.PathBase.Value + "/") }));
        }
    }
}