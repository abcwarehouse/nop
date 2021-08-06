using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging
{
    public class ProductDataProductFeature
    {
        public int Id { get; set; }
        public string FeatureCategory { get; set; }
        public string Featurevalue { get; set; }
        public string FeatureSeq { get; set; }
        public int? ProductDataProduct_id { get; set; }

    }
}
