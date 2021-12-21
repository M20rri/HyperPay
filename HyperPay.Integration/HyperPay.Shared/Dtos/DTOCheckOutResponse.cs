namespace HyperPay.Shared.Dtos
{
    public class DTOCheckOutResponse
    {
        public Result result { get; set; }
        public string buildNumber { get; set; }
        public string timestamp { get; set; }
        public string ndc { get; set; }
        public string id { get; set; }
    }

    public class Result
    {
        public string code { get; set; }
        public string description { get; set; }
    }
}
