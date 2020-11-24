using Microsoft.AspNetCore.Hosting;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using System;
using System.Configuration;
using System.Data.Odbc;
using System.Data.SqlClient;
using System.IO;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Models;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync
{
    public class AbcSiteOnTimeSyncSettings : ISettings
	{
        public string CmicApiBrandUrl { get; private set; }
        public string CmicApiUsername { get; private set; }
        public string CmicApiPassword { get; private set; }

        public bool AreValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(CmicApiBrandUrl) &&
                       !string.IsNullOrWhiteSpace(CmicApiUsername) &&
                       !string.IsNullOrWhiteSpace(CmicApiPassword);
            }
        }      

        public AbcSiteOnTimeSyncSettings FromModel(ConfiguationModel model)
		{
			return new AbcSiteOnTimeSyncSettings()
			{				
                CmicApiBrandUrl = model.CmicApiBrandUrl,
                CmicApiUsername = model.CmicApiUsername,
                CmicApiPassword = model.CmicApiPassword
            };
		}

        public static AbcSiteOnTimeSyncSettings Default()
		{
			return new AbcSiteOnTimeSyncSettings()
			{				
                CmicApiBrandUrl = "https://www.cmicdataservices.com/datacenterrj/api/brands?includeolddbid=yes",
                CmicApiUsername = "ABC Warehouse"
            };
		}

        public ConfiguationModel ToModel()
        {
            return new ConfiguationModel
            {                
                CmicApiBrandUrl = CmicApiBrandUrl,
                CmicApiUsername = CmicApiUsername,
                CmicApiPassword = CmicApiPassword
            };
        }
	}
}