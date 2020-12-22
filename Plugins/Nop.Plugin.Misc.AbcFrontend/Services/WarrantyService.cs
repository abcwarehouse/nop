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

        public WarrantyService (
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

        public void CalculateWarrantyTax(ShoppingCartItem sci, Customer customer,
            decimal sciSubTotalExclTax,
            out decimal taxRate,
            out decimal sciSubTotalInclTax)
        {
            decimal sciUnitPriceInclTax;
            decimal warrantyUnitPriceExclTax;
            decimal warrantyUnitPriceInclTax;
            CalculateWarrantyTax(sci, customer, sciSubTotalExclTax, sciSubTotalExclTax / sci.Quantity, 
                out taxRate, out sciSubTotalInclTax,
                out sciUnitPriceInclTax, out warrantyUnitPriceExclTax, out warrantyUnitPriceInclTax);
        }

        public void CalculateWarrantyTax(ShoppingCartItem sci, Customer customer, 
            decimal sciSubTotalExclTax, decimal sciUnitPriceExclTax, 
            out decimal taxRate, 
            out decimal sciSubTotalInclTax, out decimal sciUnitPriceInclTax, 
            out decimal warrantyUnitPriceExclTax, out decimal warrantyUnitPriceInclTax)
        {
            taxRate = decimal.Zero;
            var product = _productService.GetProductById(sci.ProductId);
            sciSubTotalInclTax = _taxService.GetProductPrice(product, sciSubTotalExclTax, true, customer, out taxRate);
            sciUnitPriceInclTax = _taxService.GetProductPrice(product, sciUnitPriceExclTax, true, customer, out taxRate);
            warrantyUnitPriceExclTax = decimal.Zero;
            warrantyUnitPriceInclTax = decimal.Zero;

            // warranty item handling
            ProductAttributeMapping warrantyPam = _attributeUtilities.GetWarrantyAttributeMapping(sci.AttributesXml);
            if (warrantyPam != null)
            {
                warrantyUnitPriceExclTax =
                    _productAttributeParser.ParseProductAttributeValues(sci.AttributesXml)
                    .Where(pav => pav.ProductAttributeMappingId == warrantyPam.Id)
                    .Select(pav => pav.PriceAdjustment)
                    .FirstOrDefault();

                // get warranty "product" - this is so the warranties have a tax category
                Product warrProduct = _importUtilities.GetExistingProductBySku(WARRANTY_PLACEHOLDER_SKU);

                //true if customer lives in a state where warranty can be taxed
                bool isCustomerInTaxableState = false;
                var taxCategory = _taxCategoryService.GetAllTaxCategories().FirstOrDefault(x => x.Name == "Warranties");
                isCustomerInTaxableState = _taxService.IsCustomerInTaxableState(taxCategory?.Id ?? 0, customer);
                
                if (warrProduct == null)
                {
                    // taxed warranty price
                    warrantyUnitPriceInclTax = _taxService.GetProductPrice(product, warrantyUnitPriceExclTax, false, customer, out taxRate);
                }
                else
                {
                    warrantyUnitPriceInclTax = _taxService.GetProductPrice(warrProduct, warrantyUnitPriceExclTax, isCustomerInTaxableState, customer, out taxRate);
                }

                decimal productUnitPriceInclTax 
                    = _taxService.GetProductPrice(product, sciUnitPriceExclTax - warrantyUnitPriceExclTax, true, customer, out taxRate);

                sciUnitPriceInclTax = productUnitPriceInclTax + warrantyUnitPriceInclTax;
                sciSubTotalInclTax = sciUnitPriceInclTax * sci.Quantity;
            }
        }

        public bool CartContainsWarranties(IList<ShoppingCartItem> cart)
        {
            return cart.Any(sci =>
                _productAttributeService.GetProductAttributeMappingsByProductId(
                    sci.ProductId
                )
                .Where(pam => _productAttributeService.GetProductAttributeById(
                    pam.ProductAttributeId).Name == "Warranty")
                .Any());
        }

        public string GetWarrantySkuByName(string name)
        {
            return _warrantySkuRepository.Table
                .Where(ws => ws.Name == name)
                .Select(ws => ws.Sku).FirstOrDefault();
        }
    }
}
