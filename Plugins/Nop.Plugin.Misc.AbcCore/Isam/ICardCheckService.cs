using Nop.Core.Domain.Common;
using Nop.Services.Payments;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public interface ICardCheckService
    {
        (string AuthNo, string RefNo, string StatusCode, string ResponseMessage) CheckCard(
            ProcessPaymentRequest paymentRequest,
            Address billingAddress,
            string domain,
            string ip
        );
    }
}