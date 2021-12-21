using System.Collections.Generic;

namespace HyperPay.Shared.Dtos
{
    public class DTOClientRequest
    {
        public string UserHostName { get; set; }
        public string UserHostAddress { get; set; }
        public HashSet<string> OriginValues { get; set; }
        public string RequestUri { get; set; }
        public string PathAndQuery { get; set; }
        public string MethodType { get; set; }
        public HashSet<DTOHeaderKey> Headers { get; set; }
        public string RequestBody { get; set; }
        public string ResponseBody { get; set; }
        public string Status { get; set; }
        public int UserId { get; set; }
    }
}
