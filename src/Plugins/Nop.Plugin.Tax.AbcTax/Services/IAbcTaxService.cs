using System.Threading.Tasks;
using Nop.Core;
using Nop.Plugin.Tax.AbcTax.Domain;

namespace Nop.Plugin.Tax.AbcTax.Services
{
    public partial interface IAbcTaxService
    {
        Task DeleteTaxRateAsync(AbcTaxRate taxRate);
        Task<IPagedList<AbcTaxRate>> GetAllTaxRatesAsync(int pageIndex = 0, int pageSize = int.MaxValue);
        Task<AbcTaxRate> GetTaxRateByIdAsync(int taxRateId);
        Task InsertTaxRateAsync(AbcTaxRate taxRate);
        Task UpdateTaxRateAsync(AbcTaxRate taxRate);
    }
}