using Nop.Plugin.Misc.FreshAddressNewsletterIntegration.Models;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.FreshAddressNewsletterIntegration.Services
{
    public interface IFreshAddressService
    {
        FreshAddressResponse ValidateEmail(string email);
    }
}
