using Nop.Core.Domain.Catalog;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcMattresses.Models
{
    public class AbcMattressProductAttributeModel
    {
        public ProductAttribute ProductAttribute { get; private set; }
        public IList<PredefinedProductAttributeValue> PredefinedProductAttributeValues { get; private set; }

        public AbcMattressProductAttributeModel(
            ProductAttribute productAttribute,
            IList<PredefinedProductAttributeValue> predefinedProductAttributeValues
        )
        {
            ProductAttribute = productAttribute;
            PredefinedProductAttributeValues = predefinedProductAttributeValues;
        }
    }
}
