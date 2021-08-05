using System.Linq;
using EllaSoftware.Plugin.Misc.CronTasks.Services;
using Microsoft.AspNetCore.Mvc;

namespace EllaSoftware.Plugin.Misc.CronTasks.Components
{
    [ViewComponent(Name = CronTasksDefaults.ScheduleTaskListViewComponentName)]
    public class ScheduleTaskListViewComponent : ViewComponent
    {
        private readonly ICronTaskService _cronTaskService;

        public ScheduleTaskListViewComponent(
            ICronTaskService cronTaskService)
        {
            _cronTaskService = cronTaskService;
        }

        public IViewComponentResult Invoke()
        {
            var cronTaskScheduleTaskIds = _cronTaskService.GetCronTasks().Select(t => t.Key).ToArray();

            return View("~/Plugins/EllaSoftware.CronTasks/Views/Shared/Components/ScheduleTaskList.cshtml", cronTaskScheduleTaskIds);
        }
    }
}
