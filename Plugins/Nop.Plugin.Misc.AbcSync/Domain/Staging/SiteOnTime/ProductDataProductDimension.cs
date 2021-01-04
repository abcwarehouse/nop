using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSync.Domain.Staging
{
    public class ProductDataProductDimension
    {
        public int Id { get; set; }
        public float MeasurementValue { get; set; }
        public string FractionalDimensionValue { get; set; }
        public string MeasurementName { get; set; }
        public string UnitsOfMeasuremen { get; set; }
        public int ProductDataProduct_id { get; set; }
    }

}
