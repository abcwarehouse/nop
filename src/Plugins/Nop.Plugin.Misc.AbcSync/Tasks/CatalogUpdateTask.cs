using Nop.Core.Caching;
using Nop.Core.Domain.Messages;
using Nop.Core.Infrastructure;
using Nop.Services.Caching;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    public class CatalogUpdateTask : IScheduleTask
    {
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IEmailSender _emailSender;
        ISettingService _settingService;
        ILogger _logger;
        private readonly ImportSettings _importSettings;

        public CatalogUpdateTask(ISettingService settingService,
            EmailAccountSettings emailAccountSettings,
            IEmailAccountService emailAccountService,
            IEmailSender emailSender,
            ILogger logger,
            ImportSettings importSettings)
        {

            _emailAccountSettings = emailAccountSettings;
            _emailAccountService = emailAccountService;
            _emailSender = emailSender;
            _settingService = settingService;
            _logger = logger;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            var account = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);
            bool indexesDropped = false;
            this.LogStart();
            try
            {
                await EngineContext.Current.Resolve<FillStagingTask>().ExecuteAsync();
                indexesDropped = true;
                await EngineContext.Current.Resolve<CoreUpdateTask>().ExecuteAsync();
                indexesDropped = false;
                await EngineContext.Current.Resolve<ContentUpdateTask>().ExecuteAsync();
                await EngineContext.Current.Resolve<ClearCacheTask>().ExecuteAsync();
            }
            catch (Exception e)
            {
                var ccEmails = _importSettings.CatalogUpdateFailureCCEmails?.Split(',');
                if (ccEmails.Any())
                {
                    await _emailSender.SendEmailAsync(account, "Catalog Update: Sync Failure", $"The catalog update task failed at {DateTime.Now}. Exception: {e.Message}", account.Email, account.DisplayName, ccEmails[0], "", cc: ccEmails);
                }
                else
                {
                    await _logger.WarningAsync("No catalog failure warning emails specified, email will not be sent.");
                }
                throw;
            }
            finally
            {
                if (indexesDropped)
                {
                    ImportTaskExtensions.CreateIndexes();
                }
            }

            this.LogEnd();
        }
    }
}
