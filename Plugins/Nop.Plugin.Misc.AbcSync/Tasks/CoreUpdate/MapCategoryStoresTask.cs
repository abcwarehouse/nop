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

        public void Execute()
        {
            if (_settings.SkipMapCategoryStoresTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();

            var stores = _storeService.GetAllStores();
            foreach (var store in stores)
            {
                var categories = _categoryService.GetAllCategories(store.Id);
                foreach (var category in categories)
                {
                    if (IsCategoryEmpty(category, store.Id))
                    {
                        DeleteCategoryStoreMapping(category, store);
                    }
                    else
                    {
                        AddCategoryStoreMapping(category, store);
                    }
                }
            }

            this.LogEnd();
        }

        private bool IsCategoryEmpty(Category category, int storeId)
        {
            var productIds = _categoryService.GetProductCategoriesByCategoryId(category.Id, storeId)
                                             .Select(pc => pc.ProductId)
                                             .ToArray();
            var products = _productService.GetProductsByIds(productIds);
            foreach (var p in products)
            {
                var storeMappings = _storeMappingService.GetStoreMappings<Product>(p)
                                                        .Where(sm => sm.StoreId == storeId);
                if (storeMappings.Any()) { return false; }
            }

            var childCategoryIds = _categoryService.GetChildCategoryIds(category.Id, storeId);
            foreach (var childCategoryId in childCategoryIds)
            {
                var childCategory = _categoryService.GetCategoryById(childCategoryId);
                if (!IsCategoryEmpty(childCategory, storeId))
                {
                    // found a non-empty child category, this category isn't empty
                    return false;
                }
            }

            // has no products, and no child categories, this category is empty
            return true;
        }

        private void DeleteCategoryStoreMapping(Category category, Store store)
        {
            var existingStoreMapping = _storeMappingService.GetStoreMappings<Category>(category)
                                                           .Where(sm => sm.StoreId == store.Id)
                                                           .FirstOrDefault();

            if (existingStoreMapping != null)
            {
                _storeMappingService.DeleteStoreMapping(existingStoreMapping);
                _logger.Information(
                    $"{store.Name}: Unmapped '{category.Name}' category (no products)."
                );
            }
        }

        private void AddCategoryStoreMapping(Category category, Store store)
        {
            var existingStoreMapping = _storeMappingService.GetStoreMappings<Category>(category)
                                                           .Where(sm => sm.StoreId == store.Id)
                                                           .FirstOrDefault();

            if (existingStoreMapping == null)
            {
                _storeMappingService.InsertStoreMapping<Category>(category, store.Id);
                _logger.Information(
                    $"{store.Name}: Mapped '{category.Name}' category (contained products)."
                );
            }
        }
    }
}