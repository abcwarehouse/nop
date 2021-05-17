using System.Collections.Generic;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcSync.Services
{
    public interface ICustomCategoryService : ICategoryService
    {
        IList<ProductCategory> GetProductCategoriesByCategoryId(int categoryId);
    }
}