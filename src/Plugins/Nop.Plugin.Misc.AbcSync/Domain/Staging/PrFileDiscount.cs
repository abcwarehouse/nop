using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSync.Domain.Staging
{
    public class PrFileDiscount
    {
        public int Id { get; set; }
        public string ProductSku { get; set; }
        public bool IsAbcDiscount { get; set; }
        public bool IsHawthorneDiscount { get; set; }
        public string Name { get; set; }
        public decimal DiscountAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }

}
