using System.Collections.Generic;
using Nop.Core.Domain.Common;

namespace Nop.Plugin.Misc.AbcExportOrder.Models
{
    public class YahooHeaderRowShipping : YahooHeaderRow
    {
        public YahooHeaderRowShipping(
            string prefix,
            int orderId,
            Address billingAddress,
            string stateAbbreviaton,
            string country,
            string decryptedCardName,
            string decryptedCardNumber,
            string decryptedExpirationMonth,
            string decryptedExpirationYear,
            string decryptedCvv2
        ) : base(
            's',
            prefix,
            orderId,
            billingAddress,
            stateAbbreviaton,
            country,
            decryptedCardName,
            decryptedCardNumber,
            decryptedExpirationMonth,
            decryptedExpirationYear,
            decryptedCvv2
        )
        {
            
        }
    }
}