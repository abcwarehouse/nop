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
        private readonly IAbcMattressBaseService _abcMattressBaseService;
        private readonly IAbcMattressEntryService _abcMattressEntryService;
        private readonly IAbcMattressGiftService _abcMattressGiftService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IUrlRecordService _urlRecordService;

        public AbcMattressProductService(
            IAbcMattressService abcMattressService,
            IAbcMattressBaseService abcMattressBaseService,
            IAbcMattressEntryService abcMattressEntryService,
            IAbcMattressGiftService abcMattressGiftService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            IProductService productService,
            IProductAttributeService productAttributeService,
            IUrlRecordService urlRecordService
        )
        {
            _abcMattressService = abcMattressService;
            _abcMattressBaseService = abcMattressBaseService;
            _abcMattressEntryService = abcMattressEntryService;
            _abcMattressGiftService = abcMattressGiftService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _productService = productService;
            _productAttributeService = productAttributeService;
            _urlRecordService = urlRecordService;
        }

        public Product UpsertAbcMattressProduct(AbcMattressModel abcMattressModel)
        {
            var hasExistingProduct = abcMattressModel.ProductId != null;
            Product product = (hasExistingProduct) ?
                _productService.GetProductById(abcMattressModel.ProductId.Value) :
                new Product();

            product.Name = GetProductName(abcMattressModel);
            product.Sku = abcMattressModel.Name;
            product.AllowCustomerReviews = false;
            product.Published = true;
            product.CreatedOnUtc = DateTime.UtcNow;
            product.VisibleIndividually = true;
            product.ProductType = ProductType.SimpleProduct;
            product.OrderMinimumQuantity = 1;
            product.OrderMaximumQuantity = 10000;

            if (hasExistingProduct)
            {
                _productService.UpdateProduct(product);
            }
            else
            {
                _productService.InsertProduct(product);
            }

            _urlRecordService.SaveSlug(product, _urlRecordService.ValidateSeName(product, string.Empty, product.Name, false), 0);

            if (!hasExistingProduct)
            {
                abcMattressModel.ProductId = product.Id;
                _abcMattressService.UpdateAbcMattressModel(abcMattressModel);
            }

            return product;
        }

        public void SetProductAttributes(AbcMattressModel model, Product product)
        {
            var nonBaseProductAttributes = _productAttributeService.GetAllProductAttributes()
                                                            .Where(pa => pa.Name == AbcMattressesConsts.MattressSizeName ||
                                                                         pa.Name == AbcMattressesConsts.FreeGiftName);

            foreach (var pa in nonBaseProductAttributes)
            {
                switch (pa.Name)
                {
                    case AbcMattressesConsts.MattressSizeName:
                        MergeSizes(model, pa, product);
                        break;
                    case AbcMattressesConsts.FreeGiftName:
                        MergeGifts(model, pa, product);
                        break;
                }
            }

            var baseProductAttributes = _productAttributeService.GetAllProductAttributes()
                                                            .Where(pa => AbcMattressesConsts.IsBase(pa.Name));
            foreach (var pa in baseProductAttributes)
            {
                MergeBases(model, pa, product);
            }
        }

        public void SetManufacturer(AbcMattressModel abcMattressModel, Product product)
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

        public void SetCategories(AbcMattressModel model, Product product)
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

        private string GetProductName(AbcMattressModel model)
        {
            var modelName = model.Description ?? model.Name;

            var brand = _manufacturerService.GetManufacturerById(model.ManufacturerId.Value);

            return $"{brand.Name} {modelName}";
        }

        private void MergeGifts(AbcMattressModel model, ProductAttribute pa, Product product)
        {
            var pam = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                                                          .Where(pam => pam.ProductAttributeId == pa.Id)
                                                          .FirstOrDefault();
            var modelHasGifts = _abcMattressGiftService.GetAbcMattressGiftsByModelId(model.Id).Any();

            if (pam == null && modelHasGifts)
            {
                pam = new ProductAttributeMapping()
                {
                    ProductId = product.Id,
                    ProductAttributeId = pa.Id,
                    IsRequired = false,
                    AttributeControlType = AttributeControlType.DropdownList,
                    DisplayOrder = 2
                };
                _productAttributeService.InsertProductAttributeMapping(pam);
            }
            else if (pam != null && !modelHasGifts)
            {
                _productAttributeService.DeleteProductAttributeMapping(pam);
            }

            if (!modelHasGifts) { return; }

            var existingGifts = _productAttributeService.GetProductAttributeValues(pam.Id)
                                                        .Where(pav =>
                                                            pav.ProductAttributeMappingId == pam.Id
                                                        );
            var gifts = _abcMattressGiftService.GetAbcMattressGiftsByModelId(model.Id);
            var newGifts = gifts.Select(g => g.ToProductAttributeValue(pam.Id)).ToList();

            var toBeDeleted = existingGifts
                .Where(e => !newGifts.Any(n => n.Name == e.Name));
            var toBeInserted = newGifts
                .Where(n => !existingGifts.Any(e => n.Name == e.Name));

            toBeInserted.ToList().ForEach(n => _productAttributeService.InsertProductAttributeValue(n));
            toBeDeleted.ToList().ForEach(e => _productAttributeService.DeleteProductAttributeValue(e));
        }

        private void MergeBases(AbcMattressModel model, ProductAttribute pa, Product product)
        {
            var pam = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                                                          .Where(pam => pam.ProductAttributeId == pa.Id)
                                                          .FirstOrDefault();
            var abcMattressEntry = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id)
                                                           .Where(ame => pa.Name == $"Base ({ame.Size})")
                                                           .FirstOrDefault();
            if (abcMattressEntry == null) { return; }

            var bases = _abcMattressBaseService.GetAbcMattressBasesByEntryId(abcMattressEntry.Id);

            if (pam == null && bases.Any())
            {
                var sizePa = _productAttributeService.GetAllProductAttributes()
                                                            .Where(pa => pa.Name == AbcMattressesConsts.MattressSizeName)
                                                            .FirstOrDefault();
                var sizePam = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                                                          .Where(pam => pam.ProductAttributeId == sizePa.Id)
                                                          .FirstOrDefault();
                var sizePav = _productAttributeService.GetProductAttributeValues(sizePam.Id)
                                                        .Where(pav =>
                                                            pav.ProductAttributeMappingId == sizePam.Id &&
                                                            pav.Name == abcMattressEntry.Size
                                                        )
                                                        .FirstOrDefault();
                pam = new ProductAttributeMapping()
                {
                    ProductId = product.Id,
                    ProductAttributeId = pa.Id,
                    IsRequired = false,
                    AttributeControlType = AttributeControlType.DropdownList,
                    DisplayOrder = 1,
                    TextPrompt = "Base",
                    ConditionAttributeXml = $"<Attributes><ProductAttribute ID=\"{sizePam.Id}\"><ProductAttributeValue><Value>{sizePav.Id}</Value></ProductAttributeValue></ProductAttribute></Attributes>"
                };
                _productAttributeService.InsertProductAttributeMapping(pam);
            }
            else if (pam != null && !bases.Any())
            {
                _productAttributeService.DeleteProductAttributeMapping(pam);
            }

            if (!bases.Any()) { return; }

            var existingBases = _productAttributeService.GetProductAttributeValues(pam.Id)
                                                        .Where(pav =>
                                                            pav.ProductAttributeMappingId == pam.Id
                                                        );
            var newBases = bases.Select(g => g.ToProductAttributeValue(pam.Id)).ToList();

            var toBeDeleted = existingBases
                .Where(e => !newBases.Any(n => n.Name == e.Name));
            var toBeInserted = newBases
                .Where(n => !existingBases.Any(e => n.Name == e.Name));

            toBeInserted.ToList().ForEach(n => _productAttributeService.InsertProductAttributeValue(n));
            toBeDeleted.ToList().ForEach(e => _productAttributeService.DeleteProductAttributeValue(e));
        }

        private void MergeSizes(AbcMattressModel model, ProductAttribute pa, Product product)
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
                    DisplayOrder = 0
                };
                _productAttributeService.InsertProductAttributeMapping(pam);
            }

            var existingSizes = _productAttributeService.GetProductAttributeValues(pam.Id)
                                                        .Where(pav =>
                                                            pav.ProductAttributeMappingId == pam.Id
                                                        );
            var entries = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id);
            var newSizes = entries.Select(e => e.ToProductAttributeValue(pam.Id)).ToList();

            var toBeDeleted = existingSizes
                .Where(e => !newSizes.Any(n => n.Name == e.Name &&
                                               n.PriceAdjustment == e.PriceAdjustment));
            var toBeInserted = newSizes
                .Where(n => !existingSizes.Any(e => n.Name == e.Name &&
                                                    n.PriceAdjustment == e.PriceAdjustment));

            toBeInserted.ToList().ForEach(n => _productAttributeService.InsertProductAttributeValue(n));
            toBeDeleted.ToList().ForEach(e => _productAttributeService.DeleteProductAttributeValue(e));
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
    }
}
