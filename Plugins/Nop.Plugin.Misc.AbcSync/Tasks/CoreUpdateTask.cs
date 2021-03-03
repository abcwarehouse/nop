using Nop.Core.Caching;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate;
using Nop.Plugin.Misc.AzureWebTestsIntegration.Services;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using System;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Services.Stores;
using System.Net.Http;
using System.Threading;

namespace Nop.Plugin.Misc.AbcSync
{
    public class CoreUpdateTask : IScheduleTask
    {
        ISettingService _settingService;
        ILogger _logger;
        private readonly IAzureWebTestIntegrationService _azureWebTestIntegrationService;
        private readonly IStoreService _storeService;
        private readonly ImportSettings _importSettings;

        public CoreUpdateTask(
            ISettingService settingService,
            ILogger logger,
            IAzureWebTestIntegrationService azureWebTestIntegrationService,
            IStoreService storeService,
            ImportSettings importSettings)
        {
            _settingService = settingService;
            _logger = logger;
            _azureWebTestIntegrationService = azureWebTestIntegrationService;
            _storeService = storeService;
            _importSettings = importSettings;
        }

        public void Execute()
        {
            this.LogStart();

            try
            {
                DisableUptimeCheck();
                var cacheManager = EngineContext.Current.Resolve<IStaticCacheManager>();
                _logger.Information(this.GetType().Name + " Closing Store");
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
                _logger.Information(this.GetType().Name + " Opening Store");
                _settingService.SetSetting("storeinformationsettings.storeclosed", "False");
                cacheManager.Clear();

                VerifyStoresAreOpen();
            }
            catch
            {
                _logger.Error("Error when running CoreUpdate, store is likely closed.");
                throw;
            }
            finally
            {
                EnableUptimeCheck();
            }

            this.LogEnd();
        }

        // Doing this since the stores don't always immediately open, which is
        // causing false positive uptime errors.
        private void VerifyStoresAreOpen()
        {
            const int MaxAttempts = 40;
            var stores = _storeService.GetAllStores();

            using (var client = new HttpClient())
            {
                foreach (var store in stores)
                {
                    var attemptCount = 0;
                    while (attemptCount < MaxAttempts)
                    {
                        attemptCount++;
                        var response = client.GetAsync(store.Url).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = response.Content;
                            string responseString =
                                responseContent.ReadAsStringAsync().Result;

                            if (!responseString.Contains("Oops!"))
                            {
                                // store is open
                                break;
                            }
                        }
                    }

                    if (attemptCount == MaxAttempts)
                    {
                        _logger.Warning($"Unable to verify store {store.Name} " +
                                     "opened.");
                    }
                }
            }
        }

        private void DisableUptimeCheck()
        {
            if (!_importSettings.AreUptimeTestsActive)
            {
                _logger.Warning("Uptime test data is not defined in AbcSync settings, will not disable uptime");
                return;
            }

            try
            {
                _azureWebTestIntegrationService.DisableWebTest(
                    _importSettings.MainStoreWebTestSubscriptionId,
                    _importSettings.MainStoreWebTestResourceGroup,
                    _importSettings.MainStoreWebTestName);
                _azureWebTestIntegrationService.DisableWebTest(
                    _importSettings.ClearanceStoreWebTestSubscriptionId,
                    _importSettings.ClearanceStoreWebTestResourceGroup,
                    _importSettings.ClearanceStoreWebTestName);

                // wait to allow disabling to kick in
                Thread.Sleep(10000);
            }
            catch (Exception e)
            {
                _logger.Warning("Error when disabling uptime check: " + e.Message);
            }
        }

        private void EnableUptimeCheck()
        {
            if (!_importSettings.AreUptimeTestsActive)
            {
                _logger.Warning("Uptime test data is not defined in AbcSync settings, will not enable uptime");
                return;
            }

            try
            {
                _azureWebTestIntegrationService.EnableWebTest(
                    _importSettings.MainStoreWebTestSubscriptionId,
                    _importSettings.MainStoreWebTestResourceGroup,
                    _importSettings.MainStoreWebTestName);
                _azureWebTestIntegrationService.EnableWebTest(
                    _importSettings.ClearanceStoreWebTestSubscriptionId,
                    _importSettings.ClearanceStoreWebTestResourceGroup,
                    _importSettings.ClearanceStoreWebTestName);
            }
            catch (Exception e)
            {
                _logger.Warning("Error when enabling uptime check: " + e.Message);
            }
        }
    }
}
