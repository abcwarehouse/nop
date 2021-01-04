using NUnit.Framework;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using FluentAssertions;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Misc.AbcExportOrder.Tests
{
    public class YahooDetailRowTests
    {

        private YahooDetailRow _yahooDetailRow;
        private YahooDetailRow _yahooDetailRowPickup;

        private OrderItem _orderItem = new OrderItem()
        {
            Id = 1,
            Quantity = 2
        };

        private OrderItem _orderItemPickup = new OrderItem()
        {
            Id = 1,
            AttributeDescription = "Pickup: ",
            Quantity = 3
        };

        private const string _prefix = "abcware-";
        private const string _itemId = "12345";
        private const string _itemCode = _itemId;
        private const string _itemDescription = "Testing description";
        private const string _url = "https://test.com";

        [SetUp]
        public void Setup()
        {
            _yahooDetailRow = new YahooDetailRow(
                _prefix,
                _orderItem,
                1,
                _itemId,
                _itemCode,
                0.00M,
                _itemDescription,
                _url,
                "A16"
            );

            _yahooDetailRowPickup = new YahooDetailRow(
                _prefix,
                _orderItemPickup,
                1,
                _itemId,
                "12344",
                0.00M,
                _itemDescription,
                _url,
                "A16"
            );
        }


        [Test]
        public void Initializes_Correctly()
        {
            _yahooDetailRow.Id.Should().Be($"{_prefix}{_orderItem.OrderId}+s");
            _yahooDetailRow.Code.Should().Be(_itemCode);
            _yahooDetailRow.Description.Should().Be(_itemDescription);
            _yahooDetailRow.ItemId.Should().Be(_itemId);
            _yahooDetailRow.LineNumber.Should().Be(1);
            _yahooDetailRow.Quantity.Should().Be(_orderItem.Quantity);
            _yahooDetailRow.UnitPrice.Should().Be(0.00M);
            _yahooDetailRow.Url.Should().Be(_url);

            _yahooDetailRowPickup.Id.Should().Be($"{_prefix}{_orderItem.OrderId}+p");
        }
    }
}