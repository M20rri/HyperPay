using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperPay.Shared.Dtos
{
    public class DTOPaymentStatusInfo
    {
        public string id { get; set; }
        public string paymentType { get; set; }
        public string paymentBrand { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
        public string descriptor { get; set; }
        public string merchantTransactionId { get; set; }
        public Result result { get; set; }
        public Result SAPTCOResult { get; set; }
        public ResultDetails resultDetails { get; set; }
        public ResponseCard card { get; set; }
        public ResponseCustomer customer { get; set; }
        public ThreeDSecure threeDSecure { get; set; }
        public ResponseCustomParameters customParameters { get; set; }
        public ResponseRisk risk { get; set; }
        public string buildNumber { get; set; }
        public string timestamp { get; set; }
        public string ndc { get; set; }
    }

    public class ResultDetails
    {
        public string RiskStatusCode { get; set; }
        public string ResponseCode { get; set; }
        public string RequestId { get; set; }
        public string RiskResponseCode { get; set; }
        public string action { get; set; }
        public string OrderId { get; set; }
    }

    public class ResponseCard
    {
        public string bin { get; set; }
        public string binCountry { get; set; }
        public string last4Digits { get; set; }
        public string holder { get; set; }
        public string expiryMonth { get; set; }
        public string expiryYear { get; set; }
    }

    public class ResponseCustomer
    {
        public string givenName { get; set; }
        public string surname { get; set; }
        public string email { get; set; }
        public string ip { get; set; }
        public string ipCountry { get; set; }
    }

    public class ThreeDSecure
    {
        public string eci { get; set; }
        public string xid { get; set; }
        public string paRes { get; set; }
    }

    public class ResponseCustomParameters
    {
        public string SHOPPER_EndToEndIdentity { get; set; }
        public string CTPE_DESCRIPTOR_TEMPLATE { get; set; }
    }

    public class ResponseRisk
    {
        public string score { get; set; }
    }
}