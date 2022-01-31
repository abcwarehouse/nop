using System;
using System.Collections.Generic;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Domain;
using Nop.Data.Mapping;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Mapping
{
    public partial class CartSlideoutNameCompatibility : INameCompatibility
    {
        public Dictionary<Type, string> TableNames => new Dictionary<Type, string> { };

        public Dictionary<(Type, string), string> ColumnName => new Dictionary<(Type, string), string>
        {
            { (typeof(AbcDeliveryItem), "Id"), "Item_Number" },
            { (typeof(AbcDeliveryMap), "Id"), "CategoryId" },
        };
    }
}