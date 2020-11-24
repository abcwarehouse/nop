using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
	public class ImportProductFlagsTask : IScheduleTask
	{
		private readonly IImportProductFlags _import;
		private readonly ImportSettings _importSettings;

		public ImportProductFlagsTask(
			IImportProductFlags import,
			ImportSettings importSettings
		)
		{
			_import = import;
			_importSettings = importSettings;
		}

		public void Execute()
		{
			if (_importSettings.SkipImportProductFlagsTask)
			{
				this.Skipped();
				return;
			}

            this.LogStart();
			_import.Import();
            this.LogEnd();
		}
	}
}