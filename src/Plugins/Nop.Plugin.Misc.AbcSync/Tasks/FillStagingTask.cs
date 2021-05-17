using Nop.Core.Infrastructure;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

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

        public void Execute()
        {
            this.LogStart();

            EngineContext.Current.Resolve<FillStagingProductsTask>().Execute();
            EngineContext.Current.Resolve<FillStagingPricingTask>().Execute();
            EngineContext.Current.Resolve<FillStagingAccessoriesTask>().Execute();
            EngineContext.Current.Resolve<FillStagingBrandsTask>().Execute();
            EngineContext.Current.Resolve<FillStagingProductCategoryMappingsTask>().Execute();
            EngineContext.Current.Resolve<FillStagingScandownEndDatesTask>().Execute();
            EngineContext.Current.Resolve<FillStagingWarrantiesTask>().Execute();

            this.LogEnd();
        }
    }
}
