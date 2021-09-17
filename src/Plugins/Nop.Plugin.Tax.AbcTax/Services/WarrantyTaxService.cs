using System.Threading.Tasks;
using Address = Nop.Core.Domain.Common.Address;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Customers;
using Nop.Services.Catalog;
using Nop.Services.Tax;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Core.Domain.Catalog;
using System.Linq;

namespace Nop.Plugin.Tax.AbcTax.Services
{
    public partial class WarrantyTaxService : IWarrantyTaxService
    {
        private readonly IAttributeUtilities _attributeUtilities;
        private readonly IImportUtilities _importUtilities;

        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly ITaxService _taxService;

        public WarrantyTaxService(
            IAttributeUtilities attributeUtilities,
            IImportUtilities importUtilities,
            IProductAttributeParser productAttributeParser,
            IProductService productService,
            ITaxService taxService
        )
        {
            _attributeUtilities = attributeUtilities;
            _importUtilities = importUtilities;
            _productAttributeParser = productAttributeParser;
            _productService = productService;
            _taxService = taxService;
        }

        public async Task<(decimal taxRate, decimal sciSubTotalInclTax)> CalculateWarrantyTaxAsync(
            ShoppingCartItem sci,
            Customer customer,
            decimal sciSubTotalExclTax
        ) {
            var result = await CalculateWarrantyTaxAsync(
                sci,
                customer,
                sciSubTotalExclTax,
                sciSubTotalExclTax / sci.Quantity
            );
            return (result.taxRate, result.sciSubTotalInclTax);
        }

        public async Task<(decimal taxRate, decimal sciSubTotalInclTax, decimal sciUnitPriceInclTax, decimal warrantyUnitPriceExclTax, decimal warrantyUnitPriceInclTax)> CalculateWarrantyTaxAsync(
            ShoppingCartItem sci,
            Customer customer,
            decimal sciSubTotalExclTax,
            decimal sciUnitPriceExclTax
        ) {
            var product = await _productService.GetProductByIdAsync(sci.ProductId);
            var sciSubTotalInclTax = await _taxService.GetProductPriceAsync(product, sciSubTotalExclTax, true, customer);
            var sciUnitPriceInclTax = await _taxService.GetProductPriceAsync(product, sciUnitPriceExclTax, true, customer);

            var warrantyUnitPriceExclTax = decimal.Zero;
            (decimal price, decimal taxRate) warrantyUnitPriceInclTax;
            warrantyUnitPriceInclTax.price = decimal.Zero;
            warrantyUnitPriceInclTax.taxRate = decimal.Zero;

            // warranty item handling
            ProductAttributeMapping warrantyPam = await _attributeUtilities.GetWarrantyAttributeMappingAsync(sci.AttributesXml);
            if (warrantyPam != null)
            {
                warrantyUnitPriceExclTax =
                    (await _productAttributeParser.ParseProductAttributeValuesAsync(sci.AttributesXml))
                    .Where(pav => pav.ProductAttributeMappingId == warrantyPam.Id)
                    .Select(pav => pav.PriceAdjustment)
                    .FirstOrDefault();

                // get warranty "product" - this is so the warranties have a tax category
                Product warrProduct = _importUtilities.GetExistingProductBySku("WARRPLACE_SKU");

                //true if customer lives in a state where warranty can be taxed
                bool isCustomerInTaxableState = false;
                //var taxCategory = _taxCategoryService.GetAllTaxCategories().FirstOrDefault(x => x.Name == "Warranties");
                //var isCustomerInTaxableState = _taxService.IsCustomerInTaxableState(taxCategory?.Id ?? 0, customer);

                if (warrProduct == null)
                {
                    // taxed warranty price
                    warrantyUnitPriceInclTax = await _taxService.GetProductPriceAsync(product, warrantyUnitPriceExclTax, false, customer);
                }
                else
                {
                    warrantyUnitPriceInclTax = await _taxService.GetProductPriceAsync(warrProduct, warrantyUnitPriceExclTax, isCustomerInTaxableState, customer);
                }

                var productUnitPriceInclTax
                    = await _taxService.GetProductPriceAsync(product, sciUnitPriceExclTax - warrantyUnitPriceExclTax, true, customer);

                sciUnitPriceInclTax.price = productUnitPriceInclTax.price + warrantyUnitPriceInclTax.price;
                sciSubTotalInclTax.price = sciUnitPriceInclTax.price * sci.Quantity;
            }

            return (warrantyUnitPriceInclTax.taxRate, sciSubTotalInclTax.price, sciUnitPriceInclTax.price, warrantyUnitPriceExclTax, warrantyUnitPriceInclTax.price);
        }
    }
}