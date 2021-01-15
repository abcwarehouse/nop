using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Stores;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Tasks
{
    public class UpdateMattressesTask : IScheduleTask
    {
        private readonly ILogger _logger;

        private readonly IAbcMattressModelService _abcMattressModelService;
        private readonly IAbcMattressEntryService _abcMattressEntryService;
        private readonly IAbcMattressPackageService _abcMattressPackageService;
        private readonly IAbcMattressProductService _abcMattressProductService;
        private readonly IProductService _productService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;
        private readonly IStoreService _storeService;
        private readonly IStoreMappingService _storeMappingService;

        public UpdateMattressesTask(
            ILogger logger,
            IAbcMattressModelService abcMattressModelService,
            IAbcMattressEntryService abcMattressEntryService,
            IAbcMattressPackageService abcMattressPackageService,
            IAbcMattressProductService abcMattressProductService,
            IProductService productService,
            IProductAbcDescriptionService productAbcDescriptionService,
            IStoreService storeService,
            IStoreMappingService storeMappingService
        )
        {
            _logger = logger;
            _abcMattressModelService = abcMattressModelService;
            _abcMattressEntryService = abcMattressEntryService;
            _abcMattressPackageService = abcMattressPackageService;
            _abcMattressProductService = abcMattressProductService;
            _productService = productService;
            _productAbcDescriptionService = productAbcDescriptionService;
            _storeService = storeService;
            _storeMappingService = storeMappingService;
        }

        public void Execute()
        {
            var models = _abcMattressModelService.GetAllAbcMattressModels();

            foreach (var model in models)
            {
                var product = _abcMattressProductService.UpsertAbcMattressProduct(model);
                _abcMattressProductService.SetManufacturer(model, product);
                _abcMattressProductService.SetCategories(model, product);
                _abcMattressProductService.SetProductAttributes(model, product);
            }

            UnmapOldMattressProducts();
        }

        private void UnmapOldMattressProducts()
        {
            var entryItemNos = _abcMattressEntryService.GetAllAbcMattressEntries().Select(e => e.ItemNo);
            var packageItemNos = _abcMattressPackageService.GetAllAbcMattressPackages().Select(p => p.ItemNo);
            var itemNos = entryItemNos.Union(packageItemNos).ToList();

            foreach (var itemNo in itemNos)
            {
                var pad =
                    _productAbcDescriptionService.GetProductAbcDescriptionByAbcItemNumber(
                        itemNo
                    );
                if (pad == null) { continue; }

                var product = _productService.GetProductById(pad.Product_Id);
                var mainStore = _storeService.GetAllStores()
                                             .Where(s => !s.Name.ToLower().Contains("clearance"))
                                             .FirstOrDefault();
                var storeMapping = _storeMappingService.GetStoreMappings(product)
                                                       .Where(sm => sm.StoreId == mainStore.Id)
                                                       .FirstOrDefault();

                if (storeMapping != null)
                {
                    _storeMappingService.DeleteStoreMapping(storeMapping);
                }
            }
        }
    }
}
