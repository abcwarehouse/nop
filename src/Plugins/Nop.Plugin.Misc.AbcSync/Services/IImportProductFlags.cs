using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public interface IImportProductFlags
    {
        /// <summary>
        ///		Begin the import process for the product flags.
        /// </summary>
        Task ImportAsync();
    }
}