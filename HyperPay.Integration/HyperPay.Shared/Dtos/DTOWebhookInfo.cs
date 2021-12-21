namespace HyperPay.Shared.Dtos
{
    public class DTOWebhookInfo
    {
        public string type { get; set; }
        public Payload payload { get; set; }
    }

    public class Payload
    {
        public string id { get; set; }
        public string paymentType { get; set; }
        public string paymentBrand { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
        public string presentationAmount { get; set; }
        public string presentationCurrency { get; set; }
        public string descriptor { get; set; }
        public string merchantTransactionId { get; set; }
        public string merchantInvoiceId { get; set; }
        public OperationResult result { get; set; }
        public OperationResult SAPTCOResult { get; set; }
        public Authentication authentication { get; set; }
        public Card card { get; set; }
        public Customer customer { get; set; }
        public Billing billing { get; set; }
        public CustomParameters customParameters { get; set; }
        public Risk risk { get; set; }
        public string buildNumber { get; set; }
        public string timestamp { get; set; }
        public string ndc { get; set; }
        public string merchantAccountId { get; set; }
    }

    public class OperationResult
    {
        public string code { get; set; }
        public string description { get; set; }
    }

    public class Authentication
    {
        public string entityId { get; set; }
    }

    public class Card
    {
        public string bin { get; set; }
        public string last4Digits { get; set; }
        public string holder { get; set; }
        public string expiryMonth { get; set; }
        public string expiryYear { get; set; }
    }

    public class Customer
    {
        public string givenName { get; set; }
        public string surname { get; set; }
        public string merchantCustomerId { get; set; }
        public string sex { get; set; }
        public string email { get; set; }
        public string ip { get; set; }
    }

    public class Billing
    {
        public string street1 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string postcode { get; set; }
        public string country { get; set; }
    }

    public class CustomParameters
    {
        public string SHOPPER_promoCode { get; set; }
        public string merchant_invoice_number { get; set; }
    }

    public class Risk
    {
        public string score { get; set; }
    }
}