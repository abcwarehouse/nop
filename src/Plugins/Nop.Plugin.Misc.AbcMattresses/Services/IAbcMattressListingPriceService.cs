using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressListingPriceService
    {
        Task<decimal?> GetListingPriceForMattressProductAsync(int productId);
    }
}
