using NUnit.Framework;
using Nop.Core.Domain.Common;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using FluentAssertions;

namespace Nop.Plugin.Misc.AbcExportOrder.Tests
{
    public class YahooShipToRowTests
    {
        private YahooShipToRowShipping _yahooShipToRowShipping;
        private YahooShipToRowPickup _yahooShipToRowPickup;

        private readonly Address _address = new Address()
        {
            FirstName = "John",
            LastName = "Doe"
        };

        private readonly string _stateAbbreviation = "MI";
        private readonly string _country = "USA";
        private readonly string _prefix = "abcware-";
        private readonly int _orderId = 1000;

        [SetUp]
        public void Setup()
        {
            _yahooShipToRowShipping = new YahooShipToRowShipping(
                _prefix, _orderId, _address, _stateAbbreviation, _country
            );

            _yahooShipToRowPickup = new YahooShipToRowPickup(
                _prefix, _orderId
            );
        }


        [Test]
        public void Initializes_Correctly_Shipping()
        {
            _yahooShipToRowShipping.Id.Should().Be($"{_prefix}{_orderId}+s");
            _yahooShipToRowShipping.FullName.Should().Be($"{_address.FirstName} {_address.LastName}");
            _yahooShipToRowShipping.FirstName.Should().Be(_address.FirstName);
            _yahooShipToRowShipping.LastName.Should().Be(_address.LastName);
            _yahooShipToRowShipping.Address1.Should().Be(_address.Address1);
            _yahooShipToRowShipping.Address2.Should().Be(_address.Address2);
            _yahooShipToRowShipping.City.Should().Be(_address.City);
            _yahooShipToRowShipping.State.Should().Be(_stateAbbreviation);
            _yahooShipToRowShipping.Zip.Should().Be(_address.ZipPostalCode);
            _yahooShipToRowShipping.Country.Should().Be(_country);
            _yahooShipToRowShipping.Phone.Should().Be(_address.PhoneNumber);
            _yahooShipToRowShipping.Email.Should().Be(_address.Email);
        }

        [Test]
        public void Initializes_Correctly_Pickup()
        {
            _yahooShipToRowPickup.Id.Should().Be($"{_prefix}{_orderId}+p");
        }

        [Test]
        public void Returns_String_Values_Shipping()
        {
            var values = _yahooShipToRowShipping.ToStringValues();
            values.Should().HaveCount(14);
        }

        [Test]
        public void Returns_String_Values_Pickup()
        {
            var values = _yahooShipToRowPickup.ToStringValues();
            values.Should().HaveCount(14);
        }
    }
}