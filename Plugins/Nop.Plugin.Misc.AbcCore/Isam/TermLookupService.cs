using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Core.Domain.Logging;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public class TermLookupService : ITermLookupService
    {
        private readonly IProductService _productService;
        private readonly ILogger _logger;
        private readonly CoreSettings _settings;

        public TermLookupService(
            IProductService productService,
            ILogger logger,
            CoreSettings settings
        )
        {
            _productService = productService;
            _logger = logger;
            _settings = settings;
        }

        public (string termNo, string description, string link) GetTerm(
            IList<ShoppingCartItem> cart
        )
        {
            if (_settings.AreExternalCallsSkipped)
            {
                _logger.Warning("External calls are turned off, term lookup skipped.");
                return (null, null, null);
            }

            string nl = Environment.NewLine;
            string xml = "";
            xml = $"<Request>{nl}<Term_Lookup>{nl}<Items>";
            foreach (var item in cart)
            {
                var product = _productService.GetProductById(item.ProductId);
                xml += $"{nl}<Item>{nl}<Sku>{product.Sku}</Sku>{nl}" +
                       $"<Gtin>{product.Gtin}</Gtin>{nl}" +
                       $"<Qty>{item.Quantity}</Qty>{nl}" +
                       $"<Brand>{product.Name}</Brand>{nl}" +
                       $"<Price>{product.Price}</Price>{nl}</Item>";
            }
            xml += $"{nl}</Items>{nl}</Term_Lookup>{nl}</Request>";

            if (_settings.IsDebugMode)
            {
                _logger.InsertLog(
                    LogLevel.Information,
                    "Term Lookup Request",
                    xml
                );
            }

            var webRequest = HttpWebRequest.CreateHttp(AbcConstants.StatusAPIUrl);
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml; charset=utf-8";

            byte[] byteArray = Encoding.UTF8.GetBytes(xml);
            webRequest.ContentLength = byteArray.Length;
            using (System.IO.Stream requestStream = webRequest.GetRequestStream())
            {
                requestStream.Write(byteArray, 0, byteArray.Length);
            }
            WebResponse response = null;
            try
            {
                response = webRequest.GetResponse();
            }
            catch (WebException e)
            {
                throw new IsamException($"Error when connecting to Term Lookup API: {e.Message}");
            }
            Stream r_stream = response.GetResponseStream();

            using (StreamReader reader = new StreamReader(r_stream))
            {
                string strResponse = reader.ReadToEnd();

                if (_settings.IsDebugMode)
                {
                    _logger.InsertLog(
                        LogLevel.Information,
                        "Term Lookup Response",
                        strResponse
                    );
                }

                if (string.IsNullOrEmpty(strResponse))
                {
                    throw new IsamException(
                        "Empty response received from ISAM backend during Term Lookup."
                    );
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(strResponse);

                var termNo = xmlDoc.SelectSingleNode("Response/Term_Lookup/Term_No");
                var description = xmlDoc.SelectSingleNode("Response/Term_Lookup/Description");
                var link = xmlDoc.SelectSingleNode("Response/Term_Lookup/Link");

                return (termNo?.InnerText, description?.InnerText, link?.InnerText);
            }
        }
    }
}