using Nop.Core;
using SevenSpikes.Nop.Plugins.StoreLocator.Domain.Shops;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcCore.Domain
{
    public partial class ShopAbc : BaseEntity
    {
        public virtual int ShopId { get; set; }
        public virtual string AbcId { get; set; }
        public virtual string AbcEmail { get; set; }
    }
}
