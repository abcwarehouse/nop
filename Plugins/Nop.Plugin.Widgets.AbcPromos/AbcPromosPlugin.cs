using Nop.Services.Cms;
using Nop.Services.Plugins;
using System.Collections.Generic;
using Nop.Web.Framework.Infrastructure;
using Nop.Core;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Core.Domain.Tasks;
using Nop.Services.Tasks;
using Nop.Plugin.Widgets.AbcPromos.Tasks;
using Nop.Plugin.Misc.AbcCore.Infrastructure;

namespace Nop.Plugin.Widgets.AbcPromos
{
    public class AbcPromosPlugin : BasePlugin, IWidgetPlugin
    {
        private readonly IWebHelper _webHelper;

        private readonly ILocalizationService _localizationService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ISettingService _settingService;

        private readonly string TaskType =
            $"{typeof(UpdatePromosTask).FullName}, {typeof(AbcPromosPlugin).Namespace}";

        public AbcPromosPlugin(
            IWebHelper webHelper,
            ILocalizationService localizationService,
            IScheduleTaskService scheduleTaskService,
            ISettingService settingService
        )
        {
            _webHelper = webHelper;
            _localizationService = localizationService;
            _scheduleTaskService = scheduleTaskService;
            _settingService = settingService;
        }

        public bool HideInWidgetList => false;

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "WidgetsAbcPromos";
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string> {
                PublicWidgetZones.ProductDetailsAfterBreadcrumb,
                CustomPublicWidgetZones.ProductBoxAddinfoReviews,
                CustomPublicWidgetZones.ProductDetailsAfterPrice,
                CustomPublicWidgetZones.OrderSummaryAfterProductMiniDescription
            };
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AbcPromos/Configure";
        }

        public override void Update(string currentVersion, string targetVersion)
        {
            AddTask();
            AddLocales();

            base.Update(currentVersion, targetVersion);
        }

        public override void Install()
        {
            RemoveTask();
            AddTask();
            AddLocales();

            base.Install();
        }

        public override void Uninstall()
        {
            RemoveTask();
            _localizationService.DeletePluginLocaleResources(AbcPromosLocales.Base);
            _settingService.DeleteSetting<AbcPromosSettings>();

            base.Uninstall();
        }

        private void AddLocales()
        {
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                [AbcPromosLocales.IncludeExpiredPromosOnRebatesPromosPage]
                    = "Include Expired Promos on Rebates/Promos Page",
                [AbcPromosLocales.IncludeExpiredPromosOnRebatesPromosPageHint]
                    = "Shows expired promos (by one month) on the rebates/promos page.",
            });
        }

        private void AddTask()
        {
            ScheduleTask task = new ScheduleTask
            {
                Name = $"Update Promos",
                Seconds = 14400,
                Type = TaskType,
                Enabled = true,
                StopOnError = false
            };

            _scheduleTaskService.InsertTask(task);
        }

        private void RemoveTask()
        {
            var task = _scheduleTaskService.GetTaskByType(TaskType);
            if (task != null)
            {
                _scheduleTaskService.DeleteTask(task);
            }
        }
    }
}
