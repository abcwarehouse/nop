using Nop.Plugin.Widgets.AbcSynchronyPayments.Domain;

namespace Nop.Plugin.Widgets.AbcSynchronyPayments.Services
{
    public interface IProductAbcFinanceService
    {
        ProductAbcFinance GetProductAbcFinanceByAbcItemNumber(string abcItemNumber);
    }
}
