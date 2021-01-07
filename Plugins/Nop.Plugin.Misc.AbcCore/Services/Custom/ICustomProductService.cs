using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcCore.Services.Custom
{
    public interface ICustomProductService : IProductService
    {
        IList<Product> GetProductsWithoutImages();
    }
}