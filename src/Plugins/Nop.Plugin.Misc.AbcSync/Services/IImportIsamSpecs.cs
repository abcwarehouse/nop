using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public interface IImportIsamSpecs
    {
        /// <summary>
        ///		Begin the import process for product's specifications.
        /// </summary>
        Task ImportSiteOnTimeSpecsAsync();
        Task ImportColorAsync();
    }
}