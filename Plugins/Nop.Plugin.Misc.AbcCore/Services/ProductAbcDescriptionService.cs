using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public class ProductAbcDescriptionService : IProductAbcDescriptionService
    {
        private readonly IRepository<ProductAbcDescription> _productAbcDescriptionRepository;

        public ProductAbcDescriptionService(
            IRepository<ProductAbcDescription> productAbcDescriptionRepository
        )
        {
            _productAbcDescriptionRepository = productAbcDescriptionRepository;
        }

        public ProductAbcDescription GetProductAbcDescriptionByProductId(int productId)
        {
            return _productAbcDescriptionRepository.Table
                                                   .Where(pad => pad.Product_Id == productId)
                                                   .FirstOrDefault();
        }
    }
}
