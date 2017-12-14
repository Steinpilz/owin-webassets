using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steinpilz.Owin.WebAssets
{
    class WebAssetProcessors : IWebAssetProcessor
    {
        private readonly IEnumerable<IWebAssetProcessor> processors;

        public WebAssetProcessors(IEnumerable<IWebAssetProcessor> processors)
        {
            this.processors = processors;
        }

        public WebAsset Process(WebAsset content, IOwinRequest request)
            => processors.Aggregate(content, (acc, processor) => processor.Process(acc, request));
    }

    public interface IWebAssetProcessor
    {
        WebAsset Process(WebAsset content, IOwinRequest request);
    }
}
