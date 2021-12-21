using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperPay.Entities.Models
{
    [Table("RequestResponseLog")]
    public class RequestResponseLog
    {
        public int Id { get; set; }
        public string UserHostName { get; set; }
        public string UserHostAddress { get; set; }
        public string Origin { get; set; }
        public string RequestUri { get; set; }
        public string PathAndQuery { get; set; }
        public string MethodType { get; set; }
        public string Headers { get; set; }
        public string XForwardedFor { get; set; }
        public string RequestBody { get; set; }
        public string ResponseBody { get; set; }
        public string Status { get; set; }
        public Nullable<System.DateTime> CreationDate { get; set; }
        public Nullable<int> UserId { get; set; }
        public virtual User User { get; set; }
    }
}
