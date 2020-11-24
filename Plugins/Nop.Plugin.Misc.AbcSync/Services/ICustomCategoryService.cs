using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcSync.Services
{
    public interface ICustomCategoryService : ICategoryService
    {
        // used to allow for selecting store-specific if desired.

        /// <summary>
        /// Gets product category mapping collection
        /// </summary>
        /// <param name="categoryId">Category identifier</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Product a category mapping collection</returns>
        IPagedList<ProductCategory> GetProductCategoriesByCategoryId(int categoryId,
            int storeId, int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false);
    }
}