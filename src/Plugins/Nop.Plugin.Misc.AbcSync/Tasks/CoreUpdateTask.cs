using Nop.Core.Caching;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using System;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Services.Stores;
using System.Net.Http;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public class CoreUpdateTask : IScheduleTask
    {
        ISettingService _settingService;
        ILogger _logger;
        private readonly IStoreService _storeService;
        private readonly ImportSettings _importSettings;

        private readonly ImportProductsTask _importProductsTask;
        private readonly MapCategoriesTask _mapCategoriesTask;
        private readonly ImportProductCategoryMappingsTask _importProductCategoryMappingsTask;
        private readonly AddHomeDeliveryAttributesTask _addHomeDeliveryAttributesTask;
        private readonly ImportMarkdownsTask _importMarkdownsTask;
        private readonly ImportRelatedProductsTask _importRelatedProductsTask;
        private readonly ImportWarrantiesTask _importWarrantiesTask;
        private readonly UnmapNonstockClearanceTask _unmapNonstockClearanceTask;
        private readonly MapCategoryStoresTask _mapCategoryStoresTask;

        public CoreUpdateTask(
            ISettingService settingService,
            ILogger logger,
            IStoreService storeService,
            ImportSettings importSettings,
            ImportProductsTask importProductsTask,
            MapCategoriesTask mapCategoriesTask,
            ImportProductCategoryMappingsTask importProductCategoryMappingsTask,
            AddHomeDeliveryAttributesTask addHomeDeliveryAttributesTask,
            ImportMarkdownsTask importMarkdownsTask,
            ImportRelatedProductsTask importRelatedProductsTask,
            ImportWarrantiesTask importWarrantiesTask,
            UnmapNonstockClearanceTask unmapNonstockClearanceTask,
            MapCategoryStoresTask mapCategoryStoresTask)
        {
            _settingService = settingService;
            _logger = logger;
            _storeService = storeService;
            _importSettings = importSettings;

            _importProductsTask = importProductsTask;
            _mapCategoriesTask = mapCategoriesTask;
            _importProductCategoryMappingsTask = importProductCategoryMappingsTask;
            _addHomeDeliveryAttributesTask = addHomeDeliveryAttributesTask;
            _importMarkdownsTask = importMarkdownsTask;
            _importRelatedProductsTask = importRelatedProductsTask;
            _importWarrantiesTask = importWarrantiesTask;
            _unmapNonstockClearanceTask = unmapNonstockClearanceTask;
            _mapCategoryStoresTask = mapCategoryStoresTask;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            

            try
            {
                await _logger.InformationAsync(this.GetType().Name + " Closing Store");
                await _settingService.SetSettingAsync("storeinformationsettings.storeclosed", "True");
                ImportTaskExtensions.DropIndexes();

                await _importProductsTask.ExecuteAsync();
                await _mapCategoriesTask.ExecuteAsync();
                await _importProductCategoryMappingsTask.ExecuteAsync();
                await _addHomeDeliveryAttributesTask.ExecuteAsync();
                await _importMarkdownsTask.ExecuteAsync();
                await _importRelatedProductsTask.ExecuteAsync();
                await _importWarrantiesTask.ExecuteAsync();
                await _unmapNonstockClearanceTask.ExecuteAsync();
                await _mapCategoryStoresTask.ExecuteAsync();

                ImportTaskExtensions.CreateIndexes();
                await _logger.InformationAsync(this.GetType().Name + " Opening Store");
                await _settingService.SetSettingAsync("storeinformationsettings.storeclosed", "False");
            }
            catch
            {
                await _logger.ErrorAsync("Error when running CoreUpdate, store is likely closed.");
                throw;
            }

            
        }
    }
}
