using System.Collections.Generic;

namespace Nop.Plugin.Widgets.Bronto.Domain
{
    public class BrontoCart
    {
        public string Phase { get; set; }
        public string Currency { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public int? CustomerOrderId { get; set; }
        public string EmailAddress { get; set; }
        public string CartUrl { get; set; }
        public string MobilePhoneNumber { get; set; }
        public bool OrderSmsConsentChecked { get; set; }
        public IList<BrontoLineItem> LineItems { get; set; }
    }
}
