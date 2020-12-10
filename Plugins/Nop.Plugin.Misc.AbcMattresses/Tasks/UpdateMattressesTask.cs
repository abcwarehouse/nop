using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcMattresses.Tasks
{
    public class UpdateMattressesTask : IScheduleTask
    {
        private readonly ILogger _logger;

        private readonly IAbcMattressService _abcMattressService;
        private readonly IAbcMattressEntryService _abcMattressEntryService;
        private readonly IAbcMattressPackageService _abcMattressPackageService;
        private readonly IAbcMattressProductService _abcMattressProductService;
        private readonly IProductService _productService;
        private readonly IProductAbcDescriptionService _productAbcDescriptionService;

        public UpdateMattressesTask(
            ILogger logger,
            IAbcMattressService abcMattressService,
            IAbcMattressEntryService abcMattressEntryService,
            IAbcMattressPackageService abcMattressPackageService,
            IAbcMattressProductService abcMattressProductService,
            IProductService productService,
            IProductAbcDescriptionService productAbcDescriptionService
        )
        {
            _logger = logger;
            _abcMattressService = abcMattressService;
            _abcMattressEntryService = abcMattressEntryService;
            _abcMattressPackageService = abcMattressPackageService;
            _abcMattressProductService = abcMattressProductService;
            _productService = productService;
            _productAbcDescriptionService = productAbcDescriptionService;
        }

        public void Execute()
        {
            var models = _abcMattressService.GetAllAbcMattressModels();

            foreach (var model in models)
            {
                _abcMattressProductService.UpsertAbcMattressProduct(model);
            }

            ClearOldMattressProducts();
        }

        private void ClearOldMattressProducts()
        {
            var entries = _abcMattressEntryService.GetAllAbcMattressEntries();
            foreach (var entry in entries)
            {
                var pad =
                    _productAbcDescriptionService.GetProductAbcDescriptionByAbcItemNumber(
                        entry.ItemNo.ToString()
                    );
                if (pad == null) { continue ;}

                var product = _productService.GetProductById(pad.Product_Id);
                product.Published = false;
                _productService.UpdateProduct(product);
            }

            var packages = _abcMattressPackageService.GetAllAbcMattressPackages();
            foreach (var package in packages)
            {
                var pad =
                    _productAbcDescriptionService.GetProductAbcDescriptionByAbcItemNumber(
                        package.ItemNo.ToString()
                    );
                if (pad == null) { continue ;}

                var product = _productService.GetProductById(pad.Product_Id);
                product.Published = false;
                _productService.UpdateProduct(product);
            }
        }
    }
}
