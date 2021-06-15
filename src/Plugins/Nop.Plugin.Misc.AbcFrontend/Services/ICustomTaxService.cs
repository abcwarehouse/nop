using Nop.Core.Domain.Customers;
using Nop.Services.Tax;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public interface ICustomTaxService : ITaxService
    {
        Task<bool> IsCustomerInTaxableStateAsync(int taxCategoryId, Customer customer);
    }
}
