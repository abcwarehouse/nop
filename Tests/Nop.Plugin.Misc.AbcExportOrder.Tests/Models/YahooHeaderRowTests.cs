using NUnit.Framework;
using Nop.Core.Domain.Common;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using FluentAssertions;
using Nop.Core.Domain.Orders;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcExportOrder.Tests
{
    public class YahooHeaderRowTests
    {
        private YahooHeaderRowShipping _yahooHeaderRowShipping;
        private YahooHeaderRow _yahooHeaderRowPickup;

        private readonly string _prefix = "abcware-";
        private readonly Order _order = new Order()
        {
            Id = 1
        };
        private readonly List<OrderItem> _orderItems = new List<OrderItem>()
        {
            new OrderItem() {

            }
        };
        private readonly Address _address = new Address();
        private readonly string _stateAbbreviation = "MI";
        private readonly string _country = "USA";
        private readonly string _cardName = "John Doe";
        private readonly string _cardNumber = "4111111111111111";
        private readonly string _cardMonth = "1";
        private readonly string _cardYear = "2025";
        private readonly string _cardCvv2 = "1234";
        private readonly decimal _taxCharge = 10.00M;
        private readonly decimal _shippingCharge = 10.00M;
        private readonly decimal _homeDeliveryCharge = 10.00M;
        private readonly decimal _total = 10.00M;
        private readonly string _giftCard = "1234";
        private readonly decimal _giftCardAmtUsed = 10.00M;
        private readonly string _cardRefNo = "000000";

        [SetUp]
        public void Setup()
        {
            _yahooHeaderRowShipping = new YahooHeaderRowShipping(
                _prefix,
                _order,
                _address,
                _stateAbbreviation,
                _country,
                _cardName,
                _cardNumber,
                _cardMonth,
                _cardYear,
                _cardCvv2,
                _taxCharge,
                _shippingCharge,
                _homeDeliveryCharge,
                _total,
                _giftCard,
                _giftCardAmtUsed,
                _cardRefNo
            );

            _yahooHeaderRowPickup = new YahooHeaderRow(
                _prefix,
                _order,
                _address,
                _stateAbbreviation,
                _country,
                _cardName,
                _cardNumber,
                _cardMonth,
                _cardYear,
                _cardCvv2,
                _taxCharge,
                _total,
                _giftCard,
                _giftCardAmtUsed,
                _cardRefNo
            );
        }


        [Test]
        public void Initializes_Correctly_Shipping()
        {
            _yahooHeaderRowShipping.Id.Should().Be($"{_prefix}{_order.Id}+s");
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
            _yahooHeaderRowShipping.TaxCharge.Should().Be(_taxCharge);
            _yahooHeaderRowShipping.ShippingCharge.Should().Be(_shippingCharge);
            _yahooHeaderRowShipping.HomeDeliveryCharge.Should().Be(_homeDeliveryCharge);
            _yahooHeaderRowShipping.Total.Should().Be(_total);
            
            _yahooHeaderRowShipping.Ip.Should().Be(_order.CustomerIp);
            _yahooHeaderRowShipping.GiftCard.Should().Be(_giftCard);
            _yahooHeaderRowShipping.GiftCardAmountUsed.Should().Be(_giftCardAmtUsed);
        }

        [Test]
        public void Initializes_Correctly_Pickup()
        {
            _yahooHeaderRowPickup.Id.Should().Be($"{_prefix}{_order.Id}+p");
        }

        [Test]
        public void Returns_String_Values_Shipping()
        {
            var values = _yahooHeaderRowPickup.ToStringValues();
            values.Should().HaveCount(26);
        }

        [Test]
        public void Returns_String_Values_Pickup()
        {
            var values = _yahooHeaderRowPickup.ToStringValues();
            values.Should().HaveCount(26);
        }
    }
}