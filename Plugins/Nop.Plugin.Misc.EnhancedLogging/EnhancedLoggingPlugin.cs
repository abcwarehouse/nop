using Nop.Core;
using Nop.Core.Domain.Tasks;
using Nop.Plugin.Misc.EnhancedLogging.Tasks;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Plugins;
using Nop.Services.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Nop.Plugin.Misc.EnhancedLogging
{
    public class EnhancedLoggingPlugin : BasePlugin, IMiscPlugin
    {
        public static class LocaleKey
        {
            public const string Base = "Plugins.Misc.EnhancedLogging";
            public const string DaysToKeepLogs = Base + ".Fields.DaysToKeepLogs";
        }

        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;

        private readonly string DefaultClearLogTaskType =
            $"{typeof(ClearLogTask).Namespace}.{typeof(ClearLogTask).Name}, " +
            $"{typeof(ClearLogTask).Assembly.GetName().Name}";
        private readonly string EnhancedClearLogTaskType =
            $"{typeof(EnhancedClearLogTask).Namespace}.{typeof(EnhancedClearLogTask).Name}, " +
            $"{typeof(EnhancedClearLogTask).Assembly.GetName().Name}";

        public EnhancedLoggingPlugin(
            IScheduleTaskService scheduleTaskService,
            ISettingService settingService,
            IWebHelper webHelper,
            ILocalizationService localizationService
        )
        {
            _scheduleTaskService = scheduleTaskService;
            _settingService = settingService;
            _webHelper = webHelper;
            _localizationService = localizationService;
        }

        public override void Install()
        {
            DeleteAllRelatedTasks();

            var task = new ScheduleTask()
            {
                Name = "Clear log (enhanced)",
                Seconds = 3600,
                Type = EnhancedClearLogTaskType,
                Enabled = true,
                StopOnError = false
            };
            _scheduleTaskService.InsertTask(task);

            _settingService.SaveSetting(EnhancedLoggingSettings.CreateDefault());

            AddOrUpdatePluginLocaleResources();

            base.Install();
        }

        public override void Uninstall()
        {
            DeleteAllRelatedTasks();

            var task = new ScheduleTask()
            {
                Name = "Clear log",
                Seconds = 3600,
                Type = DefaultClearLogTaskType,
                Enabled = false,
                StopOnError = false
            };
            _scheduleTaskService.InsertTask(task);

            _settingService.DeleteSetting<EnhancedLoggingSettings>();

            DeletePluginLocaleResources();

            base.Uninstall();
        }

        private void DeleteAllRelatedTasks()
        {
            var defaultClearLogTask = _scheduleTaskService.GetTaskByType(DefaultClearLogTaskType);
            if (defaultClearLogTask != null)
            {
                _scheduleTaskService.DeleteTask(defaultClearLogTask);
            }

            var enhancedClearLogTask = _scheduleTaskService.GetTaskByType(EnhancedClearLogTaskType);
            if (enhancedClearLogTask != null)
            {
                _scheduleTaskService.DeleteTask(enhancedClearLogTask);
            }

        }

        private void AddOrUpdatePluginLocaleResources()
        {
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                [LocaleKey.DaysToKeepLogs] = "Days to Keep Logs"
            });
        }

        private void DeletePluginLocaleResources()
        {
            _localizationService.DeletePluginLocaleResources(LocaleKey.Base);
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/EnhancedLogging/Configure";
        }
    }
}
