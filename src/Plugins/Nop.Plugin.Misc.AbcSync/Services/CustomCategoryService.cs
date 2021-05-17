using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Services.Caching;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.AbcSync.Services
{
    public class CustomCategoryService : CategoryService, ICustomCategoryService
    {
        private readonly IRepository<AclRecord> _aclRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IRepository<ProductCategory> _productCategoryRepository;
        private readonly IRepository<StoreMapping> _storeMappingRepository;

        private readonly ICustomerService _customerService;

        private readonly CatalogSettings _catalogSettings;

        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        public CustomCategoryService(
            IAclService aclService,
            ICustomerService customerService,
            ILocalizationService localizationService,
            IRepository<Category> categoryRepository,
            IRepository<DiscountCategoryMapping> discountCategoryMappingRepository,
            IRepository<Product> productRepository,
            IRepository<ProductCategory> productCategoryRepository,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IWorkContext workContext
        ) : base(aclService, customerService, localizationService,
                categoryRepository, discountCategoryMappingRepository,
                productRepository, productCategoryRepository,
                staticCacheManager, storeContext, storeMappingService,
                workContext)
        {
            _productCategoryRepository = productCategoryRepository;
        }

        // this copies the source from CategoryService and modifies to allow for
        // passing in the store ID.
        public IList<ProductCategory> GetProductCategoriesByCategoryId(
            int categoryId
        )
        {
            return _productCategoryRepository.Table
                                             .Where(pc => pc.CategoryId == categoryId)
                                             .ToList();
        }
    }
}