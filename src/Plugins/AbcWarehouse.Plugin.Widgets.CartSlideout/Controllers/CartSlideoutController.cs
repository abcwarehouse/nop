using System.Linq;
using System.Threading.Tasks;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Models;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Controllers
{
    public class CartSlideoutController : BaseController
    {
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;

        public CartSlideoutController(
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IShoppingCartService shoppingCartService,
            IWorkContext workContext)
        {
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateShoppingCartItem([FromBody]UpdateShoppingCartItemModel model)
        {
            if (model == null || !model.IsValid())
            {
                return BadRequest();
            }

            var itemId = model.ShoppingCartItemId;
            var customer = await _workContext.GetCurrentCustomerAsync();

            // Get the item
            var shoppingCart = await _shoppingCartService.GetShoppingCartAsync(customer);
            var shoppingCartItem = shoppingCart.FirstOrDefault(sci => sci.Id == itemId);
            if (shoppingCartItem == null)
            {
                return BadRequest($"Unable to find shopping cart item with id {itemId}");
            }

            // Manipulate the attributes
            var productAttributeMapping = await _productAttributeService.GetProductAttributeMappingByIdAsync(
                model.ProductAttributeMappingId);
            shoppingCartItem.AttributesXml = model.IsChecked.Value ?
                await AddProductAttributeAsync(
                    productAttributeMapping,
                    shoppingCartItem.AttributesXml,
                    model.ProductAttributeValueId) :
                _productAttributeParser.RemoveProductAttribute(
                    shoppingCartItem.AttributesXml,
                    productAttributeMapping);

            // Update the item
            await _shoppingCartService.UpdateShoppingCartItemAsync(
                    customer,
                    shoppingCartItem.Id,
                    shoppingCartItem.AttributesXml,
                    shoppingCartItem.CustomerEnteredPrice,
                    shoppingCartItem.RentalStartDateUtc,
                    shoppingCartItem.RentalEndDateUtc,
                    shoppingCartItem.Quantity);

            return Json(new
            {
                SubtotalHtml = await RenderViewComponentToStringAsync("CartSlideoutSubtotal", new { sci = shoppingCartItem }),
            });
        }

        // Need to remove the existing mapped value if Delivery/Pickup
        // I think this will be the same with warranties and maybe pickup in store?
        private async Task<string> AddProductAttributeAsync(
            ProductAttributeMapping pam,
            string attributesXml,
            int productAttributeValueId)
        {
            var result = attributesXml;
            var productAttribute = await _productAttributeService.GetProductAttributeByIdAsync(pam.ProductAttributeId);
            if (productAttribute.Name == CartSlideoutConsts.DeliveryPickupOptions)
            {
                result = _productAttributeParser.RemoveProductAttribute(
                    result,
                    pam);
            }

            return _productAttributeParser.AddProductAttribute(
                    result,
                    pam,
                    productAttributeValueId.ToString());
        }
    }
}