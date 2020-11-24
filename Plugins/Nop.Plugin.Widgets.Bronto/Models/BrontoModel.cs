using System.Collections.Generic;
using Nop.Plugin.Widgets.Bronto.Domain;

namespace Nop.Plugin.Widgets.Bronto.Models
{
    public class BrontoModel
    {
        public string ScriptManagerCode { get; set; }
        public BrontoCart BrontoCart { get; set; }
    }
}
