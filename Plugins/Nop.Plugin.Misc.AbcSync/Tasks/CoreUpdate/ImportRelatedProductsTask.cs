using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
	public class ImportRelatedProductsTask : IScheduleTask
	{
		private readonly IImportRelatedProducts _importService;
		private readonly ImportSettings _importSettings;

		public ImportRelatedProductsTask(
			IImportRelatedProducts importService,
			ImportSettings importSettings
		)
		{
			_importService = importService;
			_importSettings = importSettings;
		}

		public void Execute()
		{
			if (_importSettings.SkipImportRelatedProductsTask)
			{
				this.Skipped();
				return;
			}

            this.LogStart();
			_importService.Import();
            this.LogEnd();
		}
	}
}