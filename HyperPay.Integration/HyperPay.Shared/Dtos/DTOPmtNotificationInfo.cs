using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperPay.Shared.Dtos
{
    public class SAPTCOCustomParameters
    {
        public string CustomParameter1 { get; set; }
        public string CustomParameter2 { get; set; }
        public string TicketsURL { get; set; }
    }

    public class DTOPmtNotificationInfo
    {
        /// <summary>
        /// Used incase of refund
        /// </summary>
        public string PaymentId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentMethod { get; set; }
        /// <summary>
        /// dd/mm/yyyy HH:mm:ss
        /// </summary>
        public string PaymentProcessingDate { get; set; }
        public SAPTCOCustomParameters SAPTCOCustomParameters { get; set; }
    }

    public class DTOCheckoutPmtNotificationInfo : DTOPmtNotificationInfo
    {
        public string CheckoutId { get; set; }
        public long MerchantTransactionId { get; set; }
    }

    public class DTOInvoicePmtNotificationInfo : DTOPmtNotificationInfo
    {
        public long HyperPayMerchantInvoiceNumber { get; set; }
        public long OwnerSystemMerchantInvoiceNumber { get; set; }
    }
}