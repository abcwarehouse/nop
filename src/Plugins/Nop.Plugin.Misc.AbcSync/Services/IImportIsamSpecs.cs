using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    interface IImportIsamSpecs
    {
        /// <summary>
        ///		Begin the import process for product's specifications.
        /// </summary>
        Task ImportSiteOnTimeSpecsAsync();
    }
}