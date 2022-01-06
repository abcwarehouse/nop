using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcFrontend.Models;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public interface ICartSlideoutService
    {
        Task<CartSlideoutInfo> GetCartSlideoutInfoAsync(Product product);
    }
}
