using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Seo;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Services.Catalog;
using Nop.Services.Seo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressProductService : IAbcMattressProductService
    {
        private readonly IAbcMattressService _abcMattressService;
        private readonly IAbcMattressEntryService _abcMattressEntryService;
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IUrlRecordService _urlRecordService;

        public AbcMattressProductService(
            IAbcMattressService abcMattressService,
            IAbcMattressEntryService abcMattressEntryService,
            ICategoryService categoryService,
            IProductService productService,
            IUrlRecordService urlRecordService
        )
        {
            _abcMattressService = abcMattressService;
            _abcMattressEntryService = abcMattressEntryService;
            _categoryService = categoryService;
            _productService = productService;
            _urlRecordService = urlRecordService;
        }

        public Product UpsertAbcMattressProduct(AbcMattressModel abcMattressModel)
        {
            Product product = (abcMattressModel.ProductId == null) ?
                CreateAbcMattressProduct(abcMattressModel) :
                _productService.GetProductById(abcMattressModel.ProductId.Value);

            SetCategories(abcMattressModel, product);

            return product;
        }

        private void SetCategories(AbcMattressModel model, Product product)
        {
            var existingProductCategories = _categoryService.GetProductCategoriesByProductId(product.Id);
            var entries = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id);
            var newProductCategories = entries.Select(e => AbcMattressEntryToProductCategory(e));
        
            var toBeDeleted = existingProductCategories
                .Where(e => !newProductCategories.Any(n => n.ProductId == e.ProductId &&
                                                           n.CategoryId == e.CategoryId));
            var toBeInserted = newProductCategories
                .Where(n => !existingProductCategories.Any(e => n.ProductId == e.ProductId &&
                                                           n.CategoryId == e.CategoryId));

            toBeInserted.ToList().ForEach(n => _categoryService.InsertProductCategory(n));
            toBeDeleted.ToList().ForEach(e => _categoryService.DeleteProductCategory(e));
        }

        public ProductCategory AbcMattressEntryToProductCategory(AbcMattressEntry entry)
        {
            var model = _abcMattressService.GetAllAbcMattressModels()
                                           .Where(amm => amm.Id == entry.AbcMattressModelId)
                                           .FirstOrDefault();
            var convertedCategoryName = ConvertSizeToCategoryName(entry.Size);
            var category = _categoryService.GetAllCategories()
                                           .Where(c => c.Name.ToLower().Equals(convertedCategoryName))
                                           .FirstOrDefault();

            if (category == null)
            {
                throw new Exception($"Unable to find category {convertedCategoryName}");
            }

            return new ProductCategory()
            {
                ProductId = model.ProductId.Value,
                CategoryId = category.Id
            };
        }

        private string ConvertSizeToCategoryName(string size)
        {
            var loweredSize = size.ToLower();
            if (loweredSize == "twinxl")
            {
                return "twin extra long";
            }

            return loweredSize;
        }

        private Product CreateAbcMattressProduct(AbcMattressModel abcMattressModel)
        {
            var newProduct = new Product()
            {
                Name = abcMattressModel.Description ?? abcMattressModel.Name,
                Sku = abcMattressModel.Name,
                AllowCustomerReviews = false,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                VisibleIndividually = true,
                ProductType = ProductType.SimpleProduct,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000
            };
            _productService.InsertProduct(newProduct);

            var urlRecord = new UrlRecord()
            {
                EntityId = newProduct.Id,
                EntityName = "Product",
                Slug = newProduct.Sku,
                IsActive = true,
                LanguageId = 0
            };
            _urlRecordService.InsertUrlRecord(urlRecord);

            abcMattressModel.ProductId = newProduct.Id;
            _abcMattressService.UpdateAbcMattressModel(abcMattressModel);

            return newProduct;
        }
    }
}
