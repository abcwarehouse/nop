using Nop.Core.Domain.Customers;
using Nop.Services.Tax;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public interface ICustomTaxService : ITaxService
    {
        bool IsCustomerInTaxableState(int taxCategoryId, Customer customer); 
    }
}
