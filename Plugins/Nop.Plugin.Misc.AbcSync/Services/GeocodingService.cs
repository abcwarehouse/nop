using System;
using Nop.Core.Domain.Common;
using Nop.Plugin.Misc.AbcSync.Models;
using Nop.Core;
using System.Net;
using System.Xml.Linq;
using SevenSpikes.Nop.Plugins.StoreLocator.Domain;
using Nop.Services.Directory;

namespace Nop.Plugin.Misc.AbcSync.Services
{
    public class GoogleMapsGeocodingService : IGeocodingService
    {
        private readonly string ApiKey;

        private readonly StoreLocatorSettings _storeLocatorSettings;
        private readonly IStateProvinceService _stateProvinceService;

        public GoogleMapsGeocodingService(
            StoreLocatorSettings storeLocatorSettings,
            IStateProvinceService stateProvinceService
        )
        {
            _storeLocatorSettings = storeLocatorSettings;
            _stateProvinceService = stateProvinceService;

            ApiKey = _storeLocatorSettings.GoogleApiKey;
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                throw new NopException("No Google Maps API key provided, please provide an API key in Seven Spikes Store Locator to enable geocoding.");
            }
        }

        public Coordinate GeocodeAddress(Address address)
        {

            var stateProvince = _stateProvinceService.GetStateProvinceById(address.Id);
            string requestAddress =
                $"{address.Address1}, {address.City}, {stateProvince.Abbreviation}, {address.ZipPostalCode}";
            string requestUri = 
                string.Format(
                    "https://maps.googleapis.com/maps/api/geocode/xml?key={1}&address={0}&sensor=false",
                    Uri.EscapeDataString(requestAddress), ApiKey);

            WebRequest request = WebRequest.Create(requestUri);
            WebResponse response = request.GetResponse();
            XDocument xdoc = XDocument.Load(response.GetResponseStream());

            XElement geocodeResponse = xdoc.Element("GeocodeResponse");
            XElement status = geocodeResponse.Element("status");

            if (status.Value.Equals("REQUEST_DENIED"))
            {
                var errorMessage = geocodeResponse.Element("error_message").Value;

                throw new NopException($"Error returned while geocoding: {errorMessage}");
            }

            XElement result = geocodeResponse.Element("result");
            XElement locationElement = result.Element("geometry").Element("location");
            XElement lat = locationElement.Element("lat");
            XElement lng = locationElement.Element("lng");

            return new Coordinate(float.Parse(lat.Value), float.Parse(lng.Value));
        }
    }
}