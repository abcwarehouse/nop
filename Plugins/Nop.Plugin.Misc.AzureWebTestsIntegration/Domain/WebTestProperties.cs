namespace Nop.Plugin.Misc.AzureWebTestsIntegration.Domain
{
    public class WebTestProperties
    {
        public string Name { get; set; }
        public string SyntheticMonitorId { get; set; }
        public WebTestConfiguration Configuration { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public int Frequency { get; set; }
        public int Timeout { get; set; }
        public string Kind { get; set; }
        public bool RetryEnabled { get; set; }
        public WebTestLocation[] Locations { get; set; }
    }
}