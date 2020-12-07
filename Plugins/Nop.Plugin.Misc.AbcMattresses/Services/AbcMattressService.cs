using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Seo;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Services.Catalog;
using Nop.Services.Seo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressService : IAbcMattressService
    {
        private readonly IRepository<AbcMattressModel> _abcMattressModelRepository;

        private readonly IProductService _productService;
        private readonly IUrlRecordService _urlRecordService;

        public AbcMattressService(
            IRepository<AbcMattressModel> abcMattressModelRepository,
            IProductService productService,
            IUrlRecordService urlRecordService
        )
        {
            _abcMattressModelRepository = abcMattressModelRepository;
            _productService = productService;
            _urlRecordService = urlRecordService;
        }

        public IList<AbcMattressModel> GetAllAbcMattressModels()
        {
            return _abcMattressModelRepository.Table.ToList();
        }

        public Product CreateAbcMattressProduct(AbcMattressModel abcMattressModel)
        {
            var newProduct = new Product()
            {
                Name = abcMattressModel.Description,
                Sku = abcMattressModel.Name,
                AllowCustomerReviews = false,
                Published = true,
                CreatedOnUtc = DateTime.UtcNow,
                VisibleIndividually = true,
                ProductType = ProductType.SimpleProduct,
                OrderMinimumQuantity = 1,
                OrderMaximumQuantity = 10000
            };
            _productService.InsertProduct(newProduct);

            var urlRecord = new UrlRecord()
            {
                EntityId = newProduct.Id,
                EntityName = "Product",
                Slug = newProduct.Sku,
                IsActive = true,
                LanguageId = 0
            };
            _urlRecordService.InsertUrlRecord(urlRecord);

            return newProduct;
        }
    }
}
