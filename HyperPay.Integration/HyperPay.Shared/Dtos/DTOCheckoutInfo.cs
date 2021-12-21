using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HyperPay.Shared.Dtos
{
    public class DTOCheckoutInfo
    {
        public string CheckoutId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentType { get; set; }
        public string EntityId { get; set; }
        public long MerchantTransactionId { get; set; }
        public string Phone { get; set; }
        public string Lang { get; set; }
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
