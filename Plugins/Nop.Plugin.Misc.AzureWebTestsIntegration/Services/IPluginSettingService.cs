using Nop.Plugin.Misc.AzureWebTestsIntegration.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AzureWebTestsIntegration.Services
{
    public interface IPluginSettingService
    {
        SettingsModel GetSettingsModel();
        void SaveSettingsModel(SettingsModel model);
    }
}
