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

        private readonly FillStagingProductsTask _fillStagingProductsTask;
        private readonly FillStagingPricingTask _fillStagingPricingTask;
        private readonly FillStagingAccessoriesTask _fillStagingAccessoriesTask;
        private readonly FillStagingBrandsTask _fillStagingBrandsTask;
        private readonly FillStagingProductCategoryMappingsTask _fillStagingProductCategoryMappingsTask;
        private readonly FillStagingScandownEndDatesTask _fillStagingScandownEndDatesTask;
        private readonly FillStagingWarrantiesTask _fillStagingWarrantiesTask;

        public FillStagingTask(
            ILogger logger,
            ImportSettings importSettings,
            FillStagingProductsTask fillStagingProductsTask,
            FillStagingPricingTask fillStagingPricingTask,
            FillStagingAccessoriesTask fillStagingAccessoriesTask,
            FillStagingBrandsTask fillStagingBrandsTask,
            FillStagingProductCategoryMappingsTask fillStagingProductCategoryMappingsTask,
            FillStagingScandownEndDatesTask fillStagingScandownEndDatesTask,
            FillStagingWarrantiesTask fillStagingWarrantiesTask)
        {
            _logger = logger;
            _importSettings = importSettings;

            _fillStagingProductsTask = fillStagingProductsTask;
            _fillStagingPricingTask = fillStagingPricingTask;
            _fillStagingAccessoriesTask = fillStagingAccessoriesTask;
            _fillStagingBrandsTask = fillStagingBrandsTask;
            _fillStagingProductCategoryMappingsTask = fillStagingProductCategoryMappingsTask;
            _fillStagingScandownEndDatesTask = fillStagingScandownEndDatesTask;
            _fillStagingWarrantiesTask = fillStagingWarrantiesTask;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            await _fillStagingProductsTask.ExecuteAsync();
            await _fillStagingPricingTask.ExecuteAsync();
            await _fillStagingAccessoriesTask.ExecuteAsync();
            await _fillStagingBrandsTask.ExecuteAsync();
            await _fillStagingProductCategoryMappingsTask.ExecuteAsync();
            await _fillStagingScandownEndDatesTask.ExecuteAsync();
            await _fillStagingWarrantiesTask.ExecuteAsync();
        }
    }
}
