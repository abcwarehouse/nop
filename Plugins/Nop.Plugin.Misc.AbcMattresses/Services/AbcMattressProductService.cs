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
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IUrlRecordService _urlRecordService;

        public AbcMattressProductService(
            IAbcMattressService abcMattressService,
            IAbcMattressEntryService abcMattressEntryService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            IProductService productService,
            IProductAttributeService productAttributeService,
            IUrlRecordService urlRecordService
        )
        {
            _abcMattressService = abcMattressService;
            _abcMattressEntryService = abcMattressEntryService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _productService = productService;
            _productAttributeService = productAttributeService;
            _urlRecordService = urlRecordService;
        }

        public Product UpsertAbcMattressProduct(AbcMattressModel abcMattressModel)
        {
            Product product = (abcMattressModel.ProductId == null) ?
                CreateAbcMattressProduct(abcMattressModel) :
                _productService.GetProductById(abcMattressModel.ProductId.Value);

            SetManufacturer(abcMattressModel, product);
            SetCategories(abcMattressModel, product);
            SetProductAttributes(abcMattressModel, product);

            return product;
        }

        private void SetProductAttributes(AbcMattressModel model, Product product)
        {
            var productAttributes = _productAttributeService.GetAllProductAttributes()
                                                            .Where(pa => pa.Name == AbcMattressesConsts.MattressSizeName ||
                                                                         pa.Name == AbcMattressesConsts.BaseName ||
                                                                         pa.Name == AbcMattressesConsts.FreeGiftName);

            foreach (var pa in productAttributes)
            {
                var pam = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                                                          .Where(pam => pam.ProductAttributeId == pa.Id)
                                                          .FirstOrDefault();

                if (pam == null)
                {
                    pam = new ProductAttributeMapping()
                    {
                        ProductId = product.Id,
                        ProductAttributeId = pa.Id,
                        IsRequired = true,
                        AttributeControlType = AttributeControlType.DropdownList,
                        DisplayOrder = GetDisplayOrder(pa.Name)
                    };
                    _productAttributeService.InsertProductAttributeMapping(pam);
                }

                if (pa.Name == AbcMattressesConsts.MattressSizeName)
                {
                    var existingSizes = _productAttributeService.GetProductAttributeValues(pam.Id)
                                                                .Where(pav =>
                                                                    pav.ProductAttributeMappingId == pam.Id
                                                                );
                    var entries = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id);                                            
                    var newSizes = entries.Select(e => AbcMattressEntryToProductAttributeValue(pam, e)).ToList();

                    var toBeDeleted = existingSizes
                        .Where(e => !newSizes.Any(n => n.Name == e.Name &&
                                                       n.PriceAdjustment == e.PriceAdjustment));
                    var toBeInserted = newSizes
                        .Where(n => !existingSizes.Any(e => n.Name == e.Name &&
                                                            n.PriceAdjustment == e.PriceAdjustment));

                    toBeInserted.ToList().ForEach(n => _productAttributeService.InsertProductAttributeValue(n));
                    toBeDeleted.ToList().ForEach(e => _productAttributeService.DeleteProductAttributeValue(e));
                }
            }
        }

        private ProductAttributeValue AbcMattressEntryToProductAttributeValue(
            ProductAttributeMapping pam,
            AbcMattressEntry e
        )
        {
            return new ProductAttributeValue()
            {
                ProductAttributeMappingId = pam.Id,
                Name = e.Size,
                PriceAdjustment = e.Price
            };
        }

        private int GetDisplayOrder(string productAttributeName)
        {
            switch (productAttributeName)
            {
                case AbcMattressesConsts.MattressSizeName:
                    return 0;
                case AbcMattressesConsts.BaseName:
                    return 1;
                case AbcMattressesConsts.FreeGiftName:
                    return 2;
            }
            return -1;
        }

        private void SetManufacturer(AbcMattressModel abcMattressModel, Product product)
        {
            var existingProductManufacturers = _manufacturerService.GetProductManufacturersByProductId(product.Id);
            var newProductManufacturer = new ProductManufacturer()
            {
                ProductId = product.Id,
                ManufacturerId = abcMattressModel.ManufacturerId.Value
            };
        
            var toBeDeleted = existingProductManufacturers
                .Where(e => e.ProductId != newProductManufacturer.ProductId &&
                            e.ManufacturerId != newProductManufacturer.ProductId);
            toBeDeleted.ToList().ForEach(e => _manufacturerService.DeleteProductManufacturer(e));

            if (!existingProductManufacturers.Any() ||
                 toBeDeleted.Any())
            {
                _manufacturerService.InsertProductManufacturer(newProductManufacturer);
            }
        }

        private void SetCategories(AbcMattressModel model, Product product)
        {
            var existingProductCategories = _categoryService.GetProductCategoriesByProductId(product.Id);
            var entries = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id);
            var newProductCategories = entries.Select(e => AbcMattressEntryToProductCategory(e)).ToList();
            
            // comfort
            var comfortCategory = _categoryService.GetAllCategories()
                                             .Where(c => c.Name.ToLower().Equals(ConvertComfortToCategoryName(model.Comfort)))
                                             .FirstOrDefault();
            if (comfortCategory != null)
            {
                newProductCategories.Add(new ProductCategory()
                {
                    ProductId = product.Id,
                    CategoryId = comfortCategory.Id
                });
            }
            
        
            var toBeDeleted = existingProductCategories
                .Where(e => !newProductCategories.Any(n => n.ProductId == e.ProductId &&
                                                           n.CategoryId == e.CategoryId));
            var toBeInserted = newProductCategories
                .Where(n => !existingProductCategories.Any(e => n.ProductId == e.ProductId &&
                                                           n.CategoryId == e.CategoryId));

            toBeInserted.ToList().ForEach(n => _categoryService.InsertProductCategory(n));
            toBeDeleted.ToList().ForEach(e => _categoryService.DeleteProductCategory(e));
        }

        private string ConvertComfortToCategoryName(string comfort)
        {
            return comfort.Replace("-", "").ToLower();
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
