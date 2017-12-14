# Steinpilz.Owin.WebAssets

## Introduction

Owin middleware to serve static web assets from a given source with a posibility to process asset's content for each request.

It's distributed as a NuGet package `Steinpilz.Owin.WebAssets`

## Sample usage

```csharp
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
```

## FAQ

## Questions & Issues

Use built-in gitlab [issue tracker](https://github.com/Steinpilz/owin-webassets/issues)

## Maintainers
@ivanbenko

## Contribution

* Setup development environment:

1. Clone the repo
2. ```.paket\paket restore``` 
3. ```build target=build```
