using Nop.Core.Domain.Catalog;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Services.Catalog;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcSync.Services.Staging;
using Nop.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportColorsTask : IScheduleTask
    {
        private readonly IImportUtilities _importUtilities;
        private readonly IIsamProductService _isamProductService;
        private readonly ILogger _logger;
        private readonly IProductDataProductService _productDataProductService;
        private readonly IRepository<SpecificationAttribute> _specificationAttributeRepository;
        private readonly IRepository<SpecificationAttributeOption> _specificationAttributeOptionRepository;
        private readonly ISpecificationAttributeService _specificationAttributeService;
        private readonly ImportSettings _importSettings;

        private readonly Dictionary<string, int> _attrDict = new Dictionary<string, int>();
        private readonly Dictionary<Tuple<int, string>, int> _attrOptionDict = new Dictionary<Tuple<int, string>, int>();

        public ImportColorsTask(
            IImportUtilities importUtilities,
            IIsamProductService isamProductService,
            ILogger logger,
            IProductDataProductService productDataProductService,
            IRepository<SpecificationAttribute> specificationAttributeRepository,
            IRepository<SpecificationAttributeOption> specificationAttributeOptionRepository,
            ISpecificationAttributeService specificationAttributeService,
            ImportSettings importSettings
        )
        {
            _importUtilities = importUtilities;
            _isamProductService = isamProductService;
            _logger = logger;
            _productDataProductService = productDataProductService;
            _specificationAttributeRepository = specificationAttributeRepository;
            _specificationAttributeOptionRepository = specificationAttributeOptionRepository;
            _specificationAttributeService = specificationAttributeService;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportColorsTask)
            {
                this.Skipped();
                return;
            }

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

        private async System.Threading.Tasks.Task<int> GetSpecAttrOptionIdAsync( string attributeName,string attributeOptionName)
        {
            var specAttrId = GetSpecAttrId(attributeName);
            Tuple<int, string> idOptionName = new Tuple<int, string>(specAttrId, attributeOptionName);
            int id;
            if (!_attrOptionDict.TryGetValue(idOptionName, out id))
            {
                try
                {
                    id = _specificationAttributeOptionRepository.Table.Where(sao => (sao.Name.ToUpper().Trim() == attributeOptionName.ToUpper().Trim() && sao.SpecificationAttributeId == specAttrId)).First().Id;
                    _attrOptionDict[idOptionName] = id;
                }
                catch
                {
                    await _logger.ErrorAsync($"Could not find id for attributeName '{attributeName}' and attributeOptionName '{attributeOptionName}' for specAttrId {specAttrId}");
                }
            }
            return id;
        }
    }
}