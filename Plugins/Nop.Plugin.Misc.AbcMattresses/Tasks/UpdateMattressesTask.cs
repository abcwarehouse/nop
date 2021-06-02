using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Catalog;
using System.Linq;
using Nop.Services.Stores;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Core.Domain.Catalog;
using System;

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
            // use this to allow for more robust run
            var wasSuccessful = true;

            foreach (var model in models)
            {
                try
                {
                    var product = _abcMattressProductService.UpsertAbcMattressProduct(model);
                    _abcMattressProductService.SetManufacturer(model, product);
                    _abcMattressProductService.SetCategories(model, product);
                    _abcMattressProductService.SetProductAttributes(model, product);
                    _abcMattressProductService.SetComfortRibbon(model, product);
                    _abcMattressProductService.SetSpecificationAttributes(model, product);
                }
                catch (Exception e)
                {
                    _logger.Error(
                        $"Error when syncing mattress model {model.Name}: {e.Message}",
                        e
                    );
                    wasSuccessful = false;
                }

            }

            ClearOldMattressProducts();

            if (!wasSuccessful)
            {
                throw new Exception("Errors occured during mattress sync.");
            }
        }

        private void ClearOldMattressProducts()
        {
            foreach (var itemNo in _abcMattressProductService.GetMattressItemNos())
            {
                ProcessItemNo(itemNo);
            }
        }

        private void ProcessItemNo(string itemNo)
        {
            var pad = _productAbcDescriptionService.GetProductAbcDescriptionByAbcItemNumber(
                itemNo
            );
            if (pad == null) { return; }

            var product = _productService.GetProductById(pad.Product_Id);

            UnmapFromStore(product, pad);

            if (!_storeMappingService.GetStoreMappings(product).Any())
            {
                _productService.DeleteProduct(product);
            }
        }

        // currently only doing main store
        private void UnmapFromStore(Product product, ProductAbcDescription pad)
        {
            var mainStore = _storeService.GetAllStores()
                                         .Where(s => !s.Name.ToLower().Contains("clearance"))
                                         .First();
            var mainStoreMapping = _storeMappingService.GetStoreMappings(product)
                                                       .Where(sm => sm.StoreId == mainStore.Id)
                                                       .FirstOrDefault();

            if (mainStoreMapping != null)
            {
                _storeMappingService.DeleteStoreMapping(mainStoreMapping);
            }
        }
    }
}
