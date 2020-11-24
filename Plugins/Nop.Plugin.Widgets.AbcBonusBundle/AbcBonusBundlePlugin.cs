using Nop.Core;
using Nop.Core.Domain.Tasks;
using Nop.Plugin.Widgets.AbcBonusBundle.Tasks;
using Nop.Services.Cms;
using Nop.Services.Plugins;
using Nop.Services.Tasks;
using System.Collections.Generic;

namespace Nop.Plugin.Widgets.AbcBonusBundle
{
    public class AbcBonusBundlePlugin : BasePlugin, IWidgetPlugin
    {
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IWebHelper _webHelper;

        private readonly string TaskType = $"{typeof(BonusBundleProductRibbonUpdateTask).FullName}, {typeof(AbcBonusBundlePlugin).Namespace}";

        public AbcBonusBundlePlugin(
            IScheduleTaskService scheduleTaskService,
            IWebHelper webHelper
        )
        {
            _scheduleTaskService = scheduleTaskService;
            _webHelper = webHelper;
        }

        public bool HideInWidgetList => false;

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AbcBonusBundle/Configure";
        }

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "AbcBonusBundle";
        }

        public IList<string> GetWidgetZones()
        {
            return new List<string> { "productdetails_overview_bottom" };
        }

        public override void Install()
        {
            RemoveTask();
            AddTask();

            base.Install();
        }

        public override void Uninstall()
        {
            RemoveTask();

            base.Uninstall();
        }

        private void AddTask()
        {
            ScheduleTask task = new ScheduleTask
            {
                Name = $"Sync ABC Bonus Bundles Product Ribbons",
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
