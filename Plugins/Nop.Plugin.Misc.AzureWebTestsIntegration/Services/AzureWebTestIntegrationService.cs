using Newtonsoft.Json;
using Nop.Plugin.Misc.AzureWebTestsIntegration.Domain;
using Nop.Plugin.Misc.AzureWebTestsIntegration.DTOs;
using Nop.Plugin.Misc.AzureWebTestsIntegration.Models;
using Nop.Services.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration.Services
{
    public class AzureWebTestIntegrationService : IAzureWebTestIntegrationService
    {
        private readonly IPluginSettingService _pluginSettingService;
        private readonly SettingsModel _settings;

        public AzureWebTestIntegrationService(IPluginSettingService pluginSettingService)
        {
            _pluginSettingService = pluginSettingService;
            _settings = _pluginSettingService.GetSettingsModel();
        }

        public void DisableWebTest(string subscriptionId, string resourceGroup, string webTestName)
        {
            ValidateSettings();

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentException("Subscription ID provided is not valid.");
            }
            if (string.IsNullOrWhiteSpace(resourceGroup))
            {
                throw new ArgumentException("Resource group provided is not valid.");
            }
            if (string.IsNullOrWhiteSpace(webTestName))
            {
                throw new ArgumentException("Web test name provided is not valid.");
            }

            var token = GetBearerToken();
            UpdateWebTest(subscriptionId, resourceGroup, webTestName, token, false);
        }

        public void EnableWebTest(string subscriptionId, string resourceGroup, string webTestName)
        {
            ValidateSettings();

            if (string.IsNullOrWhiteSpace(subscriptionId))
            {
                throw new ArgumentException("Subscription ID provided is not valid.");
            }
            if (string.IsNullOrWhiteSpace(resourceGroup))
            {
                throw new ArgumentException("Resource group provided is not valid.");
            }
            if (string.IsNullOrWhiteSpace(webTestName))
            {
                throw new ArgumentException("Web test name provided is not valid.");
            }

            var token = GetBearerToken();
            UpdateWebTest(subscriptionId, resourceGroup, webTestName, token, true);
        }

        public AzureWebTest[] GetWebTests(string subscriptionId)
        {
            ValidateSettings();
            var token = GetBearerToken();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = client.GetAsync($"https://management.azure.com/subscriptions/{subscriptionId}/providers/Microsoft.Insights/webtests?api-version=2015-05-01").GetAwaiter().GetResult();

                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    throw new HttpRequestException($"Error when getting Web Tests: {errorResponse.error.message}");
                }

                var responseObject = JsonConvert.DeserializeObject<GetWebTestsResponse>(responseBody);
                return responseObject.Value;
            }
        }

        private void UpdateWebTest(string subscriptionId, string resourceGroup, string webTestName, string token, bool isWebTestEnabled)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var azureWebTests = GetWebTests(subscriptionId);
                var azureWebTest = azureWebTests.FirstOrDefault(wt => wt.Properties.Name == webTestName);

                if (azureWebTest == null)
                {
                    throw new HttpRequestException($"Error when updating the Web Test: unable to find web test '{webTestName}'");
                }

                azureWebTest.Properties.Enabled = isWebTestEnabled;
                azureWebTest.Properties.Configuration.WebTest = azureWebTest.Properties.Configuration.WebTest.Replace("Enabled=\"True\"", $"Enabled=\"{isWebTestEnabled}\"");
                azureWebTest.Properties.Configuration.WebTest = azureWebTest.Properties.Configuration.WebTest.Replace("Enabled=\"False\"", $"Enabled=\"{isWebTestEnabled}\"");

                var response = client.PutAsync(
                    $"https://management.azure.com/subscriptions/{subscriptionId}/resourceGroups/{resourceGroup}/providers/Microsoft.Insights/webtests/{azureWebTest.Properties.SyntheticMonitorId}?api-version=2015-05-01",
                    new StringContent(azureWebTest.ToJson(), Encoding.UTF8, "application/json")).GetAwaiter().GetResult();

                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error when updating the Web Test: {responseObject.error.message}");
                }
            }
        }

        private void ValidateSettings()
        {
            if (!_settings.IsValid)
            {
                throw new ConfigurationErrorsException(
                    "Unable to initialize, plugin is not configured with Azure service principal correctly. Please add the Client ID, Client Secret, and Tenant ID.");
            }
        }

        private string GetBearerToken()
        {
            using (HttpClient client = new HttpClient())
            {
                var formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", _settings.ClientId),
                    new KeyValuePair<string, string>("client_secret", _settings.ClientSecret),
                    new KeyValuePair<string, string>("resource", "https://management.azure.com")
                });

                var response = client.PostAsync($"https://login.microsoftonline.com/{_settings.TenantId}/oauth2/token", formContent).GetAwaiter().GetResult();

                string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Error when authenticating Azure service principal: {responseObject.error_description}");
                }

                return responseObject.access_token;
            }
        }
    }
}
