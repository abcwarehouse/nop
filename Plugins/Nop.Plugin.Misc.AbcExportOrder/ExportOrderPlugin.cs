using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Plugin.Misc.AbcExportOrder.Tasks;
using Nop.Services.Logging;
using System;
using Nop.Core.Domain.Tasks;
using Nop.Services.Tasks;
using Nop.Services.Configuration;
using Nop.Plugin.Misc.AbcExportOrder.Extensions;
using Nop.Services.Plugins;
using Nop.Core;
using System.Collections.Generic;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Core.Domain.Messages;

namespace Nop.Plugin.Misc.AbcExportOrder
{
    public class ExportOrderPlugin : BasePlugin, IMiscPlugin, IConsumer<OrderPlacedEvent>
    {
        private readonly ILogger _logger;
        private readonly IEmailAccountService _emailAccountService;
        private readonly ILocalizationService _localizationService;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly ISettingService _settingService;
        private readonly ExportOrderSettings _settings;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IWebHelper _webHelper;

        private readonly string TaskType = $"{typeof(ResubmitOrdersTask).FullName}, {typeof(ExportOrderPlugin).Namespace}";


        public ExportOrderPlugin(
            ILogger logger,
            IEmailAccountService emailAccountService,
            IQueuedEmailService queuedEmailService,
            ILocalizationService localizationService,
            IScheduleTaskService scheduleTaskService,
            ISettingService settingService,
            ExportOrderSettings settings,
            EmailAccountSettings emailAccountSettings,
            IWebHelper webHelper
        )
        {
            _logger = logger;
            _emailAccountService = emailAccountService;
            _queuedEmailService = queuedEmailService;
            _localizationService = localizationService;
            _scheduleTaskService = scheduleTaskService;
            _settingService = settingService;
            _settings = settings;
            _emailAccountSettings = emailAccountSettings;
            _webHelper = webHelper;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AbcExportOrder/Configure";
        }

        public void HandleEvent(OrderPlacedEvent eventMessage)
        {
            if (!_settings.IsValid)
            {
                _logger.Warning("ExportOrder plugin settings invalid, order will not be exported to Yahoo tables.");
                return;
            }

            Order order = eventMessage.Order;

            try
            {
                order.SubmitToISAM();
            }
            catch (Exception e)
            {
                _logger.Error($"Failure when submitting order #{order.Id} to ISAM", e);
                SendAlertEmail(order.Id, e.Message);
            }
        }

        public override void Update(string currentVersion, string targetVersion)
        {
            base.Update(currentVersion, targetVersion);
        }

        public override void Install()
        {
            RemoveTask();
            AddTask();
            UpdateLocalizations();

            base.Install();
        }

        private void UpdateLocalizations()
        {
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                [ExportOrderLocaleKeys.OrderIdPrefix] = "Order ID Prefix",
                [ExportOrderLocaleKeys.OrderIdPrefixHint] = "The order ID prefix to use when sending orders to ISAM.",
                [ExportOrderLocaleKeys.TablePrefix] = "Table Prefix",
                [ExportOrderLocaleKeys.TablePrefixHint] = "The ISAM table prefix to send to.",
                [ExportOrderLocaleKeys.FailureAlertEmail] = "Failure Alert Email",
                [ExportOrderLocaleKeys.FailureAlertEmailHint] = "Email to send failure notifications to.",
            });
        }

        public override void Uninstall()
        {
            RemoveTask();

            _localizationService.DeletePluginLocaleResources(ExportOrderLocaleKeys.Base);

            _settingService.DeleteSetting<ExportOrderSettings>();

            base.Uninstall();
        }

        private void AddTask()
        {
            ScheduleTask task = new ScheduleTask
            {
                Name = $"Resubmit Failed ISAM Order Exports",
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

        private void SendAlertEmail(int orderId, string exceptionMessage)
        {
            var failureEmail = _settings.FailureAlertEmail;
            if (!string.IsNullOrEmpty(failureEmail))
            {
                var defaultEmailAddress = _emailAccountService.GetEmailAccountById(
                    _emailAccountSettings.DefaultEmailAccountId
                );

                var email = new QueuedEmail
                {
                    Priority = QueuedEmailPriority.High,
                    From = defaultEmailAddress.Email,
                    FromName = defaultEmailAddress.DisplayName,
                    To = failureEmail,
                    Subject = $"ISAM Order Export Failed (#{orderId})",
                    Body = $"More information:\n\n{exceptionMessage}",
                    CreatedOnUtc = DateTime.UtcNow,
                    EmailAccountId = defaultEmailAddress.Id,
                    DontSendBeforeDateUtc = null
                };

                _queuedEmailService.InsertQueuedEmail(email);
            }
            else
            {
                _logger.Warning("No failure alert email provided, will not provide alert notification.");
            }
        }
    }
}
