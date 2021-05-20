using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.AbcMattresses.Models
{
    public class AbcMattressesConfigModel
    {
        [NopResourceDisplayName(AbcMattressesLocales.ShouldSyncRibbons)]
        public bool ShouldSyncRibbons { get; set; }
    }
}