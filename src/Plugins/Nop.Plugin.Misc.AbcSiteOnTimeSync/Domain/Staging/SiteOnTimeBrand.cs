using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging
{
    public class SiteOnTimeBrand
    {
        public int Id { get; set; }

        public string CommonBrandName { get; set; }

        public Nullable<bool> Haw_Only { get; set; }

        public Nullable<bool> Download { get; set; }

    }

}
