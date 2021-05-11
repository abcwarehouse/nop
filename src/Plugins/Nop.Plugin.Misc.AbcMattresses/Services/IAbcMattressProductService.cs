using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressProductService
    {
        List<string> GetMattressItemNos();
        Task<Product> UpsertAbcMattressProductAsync(AbcMattressModel model);
        Task SetManufacturerAsync(AbcMattressModel model, Product product);
        Task SetCategoriesAsync(AbcMattressModel model, Product product);
        Task SetProductAttributesAsync(AbcMattressModel model, Product product);
        bool IsMattressProduct(int productId);
    }
}
