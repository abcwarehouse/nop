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

        public CoreUpdateTask(
            ISettingService settingService,
            ILogger logger,
            IStoreService storeService,
            ImportSettings importSettings)
        {
            _settingService = settingService;
            _logger = logger;
            _storeService = storeService;
            _importSettings = importSettings;
        }

        public System.Threading.Tasks.Task ExecuteAsync()
        {
            this.LogStart();

            try
            {
                var cacheManager = EngineContext.Current.Resolve<IStaticCacheManager>();
                await _logger.InformationAsync(this.GetType().Name + " Closing Store");
                _settingService.SetSetting("storeinformationsettings.storeclosed", "True");
                cacheManager.Clear();
                ImportTaskExtensions.DropIndexes();

                EngineContext.Current.Resolve<ImportProductsTask>().Execute();
                EngineContext.Current.Resolve<MapCategoriesTask>().Execute();
                EngineContext.Current.Resolve<ImportProductCategoryMappingsTask>().Execute();
                EngineContext.Current.Resolve<AddHomeDeliveryAttributesTask>().Execute();
                EngineContext.Current.Resolve<ImportMarkdownsTask>().Execute();
                EngineContext.Current.Resolve<ImportRelatedProductsTask>().Execute();
                EngineContext.Current.Resolve<ImportWarrantiesTask>().Execute();
                // skipping shop import for now
                // EngineContext.Current.Resolve<ImportShopsTask>().Execute();
                EngineContext.Current.Resolve<UnmapNonstockClearanceTask>().Execute();
                EngineContext.Current.Resolve<MapCategoryStoresTask>().Execute();

                ImportTaskExtensions.CreateIndexes();
                await _logger.InformationAsync(this.GetType().Name + " Opening Store");
                _settingService.SetSetting("storeinformationsettings.storeclosed", "False");
                cacheManager.Clear();

                VerifyStoresAreOpen();
            }
            catch
            {
                _logger.Error("Error when running CoreUpdate, store is likely closed.");
                throw;
            }

            this.LogEnd();
        }
    }
}
