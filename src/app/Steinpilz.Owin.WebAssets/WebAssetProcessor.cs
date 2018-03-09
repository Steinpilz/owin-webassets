using Microsoft.Owin;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Steinpilz.Owin.WebAssets
{
    class WebAssetProcessors : IWebAssetProcessor
    {
        private readonly IEnumerable<IWebAssetProcessor> processors;

        public WebAssetProcessors(
            IEnumerable<IWebAssetProcessor> processors)
        {
            this.processors = processors;
        }

        public async Task<WebAsset> ProcessAsync(WebAsset content, IOwinRequest request)
            => await processors.Aggregate(
                Task.FromResult(content), 
                async (acc, processor) => await processor.ProcessAsync(await acc, request)
                );
    }

    public interface IWebAssetProcessor
    {
        Task<WebAsset> ProcessAsync(WebAsset webAsset, IOwinRequest request);
    }
}
