using NUnit.Framework;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using FluentAssertions;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.AbcExportOrder.Extensions;

namespace Nop.Plugin.Misc.AbcExportOrder.Tests
{
    public class OrderItemExtensionsTests
    {
        private OrderItem _orderItem = new OrderItem();
        private OrderItem _orderItemWithMattressSize = new OrderItem()
        {
            AttributeDescription = "Mattress Size: Twin [+$297.00]<br />"
        };

        private OrderItem _orderItemWithBase = new OrderItem()
        {
            AttributeDescription = "Base (Twin): Ease 3.0 [+$297.00]<br />"
        };

        private OrderItem _orderItemWithFreeGift = new OrderItem()
        {
            AttributeDescription = "Free Gift: FREE-55\"-4K-SMART-TV<br />"
        };
        private OrderItem _orderItemWithFreeGift2 = new OrderItem()
        {
            AttributeDescription = "Free Gift: FREE-55&quot;-4K-SMART-TV<br />"
        };
        private OrderItem _orderItemWithMattressProtector = new OrderItem()
        {
            AttributeDescription = "Mattress Protector (Twin): Good [+$80.00]<br />"
        };


        [Test]
        public void Returns_Mattress_Size()
        {
            _orderItem.GetMattressSize().Should().BeNull();
            _orderItemWithMattressSize.GetMattressSize().Should().Be("Twin");
        }

        [Test]
        public void Returns_Base()
        {
            _orderItem.GetBase().Should().BeNull();
            _orderItemWithBase.GetBase().Should().Be("Ease 3.0");
        }

        [Test]
        public void Returns_Free_Gift()
        {
            _orderItem.GetBase().Should().BeNull();
            _orderItemWithFreeGift.GetFreeGift().Should().Be("FREE-55\"-4K-SMART-TV");
            _orderItemWithFreeGift2.GetFreeGift().Should().Be("FREE-55\"-4K-SMART-TV");
        }

        [Test]
        public void Returns_Mattress_Protector()
        {
            _orderItem.GetMattressProtector().Should().BeNull();
            _orderItemWithMattressProtector.GetMattressProtector().Should().Be("Good");
        }
    }
}