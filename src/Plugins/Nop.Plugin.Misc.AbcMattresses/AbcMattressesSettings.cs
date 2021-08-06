using Nop.Core.Configuration;
using Nop.Plugin.Misc.AbcCore.Models;
using System.Configuration;
using System.Data.Odbc;
using Nop.Plugin.Misc.AbcMattresses.Models;

namespace Nop.Plugin.Misc.AbcMattresses
{
    public class AbcMattressesSettings : ISettings
    {
        public bool ShouldSyncRibbons { get; private set; }

        public static AbcMattressesSettings FromModel(AbcMattressesConfigModel model)
        {
            return new AbcMattressesSettings()
            {
                ShouldSyncRibbons = model.ShouldSyncRibbons
            };
        }

        public AbcMattressesConfigModel ToModel()
        {
            return new AbcMattressesConfigModel
            {
                ShouldSyncRibbons = ShouldSyncRibbons
            };
        }
    }
}