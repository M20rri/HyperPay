using System.Configuration;

namespace HyperPay.Shared.Dtos
{
    public class PaymentTypeWrapper
    {
        public string Value { get; private set; }
        public PaymentTypeWrapper(string value) { Value = value; }

        public static PaymentTypeWrapper VISA_MASTER { get { return new PaymentTypeWrapper(ConfigurationManager.AppSettings["VISAMasterEntityId"]); } }
        public static PaymentTypeWrapper MADA { get { return new PaymentTypeWrapper(ConfigurationManager.AppSettings["MADAEntityId"]); } }
    }
}