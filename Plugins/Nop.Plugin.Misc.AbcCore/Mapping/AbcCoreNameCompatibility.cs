using System;
using System.Collections.Generic;
using Nop.Data.Mapping;
using Nop.Plugin.Misc.AbcCore.Domain;

namespace Nop.Plugin.Misc.AbcCore.Mapping
{
    /// <summary>
    /// Base instance of backward compatibility of table naming
    /// </summary>
    public partial class AbcCoreNameCompatibility : INameCompatibility
    {
        public Dictionary<Type, string> TableNames => new Dictionary<Type, string>
        {
            { typeof(ProductPackageUpc), "Product_Package_Upc" },
            { typeof(ProductAbcDescription), "ProductAbcDescriptions" },
            { typeof(AbcPromoProductMapping), "ProductAbcPromo" }
        };

        public Dictionary<(Type, string), string> ColumnName => new Dictionary<(Type, string), string> { };
    }
}