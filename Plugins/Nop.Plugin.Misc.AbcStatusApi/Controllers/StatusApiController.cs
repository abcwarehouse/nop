using Microsoft.AspNetCore.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Web.Framework.Controllers;
using System;
using System.Linq;
using System.Net;

namespace Nop.Plugin.Misc.AbcStatusApi.Controllers
{
    public class StatusApiController : BaseController
    {
        ICustomerRegistrationService _customerRegistrationService;
        ICustomerService _customerService;
        IOrderService _orderService;
        IShipmentService _shipmentService;
        IAttributeUtilities _attributeUtilities;
        IProductService _productService;

        public StatusApiController(
            ICustomerRegistrationService customerRegistrationService,
            ICustomerService customerService,
            IOrderService orderService,
            IShipmentService shipmentService,
            IAttributeUtilities attributeUtilities,
            IProductService productService
            )
        {
            _customerRegistrationService = customerRegistrationService;
            _customerService = customerService;
            _orderService = orderService;
            _shipmentService = shipmentService;
            _attributeUtilities = attributeUtilities;
            _productService = productService;
        }

        public IActionResult SetStatus()
        {
            return Ok();
        }

        [HttpPost]
        public IActionResult SetStatus(string request)
        {
            // add a sequence number
            string email = Request.Query["email"];
            string password = Request.Query["password"];
            var loginResult = _customerRegistrationService.ValidateCustomer(email, password);

            if (loginResult == CustomerLoginResults.Successful)
            {
                Customer apiCustomer = _customerService.GetCustomerByEmail(email);
                if (_customerService.GetCustomerRoleById(apiCustomer.Id).SystemName == "Administrators")
                {
                    string statusId = Request.Query["id"];
                    string statusType = Request.Query["statusType"];
                    string statusValueStr = Request.Query["statusValue"];

                    // update the chosen status
                    bool updateSuccess;
                    string failureMessage = "403: Bad Request, ";

                    int nopOrderId = -1;

                    // id is in format <nopid>
                    Int32.TryParse(statusId, out nopOrderId);

                    // convert input status value -> int
                    int statusValue = 0;
                    if (statusType.Trim().ToLower() != "tracking")
                    {
                        statusValue = Int32.Parse(statusValueStr);
                    }

                    Order order = _orderService.GetOrderById(nopOrderId);
                    if (order != null)
                    {
                        if (statusType.Trim().ToLower() == "order")
                        {
                            // update order
                            updateSuccess = UpdateOrder(order, statusValue);

                            // failure message only used if update fails
                            // assign it every time in case of failure
                            failureMessage += $"status value {statusValueStr} not defined";
                        }
                        else if (statusType.Trim().ToLower() == "shipping")
                        {
                            // update shipping
                            updateSuccess = UpdateShipping(order, statusValue);
                            failureMessage += $"status value {statusValueStr} not defined";
                        }
                        else if (statusType.Trim().ToLower() == "payment")
                        {
                            // update payment status
                            updateSuccess = UpdatePayment(order, statusValue);
                            failureMessage += $"status value {statusValueStr} not defined";
                        }
                        else if (statusType.Trim().ToLower() == "tracking")
                        {
                            // update payment status 
                            updateSuccess = UpdateTracking(order, statusValueStr, Request.Query["itemSku"]);
                            failureMessage += $"status value {statusValueStr} not defined";
                        }
                        else
                        {
                            updateSuccess = false;
                            failureMessage += $"type {statusType} not defined";
                        }
                    }
                    else
                    {
                        updateSuccess = false;
                        failureMessage += statusId + " is not a valid order id in nopcommerce";
                    }



                    // choose what to return to the sender
                    if (updateSuccess)
                    {
                        return Ok();
                    }
                    else
                    {
                        // if the update wasn't successful, throw 400 Bad Request
                        return new BadRequestObjectResult(failureMessage);
                    }
                }
                else
                {
                    return Forbid();
                }
            }
            else
            {
                return Forbid();
            }
        }

        /// <summary>
        /// adds or updates the shipment attached to order with trackingNo to include the order item ith orderItemSku
        /// </summary>
        /// <param name="order"></param>
        /// <param name="trackingNo"></param>
        /// <param name="orderItemSku"></param>
        /// <returns></returns>
        private bool UpdateTracking(Order order, string trackingNo, string orderItemSku)
        {
            var shipment = _shipmentService.GetShipmentsByOrderId(order.Id)
                                           .Where(s => s.TrackingNumber == trackingNo)
                                           .FirstOrDefault();
            var orderItems = _orderService.GetOrderItems(order.Id)
                                          .Where(oi => _productService.GetProductById(oi.ProductId).Sku == orderItemSku)
                                          .ToList();

            // picking only non-pickup items
            OrderItem shippedOrderItem = null;
            foreach (OrderItem o in orderItems) {
                if (_attributeUtilities.GetPickupAttributeMapping(o.AttributesXml) == null)
                {
                    shippedOrderItem = o;
                }
            }

            if (shippedOrderItem == null)
            {
                return false;
            }

            if (shipment == null)
            {
                shipment = new Shipment { 
                    OrderId = order.Id, 
                    TrackingNumber = trackingNo, 
                    ShippedDateUtc = DateTime.UtcNow, 
                    CreatedOnUtc = DateTime.UtcNow
                };
                _shipmentService.InsertShipment(shipment);
            }

            //if the item is already in the shipment, do nothing
            var shipmentItems = _shipmentService.GetShipmentItemsByShipmentId(shipment.Id);
            if (shipmentItems.Any(si => si.OrderItemId == shippedOrderItem.Id))
            {
                return true;
            }
            else
            {
                _shipmentService.InsertShipmentItem(
                    new ShipmentItem {
                        ShipmentId = shipment.Id,
                        OrderItemId = shippedOrderItem.Id,
                        Quantity = shippedOrderItem.Quantity
                    });
            }

            return true;
        }

        /// <summary>
        /// updates an order with the given id & status
        /// </summary>
        /// <param name="nopOrderId"></param>
        /// <param name="statusValue"></param>
        /// <returns>successfully updated</returns>
        private bool UpdateOrder(Order order, int statusValue)
        {
            // get 
            if (Enum.IsDefined(typeof(OrderStatus), statusValue))
            {
                order.OrderStatus = (OrderStatus)statusValue;
                _orderService.UpdateOrder(order);
                return true;
            }
            return false;
        }

        /// <summary>
        /// update shipping status with given order & status
        /// </summary>
        /// <param name="order"></param>
        /// <param name="statusValue"></param>
        /// <returns>successfully updated</returns>
        private bool UpdateShipping(Order order, int statusValue)
        {
            if (Enum.IsDefined(typeof(ShippingStatus), statusValue))
            {
                order.ShippingStatus = (ShippingStatus)statusValue;
                _orderService.UpdateOrder(order);
                return true;
            }
            return false;
        }

        /// <summary>
        /// update payment status with given order & status
        /// </summary>
        /// <param name="order"></param>
        /// <param name="statusValue"></param>
        /// <returns>successfully updated</returns>
        private bool UpdatePayment(Order order, int statusValue)
        {
            if (Enum.IsDefined(typeof(PaymentStatus), statusValue))
            {
                order.PaymentStatus = (PaymentStatus)statusValue;
                _orderService.UpdateOrder(order);
                return true;
            }
            return false;
        }
    }
}