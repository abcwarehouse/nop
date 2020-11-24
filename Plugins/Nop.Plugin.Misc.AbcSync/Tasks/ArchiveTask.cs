using Nop.Plugin.Misc.AbcCore;
using Nop.Services.Tasks;
using System.Data;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    class ArchiveTask : IScheduleTask
    {
        private readonly ArchiveService _archiveService;
        private readonly CoreSettings _coreSettings;

        public ArchiveTask(
            ArchiveService archiveService,
            CoreSettings coreSettings
        )
        {
            _archiveService = archiveService;
            _coreSettings = coreSettings;
        }

        public void Execute()
        {
            this.LogStart();
            using (IDbConnection backendConn = _coreSettings.GetBackendDbConnection())
            {
                _archiveService.ArchiveProductContent(backendConn);
            }
            this.LogEnd();
        }
    }
}
