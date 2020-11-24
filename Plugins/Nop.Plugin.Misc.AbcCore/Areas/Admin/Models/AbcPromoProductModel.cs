using System;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.AbcCore.Areas.Admin.Models
{
    public partial class AbcPromoProductModel : BaseNopEntityModel
    {
        public string AbcItemNumber { get; set; }
        public string Name { get; set; }
        public bool Published { get; set; }
    }
}