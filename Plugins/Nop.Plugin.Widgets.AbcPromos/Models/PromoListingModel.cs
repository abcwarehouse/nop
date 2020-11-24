using System.Collections.Generic;
using Nop.Web.Framework.Models;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.AbcPromos.Models
{
    public partial class PromoListingModel : BaseNopEntityModel
    {
        public string Name { get; set; }
        public IList<ProductOverviewModel> Products { get; set; }
        public CatalogPagingFilteringModel PagingFilteringContext { get; set; }
        public string BannerImageUrl { get; set; }
        public string PromoFormPopup { get; set; }
    }
}