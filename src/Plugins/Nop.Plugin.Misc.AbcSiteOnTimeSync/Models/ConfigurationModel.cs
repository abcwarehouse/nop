using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Models
{
    public class ConfiguationModel
    {
        [NopResourceDisplayName(AbcSiteOnTimeSyncPlugin.LocaleKey.CmicApiBrandUrl)]
        public string CmicApiBrandUrl { get; set; }

        [NopResourceDisplayName(AbcSiteOnTimeSyncPlugin.LocaleKey.CmicApiUsername)]
        public string CmicApiUsername { get; set; }

        [NopResourceDisplayName(AbcSiteOnTimeSyncPlugin.LocaleKey.CmicApiPassword)]
        public string CmicApiPassword { get; set; }
    }
}