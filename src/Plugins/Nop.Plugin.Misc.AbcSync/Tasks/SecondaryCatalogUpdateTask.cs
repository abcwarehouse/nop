﻿using Nop.Core.Domain.Messages;
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
        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            var account = await _emailAccountService.GetEmailAccountByIdAsync(_emailAccountSettings.DefaultEmailAccountId);
            bool indexesDropped = false;
            
            try
            {
                indexesDropped = true;
                await EngineContext.Current.Resolve<CoreUpdateTask>().ExecuteAsync();
                await EngineContext.Current.Resolve<MigrateAbcWarehouseContentTask>().ExecuteAsync();
                indexesDropped = false;
                await EngineContext.Current.Resolve<ContentUpdateTask>().ExecuteAsync();
            }
            catch
            {
                var ccEmails = new List<string>();
                if (!string.IsNullOrEmpty(_importSettings.CatalogUpdateFailureCCEmails))
                {
                    ccEmails.AddRange(_importSettings.CatalogUpdateFailureCCEmails.Split(','));
                }
                await _emailSender.SendEmailAsync(account, "Catalog Update: Sync Failure", $"The catalog update task failed at {DateTime.Now}. More information can be found in the Admin Panel under System>Log ", account.Email, account.DisplayName, ccEmails[0], "", cc: ccEmails);
                throw;
            }
            finally
            {
                if (indexesDropped)
                {
                    ImportTaskExtensions.CreateIndexes();
                }
            }
        }
    }
}
