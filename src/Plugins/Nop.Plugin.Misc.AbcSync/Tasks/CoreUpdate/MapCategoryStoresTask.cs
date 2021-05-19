using Nop.Services.Stores;
using Nop.Services.Tasks;
using System.Linq;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcSync.Services;
using Nop.Services.Logging;
using Nop.Core.Domain.Stores;
using Nop.Services.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate
{
    public class MapCategoryStoresTask : IScheduleTask
    {
        private readonly ICustomCategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IStoreService _storeService;
        private readonly IStoreMappingService _storeMappingService;

        private readonly ILogger _logger;

        private readonly ImportSettings _settings;

        public MapCategoryStoresTask(
            ICustomCategoryService categoryService,
            IProductService productService,
            IStoreService storeService,
            IStoreMappingService storeMappingService,
            ILogger logger,
            ImportSettings settings
        )
        {
            _categoryService = categoryService;
            _productService = productService;
            _storeService = storeService;
            _storeMappingService = storeMappingService;
            _logger = logger;
            _settings = settings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_settings.SkipMapCategoryStoresTask)
            {
                this.Skipped();
                return;
            }

            var categories = await _categoryService.GetAllCategoriesAsync();
            foreach (var category in categories)
            {
                var stores = await _storeService.GetAllStoresAsync();
                foreach (var store in stores)
                {
                    if (await IsCategoryEmptyAsync(category, store.Id))
                    {
                        await DeleteCategoryStoreMappingAsync(category, store);
                    }
                    else
                    {
                        await AddCategoryStoreMappingAsync(category, store);
                    }
                }
            }
        }

        private async System.Threading.Tasks.Task<bool> IsCategoryEmptyAsync(Category category, int storeId)
        {
            var productIds = _categoryService.GetProductCategoriesByCategoryId(category.Id)
                                             .Select(pc => pc.ProductId)
                                             .ToArray();
            var publishedProducts = (await _productService.GetProductsByIdsAsync(productIds))
                                                   .Where(p => p.Published);
            foreach (var p in publishedProducts)
            {
                var storeMappings = (await _storeMappingService.GetStoreMappingsAsync<Product>(p))
                                                        .Where(sm => sm.StoreId == storeId);
                if (storeMappings.Any()) { return false; }
            }

            var childCategoryIds = await _categoryService.GetChildCategoryIdsAsync(category.Id, storeId);
            foreach (var childCategoryId in childCategoryIds)
            {
                var childCategory = await _categoryService.GetCategoryByIdAsync(childCategoryId);
                if (!await IsCategoryEmptyAsync(childCategory, storeId))
                {
                    // found a non-empty child category, this category isn't empty
                    return false;
                }
            }

            // has no products, and no child categories, this category is empty
            return true;
        }

        private async System.Threading.Tasks.Task DeleteCategoryStoreMappingAsync(Category category, Store store)
        {
            var existingStoreMapping = (await _storeMappingService.GetStoreMappingsAsync<Category>(category))
                                                           .Where(sm => sm.StoreId == store.Id)
                                                           .FirstOrDefault();

            if (existingStoreMapping != null)
            {
                await _storeMappingService.DeleteStoreMappingAsync(existingStoreMapping);
                await _logger.InformationAsync(
                    $"{store.Name}: Unmapped '{category.Name}' category (no products)."
                );
            }
        }

        private async System.Threading.Tasks.Task AddCategoryStoreMappingAsync(Category category, Store store)
        {
            var existingStoreMapping = (await _storeMappingService.GetStoreMappingsAsync<Category>(category))
                                                           .Where(sm => sm.StoreId == store.Id)
                                                           .FirstOrDefault();

            if (existingStoreMapping == null)
            {
                await _storeMappingService.InsertStoreMappingAsync<Category>(category, store.Id);
                await _logger.InformationAsync(
                    $"{store.Name}: Mapped '{category.Name}' category (contained products)."
                );
            }
        }
    }
}