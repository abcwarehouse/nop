using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public interface IImportMarkdowns
    {
        /// <summary>
        ///		Begin the import process for product's specifications.
        /// </summary>
        Task ImportAsync();
    }
}