using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Domain;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public interface IProductAbcDescriptionService
    {
        ProductAbcDescription GetProductAbcDescriptionByProductId(int productId);
    }
}