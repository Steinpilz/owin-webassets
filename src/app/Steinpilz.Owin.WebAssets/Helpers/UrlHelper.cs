using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steinpilz.Owin.WebAssets.Helpers
{
    public class UrlHelper
    {
        private readonly IOwinRequest request;
        
        string clientBaseHref;
        string clientHost;
        string clientPort;
        string clientScheme;

        /// <summary>
        /// Original Uri sent from the client
        /// </summary>
        public Uri OriginalClientRequestUri { get; private set; }

        /// <summary>
        /// Base href of the request. Support X-Forwarded-* headers and Owin hosting context.
        /// The value has leading slash and hos not tailing slash
        /// </summary>
        public string ClientBaseHref { get; private set; }

        internal UrlHelper(IOwinRequest request)
        {
            this.request = request;
                 
            Init();
        }

        void Init()
        {
            InitBaseHref();
            this.clientHost = request.Headers["X-Forwarded-Host"] ?? request.Uri.Host;
            this.clientPort = request.Headers["X-Forwarded-Port"] ?? ToStringSafe(request.Uri.Port) ?? "";
            this.clientScheme = request.Headers["X-Forwarded-Proto"] ?? request.Scheme;
            this.OriginalClientRequestUri
                = new Uri($"{this.clientScheme}://{this.clientHost}{ValuablePortString()}{this.clientBaseHref}{request.Path.ToUriComponent()}{request.QueryString.ToUriComponent()}");
            this.ClientBaseHref = this.clientBaseHref;
        }

        string ValuablePortString()
            => (this.clientScheme == "http" && this.clientPort == "80"
            || this.clientScheme == "https" && this.clientPort == "443")
            ? ""
            : $":{this.clientPort}";

        string ToStringSafe(int? value)
            => value == null ? null : value.Value.ToString();

        void InitBaseHref()
        {
            var virtualFolder = request.PathBase.Value;
            var stripPath = request.Headers["X-Forwarded-Strip"] ?? "";
            var prefix = request.Headers["X-Forwarded-Prefix"] ?? request.Headers["X-Virtual-Directory"] ?? "";

            if (virtualFolder.StartsWith(stripPath, StringComparison.OrdinalIgnoreCase))
                virtualFolder = virtualFolder.Remove(0, stripPath.Length);

            // Properly concat and normalize base href to avoid double slashes, etc.
            // Do not apply trailing slash to allow any kind of a folder or document (.html) to serve as a href.
            var baseHref = UriStringExtensions.UrlCombine(prefix, virtualFolder).TrimSlashes().WithLeadingSlash();

            this.clientBaseHref = baseHref;
        }
    }

    public static class OwinContextExtensions
    {
        public static UrlHelper UrlHelper(this IOwinContext owinContext)
            => new UrlHelper(owinContext.Request);

        public static UrlHelper UrlHelper(this IOwinRequest owinRequest)
            => new UrlHelper(owinRequest);
    }

    public static class UriStringExtensions
    {
        public static string TrimSlashes(this string path)
            => path.Trim('/');

        public static string RemoveLeadingSlash(this string uri)
            => uri.TrimStart(new[] { '/' });

        public static string RemoveTailingSlash(this string uri)
            => uri.TrimEnd(new[] { '/' });

        public static string WithLeadingSlash(this string uri)
            => uri.StartsWithSlash() ? uri : $"/{uri.RemoveLeadingSlash()}";

        public static string WithTailingSlash(this string uri)
            => uri.EndsWithSlash() ? uri : $"{uri.RemoveTailingSlash()}/";

        public static bool EndsWithSlash(this string uri)
            => uri.EndsWith("/");

        public static bool StartsWithSlash(this string uri)
            => uri.StartsWith("/");

        public static string UrlCombine(params string[] urlParts) =>
            string.Join("/", urlParts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(TrimSlashes));
    }
}
