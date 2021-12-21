namespace HyperPay.Shared.Dtos
{
    public class DTORefundResponse
    {
        public string id { get; set; }
        public string referencedId { get; set; }
        public string paymentType { get; set; }
        public string merchantTransactionId { get; set; }
        public Result result { get; set; }
        public Risk risk { get; set; }
        public string buildNumber { get; set; }
        public string timestamp { get; set; }
        public string ndc { get; set; }
    }

}
