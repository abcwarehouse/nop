using Nop.Core.Domain.Messages;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.AbcSync.Tasks;
using Nop.Services.Caching;
using Nop.Services.Configuration;
using Nop.Services.Messages;
using System;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public class SecondaryCatalogUpdateTask : Nop.Services.Tasks.IScheduleTask
    {
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IEmailSender _emailSender;
        ISettingService _settingService;
        private readonly ImportSettings _importSettings;

        public SecondaryCatalogUpdateTask(ISettingService settingService,
            EmailAccountSettings emailAccountSettings,
            IEmailAccountService emailAccountService,
            IEmailSender emailSender,
            ImportSettings importSettings)
        {

            _emailAccountSettings = emailAccountSettings;
            _emailAccountService = emailAccountService;
            _emailSender = emailSender;
            _settingService = settingService;
            _importSettings = importSettings;
        }

        /**
         * Shares the same core functionality of the regular catalog update with the exception of filling the staging database
         *  and the addition of a selective content migration from the primary database
         */
        public System.Threading.Tasks.Task ExecuteAsync()
        {
            var account = _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
            bool indexesDropped = false;
            this.LogStart();
            try
            {
                _emailSender.SendEmail(account, "Secondary Catalog Update: Store Closing", $"More information can be found in the Admin Panel under System>Log ", account.Email, account.DisplayName, "support@abcwarehouse.com", "");
                indexesDropped = true;
                EngineContext.Current.Resolve<CoreUpdateTask>().Execute();
                EngineContext.Current.Resolve<MigrateAbcWarehouseContentTask>().Execute();
                indexesDropped = false;
                _emailSender.SendEmail(account, "Secondary Catalog Update: Store Open", $"More information can be found in the Admin Panel under System>Log ", account.Email, account.DisplayName, "support@abcwarehouse.com", "");
                EngineContext.Current.Resolve<ContentUpdateTask>().Execute();
                EngineContext.Current.Resolve<ClearCacheTask>().Execute();
            }
            catch
            {
                var ccEmails = new List<string>();
                if (!string.IsNullOrEmpty(_importSettings.CatalogUpdateFailureCCEmails))
                {
                    ccEmails.AddRange(_importSettings.CatalogUpdateFailureCCEmails.Split(','));
                }
                _emailSender.SendEmail(account, "Catalog Update: Sync Failure", $"The catalog update task failed at {DateTime.Now}. More information can be found in the Admin Panel under System>Log ", account.Email, account.DisplayName, ccEmails[0], "", cc: ccEmails);
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
