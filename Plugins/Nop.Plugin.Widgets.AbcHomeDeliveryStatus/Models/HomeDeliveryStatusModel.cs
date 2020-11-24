using Nop.Web.Framework.Models;

namespace Nop.Plugin.Widgets.AbcHomeDeliveryStatus.Models
{
    public class HomeDeliveryStatusModel : BaseNopModel
    {
        public string Invoice { get; set; }
        public string Zipcode { get; set; }

        public StatusInfo StatusInfo { get; set; }
    }
}
