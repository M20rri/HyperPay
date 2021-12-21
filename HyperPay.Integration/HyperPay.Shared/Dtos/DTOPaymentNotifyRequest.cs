using System.Configuration;

namespace HyperPay.Shared.Dtos
{
    public class DTOPaymentNotifyRequest
    {
        public string WebhookDecryptionKey { get; set; } = ConfigurationManager.AppSettings["WebhookDecryptionKey"];
        public string httpBody { get; set; } = "";
        public string ivFromHttpHeader { get; set; }
        public string authTagFromHttpHeader { get; set; }
    }
}
