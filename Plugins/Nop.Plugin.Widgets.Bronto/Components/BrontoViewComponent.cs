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

namespace Nop.Plugin.Widgets.Bronto.Components
{
    public class BrontoViewComponent : NopViewComponent
    {
        private readonly BrontoSettings _settings;

        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;

        private readonly IShoppingCartModelFactory _shoppingCartModelFactory;

        private readonly ICustomerService _customerService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;

        public BrontoViewComponent(
            BrontoSettings settings,
            IWorkContext workContext,
            IStoreContext storeContext,
            IShoppingCartModelFactory shoppingCartModelFactory,
            ICustomerService customerService,
            IShoppingCartService shoppingCartService,
            IOrderService orderService,
            ILogger logger
        ) {
            _settings = settings;
            _workContext = workContext;
            _storeContext = storeContext;
            _shoppingCartModelFactory = shoppingCartModelFactory;
            _customerService = customerService;
            _shoppingCartService = shoppingCartService;
            _orderService = orderService;
            _logger = logger;
        }

        public IViewComponentResult Invoke(string widgetZone, object additionalData = null)
        {
            // Process Cart Recovery
            if (string.IsNullOrWhiteSpace(_settings.ScriptManagerCode))
            {
                _logger.Error("Widgets.Bronto: Script Manager code not provided, will not provide Bronto Cart Recovery integration.");
                return Content("");
            }

            var phase = DetermineBrontoPhase();

            var customer = _workContext.CurrentCustomer;
            var mobilePhoneNumber = _customerService.GetCustomerBillingAddress(customer)?.PhoneNumber;
            var cart = _shoppingCartService.GetShoppingCart(customer);

            var model = new BrontoModel()
            {
                ScriptManagerCode = _settings.ScriptManagerCode,
                BrontoCart = new BrontoCart()
                {
                    Phase = phase,
                    Currency = _workContext.WorkingCurrency.CurrencyCode,
                    EmailAddress = customer.Email ?? null,
                    CartUrl = _storeContext.CurrentStore.Url.TrimEnd('/') + "/cart",
                    MobilePhoneNumber = mobilePhoneNumber ?? "",
                    OrderSmsConsentChecked = false // currently not used
                }
            };

            // Populate values based on order status
            switch (phase) {
                case BrontoPhases.OrderComplete:
                    PopulateModelForOrderComplete(model);
                    break;
                default:
                    PopulateModelForShopping(cart, model);
                    break;
            }

            return View("~/Plugins/Widgets.Bronto/Views/Display.cshtml", model);
        }

        private void PopulateModelForShopping(IList<ShoppingCartItem> cart, BrontoModel model)
        {
            var orderTotalsModel = _shoppingCartModelFactory.PrepareOrderTotalsModel(cart, false);
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

            model.BrontoCart.LineItems = BrontoLineItem.FromCart(cart);
        }

        private void PopulateModelForOrderComplete(BrontoModel model)
        {
            var routeData = Url.ActionContext.RouteData;
            var orderId = Convert.ToInt32(routeData.Values["orderId"]);
            var order = _orderService.GetOrderById(orderId);
            model.BrontoCart.Subtotal = order.OrderSubtotalExclTax;
            model.BrontoCart.DiscountAmount = order.OrderSubTotalDiscountExclTax;
            model.BrontoCart.TaxAmount = order.OrderTax;
            model.BrontoCart.GrandTotal = order.OrderTotal;
            model.BrontoCart.CustomerOrderId = order.Id;
            model.BrontoCart.LineItems = BrontoLineItem.FromOrderItems(_orderService.GetOrderItems(order.Id));
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
