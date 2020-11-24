using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.AbcSync.Data;
using Nop.Services.Seo;
using Nop.Services.Tasks;
using System.Data;
using System.Linq;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    // This class updates products to use a slug with either:
    //  1. The Fact Tag (ABC products)
    //  2. Short Description (SOT Products)
    // appended to the product name. We use saving functionality here to make sure we're not breaking existing links.
    class UpdateProductURLsTask : IScheduleTask
    {
        private readonly INopDataProvider _nopDbContext;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IRepository<Product> _productRepository;
        private readonly ImportSettings _importSettings;
        private readonly StagingDb _stagingDb;

        public UpdateProductURLsTask(
            INopDataProvider nopDbContext,
            IUrlRecordService urlRecordService,
            IRepository<Product> productRepository,
            ImportSettings importSettings,
            StagingDb stagingDb
        )
        {
            _nopDbContext = nopDbContext;
            _urlRecordService = urlRecordService;
            _productRepository = productRepository;
            _importSettings = importSettings;
            _stagingDb = stagingDb;
        }

        public void Execute()
        {
            this.LogStart();

            var stagingProducts = _stagingDb.GetProducts();

            var ExistingSkuToId = _productRepository.Table.Select(p => new { p.Sku, p.Id }).ToDictionary(p => p.Sku, p => p.Id);

            foreach (var stagingProduct in stagingProducts)
            {
                if (string.IsNullOrEmpty(stagingProduct.Sku))
                    continue;

                Product product = null;
                if (ExistingSkuToId.ContainsKey(stagingProduct.Sku))
                {
                    var nopId = ExistingSkuToId[stagingProduct.Sku];
                    product = _productRepository.Table.Where(prod => prod.Id == nopId).First();
                }

                if (product != null && product.Deleted)
                {
                    continue;
                }

                var activeSlug = _urlRecordService.GetActiveSlug(product.Id, "Product", 0);
                var desiredSuffix = string.IsNullOrWhiteSpace(stagingProduct.ISAMItemNo) ? product.ShortDescription : stagingProduct.FactTag;
                var desiredSlug = _urlRecordService.ValidateSeName(product, "", product.Name + " " + desiredSuffix, true);
                if (activeSlug != desiredSlug)
                {
                    _urlRecordService.SaveSlug(product, desiredSlug, 0);
                }
            }
                

            this.LogEnd();
        }
    }
}
