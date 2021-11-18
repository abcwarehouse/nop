using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using System.Linq;
using System.Collections.Generic;
using System;
using Nop.Data;
using Nop.Plugin.Misc.AbcSync.Services.Staging;
using Nop.Plugin.Misc.AbcCore.Services;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcSync.Data;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportIsamSpecs : BaseAbcWarehouseService, IImportIsamSpecs
    {
        private readonly IProductService _productService;
        private readonly ILogger _logger;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly IImportUtilities _importUtilities;
        private readonly IRepository<SpecificationAttribute> _specificationAttributeRepository;
        private readonly IRepository<SpecificationAttributeOption> _specificationAttributeOptionRepository;
        private readonly IRepository<ProductSpecificationAttribute> _productSpecificationAttributeRepository;
        private readonly ImportSettings _importSettings;
        private readonly INopDataProvider _nopDbContext;
        private readonly IProductDataProductService _productDataProductService;
        private readonly IIsamProductService _isamProductService;
        private readonly StagingDb _stagingDb;

        private readonly Dictionary<string, int> _attrDict = new Dictionary<string, int>();
        private readonly Dictionary<Tuple<int, string>, int> _attrOptionDict = new Dictionary<Tuple<int, string>, int>();

        public ImportIsamSpecs(
            IProductService productService,
            ILogger logger,
            ISpecificationAttributeService specificationAttributeService,
            IImportUtilities importUtilities,
            IRepository<SpecificationAttribute> specificationAttributeRepository,
            IRepository<SpecificationAttributeOption> specificationAttributeOptionRepository,
            IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
            ImportSettings importSettings,
            INopDataProvider nopDbContext,
            IProductDataProductService productDataProductService,
            IIsamProductService isamProductService,
            StagingDb stagingDb
        )
        {
            _productService = productService;
            _logger = logger;
            _specificationAttributeService = specificationAttributeService;
            _importUtilities = importUtilities;
            _specificationAttributeRepository = specificationAttributeRepository;
            _specificationAttributeOptionRepository = specificationAttributeOptionRepository;
            _productSpecificationAttributeRepository = productSpecificationAttributeRepository;
            _importSettings = importSettings;
            _nopDbContext = nopDbContext;
            _productDataProductService = productDataProductService;
            _isamProductService = isamProductService;
            _stagingDb = stagingDb;
        }

        public async Task ImportColorAsync()
        {
            var colorSpecificationName = "Color";

            //creating color as a specification. if one exists we delete and remake it to clear old options and mappings
            var colorSpecAttr = _specificationAttributeRepository.Table.Where(sa => sa.Name.ToUpper() == colorSpecificationName.ToUpper()).FirstOrDefault();
            if (colorSpecAttr != null)
            {
                await _specificationAttributeService.DeleteSpecificationAttributeAsync(colorSpecAttr);
            }
            colorSpecAttr = new SpecificationAttribute { Name = colorSpecificationName };
            await _specificationAttributeService.InsertSpecificationAttributeAsync(colorSpecAttr);


            var attrOptionManager = new EntityManager<SpecificationAttributeOption>();
            //a join to get a union of color options from both sources
            var sotColorValues = _productDataProductService.GetProductDataProducts().Where(sp=>sp.StandardColor != null).Select(sp => sp.StandardColor.Trim().ToUpperInvariant()).Distinct().ToList();
            var isamColorValues = _isamProductService.GetIsamProducts().Where(ip => ip.Color != null).Select(ip => ip.Color.Trim().ToUpperInvariant()).Distinct().ToList();
            var colorValues = sotColorValues.Union(isamColorValues);
            var colorSpecAttrId = GetSpecAttrId(colorSpecificationName);
            //creating all color options that will be mapped to products. all old options have already been cleared
            foreach (var value in colorValues)
            {
                var newAttrOption = new SpecificationAttributeOption { Name = value, SpecificationAttributeId = colorSpecAttrId };
                await attrOptionManager.InsertAsync(newAttrOption);
            }
            await attrOptionManager.FlushAsync();

            var prodAttrOptionManager = new EntityManager<ProductSpecificationAttribute>();
            //getting sku and sot/isam color options for all products with color. strange syntax is a left join
            var sotSkusColors = _productDataProductService.GetProductDataProducts().Where(sp => sp.StandardColor != null).Select(sp => new { Sku = sp.SKU, Color = sp.StandardColor });
            var isamSkusColors = _isamProductService.GetIsamProducts().Where(ip => ip.Color != null).Select(ip => new { ip.Sku, ip.Color });
            var skusColors = sotSkusColors.Union(isamSkusColors).Where(s => !string.IsNullOrWhiteSpace(s.Color));
            //mapping created options to their product
            foreach (var skuColors in skusColors)
            {
                var sku = skuColors.Sku;
                var colorOptionName = skuColors.Color;

                var product = _importUtilities.GetExistingProductBySku(sku);
                if (product == null)
                    continue;

                //add mapping for color option
                var specAttrOptionId = await GetSpecAttrOptionIdAsync(colorSpecificationName, colorOptionName);

                var newProdAttrOption = new ProductSpecificationAttribute
                {
                    ProductId = product.Id,
                    ShowOnProductPage = true,
                    AllowFiltering = true,
                    DisplayOrder = 0,
                    SpecificationAttributeOptionId = specAttrOptionId
                };
                await prodAttrOptionManager.InsertAsync(newProdAttrOption);
                
            }
            await prodAttrOptionManager.FlushAsync();
        }

        /// <summary>
        /// Import site on time filters as filterable specification attributes
        /// </summary>
        public async Task ImportSiteOnTimeSpecsAsync()
        {
            await _nopDbContext.ExecuteNonQueryAsync("EXECUTE ImportSiteOnTimeFilters;");
        }


        /// <summary>
        /// gets the id of the specification attribute associated with the given name. the specification attribute must exist. ignores name case
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private int GetSpecAttrId(string name)
        {
            int id;
            if (!_attrDict.TryGetValue(name, out id))
            {
                id = _specificationAttributeRepository.Table.ToList().Where(sa => sa.Name.ToUpper() == name.ToUpper()).First().Id;
                _attrDict[name] = id;
            }
            return id;
        }

        private async Task<int> GetSpecAttrOptionIdAsync( string attributeName,string attributeOptionName)
        {
            var specAttrId = GetSpecAttrId(attributeName);
            Tuple<int, string> idOptionName = new Tuple<int, string>(specAttrId, attributeOptionName);
            int id;
            if (!_attrOptionDict.TryGetValue(idOptionName, out id))
            {
                try
                {
                    id = _specificationAttributeOptionRepository.Table.Where(sao => (sao.Name.ToUpper() == attributeOptionName.ToUpper() && sao.SpecificationAttributeId == specAttrId)).First().Id;
                    _attrOptionDict[idOptionName] = id;
                }
                catch
                {
                    await _logger.ErrorAsync($"Could not find id for attributeName {attributeName} and attributeOptionName {attributeOptionName} for specAttrId {specAttrId}");
                }
            }
            return id;
        }

    }
}