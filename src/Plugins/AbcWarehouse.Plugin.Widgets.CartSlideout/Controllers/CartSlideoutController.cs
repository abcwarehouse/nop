using System.Linq;
using System.Threading.Tasks;
using AbcWarehouse.Plugin.Widgets.CartSlideout.Models;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Controllers
{
    public class CartSlideoutController : BaseController
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IWorkContext _workContext;

        public CartSlideoutController(
            IShoppingCartService shoppingCartService,
            IWorkContext workContext)
        {
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
            // TODO
            // This will use ProductAttributeParser.AddProductAttribute in some way

            // Update the item
            await _shoppingCartService.UpdateShoppingCartItemAsync(
                    customer,
                    shoppingCartItem.Id,
                    shoppingCartItem.AttributesXml,
                    shoppingCartItem.CustomerEnteredPrice,
                    shoppingCartItem.RentalStartDateUtc,
                    shoppingCartItem.RentalEndDateUtc,
                    shoppingCartItem.Quantity);

            // Get the unit price to pass back as a response
            // Should consider putting this in the Subtotal
            // ViewComponent
            var unitPrice = await _shoppingCartService.GetUnitPriceAsync(shoppingCartItem, false);

            return Json(new
            {
                SubtotalHtml = await RenderViewComponentToStringAsync("CartSlideoutSubtotal", new { price = unitPrice.unitPrice }),
            });
        }
    }
}