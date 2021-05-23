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
            IIsamProductService isamProductService
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
    }
}