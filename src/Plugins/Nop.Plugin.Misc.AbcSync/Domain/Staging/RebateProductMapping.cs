using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSync.Domain.Staging
{
    public class RebateProductMapping
    {
        public int Id { get; set; }
        public string AbcBrand { get; set; }
        public string AbcRebateId { get; set; }
        public string ItemSku { get; set; }
    }
}
