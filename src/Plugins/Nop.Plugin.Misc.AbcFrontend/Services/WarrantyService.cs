using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Catalog;
using Nop.Services.Tax;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public class WarrantyService : IWarrantyService
    {
        public readonly string WARRANTY_PLACEHOLDER_SKU = "WARRPLACE_SKU";

        private readonly IRepository<WarrantySku> _warrantySkuRepository;

        public readonly IAttributeUtilities _attributeUtilities;
        public readonly ICustomTaxService _taxService;
        private readonly ITaxCategoryService _taxCategoryService;
        public readonly IProductAttributeParser _productAttributeParser;
        public readonly IImportUtilities _importUtilities;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;

        public WarrantyService(
            IRepository<WarrantySku> warrantySkuRepository,
            IAttributeUtilities attributeUtilities,
            ICustomTaxService taxService,
            ITaxCategoryService taxCategoryService,
            IProductAttributeParser productAttributeParser,
            IImportUtilities importUtilities,
            IProductService productService,
            IProductAttributeService productAttributeService
        )
        {
            _warrantySkuRepository = warrantySkuRepository;
            _attributeUtilities = attributeUtilities;
            _taxService = taxService;
            _taxCategoryService = taxCategoryService;
            _productAttributeParser = productAttributeParser;
            _importUtilities = importUtilities;
            _productService = productService;
            _productAttributeService = productAttributeService;
        }

        public async Task<(decimal taxRate, decimal sciSubTotalInclTax)> CalculateWarrantyTaxAsync(
            ShoppingCartItem sci,
            Customer customer,
            decimal sciSubTotalExclTax)
        {
            var result = await CalculateWarrantyTaxAsync(sci, customer, sciSubTotalExclTax, sciSubTotalExclTax / sci.Quantity);
            return (result.taxRate, result.sciSubTotalInclTax);
        }

        public async Task<(decimal taxRate, decimal sciSubTotalInclTax, decimal sciUnitPriceInclTax, decimal warrantyUnitPriceExclTax, decimal warrantyUnitPriceInclTax)> CalculateWarrantyTaxAsync(ShoppingCartItem sci, Customer customer,
            decimal sciSubTotalExclTax, decimal sciUnitPriceExclTax)
        {
            var taxRate = decimal.Zero;
            var warrantyUnitPriceExclTax = decimal.Zero;
            (decimal price, decimal taxRate) warrantyUnitPriceInclTax;
            warrantyUnitPriceInclTax.price = decimal.Zero;
            warrantyUnitPriceInclTax.taxRate = decimal.Zero;
            var product = await _productService.GetProductByIdAsync(sci.ProductId);
            var sciSubTotalInclTax = await _taxService.GetProductPriceAsync(product, sciSubTotalExclTax, true, customer);
            var sciUnitPriceInclTax = await _taxService.GetProductPriceAsync(product, sciUnitPriceExclTax, true, customer);
            
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
                Product warrProduct = _importUtilities.GetExistingProductBySku(WARRANTY_PLACEHOLDER_SKU);

                //true if customer lives in a state where warranty can be taxed
                bool isCustomerInTaxableState = false;
                var taxCategory = (await _taxCategoryService.GetAllTaxCategoriesAsync()).FirstOrDefault(x => x.Name == "Warranties");
                isCustomerInTaxableState = await _taxService.IsCustomerInTaxableStateAsync(taxCategory?.Id ?? 0, customer);

                // custom
                warrantyUnitPriceInclTax = await _taxService.GetProductPriceAsync(
                    product,
                    warrantyUnitPriceExclTax,
                    warrProduct == null ? false : isCustomerInTaxableState,
                    customer
                );

                var productUnitPriceInclTax
                    = await _taxService.GetProductPriceAsync(product, sciUnitPriceExclTax - warrantyUnitPriceExclTax, true, customer);

                sciUnitPriceInclTax.price = productUnitPriceInclTax.price + warrantyUnitPriceInclTax.price;
                sciSubTotalInclTax.price = sciUnitPriceInclTax.price * sci.Quantity;
            }

            return (taxRate, sciSubTotalInclTax.price, sciUnitPriceInclTax.price, warrantyUnitPriceExclTax, warrantyUnitPriceInclTax.price);
        }

        public async Task<bool> CartContainsWarrantiesAsync(IList<ShoppingCartItem> cart)
        {
            foreach (var sci in cart)
            {
                var pams = await _productAttributeService.GetProductAttributeMappingsByProductIdAsync(sci.ProductId);
                foreach (var pam in pams)
                {
                    var pa = await _productAttributeService.GetProductAttributeByIdAsync(pam.ProductAttributeId);
                    if (pa.Name == "Warranty") { return true; }
                }
            }

            return false;
        }

        public string GetWarrantySkuByName(string name)
        {
            return _warrantySkuRepository.Table
                .Where(ws => ws.Name == name)
                .Select(ws => ws.Sku).FirstOrDefault();
        }
    }
}
