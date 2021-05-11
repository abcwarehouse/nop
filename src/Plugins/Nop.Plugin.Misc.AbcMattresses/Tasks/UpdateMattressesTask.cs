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
using System.Threading.Tasks;

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

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            var models = _abcMattressModelService.GetAllAbcMattressModels();
            // use this to allow for more robust run
            var wasSuccessful = true;

            foreach (var model in models)
            {
                try
                {
                    var product = await _abcMattressProductService.UpsertAbcMattressProductAsync(model);
                    await _abcMattressProductService.SetManufacturerAsync(model, product);
                    await _abcMattressProductService.SetCategoriesAsync(model, product);
                    await _abcMattressProductService.SetProductAttributesAsync(model, product);
                }
                catch (Exception e)
                {
                    await _logger.ErrorAsync(
                        $"Error when syncing mattress model {model.Name}: {e.Message}",
                        e
                    );
                    wasSuccessful = false;
                }

            }

            await ClearOldMattressProductsAsync();

            if (!wasSuccessful)
            {
                throw new Exception("Errors occured during mattress sync.");
            }
        }

        private async System.Threading.Tasks.Task ClearOldMattressProductsAsync()
        {
            foreach (var itemNo in _abcMattressProductService.GetMattressItemNos())
            {
                await ProcessItemNoAsync(itemNo);
            }
        }

        private async System.Threading.Tasks.Task ProcessItemNoAsync(string itemNo)
        {
            var pad = _productAbcDescriptionService.GetProductAbcDescriptionByAbcItemNumber(
                itemNo
            );
            if (pad == null) { return; }

            var product = await _productService.GetProductByIdAsync(pad.Product_Id);

            await UnmapFromStoreAsync(product, pad);

            if (!(await _storeMappingService.GetStoreMappingsAsync(product)).Any())
            {
                await _productService.DeleteProductAsync(product);
            }
        }

        // currently only doing main store
        private async System.Threading.Tasks.Task UnmapFromStoreAsync(Product product, ProductAbcDescription pad)
        {
            var mainStore = (await _storeService.GetAllStoresAsync())
                                         .Where(s => !s.Name.ToLower().Contains("clearance"))
                                         .First();
            var mainStoreMapping = (await _storeMappingService.GetStoreMappingsAsync(product))
                                                       .Where(sm => sm.StoreId == mainStore.Id)
                                                       .FirstOrDefault();

            if (mainStoreMapping != null)
            {
                await _storeMappingService.DeleteStoreMappingAsync(mainStoreMapping);
            }
        }
    }
}
