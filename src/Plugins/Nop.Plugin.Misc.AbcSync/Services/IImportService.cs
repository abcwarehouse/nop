using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public interface IImportService
    {
        Task ImportFeaturedProductsAsync();
    }
}
