using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcFrontend.Models;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public interface IAddToCartSlideoutService
    {
        Task<AddToCartSlideoutInfo> GetAddToCartSlideoutInfoAsync(Product product);
    }
}
