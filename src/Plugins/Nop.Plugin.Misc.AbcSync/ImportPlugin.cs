using Nop.Core;
using Nop.Core.Domain.Tasks;
using Nop.Data;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportPlugin : BasePlugin, IMiscPlugin
    {
        private readonly ILocalizationService _localizationService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IRepository<ScheduleTask> _scheduleTaskRepository;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;

        private readonly INopDataProvider _nopDataProvider;

        public ImportPlugin(
            ILocalizationService localizationService,
            IScheduleTaskService scheduleTaskService,
            IRepository<ScheduleTask> scheduleTaskRepository,
            ISettingService settingService,
            IWebHelper webHelper,
            INopDataProvider nopDataProvider
        )
        {
            _localizationService = localizationService;
            _scheduleTaskService = scheduleTaskService;
            _scheduleTaskRepository = scheduleTaskRepository;
            _settingService = settingService;
            _webHelper = webHelper;
            _nopDataProvider = nopDataProvider;
        }

        private async System.Threading.Tasks.Task AddTaskAsync(string name, int seconds, bool isActive = false)
        {
            ScheduleTask task = new ScheduleTask();
            task.Name = $"AbcWarehouse: {name}";
            task.Seconds = seconds;
            task.Type = $"{typeof(ImportPlugin).Namespace}.{name}Task, {typeof(ImportPlugin).Namespace}";
            task.Enabled = isActive;
            task.StopOnError = false;

            await _scheduleTaskService.InsertTaskAsync(task);
        }

        private async System.Threading.Tasks.Task RemoveTasksAsync()
        {
            var oldTasks = _scheduleTaskRepository.Table.ToList().Where(st => st.Name.StartsWith("AbcWarehouse: "));
            foreach (var task in oldTasks)
            {
                if (task != null)
                    await _scheduleTaskService.DeleteTaskAsync(task);
            }
        }

        public override async System.Threading.Tasks.Task InstallAsync()
        {
            // Clean up before installation
            await RemoveTasksAsync();

            //each task corresponds to an import
            await AddTaskAsync("Archive", 86400);
            await AddTaskAsync("FillStaging", 86400);
            await AddTaskAsync("CoreUpdate", 86400);
            await AddTaskAsync("ContentUpdate", 86400);
            await AddTaskAsync("CatalogUpdate", 86400);
            await AddTaskAsync("SecondaryCatalogUpdate", 86400);
            await AddTaskAsync("MissingImageReport", 86400);

            await _settingService.SaveSettingAsync(ImportSettings.CreateDefault());

            await base.InstallAsync();
        }

        public override async System.Threading.Tasks.Task UninstallAsync()
        {
            await RemoveTasksAsync();

            await base.UninstallAsync();
        }

        public override async System.Threading.Tasks.Task UpdateAsync(string currentVersion, string targetVersion)
        {
            await UpdateLocalesAsync();
        }

        public override string GetConfigurationPageUrl()
        {
            return
                $"{_webHelper.GetStoreLocation()}Admin/AbcSync/Configure";
        }

        private async System.Threading.Tasks.Task UpdateLocalesAsync()
        {
            await _localizationService.AddLocaleResourceAsync(
                new Dictionary<string, string>
                {
                    [ImportPluginLocales.SkipOldMattressesImport] = "Skip Import of Old Mattresses",

                    [ImportPluginLocales.SkipFillStagingAccessoriesTask] = "FillStagingAccessoriesTask",
                    [ImportPluginLocales.SkipFillStagingBrandsTask] = "FillStagingBrandsTask",
                    [ImportPluginLocales.SkipFillStagingPricingTask] = "FillStagingPricingTask",
                    [ImportPluginLocales.SkipFillStagingProductCategoryMappingsTask] = "FillStagingProductCategoryMappingsTask",
                    [ImportPluginLocales.SkipFillStagingRebatesTask] = "FillStagingRebatesTask",
                    [ImportPluginLocales.SkipFillStagingScandownEndDatesTask] = "FillStagingScandownEndDatesTask",
                    [ImportPluginLocales.SkipFillStagingWarrantiesTask] = "FillStagingWarrantiesTask",

                    [ImportPluginLocales.SkipImportProductsTask] = "ImportProductsTask",
                    [ImportPluginLocales.SkipMapCategoriesTask] = "MapCategoriesTask",
                    [ImportPluginLocales.SkipImportProductCategoryMappingsTask] = "ImportProductCategoryMappingsTask",
                    [ImportPluginLocales.SkipAddHomeDeliveryAttributesTask] = "AddHomeDeliveryAttributesTask",
                    [ImportPluginLocales.SkipImportMarkdownsTask] = "ImportMarkdownsTask",
                    [ImportPluginLocales.SkipImportRelatedProductsTask] = "ImportRelatedProductsTask",
                    [ImportPluginLocales.SkipImportWarrantiesTask] = "ImportWarrantiesTask",
                    [ImportPluginLocales.SkipUnmapNonstockClearanceTask] = "UnmapNonstockClearanceTask",
                    [ImportPluginLocales.SkipCleanDuplicateImagesTask] = "CleanDuplicateImagesTask",
                    [ImportPluginLocales.SkipMapCategoryStoresTask] = "MapCategoryStoresTask",
                    [ImportPluginLocales.SkipSliExportTask] = "SliExportTask",

                    [ImportPluginLocales.SkipImportDocumentsTask] = "ImportDocumentsTask",
                    [ImportPluginLocales.SkipImportIsamSpecsTask] = "ImportIsamSpecsTask",
                    [ImportPluginLocales.SkipImportFeaturedProductsTask] = "ImportFeaturedProductsTask",
                    [ImportPluginLocales.SkipImportProductFlagsTask] = "ImportProductFlagsTask",
                    [ImportPluginLocales.SkipImportLocalPicturesTask] = "ImportLocalPicturesTask"
                }
            );
        }
    }
}