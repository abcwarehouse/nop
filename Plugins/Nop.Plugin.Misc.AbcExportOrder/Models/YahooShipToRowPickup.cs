namespace Nop.Plugin.Misc.AbcExportOrder.Models
{
    public class YahooShipToRowPickup : YahooShipToRow
    {
        public YahooShipToRowPickup(
            string prefix,
            int orderId
        )
        {
            Id = $"{prefix}{orderId}+p";
        }
    }
}
