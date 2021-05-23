using Nop.Core.Infrastructure;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public class FillStagingTask : IScheduleTask
    {
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;

        public FillStagingTask(
            ILogger logger,
            ImportSettings importSettings)
        {
            _logger = logger;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            

            await EngineContext.Current.Resolve<FillStagingProductsTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<FillStagingPricingTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<FillStagingAccessoriesTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<FillStagingBrandsTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<FillStagingProductCategoryMappingsTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<FillStagingScandownEndDatesTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<FillStagingWarrantiesTask>().ExecuteAsync();

            
        }
    }
}
