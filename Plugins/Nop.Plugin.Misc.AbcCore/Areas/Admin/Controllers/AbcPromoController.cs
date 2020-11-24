using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AbcCore.Areas.Admin.Models;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Framework.Models.Extensions;
using System.Linq;
using Nop.Services.Catalog;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Services.Seo;

namespace Nop.Plugin.Misc.AbcCore.Areas.Admin.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
	public class AbcPromoController : BaseAdminController
	{
		private readonly IAbcPromoService _abcPromoService;
		private readonly IManufacturerService _manufacturerService;
		private readonly IProductAbcDescriptionService _productAbcDescriptionService;
		private readonly IUrlRecordService _urlRecordService;

		public AbcPromoController(
            IAbcPromoService abcPromoService,
			IManufacturerService manufacturerService,
			IProductAbcDescriptionService productAbcDescriptionService,
			IUrlRecordService urlRecordService
		)
		{
			_abcPromoService = abcPromoService;
			_manufacturerService = manufacturerService;
			_productAbcDescriptionService = productAbcDescriptionService;
			_urlRecordService = urlRecordService;
		}

		public IActionResult List()
		{
			return View(
				"~/Plugins/Misc.AbcCore/Areas/Admin/Views/AbcPromo/List.cshtml",
				new AbcPromoSearchModel()
			);
		}

		[HttpPost]
        public virtual IActionResult List(AbcPromoSearchModel searchModel)
        {
			var promos  = _abcPromoService.GetAllPromos().ToPagedList(searchModel);
			var model = new AbcPromoListModel().PrepareToGrid(searchModel, promos, () =>
            {
                //fill in model values from the entity
                return promos.Select(promo =>
                {
                    //fill in model values from the entity
					var manufacturerName =
						promo.ManufacturerId.HasValue ?
							_manufacturerService.GetManufacturerById(promo.ManufacturerId.Value).Name :
							"";

					var slug = _urlRecordService.GetActiveSlug(promo.Id, "AbcPromo", 0);

                   	var abcPromoModel = new AbcPromoModel()
					{
						Id = promo.Id,
						Name = promo.Name,
						Description = promo.Description,
						StartDate = promo.StartDate,
						EndDate = promo.EndDate,
						IsActive = promo.IsActive(),
						Manufacturer = manufacturerName,
						Slug = slug,
						ProductCount = _abcPromoService.GetProductsByPromoId(promo.Id).Count()
					};

					return abcPromoModel;
                });
            });

            return Json(model);
        }

		public IActionResult Products(int abcPromoId)
		{
			var abcPromo = _abcPromoService.GetPromoById(abcPromoId);

			var model = new AbcPromoProductSearchModel()
			{
				AbcPromoId = abcPromo.Id,
				AbcPromoName = abcPromo.Name
			};

			return View(
				"~/Plugins/Misc.AbcCore/Areas/Admin/Views/AbcPromo/Products.cshtml",
				model
			);
		}

		[HttpPost]
        public virtual IActionResult Products(AbcPromoProductSearchModel searchModel)
        {
			var products  = _abcPromoService.GetProductsByPromoId(searchModel.AbcPromoId)
											.ToPagedList(searchModel);
			var model = new AbcPromoProductListModel().PrepareToGrid(searchModel, products, () =>
            {
                return products.Select(product =>
                {
					var productAbcDescription =
						_productAbcDescriptionService.GetProductAbcDescriptionByProductId(product.Id);
					var abcItemNumber = productAbcDescription?.AbcItemNumber;

                   	var abcPromoProductModel = new AbcPromoProductModel()
					{
						AbcItemNumber = abcItemNumber,
						Name = product.Name,
						Published = product.Published
					};

					return abcPromoProductModel;
                });
            });

            return Json(model);
        }
	}
}
