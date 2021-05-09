using Nop.Core;
using Nop.Data;
using System;
using System.Linq;

namespace Nop.Plugin.Misc.AbcCore.Domain
{
    public partial class ProductPackageUpc : BaseEntity
    {
        public static Func<ProductPackageUpc> GetByProductIdFunc(IRepository<ProductPackageUpc> repo, string sku)
        {
            return () => { return repo.Table.Where(p => p.Sku == sku).FirstOrDefault(); };
        }

        public virtual string Sku { get; set; }
        public virtual int Product_Id { get; set; }
        public virtual string Description { get; set; }
        public virtual string Upc { get; set; }
    }
}