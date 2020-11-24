using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging
{
    public class ProductDataProductFilter
    {
        public int Id { get; set; }
        public string SortField { get; set; }
        public string SortValue { get; set; }
        public string Units { get; set; }
        public int ValueDisplayOrder { get; set; }
        public int? ProductDataProduct_id { get; set; }

    }
}
