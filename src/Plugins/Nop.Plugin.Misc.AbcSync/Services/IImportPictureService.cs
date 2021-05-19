using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync.Services
{
    public interface IImportPictureService
    {
        Task ImportSiteOnTimePicturesAsync();
    }
}