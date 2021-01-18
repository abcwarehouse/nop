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

        private void AddTask(string name, int seconds, bool isActive = false)
        {
            ScheduleTask task = new ScheduleTask();
            task.Name = $"AbcWarehouse: {name}";
            task.Seconds = seconds;
            task.Type = $"{typeof(ImportPlugin).Namespace}.{name}Task, {typeof(ImportPlugin).Namespace}";
            task.Enabled = isActive;
            task.StopOnError = false;

            _scheduleTaskService.InsertTask(task);
        }

        private void RemoveTasks()
        {
            var oldTasks = _scheduleTaskRepository.Table.ToList().Where(st => st.Name.StartsWith("AbcWarehouse: "));
            foreach (var task in oldTasks)
            {
                if (task != null)
                    _scheduleTaskService.DeleteTask(task);
            }
        }

        public override void Install()
        {
            // Clean up before installation
            RemoveTasks();

            //each task corresponds to an import
            AddTask("Archive", 86400);
            AddTask("FillStaging", 86400);
            AddTask("CoreUpdate", 86400);
            AddTask("ContentUpdate", 86400);
            AddTask("CatalogUpdate", 86400);
            AddTask("SecondaryCatalogUpdate", 86400);
            AddTask("MissingImageReport", 86400);

            _settingService.SaveSetting(ImportSettings.CreateDefault());

            base.Install();
        }

        public override void Uninstall()
        {
            RemoveTasks();

            base.Uninstall();
        }

        public override void Update(string currentVersion, string targetVersion)
        {
            UpdateLocales();
        }

        public override string GetConfigurationPageUrl()
        {
            return
                $"{_webHelper.GetStoreLocation()}Admin/AbcSync/Configure";
        }

        private void UpdateLocales()
        {
            _localizationService.AddPluginLocaleResource(
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
                    [ImportPluginLocales.SkipUnmapEmptyCategoriesTask] = "UnmapEmptyCategoriesTask",
                    [ImportPluginLocales.SkipSliExportTask] = "SliExportTask",

                    [ImportPluginLocales.SkipImportDocumentsTask] = "ImportDocumentsTask",
                    [ImportPluginLocales.SkipImportIsamSpecsTask] = "ImportIsamSpecsTask",
                    [ImportPluginLocales.SkipImportFeaturedProductsTask] = "ImportFeaturedProductsTask",
                    [ImportPluginLocales.SkipImportProductFlagsTask] = "ImportProductFlagsTask",
                    [ImportPluginLocales.SkipImportSotPicturesTask] = "ImportSotPicturesTask",
                    [ImportPluginLocales.SkipImportLocalPicturesTask] = "ImportLocalPicturesTask"
                });
        }
    }
}