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
            CatalogSettings catalogSettings,
            IAclService aclService,
            ICacheKeyService cacheKeyService,
            ICustomerService customerService,
            IEventPublisher eventPublisher,
            ILocalizationService localizationService,
            IRepository<AclRecord> aclRepository,
            IRepository<Category> categoryRepository,
            IRepository<DiscountCategoryMapping> discountCategoryMappingRepository,
            IRepository<Product> productRepository,
            IRepository<ProductCategory> productCategoryRepository,
            IRepository<StoreMapping> storeMappingRepository,
            IStaticCacheManager staticCacheManager,
            IStoreContext storeContext,
            IStoreMappingService storeMappingService,
            IWorkContext workContext
        ) : base(catalogSettings, aclService, cacheKeyService, customerService,
                eventPublisher, localizationService, aclRepository, categoryRepository,
                discountCategoryMappingRepository, productRepository, productCategoryRepository,
                storeMappingRepository, staticCacheManager, storeContext, storeMappingService,
                workContext)
        {
            _aclRepository = aclRepository;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
            _productCategoryRepository = productCategoryRepository;
            _storeMappingRepository = storeMappingRepository;

            _customerService = customerService;

            _catalogSettings = catalogSettings;

            _storeContext = storeContext;
            _workContext = workContext;
        }

        // this copies the source from CategoryService and modifies to allow for
        // passing in the store ID.
        public IPagedList<ProductCategory> GetProductCategoriesByCategoryId(
            int categoryId,
            int storeId = 0,
            int pageIndex = 0,
            int pageSize = int.MaxValue,
            bool showHidden = false)
        {
            if (categoryId == 0)
                return new PagedList<ProductCategory>(new List<ProductCategory>(), pageIndex, pageSize);

            var query = from pc in _productCategoryRepository.Table
                        join p in _productRepository.Table on pc.ProductId equals p.Id
                        where pc.CategoryId == categoryId &&
                              !p.Deleted &&
                              (showHidden || p.Published)
                        orderby pc.DisplayOrder, pc.Id
                        select pc;

            if (!showHidden && (!_catalogSettings.IgnoreAcl || !_catalogSettings.IgnoreStoreLimitations))
            {
                if (!_catalogSettings.IgnoreAcl)
                {
                    //ACL (access control list)
                    var allowedCustomerRolesIds = _customerService.GetCustomerRoleIds(_workContext.CurrentCustomer);
                    query = from pc in query
                            join c in _categoryRepository.Table on pc.CategoryId equals c.Id
                            join acl in _aclRepository.Table
                                on new
                                {
                                    c1 = c.Id,
                                    c2 = nameof(Category)
                                }
                                equals new
                                {
                                    c1 = acl.EntityId,
                                    c2 = acl.EntityName
                                }
                                into c_acl
                            from acl in c_acl.DefaultIfEmpty()
                            where !c.SubjectToAcl || allowedCustomerRolesIds.Contains(acl.CustomerRoleId)
                            select pc;
                }

                if (!_catalogSettings.IgnoreStoreLimitations)
                {
                    //Store mapping
                    // DFar: this is the modified line
                    var currentStoreId = storeId != 0 ? storeId : _storeContext.CurrentStore.Id;
                    query = from pc in query
                            join c in _categoryRepository.Table on pc.CategoryId equals c.Id
                            join sm in _storeMappingRepository.Table
                                on new
                                {
                                    c1 = c.Id,
                                    c2 = nameof(Category)
                                }
                                equals new
                                {
                                    c1 = sm.EntityId,
                                    c2 = sm.EntityName
                                }
                                into c_sm
                            from sm in c_sm.DefaultIfEmpty()
                            where !c.LimitedToStores || currentStoreId == sm.StoreId
                            select pc;
                }

                query = query.Distinct().OrderBy(pc => pc.DisplayOrder).ThenBy(pc => pc.Id);
            }

            var productCategories = new PagedList<ProductCategory>(query, pageIndex, pageSize);

            return productCategories;
        }
    }
}