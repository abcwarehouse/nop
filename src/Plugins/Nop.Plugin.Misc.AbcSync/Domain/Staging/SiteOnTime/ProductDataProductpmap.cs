using System;

namespace Nop.Plugin.Misc.AbcSync.Domain.Staging.SiteOnTime
{
    public class ProductDataProductpmap
    {
        public int Id { get; set; }

        public decimal? price { get; set; }

        public decimal? discount { get; set; }

        public DateTime? startDate { get; set; }

        public DateTime? endDate { get; set; }

        public int? ProductDataProduct_id { get; set; }

    }
}