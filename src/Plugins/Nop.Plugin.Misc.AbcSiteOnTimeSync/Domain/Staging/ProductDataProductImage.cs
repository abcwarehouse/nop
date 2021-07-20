using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging
{
    public class ProductDataProductImage
    {
        public int Id { get; set; }
        public string Large { get; set; }
        public string Thumb { get; set; }
        public int? ProductDataProduct_id { get; set; }
    }
}
