using Nop.Core;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSync.Domain.Staging
{
    public class Rebate
    {
        public int Id { get; set; }
        public string AbcBrand { get; set; }
        public string AbcId { get; set; }
        public string Name { get; set; }
        public decimal RebateAmount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}