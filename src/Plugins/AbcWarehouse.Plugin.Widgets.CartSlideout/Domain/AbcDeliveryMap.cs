/* This table is populated from John */

using Nop.Core;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout.Domain
{
    public class AbcDeliveryMap : BaseEntity
    {
        // We exclude FedEx, since Delivery Options doesn't handle
        private readonly int _fedex = 90085;

        public int CategoryId { get; set; }

        public int DeliveryOnly { get; set; }

        public int DeliveryInstall { get; set; }

        public int DeliveryHaulway { get; set; }

        public int DeliveryHaulwayInstall { get; set; }

        public bool HasDeliveryOptions()
        {
            return (DeliveryOnly != 0 && DeliveryOnly != _fedex) ||
                   DeliveryInstall != 0 ||
                   DeliveryHaulway != 0 ||
                   DeliveryHaulwayInstall != 0;
        }
    }
}