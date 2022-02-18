using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcCore.Nop
{
    public interface IAbcProductAttributeService : IProductAttributeService
    {
        Task SaveProductAttributeAsync(ProductAttribute pa);
    }
}
