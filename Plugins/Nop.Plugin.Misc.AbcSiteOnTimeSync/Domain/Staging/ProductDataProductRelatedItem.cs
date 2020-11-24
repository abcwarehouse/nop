using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging
{
    public class ProductDataProductRelatedItem
    {
        public int Id { get; set; }
        public string Related { get; set; }
        public int? ProductDataProduct_id { get; set; }

    }
}
