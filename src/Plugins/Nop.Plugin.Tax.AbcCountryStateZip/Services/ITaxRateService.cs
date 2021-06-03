using Nop.Core;
using Nop.Plugin.Tax.AbcCountryStateZip.Domain;
using System.Threading.Tasks;

namespace Nop.Plugin.Tax.AbcCountryStateZip.Services
{
    public partial interface ITaxRateService
    {
        Task DeleteTaxRateAsync(TaxRate taxRate);
        Task<IPagedList<TaxRate>> GetAllTaxRatesAsync(int pageIndex = 0, int pageSize = int.MaxValue);
        Task<TaxRate> GetTaxRateByIdAsync(int taxRateId);
        Task InsertTaxRateAsync(TaxRate taxRate);
        Task UpdateTaxRateAsync(TaxRate taxRate);
    }
}
