using Nop.Core;
using Nop.Core.Domain.Tasks;
using Nop.Data;
using Nop.Plugin.Misc.AbcSync.Data;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync
{
    public class AbcSiteOnTimeSyncPlugin : BasePlugin, IMiscPlugin
    {
        public static class LocaleKey
        {
            public const string Base = "Plugins.Misc.AbcSiteOnTimeSync.Fields.";
            public const string CmicApiBrandUrl = Base + "CmicApiBrandUrl";
            public const string CmicApiBrandUrlHint = CmicApiBrandUrl + ".Hint";
            public const string CmicApiUsername = Base + "CmicApiUsername";
            public const string CmicApiUsernameHint = CmicApiUsername + ".Hint";
            public const string CmicApiPassword = Base + "CmicApiPassword";
            public const string CmicApiPasswordHint = CmicApiPassword + ".Hint";
        }

        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly IRepository<ScheduleTask> _scheduleTaskRepository;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly StagingDb _stagingDb;

        private readonly string TaskType =
            $"{typeof(SyncSiteOnTimeTask).Namespace}.{typeof(SyncSiteOnTimeTask).Name}, " +
            $"{typeof(SyncSiteOnTimeTask).Assembly.GetName().Name}";

        public AbcSiteOnTimeSyncPlugin(
            IScheduleTaskService scheduleTaskService,
            IRepository<ScheduleTask> scheduleTaskRepository,
            ISettingService settingService,
            IWebHelper webHelper,
            ILocalizationService localizationService,
            StagingDb stagingDb
        )
        {
            _scheduleTaskService = scheduleTaskService;
            _scheduleTaskRepository = scheduleTaskRepository;
            _settingService = settingService;
            _webHelper = webHelper;
            _localizationService = localizationService;
            _stagingDb = stagingDb;
        }

        public override async System.Threading.Tasks.Task InstallAsync()
        {
            await RemoveTasksAsync();
            await AddTaskAsync();
            CreateSiteOnTimeBrandTable();
            ClearPDPTables();

            await _settingService.SaveSettingAsync(AbcSiteOnTimeSyncSettings.Default());

            await _localizationService.AddLocaleResourceAsync(
            new Dictionary<string, string>
                {
                    [LocaleKey.CmicApiBrandUrl] = "CMIC API Brand URL",
                    [LocaleKey.CmicApiBrandUrlHint] = "The API URL specific to getting brands.",
                    [LocaleKey.CmicApiPassword] = "CMIC API Password",
                    [LocaleKey.CmicApiPasswordHint] = "Password to access CMIC API.",
                    [LocaleKey.CmicApiUsername] = "CMIC API Username",
                    [LocaleKey.CmicApiUsernameHint] = "Username to access CMIC API.",
                }
            );

            await base.InstallAsync();
        }

        public override async System.Threading.Tasks.Task UninstallAsync()
        {
            await RemoveTasksAsync();
            DeleteSiteOnTimeBrandTable();
            ClearPDPTables();

            await _localizationService.DeleteLocaleResourcesAsync(LocaleKey.Base);

            await base.UninstallAsync();
        }

        public override string GetConfigurationPageUrl()
        {
            return
                $"{_webHelper.GetStoreLocation()}Admin/AbcSiteOnTimeSync/Configure";
        }

        private void CreateSiteOnTimeBrandTable()
        {
            DeleteSiteOnTimeBrandTable();
            var schemaCommand = @"
                CREATE TABLE [dbo].[SiteOnTimeBrand](
                    [Id] [int] NOT NULL,
                    [CommonBrandName] [nvarchar](400) NOT NULL,
                    [Haw_Only] [bit] NULL,
                    [Download] [bit] NULL,
                    CONSTRAINT [PK_SiteOnTimeBrand] PRIMARY KEY CLUSTERED 
                    (
                        [Id] ASC
                    )
                )";
            _stagingDb.ExecuteNonQuery(schemaCommand);

            // For the safe of completion I'll populate data, but thsi should really be in NOPCommerce
            var seedCommand = @"
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (9, N'Amana', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (18, N'Asko', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (20, N'Avanti', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (22, N'Bell''O', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (25, N'Bosch', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (27, N'Bose', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (28, N'Broan', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (35, N'Coby', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (42, N'Dacor', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (43, N'Daewoo', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (44, N'Danby', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (45, N'DCS', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (56, N'Electrolux', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (57, N'Elite', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (61, N'Estate', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (62, N'Eureka', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (64, N'Faber', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (67, N'Fisher & Paykel', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (69, N'Friedrich', 0, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (70, N'Frigidaire', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (71, N'Frigidaire AC', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (79, N'Gladiator', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (85, N'Haier', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (86, N'Haier Electronics', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (91, N'Hitachi', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (93, N'Harman Kardon', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (94, N'Hoover', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (95, N'Hotpoint', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (98, N'Electrolux Icon', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (100, N'Insinkerator', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (102, N'JBL', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (103, N'JENN-AIR', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (106, N'JVC', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (110, N'KitchenAid', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (114, N'Klipsch', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (118, N'Ashley Furniture', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (123, N'LG Appliances', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (124, N'LG Electronics', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (130, N'Magnavox', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (133, N'Marvel', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (135, N'Maytag', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (139, N'Miele', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (141, N'Mitsubishi', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (143, N'GE Monogram', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (144, N'Monster', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (154, N'Omni Mount', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (155, N'Onkyo', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (156, N'Oreck', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (157, N'Panasonic', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (159, N'Panasonic', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (162, N'Peerless A/V', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (163, N'Philips', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (164, N'Pioneer', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (165, N'Pioneer Elite', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (169, N'Polaroid', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (174, N'Premier', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (183, N'RCA', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (184, N'RCA Audio/Video', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (190, N'Samsung Electronics', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (191, N'Samsung Appliances', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (193, N'Sanus', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (196, N'Scotsman', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (198, N'Sharp Electronics', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (199, N'Sharp Appliances', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (208, N'Sony', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (213, N'Subzero', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (221, N'TechCraft', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (222, N'Tempur-Pedic', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (224, N'Thermador', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (226, N'Toshiba', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (232, N'Uline', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (235, N'Ventahood', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (236, N'ViewSonic', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (237, N'Viking', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (239, N'Waste King', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (241, N'Weber', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (243, N'White-Westinghouse', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (245, N'Whirlpool', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (250, N'Wolf', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (252, N'Yamaha', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (260, N'JVC Procision', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (267, N'GE', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (271, N'Integra', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (280, N'Zephyr', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (281, N'Sansui', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (297, N'Best', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (326, N'Sealy', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (335, N'Gaggenau', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (341, N'Chief Manufacturing', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (355, N'Sennheiser', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (356, N'Universal Remote', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (360, N'LG Air Conditioners', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (361, N'Paradigm', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (369, N'Diamond', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (370, N'Danby', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (371, N'Big Green Egg', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (392, N'Bertazzoni', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (394, N'LG Studio', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (397, N'SPEED QUEEN', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (406, N'Liebherr', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (407, N'Bellagio by Serta', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (408, N'SmartChoice by Serta', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (416, N'Capital', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (417, N'Atlantic Technology', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (418, N'Energy', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (420, N'Mirage', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (421, N'Boston Acoustics', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (422, N'Anthem', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (423, N'Crosley', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (424, N'EdenPURE', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (425, N'i COMFORT', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (426, N'Blomberg', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (427, N'Viking D3', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (428, N'Denon', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (429, N'Sonos', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (430, N'Plateau', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (431, N'Alfresco Grills', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (432, N'Traeger Grills', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (433, N'Martin Logan', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (434, N'Sunfire', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (435, N'Epson', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (436, N'Polk Audio', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (437, N'URC', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (438, N'Revel Speakers', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (439, N'SunBrite TV', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (440, N'Marantz', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (441, N'Synthesis', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (442, N'Niles Audio', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (444, N'Haier', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (453, N'AudioSource', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (460, N'AGA', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (461, N'Heartland', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (464, N'Electrolux', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (465, N'Frigidaire Small App', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (466, N'BeautySleep', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (467, N'Beautyrest Recharge', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (468, N'Beautyrest WC', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (469, N'ComforPedic', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (470, N'Beautyrest TruEnergy', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (471, N'Beautyrest Black', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (472, N'Posturepedic', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (473, N'Optimum', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (475, N'iComfort Directions', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (476, N'Perfect Elements', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (477, N'BlueStar', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (478, N'Kenmore', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (479, N'Kenmore Elite', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (481, N'SEIKI', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (482, N'Sherwood', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (483, N'Perfect Sleeper', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (484, N'Trump Home iSeries', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (485, N'Evo Sleep by Sherwoo', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (486, N'Lumina by Sherwood', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (487, N'Encore by Sherwood', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (488, N'IAmerica', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (489, N'Napoleon Grills', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (490, N'iSeries', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (491, N'Elica', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (492, N'Elmira', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (493, N'SMEG', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (494, N'BROIL KING', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (495, N'LYNX', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (496, N'Comfort-Aire', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (497, N'TCL', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (498, N'KAMADO JOE', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (500, N'KORUS', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (501, N'Monitor Audio', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (502, N'iSeries Profiles', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (503, N'Beats by Dr. Dre', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (504, N'SWISS GRILL', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (534, N'American Range', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (535, N'Perlick', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (549, N'Sertapedic', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (563, N'Stanley', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (605, N'SVS Sound', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (606, N'Broilmaster', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (607, N'TRUE', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (608, N'Primo Grills', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (609, N'Maytag Heritage', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (615, N'Arctic King', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (616, N'Kitchenaid Black', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (618, N'Coyote', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (631, N'Brown', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (639, N'LG Ref Electronics', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (650, N'Tempur-Flex', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (651, N'Frigidaire Professional', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (660, N'Holland Grill', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (666, N'Char-Broil', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (673, N'Verona', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (674, N'XO Ventilation', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (684, N'Bosch Benchmark', 1, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (688, N'Thor', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (689, N'FAR', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (699, N'Windster', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (703, N'Sony XBR', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (727, N'LG Signature', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (807, N'GE Café', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (881, N'Ashley', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (882, N'Millennium by Ashley', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (883, N'Signature by Ashley', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (884, N'Benchcraft by Ashley', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (885, N'Ashley Sleep', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (886, N'Sierra Sleep', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1112, N'Drexel Heritage', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1114, N'A America', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1122, N'Anthony California', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1123, N'Aspen Home Furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1127, N'Best Home Furnishings', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1136, N'Coaster', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1176, N'Klaussner Home Furnishings', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1180, N'LaCrosse Furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1187, N'La-z-Boy', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (1193, N'Riverside Furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (2450, N'Frigidaire Professional', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (2451, N'GE Cafe', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (2452, N'GE Profile', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11104, N'Stanley Furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11127, N'Emerald Home Furnishings', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11153, N'Vaughan Bassett Furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11170, N'Signature Design by Ashley', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11171, N'Millennium', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11176, N'United Furniture Industries', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11191, N'New Classic Furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11211, N'Generations by Coaster', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11225, N'Southern Motion', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11256, N'Powell', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11404, N'Lifestyle Solutions', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11437, N'Cresent Fine Furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11510, N'Sunny Designs', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11588, N'Ashley Sleep', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11708, N'Four Hands', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11709, N'Kian usa furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11717, N'Benchcraft', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11788, N'United Furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11805, N'Millennium by Ashley', NULL, 1)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11900, N'Malouf Furniture Co.', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11901, N'MLily', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11902, N'Adesso', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11903, N'Boraam', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11904, N'DwellStudio', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11905, N'Emerald', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11906, N'Night and Day', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11907, N'Nova Furniture', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11908, N'Uttermost', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11909, N'BeautyRest', NULL, NULL)
                    
                    INSERT [dbo].[SiteOnTimeBrand] ([Id], [CommonBrandName], [Haw_Only], [Download]) VALUES (11910, N'test001', NULL, NULL)
                    ";
            _stagingDb.ExecuteNonQuery(seedCommand);
        }

        private void DeleteSiteOnTimeBrandTable()
        {
            _stagingDb.ExecuteNonQuery("DROP TABLE IF EXISTS SiteOnTimeBrand");
        }

        private async System.Threading.Tasks.Task AddTaskAsync()
        {
            ScheduleTask task = new ScheduleTask();
            task.Name = $"Sync Site on Time Products";
            task.Seconds = 86400;
            task.Type = TaskType;
            task.Enabled = false;
            task.StopOnError = false;

            await _scheduleTaskService.InsertTaskAsync(task);
        }

        private async System.Threading.Tasks.Task RemoveTasksAsync()
        {
            var task = await _scheduleTaskService.GetTaskByTypeAsync(TaskType);
            if (task != null)
            {
                await _scheduleTaskService.DeleteTaskAsync(task);
            }
        }

        private void ClearPDPTables()
        {
            _stagingDb.ExecuteNonQuery(@"
                delete from ProductDataProductDimensions
                delete from ProductDataProductDownloads
                delete from ProductDataProductFeatures
                delete from ProductDataProductFilters
                delete from ProductDataProductImages
                delete from ProductDataProductRelatedItems
                delete FROM ProductDataProductpmaps
                delete from ProductDataProducts
            ");
        }
    }
}