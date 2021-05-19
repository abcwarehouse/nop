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

        public CoreUpdateTask(
            ISettingService settingService,
            ILogger logger,
            IStoreService storeService,
            ImportSettings importSettings,
            ImportProductsTask importProductsTask)
        {
            _settingService = settingService;
            _logger = logger;
            _storeService = storeService;
            _importSettings = importSettings;

            _importProductsTask = importProductsTask;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            this.LogStart();

            try
            {
                await _logger.InformationAsync(this.GetType().Name + " Closing Store");
                await _settingService.SetSettingAsync("storeinformationsettings.storeclosed", "True");
                ImportTaskExtensions.DropIndexes();

                await _importProductsTask.ExecuteAsync();
                await EngineContext.Current.Resolve<MapCategoriesTask>().ExecuteAsync();
                await EngineContext.Current.Resolve<ImportProductCategoryMappingsTask>().ExecuteAsync();
                await EngineContext.Current.Resolve<AddHomeDeliveryAttributesTask>().ExecuteAsync();
                await EngineContext.Current.Resolve<ImportMarkdownsTask>().ExecuteAsync();
                await EngineContext.Current.Resolve<ImportRelatedProductsTask>().ExecuteAsync();
                await EngineContext.Current.Resolve<ImportWarrantiesTask>().ExecuteAsync();
                await EngineContext.Current.Resolve<UnmapNonstockClearanceTask>().ExecuteAsync();
                await EngineContext.Current.Resolve<MapCategoryStoresTask>().ExecuteAsync();

                ImportTaskExtensions.CreateIndexes();
                await _logger.InformationAsync(this.GetType().Name + " Opening Store");
                await _settingService.SetSettingAsync("storeinformationsettings.storeclosed", "False");
            }
            catch
            {
                await _logger.ErrorAsync("Error when running CoreUpdate, store is likely closed.");
                throw;
            }

            this.LogEnd();
        }
    }
}
