using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Html;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Seo;

namespace Nop.Plugin.Widgets.Bronto.Domain
{
    public class BrontoLineItem
    {
        public string Sku { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Category { get; private set; }
        public string Other { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal SalePrice { get; private set; }
        public int Quantity { get; private set; }
        public decimal TotalPrice { get; private set; }
        public string ImageUrl { get; private set; }
        public string ProductUrl { get; private set; }
    }
}
