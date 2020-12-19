using NUnit.Framework;
using Nop.Core.Domain.Common;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using FluentAssertions;

namespace Nop.Plugin.Misc.AbcExportOrder.Tests
{
    public class YahooHeaderRowTests
    {
        private YahooHeaderRowShipping _yahooHeaderRowShipping;
        private YahooHeaderRowPickup _yahooHeaderRowPickup;

        private readonly string _prefix = "abcware-";
        private readonly int _orderId = 1;
        private readonly Address _address = new Address();
        private readonly string _stateAbbreviation = "MI";
        private readonly string _country = "USA";
        private readonly string _cardName = "John Doe";
        private readonly string _cardNumber = "4111111111111111";
        private readonly string _cardMonth = "1";
        private readonly string _cardYear = "2025";
        private readonly string _cardCvv2 = "1234";

        [SetUp]
        public void Setup()
        {
            _yahooHeaderRowShipping = new YahooHeaderRowShipping(
                _prefix, _orderId, _address, _stateAbbreviation, _country, _cardName, _cardNumber, _cardMonth, _cardYear, _cardCvv2
            );
            _yahooHeaderRowPickup = new YahooHeaderRowPickup(
                _prefix, _orderId, _address, _stateAbbreviation, _country, _cardName, _cardNumber, _cardMonth, _cardYear, _cardCvv2
            );
        }


        [Test]
        public void Initializes_Correctly_Shipping()
        {
            _yahooHeaderRowShipping.Id.Should().Be($"{_prefix}{_orderId}+s");
            _yahooHeaderRowShipping.FullName.Should().Be($"{_address.FirstName} {_address.LastName}");
            _yahooHeaderRowShipping.FirstName.Should().Be(_address.FirstName);
            _yahooHeaderRowShipping.LastName.Should().Be(_address.LastName);
            _yahooHeaderRowShipping.Address1.Should().Be(_address.Address1);
            _yahooHeaderRowShipping.City.Should().Be(_address.City);
            _yahooHeaderRowShipping.State.Should().Be(_stateAbbreviation);
            _yahooHeaderRowShipping.Zip.Should().Be(_address.ZipPostalCode);
            _yahooHeaderRowShipping.Country.Should().Be(_country);
            _yahooHeaderRowShipping.Phone.Should().Be(_address.PhoneNumber);
            _yahooHeaderRowShipping.Email.Should().Be(_address.Email);
            _yahooHeaderRowShipping.CardName.Should().Be(_cardName);
            _yahooHeaderRowShipping.CardNumber.Should().Be(_cardNumber);
            _yahooHeaderRowShipping.CardExpiry.Should().Be($"{_cardMonth}/{_cardYear}");
            _yahooHeaderRowShipping.CardCvv2.Should().Be(_cardCvv2);
            // _yahooHeaderRowShipping.TaxCharge.Should().Be();
            // _yahooHeaderRowShipping.ShippingCharge.Should().Be($"{_prefix}{_orderId}+s");
            // _yahooHeaderRowShipping.Total.Should().Be($"{_prefix}{_orderId}+s");
            
            // _yahooHeaderRowShipping.Ip.Should().Be($"{_prefix}{_orderId}+s");
            // _yahooHeaderRowShipping.GiftCard.Should().Be($"{_prefix}{_orderId}+s");
            // _yahooHeaderRowShipping.GiftCardAmountUsed.Should().Be($"{_prefix}{_orderId}+s");
            // _yahooHeaderRowShipping.AuthCode.Should().Be($"{_prefix}{_orderId}+s");
            // _yahooHeaderRowShipping.HomeDeliveryCost.Should().Be($"{_prefix}{_orderId}+s");
            // _yahooHeaderRowShipping.CcRefNo.Should().Be($"{_prefix}{_orderId}+s");
        }

        [Test]
        public void Initializes_Correctly_Pickup()
        {
            // _yahooShipToRowPickup.Id.Should().Be($"{_prefix}{_orderId}+p");
        }

        [Test]
        public void Returns_String_Values_Shipping()
        {
            // var values = _yahooShipToRowShipping.ToStringValues();
            // values.Should().HaveCount(14);
        }

        [Test]
        public void Returns_String_Values_Pickup()
        {
            // var values = _yahooShipToRowPickup.ToStringValues();
            // values.Should().HaveCount(14);
        }
    }
}