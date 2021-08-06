using System.Data.SqlClient;
using System;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging
{
    public class ProductDataProductDownload
    {
        public int Id { get; set; }
        public string AST_URL_Txt { get; set; }
        public string AST_Role_Txt { get; set; }
        public int? ProductDataProduct_id { get; set; }
    }
}
