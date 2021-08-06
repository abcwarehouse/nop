using Nop.Core;
using Nop.Services.Logging;
using Microsoft.AspNetCore.Mvc;
using Nop.Web.Framework.Components;
using Nop.Services.Customers;
using Nop.Plugin.Widgets.Bronto.Models;
using Nop.Services.Orders;
using Nop.Web.Factories;
using System;
using Nop.Plugin.Widgets.Bronto.Domain;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Web.Framework.Infrastructure;
using System.Threading.Tasks;
using Nop.Plugin.Widgets.Bronto.Services;

namespace Nop.Plugin.Widgets.Bronto.Components
{
    public class BrontoViewComponent : NopViewComponent
    {
        private readonly BrontoSettings _settings;

        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;

        private readonly IBrontoLineItemService _brontoLineItemService;
        private readonly ICustomerService _customerService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;

        public BrontoViewComponent(
            BrontoSettings settings,
            IWorkContext workContext,
            IStoreContext storeContext,
            IShoppingCartModelFactory shoppingCartModelFactory,
            IBrontoLineItemService brontoLineItemService,
            ICustomerService customerService,
            IShoppingCartService shoppingCartService,
            IOrderService orderService,
            ILogger logger
        )
        {
            _settings = settings;
            _workContext = workContext;
            _storeContext = storeContext;
            _shoppingCartModelFactory = shoppingCartModelFactory;
            _brontoLineItemService = brontoLineItemService;
            _customerService = customerService;
            _shoppingCartService = shoppingCartService;
            _orderService = orderService;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData = null)
        {
            // Process Cart Recovery
            if (string.IsNullOrWhiteSpace(_settings.ScriptManagerCode))
            {
                await _logger.ErrorAsync("Widgets.Bronto: Script Manager code not provided, will not provide Bronto Cart Recovery integration.");
                return Content("");
            }

            var phase = DetermineBrontoPhase();

            var customer = await _workContext.GetCurrentCustomerAsync();
            var mobilePhoneNumber = (await _customerService.GetCustomerBillingAddressAsync(customer))?.PhoneNumber;
            var cart = await _shoppingCartService.GetShoppingCartAsync(customer);

            var model = new BrontoModel()
            {
                ScriptManagerCode = _settings.ScriptManagerCode,
                BrontoCart = new BrontoCart()
                {
                    Phase = phase,
                    Currency = (await _workContext.GetWorkingCurrencyAsync()).CurrencyCode,
                    EmailAddress = customer.Email ?? null,
                    CartUrl = (await _storeContext.GetCurrentStoreAsync()).Url.TrimEnd('/') + "/cart",
                    MobilePhoneNumber = mobilePhoneNumber ?? "",
                    OrderSmsConsentChecked = false // currently not used
                }
            };

            // Populate values based on order status
            switch (phase)
            {
                case BrontoPhases.OrderComplete:
                    await PopulateModelForOrderCompleteAsync(model);
                    break;
                default:
                    await PopulateModelForShoppingAsync(cart, model);
                    break;
            }

            return View("~/Plugins/Widgets.Bronto/Views/Display.cshtml", model);
        }

        private async Task PopulateModelForShoppingAsync(IList<ShoppingCartItem> cart, BrontoModel model)
        {
            var orderTotalsModel = await _shoppingCartModelFactory.PrepareOrderTotalsModelAsync(cart, false);
            model.BrontoCart.Subtotal = Convert.ToDecimal(orderTotalsModel.SubTotal?.Replace("$", ""));
            model.BrontoCart.DiscountAmount = Convert.ToDecimal(orderTotalsModel.OrderTotalDiscount?.Replace("$", ""));
            model.BrontoCart.TaxAmount = Convert.ToDecimal(orderTotalsModel.Tax?.Replace("$", ""));
            model.BrontoCart.GrandTotal = Convert.ToDecimal(orderTotalsModel.OrderTotal?.Replace("$", ""));

            // grand total comes up as zero if shipping isn't selected yet
            if (model.BrontoCart.GrandTotal == 0)
            {
                model.BrontoCart.GrandTotal = model.BrontoCart.Subtotal -
                                              model.BrontoCart.DiscountAmount +
                                              model.BrontoCart.TaxAmount;
            }

            model.BrontoCart.LineItems = await _brontoLineItemService.GetBrontoLineItemsAsync(cart);
        }

        private async Task PopulateModelForOrderCompleteAsync(BrontoModel model)
        {
            var routeData = Url.ActionContext.RouteData;
            var orderId = Convert.ToInt32(routeData.Values["orderId"]);
            var order = await _orderService.GetOrderByIdAsync(orderId);
            model.BrontoCart.Subtotal = order.OrderSubtotalExclTax;
            model.BrontoCart.DiscountAmount = order.OrderSubTotalDiscountExclTax;
            model.BrontoCart.TaxAmount = order.OrderTax;
            model.BrontoCart.GrandTotal = order.OrderTotal;
            model.BrontoCart.CustomerOrderId = order.Id;

            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
            model.BrontoCart.LineItems = await _brontoLineItemService.GetBrontoLineItemsAsync(orderItems);
        }

        private string DetermineBrontoPhase()
        {
            var routeData = Url.ActionContext.RouteData;
            var controller = routeData.Values["controller"];
            var action = routeData.Values["action"];

            return controller.ToString() == "Checkout" &&
                   action.ToString() == "Completed" ?
                BrontoPhases.OrderComplete :
                BrontoPhases.Shopping;
        }
    }
}
