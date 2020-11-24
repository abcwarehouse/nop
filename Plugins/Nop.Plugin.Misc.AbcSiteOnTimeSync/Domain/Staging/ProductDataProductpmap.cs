using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging
{
    public class ProductDataProductpmap
    {
        public int Id { get; set; }
        public decimal price { get; set; }
        public decimal discount { get; set; }
        public DateTime? startdate { get; set; }
        public DateTime? enddate { get; set; }
        public int? ProductDataProduct_id { get; set; }

    }
}
