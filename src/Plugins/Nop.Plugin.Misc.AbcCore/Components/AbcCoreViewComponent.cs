using Nop.Core;
using Nop.Core.Domain.Messages;
using Nop.Services.Logging;
using Nop.Services.Messages;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Data;
using Nop.Web.Framework.Components;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Core.Domain.Security;
using Nop.Services.Customers;
using Nop.Services.Catalog;
using System.IO;
using Nop.Plugin.Misc.AbcCore;
using Nop.Web.Models.Catalog;
using System.Threading.Tasks;
using Nop.Services.Common;
using Nop.Core.Domain.Catalog;
using Nop.Web.Areas.Admin.Models.Catalog;
using Nop.Plugin.Misc.AbcCore.Models;

namespace Nop.Plugin.Misc.AbcCore.Components
{
    [ViewComponent(Name = "AbcCore")]
    public class AbcCoreViewComponent : NopViewComponent
    {
        private readonly IGenericAttributeService _genericAttributeService;

        public AbcCoreViewComponent(
            IGenericAttributeService genericAttributeService
        ) {
            _genericAttributeService = genericAttributeService;
        }

        public async Task<IViewComponentResult> InvokeAsync(string widgetZone, ProductModel additionalData = null)
        {
            var productId = additionalData.Id;
            var plpDescription = await _genericAttributeService.GetAttributeAsync<Product, string>(
                productId, "PLPDescription"
            );

            var model = new ABCProductDetailsModel
            {
                ProductId = productId,
                PLPDescription = plpDescription
            };

            return View("~/Plugins/Misc.AbcCore/Views/ProductDetails.cshtml", model);
        }

    }
}
