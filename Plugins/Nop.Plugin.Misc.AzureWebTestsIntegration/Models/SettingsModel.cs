using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration.Models
{
    public class SettingsModel
    {
        [NopResourceDisplayName("Client ID")]
        public string ClientId { get; set; }

        [NopResourceDisplayName("Client Secret")]
        public string ClientSecret { get; set; }

        [NopResourceDisplayName("Tenant ID")]
        public string TenantId { get; set; }

        public bool IsValid
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ClientId) &&
                       !string.IsNullOrWhiteSpace(ClientSecret) &&
                       !string.IsNullOrWhiteSpace(TenantId);
            }
        }
    }
}
