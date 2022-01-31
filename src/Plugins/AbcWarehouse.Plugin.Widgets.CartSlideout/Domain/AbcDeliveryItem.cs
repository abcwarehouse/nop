/* This table is populated from John */

using Nop.Core;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Domain
{
    public class AbcDeliveryItem : BaseEntity
    {
        public int Item_Number { get; set; }

        public decimal Price { get; set; }
    }
}