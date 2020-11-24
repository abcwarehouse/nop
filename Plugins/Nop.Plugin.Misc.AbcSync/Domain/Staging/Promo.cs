using Nop.Core;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSync.Domain.Staging
{
    public class Promo
    {
        public int Id { get; set; }
        public string AbcBuyerId { get; set; }
        public string AbcDeptCode { get; set; }
        public string AbcPromoCode { get; set; }
        public string Name { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool DiscountUsesPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercentage { get; set; }
    }
}