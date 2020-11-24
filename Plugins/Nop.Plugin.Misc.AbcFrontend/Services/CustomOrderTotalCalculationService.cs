using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Services.Tax;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public class CustomOrderTotalCalculationService : OrderTotalCalculationService
    {
        private readonly CatalogSettings _catalogSettings;
        private readonly IAddressService _addressService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICustomerService _customerService;
        private readonly IDiscountService _discountService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IGiftCardService _giftCardService;
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IProductService _productService;
        private readonly IRewardPointService _rewardPointService;
        private readonly IShippingPluginManager _shippingPluginManager;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly IWorkContext _workContext;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly TaxSettings _taxSettings;

        // custom
        private readonly IWarrantyService _warrantyService;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="workContext">Work context</param>
        /// <param name="storeContext">Store context</param>
        /// <param name="priceCalculationService">Price calculation service</param>
        /// <param name="taxService">Tax service</param>
        /// <param name="shippingService">Shipping service</param>
        /// <param name="paymentService">Payment service</param>
        /// <param name="checkoutAttributeParser">Checkout attribute parser</param>
        /// <param name="discountService">Discount service</param>
        /// <param name="giftCardService">Gift card service</param>
        /// <param name="genericAttributeService">Generic attribute service</param>
        /// <param name="rewardPointService">Reward point service</param>
        /// <param name="taxSettings">Tax settings</param>
        /// <param name="rewardPointsSettings">Reward points settings</param>
        /// <param name="shippingSettings">Shipping settings</param>
        /// <param name="shoppingCartSettings">Shopping cart settings</param>
        /// <param name="catalogSettings">Catalog settings</param>
        public CustomOrderTotalCalculationService(CatalogSettings catalogSettings,
            IAddressService addressService,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICustomerService customerService,
            IDiscountService discountService,
            IGenericAttributeService genericAttributeService,
            IGiftCardService giftCardService,
            IOrderService orderService,
            IPaymentService paymentService,
            IPriceCalculationService priceCalculationService,
            IProductService productService,
            IRewardPointService rewardPointService,
            IShippingPluginManager shippingPluginManager,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            ITaxService taxService,
            IWorkContext workContext,
            RewardPointsSettings rewardPointsSettings,
            ShippingSettings shippingSettings,
            ShoppingCartSettings shoppingCartSettings,
            TaxSettings taxSettings,
            // custom
            IWarrantyService warrantyService
        )
            : base(catalogSettings, addressService, checkoutAttributeParser, customerService,
                discountService, genericAttributeService, giftCardService, orderService,
                paymentService, priceCalculationService, productService, rewardPointService,
                shippingPluginManager, shippingService, shoppingCartService,
                storeContext, taxService, workContext, rewardPointsSettings,
                shippingSettings, shoppingCartSettings, taxSettings)
        {
            _catalogSettings = catalogSettings;
            _addressService = addressService;
            _checkoutAttributeParser = checkoutAttributeParser;
            _customerService = customerService;
            _discountService = discountService;
            _genericAttributeService = genericAttributeService;
            _giftCardService = giftCardService;
            _orderService = orderService;
            _paymentService = paymentService;
            _priceCalculationService = priceCalculationService;
            _productService = productService;
            _rewardPointService = rewardPointService;
            _shippingPluginManager = shippingPluginManager;
            _shippingService = shippingService;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _taxService = taxService;
            _workContext = workContext;
            _rewardPointsSettings = rewardPointsSettings;
            _shippingSettings = shippingSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _taxSettings = taxSettings;

            // custom
            _warrantyService = warrantyService;
        }

        /// <summary>
        /// Gets shopping cart subtotal
        /// </summary>
        /// <param name="cart">Cart</param>
        /// <param name="includingTax">A value indicating whether calculated price should include tax</param>
        /// <param name="discountAmount">Applied discount amount</param>
        /// <param name="appliedDiscounts">Applied discounts</param>
        /// <param name="subTotalWithoutDiscount">Sub total (without discount)</param>
        /// <param name="subTotalWithDiscount">Sub total (with discount)</param>
        /// <param name="taxRates">Tax rates (of order sub total)</param>
        public override void GetShoppingCartSubTotal(
            IList<ShoppingCartItem> cart,
            bool includingTax,
            out decimal discountAmount,
            out List<Discount> appliedDiscounts,
            out decimal subTotalWithoutDiscount,
            out decimal subTotalWithDiscount,
            out SortedDictionary<decimal, decimal> taxRates)
        {
            discountAmount = decimal.Zero;
            appliedDiscounts = new List<Discount>();
            subTotalWithoutDiscount = decimal.Zero;
            subTotalWithDiscount = decimal.Zero;
            taxRates = new SortedDictionary<decimal, decimal>();

            if (!cart.Any())
                return;

            //get the customer 
            var customer = _customerService.GetShoppingCartCustomer(cart);
            
            //sub totals
            decimal subTotalExclTaxWithoutDiscount = decimal.Zero;
            decimal subTotalInclTaxWithoutDiscount = decimal.Zero;
            foreach (var shoppingCartItem in cart)
            {
                decimal sciSubTotal = _shoppingCartService.GetSubTotal(shoppingCartItem);
                var product = _productService.GetProductById(shoppingCartItem.ProductId);

                decimal taxRate;
                decimal sciExclTax = _taxService.GetProductPrice(
                    product, sciSubTotal, false, customer, out taxRate);
                decimal sciInclTax;
                _warrantyService.CalculateWarrantyTax(
                    shoppingCartItem, customer, sciExclTax, out taxRate, out sciInclTax);

                subTotalExclTaxWithoutDiscount += sciExclTax;
                subTotalInclTaxWithoutDiscount += sciInclTax;


                //tax rates
                decimal sciTax = sciInclTax - sciExclTax;
                if (taxRate > decimal.Zero && sciTax > decimal.Zero)
                {
                    if (!taxRates.ContainsKey(taxRate))
                    {
                        taxRates.Add(taxRate, sciTax);
                    }
                    else
                    {
                        taxRates[taxRate] = taxRates[taxRate] + sciTax;
                    }
                }
            }

            //checkout attributes
            if (customer != null)
            {
                var checkoutAttributesXml = _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.CheckoutAttributes, _storeContext.CurrentStore.Id);
                var attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(checkoutAttributesXml);
                if (attributeValues != null)
                {
                    foreach (var (attribute, values) in attributeValues)
                    {
                        foreach (var attributeValue in values)
                        {
                            var caExclTax = _taxService.GetCheckoutAttributePrice(attribute, attributeValue, false, customer, out var taxRate);
                            var caInclTax = _taxService.GetCheckoutAttributePrice(attribute, attributeValue, true, customer, out taxRate);

                            subTotalExclTaxWithoutDiscount += caExclTax;
                            subTotalInclTaxWithoutDiscount += caInclTax;

                            //tax rates
                            var caTax = caInclTax - caExclTax;
                            if (taxRate <= decimal.Zero || caTax <= decimal.Zero)
                                continue;

                            if (!taxRates.ContainsKey(taxRate))
                            {
                                taxRates.Add(taxRate, caTax);
                            }
                            else
                            {
                                taxRates[taxRate] = taxRates[taxRate] + caTax;
                            }
                        }
                    }
                }
            }

            //subtotal without discount
            subTotalWithoutDiscount = includingTax ? subTotalInclTaxWithoutDiscount : subTotalExclTaxWithoutDiscount;
            if (subTotalWithoutDiscount < decimal.Zero)
                subTotalWithoutDiscount = decimal.Zero;

            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                subTotalWithoutDiscount = _priceCalculationService.RoundPrice(subTotalWithoutDiscount);

            //We calculate discount amount on order subtotal excl tax (discount first)
            //calculate discount amount ('Applied to order subtotal' discount)
            decimal discountAmountExclTax = GetOrderSubtotalDiscount(customer, subTotalExclTaxWithoutDiscount, out appliedDiscounts);
            if (subTotalExclTaxWithoutDiscount < discountAmountExclTax)
                discountAmountExclTax = subTotalExclTaxWithoutDiscount;
            decimal discountAmountInclTax = discountAmountExclTax;
            //subtotal with discount (excl tax)
            decimal subTotalExclTaxWithDiscount = subTotalExclTaxWithoutDiscount - discountAmountExclTax;
            decimal subTotalInclTaxWithDiscount = subTotalExclTaxWithDiscount;

            //add tax for shopping items & checkout attributes
            var tempTaxRates = new Dictionary<decimal, decimal>(taxRates);
            foreach (KeyValuePair<decimal, decimal> kvp in tempTaxRates)
            {
                decimal taxRate = kvp.Key;
                decimal taxValue = kvp.Value;

                if (taxValue != decimal.Zero)
                {
                    //discount the tax amount that applies to subtotal items
                    if (subTotalExclTaxWithoutDiscount > decimal.Zero)
                    {
                        decimal discountTax = taxRates[taxRate] * (discountAmountExclTax / subTotalExclTaxWithoutDiscount);
                        discountAmountInclTax += discountTax;
                        taxValue = taxRates[taxRate] - discountTax;
                        if (_shoppingCartSettings.RoundPricesDuringCalculation)
                            taxValue = _priceCalculationService.RoundPrice(taxValue);
                        taxRates[taxRate] = taxValue;
                    }

                    //subtotal with discount (incl tax)
                    subTotalInclTaxWithDiscount += taxValue;
                }
            }

            if (_shoppingCartSettings.RoundPricesDuringCalculation)
            {
                discountAmountInclTax = _priceCalculationService.RoundPrice(discountAmountInclTax);
                discountAmountExclTax = _priceCalculationService.RoundPrice(discountAmountExclTax);
            }

            if (includingTax)
            {
                subTotalWithDiscount = subTotalInclTaxWithDiscount;
                discountAmount = discountAmountInclTax;
            }
            else
            {
                subTotalWithDiscount = subTotalExclTaxWithDiscount;
                discountAmount = discountAmountExclTax;
            }

            if (subTotalWithDiscount < decimal.Zero)
                subTotalWithDiscount = decimal.Zero;

            if (_shoppingCartSettings.RoundPricesDuringCalculation)
                subTotalWithDiscount = _priceCalculationService.RoundPrice(subTotalWithDiscount);
        }
    }

}
