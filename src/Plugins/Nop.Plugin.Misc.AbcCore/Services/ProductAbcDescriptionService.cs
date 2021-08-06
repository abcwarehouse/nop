using System.Linq;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Domain;

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

        public ProductAbcDescription GetProductAbcDescriptionByAbcItemNumber(string abcitemNumber)
        {
            return _productAbcDescriptionRepository.Table
                                                   .Where(pad => pad.AbcItemNumber == abcitemNumber)
                                                   .FirstOrDefault();
        }
    }
}
