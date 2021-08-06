using Nop.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Widgets.Bronto.Models;

namespace Nop.Plugin.Widgets.Bronto
{
    public class BrontoSettings : ISettings
    {
        public string ScriptManagerCode { get; private set; }
        public string DirectAddListId { get; private set; }

        public ConfigurationModel ToModel()
        {
            return new ConfigurationModel
            {
                ScriptManagerCode = ScriptManagerCode,
                DirectAddListId = DirectAddListId
            };
        }

        public static BrontoSettings FromModel(ConfigurationModel model)
        {
            return new BrontoSettings
            {
                ScriptManagerCode = model.ScriptManagerCode,
                DirectAddListId = model.DirectAddListId
            };
        }
    }
}
