using Nop.Core;
using Nop.Core.Domain.Catalog;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nop.Plugin.Misc.AbcSync.Domain
{
    /// <summary>
    /// A mapping between products and categories/manufacturers
    /// </summary>
    public partial class EntityFeaturedProduct : BaseEntity
    {
        public virtual int Product_Id { get; set; }
        public virtual int Entity_Id { get; set; }
        public virtual int DisplayOrder { get; set; }
        public virtual bool isCategory { get; set; }

    }
}