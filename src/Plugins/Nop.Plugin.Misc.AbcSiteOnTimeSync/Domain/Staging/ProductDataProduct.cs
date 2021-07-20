using System.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging
{
    public class ProductDataProduct
    {
        public int id { get; set; }
        public string SKU { get; set; }
        public int pkID { get; set; }
        public string Brand { get; set; }
        public string SeriesName { get; set; }
        public int pkBrand { get; set; }
        public string cgDescription { get; set; }
        public string CatDescription { get; set; }
        public int pkCategory { get; set; }
        public string ModelDescription { get; set; }
        public string CustomDescription { get; set; }
        public string StandardColor { get; set; }
        public string ColorDescription { get; set; }
        public string KeyFeature1 { get; set; }
        public string KeyFeature2 { get; set; }
        public string KeyFeature3 { get; set; }
        public string KeyFeature4 { get; set; }
        public string KeyFeature5 { get; set; }
        public float MSRP { get; set; }
        public float Sale { get; set; }
        public string ExpandedDescription { get; set; }
        public string thumb { get; set; }
        public string large { get; set; }
        public string RootModelNumber { get; set; }
        public string ModelStatus { get; set; }
        public string UPC { get; set; }
        public string Haw_Only { get; set; }

        public List<ProductDataProductDimension> Dimensions;

        public List<ProductDataProductFeature> Features;

        public List<ProductDataProductImage> Images;

        public List<ProductDataProductDownload> Downloads;

        public List<ProductDataProductFilter> Filters;

        public List<ProductDataProductpmap> pmaps;

        public List<ProductDataProductRelatedItem> RelatedItems;
    }

}
