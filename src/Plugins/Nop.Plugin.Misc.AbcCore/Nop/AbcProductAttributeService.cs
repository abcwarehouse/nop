using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcCore.Nop
{
    public class AbcProductAttributeService : ProductAttributeService, IAbcProductAttributeService
    {
        public AbcProductAttributeService(
            IRepository<PredefinedProductAttributeValue> predefinedProductAttributeValueRepository,
            IRepository<ProductAttribute> productAttributeRepository,
            IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
            IRepository<ProductAttributeMapping> productAttributeMappingRepository,
            IRepository<ProductAttributeValue> productAttributeValueRepository,
            IStaticCacheManager staticCacheManager)
        : base(predefinedProductAttributeValueRepository, productAttributeRepository, productAttributeCombinationRepository,
               productAttributeMappingRepository, productAttributeValueRepository, staticCacheManager)
        {

        }

        public async Task SaveProductAttributeAsync(ProductAttribute pa)
        {
            var existingPa = (await GetAllProductAttributesAsync()).FirstOrDefault(epa => epa.Name == pa.Name);

            if (existingPa != null)
            {
                pa.Id = existingPa.Id;
                await UpdateProductAttributeAsync(pa);
            }
            else
            {
                await InsertProductAttributeAsync(pa);
            }
        }
    }
}
