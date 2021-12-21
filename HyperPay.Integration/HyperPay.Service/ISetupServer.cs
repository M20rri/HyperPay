using HyperPay.Shared.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HyperPay.Service
{
    public interface ISetupServer
    {
        Task<DTOCheckOutResponse> CheckoutsAsync(DTOMerchand model, DTOUserMaster user);
        Task<DTOPaymentStatusInfo> PaymentStatus(DTOPaymentStatus model);
        Task<bool> PaymentNotify(DTOPaymentNotifyRequest model);
        Task<string> FindEntityIdFromPaymentMethod(string paymentMethod);
        Task<string> GenerateSimpleInvoice(DTOSimpleInvoice simpleInvoice, DTOUserMaster user);
        Task<bool> ProcessPmtNotification(string checkoutId, DTOPaymentStatusInfo dTOPaymentStatusInfo);
        Task<DTOInvoiceSuccessResponse> SuccessSimpleInvoiceResponse(string response);
        Task<DTOInvoiceErrorResponse> ErrorSimpleInvoiceResponse(string response);
        Task<string> CreatePaymentForm(string checkoutId, string returnURL);
        Task<DTOTRPaidResult> TRPaid(DTOTRPaid model);
        Task<DTORefundResponse> ReBillPayment(DTOReBillPayment model);
        Task<GlobalResponseMessage> VacationsInformation(DTOEmployeeVacationRequest model);
        Task<GlobalResponseMessage> ViolationInformation(DTOViolationRequest model);
        Task<DTOPaySlipResInfo> PaySlipInformation(DTOPaySlipReqInfo model);
        Task<DTOTicketBalanceRes> TicketBalance(DTOTicketBalanceReq model);
        Task<bool> IsInvoiceExist(long invoiceNumber, int systemId);
    }
}
