﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;

// 
// This source code was auto-generated by xsd, Version=4.0.30319.33440.
// 
namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging
{
    public partial class CJsonRoot
    {
        public CJsonCollection collection { get; set; }
    }

    public partial class CJsonCollection
    {
        public string version { get; set; }
        public string href { get; set; }
        public List<CJsonItem> items { get; set; }
        public List<CJsonLink> links { get; set; }
        //public string template { get; set; }
    }

    public partial class CJsonItem
    {
        public string href { get; set; }
        public List<CJsonData> data { get; set; }
        public List<CJsonLink> links { get; set; }
        public string characterization { get; set; }
    }

    public partial class CJsonData
    {
        public string id { get; set; }
        public int oldDbId { get; set; }
        public int oldDbProductId { get; set; }
        public string name { get; set; }
        public string value { get; set; }
        public string prompt { get; set; }
    }

    public partial class CJsonLink
    {
        public string rel { get; set; }
        public string href { get; set; }
        public string render { get; set; }
    }

}
