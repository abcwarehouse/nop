using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.ModelBinding;
using System;

namespace Nop.Plugin.Widgets.Bronto.Models
{
    public class ConfigurationModel
    {
        [NopResourceDisplayName(BrontoPluginLocaleKeys.ScriptManagerCode)]
        public string ScriptManagerCode { get; set; }

        [NopResourceDisplayName(BrontoPluginLocaleKeys.DirectAddListId)]
        public string DirectAddListId { get; set; }
    }
}
