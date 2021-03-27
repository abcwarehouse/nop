using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Seo;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Logging;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Tax;
using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Plugin.Widgets.AbcSynchronyPayments.Services;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressProductService : IAbcMattressProductService
    {
        private readonly IAbcMattressModelService _abcMattressService;
        private readonly IAbcMattressBaseService _abcMattressBaseService;
        private readonly IAbcMattressEntryService _abcMattressEntryService;
        private readonly IAbcMattressFrameService _abcMattressFrameService;
        private readonly IAbcMattressGiftService _abcMattressGiftService;
        private readonly IAbcMattressPackageService _abcMattressPackageService;
        private readonly IAbcMattressProtectorService _abcMattressProtectorService;
        private readonly ICategoryService _categoryService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IProductService _productService;
        private readonly IProductAbcFinanceService _productAbcFinanceService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IStoreService _storeService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly IUrlRecordService _urlRecordService;
        private readonly ILogger _logger;

        public AbcMattressProductService(
            IAbcMattressModelService abcMattressService,
            IAbcMattressBaseService abcMattressBaseService,
            IAbcMattressEntryService abcMattressEntryService,
            IAbcMattressFrameService abcMattressFrameService,
            IAbcMattressGiftService abcMattressGiftService,
            IAbcMattressPackageService abcMattressPackageService,
            IAbcMattressProtectorService abcMattressProtectorService,
            ICategoryService categoryService,
            IGenericAttributeService genericAttributeService,
            IManufacturerService manufacturerService,
            IProductService productService,
            IProductAbcFinanceService productAbcFinanceService,
            IProductAttributeService productAttributeService,
            IStoreService storeService,
            IStoreMappingService storeMappingService,
            ITaxCategoryService taxCategoryService,
            IUrlRecordService urlRecordService,
            ILogger logger
        )
        {
            _abcMattressService = abcMattressService;
            _abcMattressBaseService = abcMattressBaseService;
            _abcMattressEntryService = abcMattressEntryService;
            _abcMattressFrameService = abcMattressFrameService;
            _abcMattressGiftService = abcMattressGiftService;
            _abcMattressPackageService = abcMattressPackageService;
            _abcMattressProtectorService = abcMattressProtectorService;
            _categoryService = categoryService;
            _genericAttributeService = genericAttributeService;
            _manufacturerService = manufacturerService;
            _productService = productService;
            _productAbcFinanceService = productAbcFinanceService;
            _productAttributeService = productAttributeService;
            _storeMappingService = storeMappingService;
            _storeService = storeService;
            _taxCategoryService = taxCategoryService;
            _urlRecordService = urlRecordService;
            _logger = logger;
        }

        public List<string> GetMattressItemNos()
        {
            var entryItemNos = _abcMattressEntryService.GetAllAbcMattressEntries().Select(e => e.ItemNo);
            var packageItemNos = _abcMattressPackageService.GetAllAbcMattressPackages().Select(p => p.ItemNo);

            return entryItemNos.Union(packageItemNos).ToList();
        }

        public Product UpsertAbcMattressProduct(AbcMattressModel abcMattressModel)
        {
            var entries = _abcMattressEntryService.GetAbcMattressEntriesByModelId(
                abcMattressModel.Id
            );

            var hasExistingProduct = abcMattressModel.ProductId != null;
            Product product = hasExistingProduct ?
                _productService.GetProductById(abcMattressModel.ProductId.Value) :
                new Product();

            product.Name = GetProductName(abcMattressModel);
            // So I'd like to only use this once we totally migrate off of
            // old mattresses
            product.Sku = $"M{abcMattressModel.Name}";
            product.AllowCustomerReviews = false;
            product.Published = entries.Any();
            product.CreatedOnUtc = DateTime.UtcNow;
            product.VisibleIndividually = true;
            product.ProductType = ProductType.SimpleProduct;
            product.OrderMinimumQuantity = 1;
            product.OrderMaximumQuantity = 10000;
            product.IsShipEnabled = true;
            product.Price = CalculatePrice(abcMattressModel, entries);
            product.TaxCategoryId = _taxCategoryService.GetAllTaxCategories()
                                                       .Where(tc => tc.Name == "Everything")
                                                       .Select(tc => tc.Id)
                                                       .FirstOrDefault();

            MapProductToStore(product);

            if (hasExistingProduct)
            {
                _productService.UpdateProduct(product);
            }
            else
            {
                _productService.InsertProduct(product);
            }

            _urlRecordService.SaveSlug(product, _urlRecordService.ValidateSeName(
                product,
                string.Empty,
                product.Name,
                false),
                0
            );

            if (!hasExistingProduct)
            {
                abcMattressModel.ProductId = product.Id;
                _abcMattressService.UpdateAbcMattressModel(abcMattressModel);
            }
            if (!string.IsNullOrWhiteSpace(abcMattressModel.Sku))
            {
                _genericAttributeService.SaveAttribute<string>(
                    product,
                    "MattressSku",
                    abcMattressModel.Sku
                );
            }

            // add information relating to Synchrony payments
            SyncSynchronyPaymentsData(product, abcMattressModel);

            return product;
        }

        private void SyncSynchronyPaymentsData(Product product, AbcMattressModel model)
        {
            var entries = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id);
            var packages = _abcMattressPackageService.GetAbcMattressPackagesByEntryIds(entries.Select(e => e.Id));

            var itemNos = entries.Select(e => e.ItemNo).Union(packages.Select(p => p.ItemNo));

            int? months = null;
            bool? isMinimumPayment = null;
            DateTime? startDate = null;
            DateTime? endDate = null;

            foreach (var itemNo in itemNos)
            {
                var productAbcFinance = _productAbcFinanceService.GetProductAbcFinanceByAbcItemNumber(itemNo);

                if (productAbcFinance == null) { continue; }

                months = productAbcFinance.Months;
                isMinimumPayment = productAbcFinance.IsDeferredPricing;
                startDate = productAbcFinance.StartDate.Value;
                endDate = productAbcFinance.EndDate.Value;
            }

            _genericAttributeService.SaveAttribute<int?>(
                product,
                "SynchronyPaymentMonths",
                months
            );
            _genericAttributeService.SaveAttribute<bool?>(
                product,
                "SynchronyPaymentIsMinimum",
                isMinimumPayment
            );
            _genericAttributeService.SaveAttribute<DateTime?>(
                product,
                "SynchronyPaymentOfferValidFrom",
                startDate
            );
            _genericAttributeService.SaveAttribute<DateTime?>(
                product,
                "SynchronyPaymentOfferValidTo",
                endDate
            );
        }

        private void MapProductToStore(Product product)
        {
            // hardcoded to ABC Warehouse currently
            var abcWarehouseStore = _storeService.GetAllStores()
                                                   .Where(s => s.Name == "ABC Warehouse")
                                                   .FirstOrDefault();
            if (abcWarehouseStore == null)
            {
                throw new Exception("Unable to find ABC Warehouse store.");
            }

            _productService.UpdateProductStoreMappings(
                product,
                new int[] { abcWarehouseStore.Id }
            );
        }

        private decimal CalculatePrice(AbcMattressModel model, IList<AbcMattressEntry> entries)
        {
            if (!entries.Any()) return 0;

            var entry = entries.Where(e => e.Size.ToLower() == "queen")
                                       .FirstOrDefault();
            if (entry == null)
            {
                _logger.Warning(
                    $"Mattress model {model.Name} has no queen, using mid-priced item");

                entry = entries.OrderBy(e => e.Price)
                               .Skip(entries.Count / 2)
                               .First();
            }

            return entry.Price;
        }

        public void SetProductAttributes(AbcMattressModel model, Product product)
        {
            var nonBaseProductAttributes = _productAttributeService.GetAllProductAttributes()
                                                            .Where(pa => pa.Name == "Home Delivery" ||
                                                                         pa.Name == AbcMattressesConsts.MattressSizeName ||
                                                                         pa.Name == AbcMattressesConsts.FreeGiftName);

            foreach (var pa in nonBaseProductAttributes)
            {
                switch (pa.Name)
                {
                    case "Home Delivery":
                        MergeHomeDelivery(pa, product);
                        break;
                    case AbcMattressesConsts.MattressSizeName:
                        MergeSizes(model, pa, product);
                        break;
                    case AbcMattressesConsts.FreeGiftName:
                        MergeGifts(model, pa, product);
                        break;
                }
            }

            SetBases(model, product);
            SetMattressProtectors(model, product);
            SetFrames(model, product);
        }

        private void SetFrames(AbcMattressModel model, Product product)
        {
            var frameAttributes = _productAttributeService.GetAllProductAttributes()
                                                                        .Where(pa => AbcMattressesConsts.IsFrame(pa.Name));
            foreach (var pa in frameAttributes)
            {
                MergeFrames(model, pa, product);
            }
        }

        private void SetMattressProtectors(AbcMattressModel model, Product product)
        {
            var mattressProtectorAttributes = _productAttributeService.GetAllProductAttributes()
                                                                        .Where(pa => AbcMattressesConsts.IsMattressProtector(pa.Name));
            foreach (var pa in mattressProtectorAttributes)
            {
                MergeMattressProtectors(model, pa, product);
            }
        }

        private void SetBases(AbcMattressModel model, Product product)
        {
            var baseProductAttributes = _productAttributeService.GetAllProductAttributes()
                                                                        .Where(pa => AbcMattressesConsts.IsBase(pa.Name));
            foreach (var pa in baseProductAttributes)
            {
                MergeBases(model, pa, product);
            }
        }

        private void MergeFrames(AbcMattressModel model, ProductAttribute pa, Product product)
        {
            var pam = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                                                          .Where(pam => pam.ProductAttributeId == pa.Id)
                                                          .FirstOrDefault();
            var abcMattressEntry = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id)
                                                           .Where(ame => pa.Name == $"Frame ({ame.Size})")
                                                           .FirstOrDefault();
            if (abcMattressEntry == null) { return; }

            var frames = _abcMattressFrameService.GetAbcMattressFramesBySize(abcMattressEntry.Size);

            if (pam == null && frames.Any())
            {
                var sizeAttrs = GetSizeAttributes(product, abcMattressEntry);

                pam = new ProductAttributeMapping()
                {
                    ProductId = product.Id,
                    ProductAttributeId = pa.Id,
                    IsRequired = false,
                    AttributeControlType = AttributeControlType.DropdownList,
                    DisplayOrder = 30,
                    TextPrompt = "Frame",
                    ConditionAttributeXml = $"<Attributes><ProductAttribute ID=\"{sizeAttrs.pam.Id}\"><ProductAttributeValue><Value>{sizeAttrs.pav.Id}</Value></ProductAttributeValue></ProductAttribute></Attributes>"
                };
                _productAttributeService.InsertProductAttributeMapping(pam);
            }
            else if (pam != null && !frames.Any())
            {
                _productAttributeService.DeleteProductAttributeMapping(pam);
            }
            else if (pam != null)
            {
                UpdatePam(product, pam, abcMattressEntry);
            }

            if (!frames.Any()) { return; }

            var existingFrames = _productAttributeService.GetProductAttributeValues(pam.Id)
                                                        .Where(pav =>
                                                            pav.ProductAttributeMappingId == pam.Id
                                                        );
            var newFrames = frames.Select(np => np.ToProductAttributeValue(
                pam.Id
            )).OrderBy(f => f.PriceAdjustment).ToList();

            ApplyDisplayOrder(newFrames);

            var toBeDeleted = existingFrames
                .Where(e => !newFrames.Any(n => n.Name == e.Name && n.DisplayOrder == e.DisplayOrder &&
                                                       n.PriceAdjustment == e.PriceAdjustment));
            var toBeInserted = newFrames
                .Where(n => !existingFrames.Any(e => n.Name == e.Name && n.DisplayOrder == e.DisplayOrder &&
                                                       n.PriceAdjustment == e.PriceAdjustment));

            toBeInserted.ToList().ForEach(n => _productAttributeService.InsertProductAttributeValue(n));
            toBeDeleted.ToList().ForEach(e => _productAttributeService.DeleteProductAttributeValue(e));
        }

        private void UpdatePam(Product product, ProductAttributeMapping pam, AbcMattressEntry abcMattressEntry)
        {
            var sizeAttrs = GetSizeAttributes(product, abcMattressEntry);

            pam.ConditionAttributeXml = $"<Attributes><ProductAttribute ID=\"{sizeAttrs.pam.Id}\"><ProductAttributeValue><Value>{sizeAttrs.pav.Id}</Value></ProductAttributeValue></ProductAttribute></Attributes>";

            _productAttributeService.UpdateProductAttributeMapping(pam);
        }

        private (ProductAttributeMapping pam, ProductAttributeValue pav) GetSizeAttributes(
            Product product,
            AbcMattressEntry abcMattressEntry
        )
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

            return (sizePam, sizePav);
        }

        private void MergeMattressProtectors(AbcMattressModel model, ProductAttribute pa, Product product)
        {
            var displayOrder = 40;
            var pam = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                                                          .Where(pam => pam.ProductAttributeId == pa.Id)
                                                          .FirstOrDefault();
            var abcMattressEntry = _abcMattressEntryService.GetAbcMattressEntriesByModelId(model.Id)
                                                           .Where(ame => pa.Name == $"Mattress Protector ({ame.Size})")
                                                           .FirstOrDefault();
            if (abcMattressEntry == null) { return; }

            var protectors = _abcMattressProtectorService.GetAbcMattressProtectorsBySize(abcMattressEntry.Size);

            if (pam == null && protectors.Any())
            {
                var sizeAttrs = GetSizeAttributes(product, abcMattressEntry);
                pam = new ProductAttributeMapping()
                {
                    ProductId = product.Id,
                    ProductAttributeId = pa.Id,
                    IsRequired = false,
                    AttributeControlType = AttributeControlType.DropdownList,
                    DisplayOrder = displayOrder,
                    TextPrompt = "Mattress Protector",
                    ConditionAttributeXml = $"<Attributes><ProductAttribute ID=\"{sizeAttrs.pam.Id}\"><ProductAttributeValue><Value>{sizeAttrs.pav.Id}</Value></ProductAttributeValue></ProductAttribute></Attributes>"
                };
                _productAttributeService.InsertProductAttributeMapping(pam);
            }
            else if (pam != null && !protectors.Any())
            {
                _productAttributeService.DeleteProductAttributeMapping(pam);
            }
            else if (pam != null)
            {
                UpdatePam(product, pam, abcMattressEntry);
            }

            if (!protectors.Any()) { return; }

            var existingMattressProtectors = _productAttributeService.GetProductAttributeValues(pam.Id)
                                                        .Where(pav =>
                                                            pav.ProductAttributeMappingId == pam.Id
                                                        );
            var newMattressProtectors = protectors.Select(np => np.ToProductAttributeValue(
                pam.Id
            )).OrderBy(mp => mp.PriceAdjustment).ToList();

            ApplyDisplayOrder(newMattressProtectors);

            var toBeDeleted = existingMattressProtectors
                .Where(e => !newMattressProtectors.Any(n => n.Name == e.Name &&
                                                       n.DisplayOrder == e.DisplayOrder &&
                                                       n.PriceAdjustment == e.PriceAdjustment));
            var toBeInserted = newMattressProtectors
                .Where(n => !existingMattressProtectors.Any(e => n.Name == e.Name &&
                                                            n.DisplayOrder == e.DisplayOrder &&
                                                            n.PriceAdjustment == e.PriceAdjustment));

            toBeInserted.ToList().ForEach(n => _productAttributeService.InsertProductAttributeValue(n));
            toBeDeleted.ToList().ForEach(e => _productAttributeService.DeleteProductAttributeValue(e));
        }

        private void MergeHomeDelivery(ProductAttribute pa, Product product)
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
                    IsRequired = false,
                    AttributeControlType = AttributeControlType.MultilineTextbox,
                    DisplayOrder = 0
                };
                _productAttributeService.InsertProductAttributeMapping(pam);
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

            // brand
            if (model.BrandCategoryId.HasValue)
            {
                var brandCategory = _categoryService.GetCategoryById(model.BrandCategoryId.Value);
                if (brandCategory != null)
                {
                    newProductCategories.Add(new ProductCategory()
                    {
                        ProductId = product.Id,
                        CategoryId = brandCategory.Id
                    });
                }
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
            var displayOrder = 50;
            var pam = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                                                          .Where(pam => pam.ProductAttributeId == pa.Id && pam.DisplayOrder == displayOrder)
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
                    DisplayOrder = displayOrder
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
                .Where(e => !newGifts.Any(n => n.Name == e.Name && n.DisplayOrder == e.DisplayOrder));
            var toBeInserted = newGifts
                .Where(n => !existingGifts.Any(e => n.Name == e.Name && n.DisplayOrder == e.DisplayOrder));

            toBeInserted.ToList().ForEach(n => _productAttributeService.InsertProductAttributeValue(n));
            toBeDeleted.ToList().ForEach(e => _productAttributeService.DeleteProductAttributeValue(e));
        }

        private void MergeBases(AbcMattressModel model, ProductAttribute pa, Product product)
        {
            var attributeName = "Box Spring or Adjustable Base";
            var pam = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                                                          .Where(pam => pam.ProductAttributeId == pa.Id && pam.TextPrompt == attributeName)
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
                    DisplayOrder = 10,
                    TextPrompt = attributeName,
                    ConditionAttributeXml = $"<Attributes><ProductAttribute ID=\"{sizePam.Id}\"><ProductAttributeValue><Value>{sizePav.Id}</Value></ProductAttributeValue></ProductAttribute></Attributes>"
                };
                _productAttributeService.InsertProductAttributeMapping(pam);
            }
            else if (pam != null && !bases.Any())
            {
                _productAttributeService.DeleteProductAttributeMapping(pam);
            }
            else if (pam != null)
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

                pam.ConditionAttributeXml = $"<Attributes><ProductAttribute ID=\"{sizePam.Id}\"><ProductAttributeValue><Value>{sizePav.Id}</Value></ProductAttributeValue></ProductAttribute></Attributes>";

                _productAttributeService.UpdateProductAttributeMapping(pam);
            }

            if (!bases.Any()) { return; }

            var existingBases = _productAttributeService.GetProductAttributeValues(pam.Id)
                                                        .Where(pav =>
                                                            pav.ProductAttributeMappingId == pam.Id
                                                        );
            var newBases = bases.Select(nb => nb.ToProductAttributeValue(
                pam.Id,
                _abcMattressPackageService.GetAbcMattressPackageByEntryIdAndBaseId(abcMattressEntry.Id, nb.Id).Price,
                abcMattressEntry.Price
            )).OrderBy(nb => nb.PriceAdjustment).ToList();

            ApplyDisplayOrder(newBases);

            var toBeDeleted = existingBases
                .Where(e => !newBases.Any(n => n.Name == e.Name && n.DisplayOrder == e.DisplayOrder));
            var toBeInserted = newBases
                .Where(n => !existingBases.Any(e => n.Name == e.Name && n.DisplayOrder == e.DisplayOrder));

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
            var newSizes = entries.Select(e => e.ToProductAttributeValue(pam.Id, product.Price)).ToList();

            var toBeDeleted = existingSizes
                .Where(e => !newSizes.Any(n => n.Name == e.Name &&
                                               n.PriceAdjustment == e.PriceAdjustment &&
                                               n.DisplayOrder == e.DisplayOrder));
            var toBeInserted = newSizes
                .Where(n => !existingSizes.Any(e => n.Name == e.Name &&
                                                    n.PriceAdjustment == e.PriceAdjustment &&
                                                    n.DisplayOrder == e.DisplayOrder));

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
            switch (loweredSize)
            {
                case "twinxl":
                    return "twin extra long";
                case "queen-flexhead":
                    return "queen";
                case "king-flexhead":
                    return "king";
                case "california king-flexhead":
                    return "california king";
                default:
                    return loweredSize;
            }
        }

        private static void ApplyDisplayOrder(List<ProductAttributeValue> values)
        {
            var displayOrderCounter = 0;
            foreach (var value in values)
            {
                value.DisplayOrder = displayOrderCounter;
                displayOrderCounter++;
            }
        }
    }
}
