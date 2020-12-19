using System.Collections.Generic;
using Nop.Core.Domain.Common;

namespace Nop.Plugin.Misc.AbcExportOrder.Models
{
    public abstract class YahooHeaderRow
    {
        public string Id { get; protected set; }
        public string Datestamp { get; protected set; }
        public string FullName { get; protected set; }
        public string FirstName { get; protected set; }
        public string LastName { get; protected set; }
        public string Address1 { get; protected set; }
        public string Address2 { get; protected set; }
        public string City { get; protected set; }
        public string State { get; protected set; }
        public string Zip { get; protected set; }
        public string Country { get; protected set; }
        public string Phone { get; protected set; }
        public string Email { get; protected set; }
        public string CardName { get; protected set; }
        public string CardNumber { get; protected set; }
        public string CardExpiry { get; protected set; }
        public string CardCvv2 { get; protected set; }
        public decimal TaxCharge { get; protected set; }
        public decimal ShippingCharge { get; protected set; }
        public decimal Total { get; protected set; }
        public string GiftCard { get; protected set; }
        public decimal GiftCardAmountUsed { get; protected set; }
        public string AuthCode { get; protected set; }
        public string HomeDeliveryCost { get; protected set; }
        public string CcRefNo { get; protected set; }

        public YahooHeaderRow(
            char orderTypeFlag,
            string prefix,
            int orderId,
            Address billingAddress,
            string stateAbbreviation,
            string country,
            string decryptedCardName,
            string decryptedCardNumber,
            string decryptedExpirationMonth,
            string decryptedExpirationYear,
            string decryptedCvv2
        )
        {
            Id = $"{prefix}{orderId}+{orderTypeFlag}";
            FullName = $"{billingAddress.FirstName} {billingAddress.LastName}";
            FirstName = billingAddress.FirstName;
            LastName = billingAddress.LastName;
            Address1 = billingAddress.Address1;
            Address2 = billingAddress.Address2;
            City = billingAddress.City;
            State = stateAbbreviation;
            Zip = billingAddress.ZipPostalCode;
            Country = country;
            Phone = billingAddress.PhoneNumber;
            Email = billingAddress.Email;
            CardName = decryptedCardName;
            CardNumber = decryptedCardNumber;
            CardExpiry = $"{decryptedExpirationMonth}/{decryptedExpirationYear}";
            CardCvv2 = decryptedCvv2;
        }
    }
}