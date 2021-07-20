using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Payments.AbcCard.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.AbcCard.Components
{
    [ViewComponent(Name = "AbcCard")]
    public class AbcCardViewComponent : NopViewComponent
    {
        private readonly AbcCardPaymentSettings _abcCardPaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;

        public AbcCardViewComponent(
            AbcCardPaymentSettings abcCardPaymentSettings,
            ILocalizationService localizationService,
            IStoreContext storeContext,
            IWorkContext workContext)
        {
            _abcCardPaymentSettings = abcCardPaymentSettings;
            _localizationService = localizationService;
            _storeContext = storeContext;
            _workContext = workContext;
        }

        public IViewComponentResult Invoke()
        {
            var model = new PaymentInfoModel
            {
                DescriptionText = _localizationService.GetLocalizedSetting(
                    _abcCardPaymentSettings,
                    x => x.DescriptionText,
                    _workContext.WorkingLanguage.Id,
                    _storeContext.CurrentStore.Id)
            };

            return View("~/Plugins/Payments.AbcCard/Views/PaymentInfo.cshtml", model);
        }
    }
}