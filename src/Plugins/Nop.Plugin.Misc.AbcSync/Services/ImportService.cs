using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.AbcSync.Data;
using Nop.Services.Seo;
using OfficeOpenXml;
using System.IO;
using SevenSpikes.Nop.Plugins.StoreLocator.Services;
using Nop.Services.Stores;
using Nop.Services.Logging;
using Nop.Plugin.Misc.AbcSync.Domain;
using Nop.Core.Domain.Stores;
using Nop.Services.Security;
using Nop.Core.Domain.Tax;
using System.Data;
using Nop.Services.Messages;
using Nop.Core.Domain.Messages;
using Nop.Services.Common;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Services;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportService : BaseAbcWarehouseService, IImportService
    {
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly IRepository<Product> _productRepository;
        private readonly IProductService _productService;
        private readonly IImportUtilities _importUtilities;
        private readonly ICategoryService _categoryService;
        private readonly IRepository<ProductCategory> _productCategoryRepository;
        private readonly IRepository<ProductCartPrice> _productCartPriceRepository;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IRepository<Manufacturer> _manufacturerRepository;
        private readonly IRepository<ProductManufacturer> _productManufacturerRepository;
        private readonly IProductAttributeService _productAttributeService;
        private readonly INopDataProvider _nopDbContext;
        private readonly IShopService _shopService;
        private readonly IRepository<ShopAbc> _shopAbcRepository;
        private readonly IRepository<ProductAttributeMapping> _productAttributeMappingRepository;
        private readonly IStoreService _storeService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IRepository<StoreMapping> _storeMappingRepository;
        private readonly IAclService _aclService;
        private readonly IRepository<TaxCategory> _taxCategoryRepository;
        private readonly IEmailSender _emailSender;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IEmailAccountService _emailAccountService;
        private readonly IGenericAttributeService _genericAttributeService;

        private Dictionary<string, Manufacturer> _nameToManufacturer = new Dictionary<string, Manufacturer>();

        public ImportService(
            ILogger logger,
            ImportSettings importSettings,
            IRepository<Product> productRepository,
            IProductService productService,
            IImportUtilities importUtilities,
            ICategoryService categoryService,
            IRepository<ProductCategory> productCategoryRepository,
            IRepository<ProductCartPrice> productCartPriceRepository,
            IUrlRecordService urlRecordService,
            IManufacturerService manufacturerService,
            IRepository<Manufacturer> manufacturerRepository,
            IRepository<ProductManufacturer> productManufacturerRepository,
            INopDataProvider nopDbContext,
            IProductAttributeService productAttributeService,
            IShopService shopService,
            IRepository<ShopAbc> shopAbcRepository,
            IRepository<ProductAttributeMapping> productAttributeMappingRepository,
            IStoreService storeService,
            IStoreMappingService storeMappingService,
            IRepository<StoreMapping> storeMappingRepository,
            IAclService aclService,
            IRepository<TaxCategory> taxCategoryRepository,
            IEmailSender emailSender,
            IEmailAccountService emailAccountService,
            EmailAccountSettings emailAccountSettings,
            IGenericAttributeService genericAttributeService
            )
        {
            _logger = logger;
            _importSettings = importSettings;
            _productRepository = productRepository;
            _productService = productService;
            _importUtilities = importUtilities;
            _categoryService = categoryService;
            _productCategoryRepository = productCategoryRepository;
            _productCartPriceRepository = productCartPriceRepository;
            _urlRecordService = urlRecordService;
            _manufacturerService = manufacturerService;
            _manufacturerRepository = manufacturerRepository;
            _productManufacturerRepository = productManufacturerRepository;
            _nopDbContext = nopDbContext;
            _shopService = shopService;
            _shopAbcRepository = shopAbcRepository;
            _productAttributeService = productAttributeService;
            _productAttributeMappingRepository = productAttributeMappingRepository;
            _storeService = storeService;
            _storeMappingService = storeMappingService;
            _storeMappingRepository = storeMappingRepository;
            _aclService = aclService;
            _taxCategoryRepository = taxCategoryRepository;
            _emailSender = emailSender;
            _emailAccountSettings = emailAccountSettings;
            _emailAccountService = emailAccountService;
            _genericAttributeService = genericAttributeService;
        }

        /// <summary>
        /// import featured product from the excel file given in web config. skus that do not map to products or manufacturer names that do not map to manufacturers will be skipped
        /// </summary>
        public async Task ImportFeaturedProductsAsync()
        {
            string featuredProductsPath = _importSettings.GetFeaturedProductsFile();

            using (var xlPackage = new ExcelPackage(new FileInfo(featuredProductsPath)))
            {
                // get the first worksheet in the workbook
                var categoryWorksheet = xlPackage.Workbook.Worksheets[0];
                if (categoryWorksheet == null)
                    throw new NopException("No Category worksheet");

                var manufacturerWorksheet = xlPackage.Workbook.Worksheets[1];
                if (manufacturerWorksheet == null)
                    throw new NopException("No Manufacturer Worksheet");

                //add featured products for categories
                int iRow = 2;
                while (categoryWorksheet.Cells[iRow, 1].Value != null && !String.IsNullOrEmpty(categoryWorksheet.Cells[iRow, 1].Value.ToString()))
                {

                    var productSku = Convert.ToString(categoryWorksheet.Cells[iRow, 1].Value);
                    var product = _importUtilities.GetExistingProductBySku(productSku);
                    // TODO: log if fail
                    if (product == null)
                    {
                        iRow++;
                        continue;
                    }

                    var categoryId = 0;
                    try
                    {
                        categoryId = Convert.ToInt32(categoryWorksheet.Cells[iRow, 2].Value);
                    }
                    catch (Exception ex)
                    {
                        throw new NopException($"Unable to convert cell {iRow},2 to an integer in {categoryWorksheet.Name}", ex);
                    }

                    bool categoryExists = await _categoryService.GetCategoryByIdAsync(categoryId) != null;

                    // TODO: log if fail
                    if (!categoryExists)
                    {
                        iRow++;
                        continue;
                    }

                    var displayOrder = 0;
                    try
                    {
                        displayOrder = Convert.ToInt32(categoryWorksheet.Cells[iRow, 3].Value);
                    }
                    catch (Exception ex)
                    {
                        throw new NopException($"Unable to convert cell {iRow},3 to an integer in {categoryWorksheet.Name}", ex);
                    }

                    var productCategory = _productCategoryRepository.Table
                        .Where(pc => pc.ProductId == product.Id && pc.CategoryId == categoryId)
                        .Select(pc => pc).FirstOrDefault();
                    if (productCategory == null)
                    {
                        // generate new productCategory for this featured product
                        productCategory = new ProductCategory
                        {
                            CategoryId = categoryId,
                            ProductId = product.Id,
                            DisplayOrder = displayOrder,
                            IsFeaturedProduct = true
                        };
                        await _productCategoryRepository.InsertAsync(productCategory);
                    }
                    else
                    {
                        productCategory.DisplayOrder = displayOrder;
                        productCategory.IsFeaturedProduct = true;
                        await _productCategoryRepository.UpdateAsync(productCategory);
                    }

                    ++iRow;
                }

                //add featured products for manufacturers
                iRow = 2;
                while (manufacturerWorksheet.Cells[iRow, 1].Value != null && !String.IsNullOrEmpty(manufacturerWorksheet.Cells[iRow, 1].Value.ToString()))
                {
                    var productSku = Convert.ToString(manufacturerWorksheet.Cells[iRow, 1].Value);
                    var product = _importUtilities.GetExistingProductBySku(productSku);
                    if (product == null)
                    {
                        iRow++;
                        continue;
                    }

                    var manufacturerName = Convert.ToString(manufacturerWorksheet.Cells[iRow, 2].Value);

                    Manufacturer manufacturer = null;
                    if (!String.IsNullOrEmpty(manufacturerName))
                        manufacturer = (await _manufacturerService.GetAllManufacturersAsync(manufacturerName: manufacturerName.ToUpper(), showHidden: true)).FirstOrDefault();

                    var displayOrder = 0;
                    try
                    {
                        displayOrder = Convert.ToInt32(manufacturerWorksheet.Cells[iRow, 3].Value);
                    }
                    catch (Exception ex)
                    {
                        throw new NopException($"Unable to convert cell {iRow},3 to an integer in {manufacturerWorksheet.Name}", ex);
                    }

                    if (manufacturer != null)
                    {
                        var productManufacturer = _productManufacturerRepository.Table
                            .Where(pm => pm.ProductId == product.Id && pm.ManufacturerId == manufacturer.Id)
                            .Select(pm => pm).FirstOrDefault();
                        if (productManufacturer != null)
                        {
                            // update the table if it exists
                            productManufacturer.DisplayOrder = displayOrder;
                            productManufacturer.IsFeaturedProduct = true;
                            await _productManufacturerRepository.UpdateAsync(productManufacturer);
                        }
                    }

                    ++iRow;
                }
            }
        }
    }
}
