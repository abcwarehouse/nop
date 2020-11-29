namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressListingPriceService : IAbcMattressListingPriceService
    {
        public decimal GetListingPriceForMattressProduct(int productId)
        {
            return 175.0M;
        }
    }
}
