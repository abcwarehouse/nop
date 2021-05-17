using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcSync.Services.Staging;
using SevenSpikes.Nop.Conditions.Domain;
using SevenSpikes.Nop.Conditions.Services;
using SevenSpikes.Nop.Mappings.Domain;
using SevenSpikes.Nop.Mappings.Services;
using SevenSpikes.Nop.Plugins.NopQuickTabs.Domain;
using SevenSpikes.Nop.Plugins.NopQuickTabs.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSync
{
    public class DocumentImportService : BaseAbcWarehouseService, IDocumentImportService
    {
        private readonly ImportSettings _importSettings;
        private readonly IImportUtilities _importUtilities;
        private readonly IRepository<Product> _productRepository;
        private readonly ITabService _tabService;
        private readonly IRepository<Tab> _tabRepository;
        private readonly IConditionService _conditionService;
        private readonly IRepository<Condition> _conditionRepository;
        private readonly IRepository<EntityCondition> _entityConditionRepository;
        private readonly IEntityMappingService _entityMappingService;
        private readonly IRepository<EntityMapping> _entityMappingRepository;
        private readonly IRepository<ProductEnergyGuide> _energyGuideRepository;
        private readonly IRepository<ProductDocuments> _prodDocRepository;
        private readonly INopDataProvider _nopDbContext;
        private readonly IProductDataProductService _productDataProductService;
        private readonly IProductDataProductDownloadService _productDataProductDownloadService;
        private readonly IIsamProductService _isamProductService;

        public DocumentImportService(ImportSettings importSettings,
            IImportUtilities importUtilities,
            IRepository<Product> productRepository,
            ITabService tabService,
            IRepository<Tab> tabRepository,
            IEntityMappingService entityMappingService,
            IRepository<EntityMapping> entityMappingRepository,
            IConditionService conditionService,
            IRepository<Condition> conditionRepository,
            IRepository<EntityCondition> entityConditionRepository,
            IRepository<ProductEnergyGuide> energyGuideRepository,
            IRepository<ProductDocuments> prodDocRepository,
            INopDataProvider nopDbContext,
            IProductDataProductService productDataProductService,
            IProductDataProductDownloadService productDataProductDownloadService,
            IIsamProductService isamProductService
        )
        {
            _importSettings = importSettings;
            _importUtilities = importUtilities;
            _productRepository = productRepository;
            _tabService = tabService;
            _tabRepository = tabRepository;
            _conditionService = conditionService;
            _entityConditionRepository = entityConditionRepository;
            _conditionRepository = conditionRepository;
            _entityMappingService = entityMappingService;
            _entityMappingRepository = entityMappingRepository;
            _energyGuideRepository = energyGuideRepository;
            _prodDocRepository = prodDocRepository;
            _nopDbContext = nopDbContext;
            _productDataProductService = productDataProductService;
            _productDataProductDownloadService = productDataProductDownloadService;
            _isamProductService = isamProductService;
        }

        /// <summary>
        /// Import all extra documents associated with a product, will take site on time documents over isam documents
        /// </summary>
        public void ImportDocuments()
        {
            //this set will contain skus that already have a tab
            var idsWithDocuments = new HashSet<int>();
            var idsWithEguides = new HashSet<int>();
            var idsWithSpecs = new HashSet<int>();

            await _nopDbContext.ExecuteNonQueryAsync(
                $"DELETE FROM {_nopDbContext.GetTable<ProductDocuments>().TableName}");
            await _nopDbContext.ExecuteNonQueryAsync(
                $"DELETE FROM {_nopDbContext.GetTable<ProductEnergyGuide>().TableName}");

            var productDocumentsManager = new EntityManager<ProductDocuments>();
            var energyGuideManager = new EntityManager<ProductEnergyGuide>();

            //add all site on time documents first
            foreach (var sotProduct in _productDataProductService.GetProductDataProducts())
            {
                var product = _importUtilities.GetExistingProductBySku(sotProduct.SKU);
                if (product == null)
                    continue;

                //do not add a new tab if one exists
                if (idsWithDocuments.Contains(product.Id))
                {
                    continue;
                }
                var description = "";

                //populate tab. Skipping some downloads, such as extra pictures
                foreach (var download in _productDataProductDownloadService.GetProductDataProductDownloadsByProductDataProductId(sotProduct.id))
                {
                    switch (download.AST_Role_Txt)
                    {
                        case "OwnersManual":
                            description += GetTabLinkItem(download.AST_URL_Txt, "Owners Manual");
                            break;
                        case "InstallationGuide":
                            description += GetTabLinkItem(download.AST_URL_Txt, "Installation Guide");
                            break;
                        case "EnergyGuide":
                            description += GetTabLinkItem(download.AST_URL_Txt, "Energy Guide");
                            energyGuideManager.Insert(new ProductEnergyGuide { ProductId = product.Id, EnergyGuideUrl = download.AST_URL_Txt });
                            idsWithEguides.Add(product.Id);
                            break;
                        case "SpecPage":
                            description += GetTabLinkItem(download.AST_URL_Txt, "Specification Page");
                            idsWithSpecs.Add(product.Id);
                            break;
                        case "WarrantyInformation":
                            description += GetTabLinkItem(download.AST_URL_Txt, "Warranty Information");
                            break;
                        case "DimensionsGuide":
                            description += GetTabLinkItem(download.AST_URL_Txt, "Dimensions Guide");
                            break;
                        default:
                            break;
                    }
                }

                //if there were downloads to populate the tab, add a new tab and mappings
                if (!string.IsNullOrEmpty(description))
                {
                    productDocumentsManager.Insert(new ProductDocuments { ProductId = product.Id, Documents = description });
                    idsWithDocuments.Add(product.Id);
                }

            }

            energyGuideManager.Flush();
            productDocumentsManager.Flush();

            //add ISAM products next
            foreach (var isamProduct in _isamProductService.GetIsamProducts())
            {
                var product = _importUtilities.GetExistingProductBySku(isamProduct.Sku);
                if (product == null)
                    continue;

                if (idsWithEguides.Contains(product.Id) && idsWithSpecs.Contains(product.Id))
                {
                    continue;
                }


                var description = "";
                var eguidePath = _importSettings.GetEnergyGuidePdfPath() + $"/{isamProduct.ItemNumber.Trim()}_eguide.pdf";
                var specPath = _importSettings.GetSpecificationPdfPath() + $"/{isamProduct.ItemNumber.Trim()}_spec.pdf";
                if (File.Exists(eguidePath) && !idsWithEguides.Contains(product.Id))
                {
                    description += GetTabLinkItem($"/energy_guides/{isamProduct.ItemNumber.Trim()}_eguide.pdf", "Energy Guide");
                    _energyGuideRepository.Insert(new ProductEnergyGuide
                    {
                        ProductId = product.Id,
                        EnergyGuideUrl = $"/energy_guides/{isamProduct.ItemNumber.Trim()}_eguide.pdf"
                    });
                    idsWithEguides.Add(product.Id);
                }
                if (File.Exists(specPath) && !idsWithSpecs.Contains(product.Id))
                {
                    description += GetTabLinkItem($"/product_specs/{isamProduct.ItemNumber.Trim()}_spec.pdf", "Specification Page");
                    idsWithSpecs.Add(product.Id);
                }

                if (!string.IsNullOrEmpty(description))
                {
                    if (idsWithDocuments.Contains(product.Id))
                    {
                        var productDocuments = _prodDocRepository.Table.Where(pd => pd.ProductId == product.Id).FirstOrDefault();
                        productDocuments.Documents += description;
                        productDocumentsManager.Update(productDocuments);
                    }
                    else
                    {
                        productDocumentsManager.Insert(new ProductDocuments { ProductId = product.Id, Documents = description });
                        idsWithDocuments.Add(product.Id);
                    }
                }
            }

            energyGuideManager.Flush();
            productDocumentsManager.Flush();
        }

        private string GetPopUpScript(string resourcePath, string windowName)
        {
            return $"javascript:win = window.open('{resourcePath}', '{windowName}', 'height=500,width=750,top=25,left=25,resizable=yes'); win.focus()";
        }

        private string GetTabLinkItem(string url, string name)
        {
            return $"<div><a href=\"{GetPopUpScript(url, name)}\">{name}</a></div><br>";
        }
    }
}