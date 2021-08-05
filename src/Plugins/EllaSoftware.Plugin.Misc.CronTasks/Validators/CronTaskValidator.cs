﻿using EllaSoftware.Plugin.Misc.CronTasks.Models;
using FluentValidation;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;
using Quartz;

namespace EllaSoftware.Plugin.Misc.CronTasks.Validators
{
    public class CronTaskValidator : BaseNopValidator<CronTaskModel>
    {
        public CronTaskValidator(ILocalizationService localizationService)
        {
            //if validation without this set rule is applied, in this case nothing will be validated
            //it's used to prevent auto-validation of child models
            RuleSet(NopValidatorDefaults.ValidationRuleSet, () =>
            {
                RuleFor(x => x.ScheduleTaskId)
                    .GreaterThan(0)
                        .WithMessage(localizationService.GetResource("EllaSoftware.Plugin.Misc.CronTasks.CronTask.ScheduleTaskId.Required"));
                RuleFor(x => x.CronExpression)
                    .NotEmpty()
                        .WithMessage(localizationService.GetResource("EllaSoftware.Plugin.Misc.CronTasks.CronTask.CronExpression.Required"))
                    .Must(ex => CronExpression.IsValidExpression(ex))
                        .WithMessage(localizationService.GetResource("EllaSoftware.Plugin.Misc.CronTasks.CronTask.CronExpression.Invalid"));
            });
        }
    }
}
