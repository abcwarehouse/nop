using Nop.Core.Configuration;
using Nop.Plugin.Widgets.PowerReviews.Models;

namespace Nop.Plugin.Widgets.PowerReviews
{
    public class PowerReviewsSettings : ISettings
    {
        public string APIKey { get; private set; }
        public string MerchantGroupId { get; private set; }
        public string MerchantId { get; private set; }

        public static PowerReviewsSettings FromModel(PowerReviewsConfigModel model)
        {
            return new PowerReviewsSettings()
            {
                APIKey = model.APIKey,
                MerchantGroupId = model.MerchantGroupId,
                MerchantId = model.MerchantId,
            };
        }

        public PowerReviewsConfigModel ToModel()
        {
            return new PowerReviewsConfigModel
            {
                APIKey = APIKey,
                MerchantGroupId = MerchantGroupId,
                MerchantId = MerchantId,
            };
        }

        public bool IsValid()
        {
            foreach (var property in this.GetType().GetProperties())
            {
                if (string.IsNullOrWhiteSpace((string)property.GetValue(this, null)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}