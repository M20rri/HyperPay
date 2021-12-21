using System;
using System.Collections.Generic;

namespace HyperPay.Shared.Dtos
{
    public class DTOSimpleInvoice
    {
        public string amount { get; set; }
        public string currency { get; set; }
        public string payment_type { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string lang { get; set; }
        public string merchant_invoice_number { get; set; }
        /// <summary>
        /// yyyy-MM-dd HH:mm:ss
        /// </summary>
        public string expiration_date { get; set; }
        public string custom_83 { get; } = "true";
        public SAPTCOCustomParameters SAPTCOCustomParameters { get; set; }
    }

    public class DTOInvoiceStatus
    {
        public bool status { get; set; }
    }

    public class DTOInvoiceErrorResponse
    {
        public bool status { get; set; }
        public List<object> data { get; set; }
        public string message { get; set; }
        public DTOErrors errors { get; set; }
    }

    public class DTOErrors
    {
        public List<string> merchant_invoice_number { get; set; }
    }

    public class DTOInvoiceSuccessResponse
    {
        public bool status { get; set; }
        public string url { get; set; }
        public DTOInvoiceResponseData data { get; set; }
        public string message { get; set; }
        public List<object> errors { get; set; }
    }

    public class DTOInvoiceResponseData
    {
        public string amount { get; set; }
        public string currency { get; set; }
        public string payment_type { get; set; }
        public string phone { get; set; }
        public string lang { get; set; }
        public string merchant_invoice_number { get; set; }
        public string expiration_date { get; set; }
        public string client_email { get; set; }
        public int created_by { get; set; }
        public string invoice_no { get; set; }
        public int organization_id { get; set; }
        public List<string> sent_by { get; set; }
        public string client_name { get; set; }
        public int template_id { get; set; }
        public int sms_template_id { get; set; }
        public int email_template_id { get; set; }
        public string updated_at { get; set; }
        public string created_at { get; set; }
        public int id { get; set; }
    }

    public class DTOInvoiceInfo
    {
        public long HyperPayMerchantInvoiceNumber { get; set; }
        public long OwnerSystemMerchantInvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentType { get; set; }
        public string Phone { get; set; }
        public string Lang { get; set; }
        public string ExpirationDate { get; set; }
        public string CustomParameter1 { get; set; }
        public string CustomParameter2 { get; set; }
        public int SystemId { get; set; }
        public int UserId { get; set; }
        public int OwnerSystemDeliveryStatus { get; set; }
        public string OwnerSystemPmtNotificationURL { get; set; }
        public string OwnerSystemPmtNotificationUserName { get; set; }
        public string OwnerSystemPmtNotificationPassword { get; set; }
        public int OwnerChannelDeliveryStatus { get; set; }
        public string OwnerChannelHookNotificationURL { get; set; }
        public string OwnerChannelHookNotificationUserName { get; set; }
        public string OwnerChannelHookNotificationPassword { get; set; }
        public string HookNotificationTicketsUrl { get; set; }
    }
}
