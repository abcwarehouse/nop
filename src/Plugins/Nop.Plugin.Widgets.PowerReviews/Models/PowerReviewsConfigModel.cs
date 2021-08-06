using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Widgets.PowerReviews.Models
{
    public class PowerReviewsConfigModel
    {
        [NopResourceDisplayName(PowerReviewsLocales.APIKey)]
        public string APIKey { get; set; }

        [NopResourceDisplayName(PowerReviewsLocales.MerchantGroupId)]
        public string MerchantGroupId { get; set; }

        [NopResourceDisplayName(PowerReviewsLocales.MerchantId)]
        public string MerchantId { get; set; }
    }
}
