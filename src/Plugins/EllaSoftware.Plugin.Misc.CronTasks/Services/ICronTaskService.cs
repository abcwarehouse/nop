using System;
using System.Collections.Generic;
using EllaSoftware.Plugin.Misc.CronTasks.Domain;
using Nop.Core.Domain.Tasks;
using Task = System.Threading.Tasks.Task;

namespace EllaSoftware.Plugin.Misc.CronTasks.Services
{
    public interface ICronTaskService
    {
        System.Threading.Tasks.Task<IDictionary<int, string>> GetCronTasksAsync();
        Task InsertCronTaskAsync(int scheduleTaskId, string cronExpression);
        Task UpdateCronTaskAsync(int scheduleTaskId, string cronExpression);
        Task DeleteCronTaskAsync(int scheduleTaskId);
        Task ExecuteCronTaskAsync(ScheduleTask scheduleTask);

        System.Threading.Tasks.Task RescheduleQuartzJob(int scheduleTaskId, string cronExpression);
        DateTime? GetQuartzJobNextOccurrence(int scheduleTaskId);
        CronTaskExecutionStatus GetQuartzJobExecutionStatus(int scheduleTaskId);
    }
}
