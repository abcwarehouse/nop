using System.IO;
using System.Net;
using System.Web;
using Newtonsoft.Json;

namespace Nop.Plugin.Tax.AbcTax.TaxJar
{
    public class TaxJarRate
    {
        [JsonProperty(PropertyName = "country")]
        public string CountryCode { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string CountryName { get; set; }

        [JsonProperty(PropertyName = "standard_rate")]
        public string StandardRate { get; set; }

        [JsonProperty(PropertyName = "reduced_rate")]
        public string ReducedRate { get; set; }

        [JsonProperty(PropertyName = "super_reduced_rate")]
        public string SuperReducedRate { get; set; }

        [JsonProperty(PropertyName = "parking_rate")]
        public string ParkingRate { get; set; }

        [JsonProperty(PropertyName = "distance_sale_threshold")]
        public string DistanceSaleThreshold { get; set; }

        [JsonProperty(PropertyName = "freight_taxable")]
        public bool FreightTaxable { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "county")]
        public string County { get; set; }

        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        [JsonProperty(PropertyName = "zip")]
        public string ZipCode { get; set; }

        [JsonProperty(PropertyName = "state_rate")]
        public string StateRate { get; set; }

        [JsonProperty(PropertyName = "county_rate")]
        public string CountyRate { get; set; }

        [JsonProperty(PropertyName = "city_rate")]
        public string CityRate { get; set; }

        [JsonProperty(PropertyName = "combined_district_rate")]
        public string CombinedDistrictRate { get; set; }

        [JsonProperty(PropertyName = "combined_rate")]
        public string CombinedRate { get; set; }

        public bool IsUsCanada
        {
            get { return string.IsNullOrEmpty(CountryName); }
        }
        
        public decimal TaxRate
        {
            get
            {
                decimal rate;
                if (IsUsCanada)
                    decimal.TryParse(CombinedRate, out rate);
                else
                    decimal.TryParse(StandardRate, out rate);

                return rate;
            }
        }
    }
}
