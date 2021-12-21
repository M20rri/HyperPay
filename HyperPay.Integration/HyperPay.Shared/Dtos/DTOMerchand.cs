namespace HyperPay.Shared.Dtos
{
    public class DTOMerchand
    {
        public string PaymentMethod { get; set; }
        public double Amount { get; set; }
        public string Currency { get; set; } = "SAR";
        public string PaymentType { get; set; } = "DB";
        public int MerchantTransactionId { get; set; }
        public string Email { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostCode { get; set; }
        public string GivenName { get; set; }
        public string SureName { get; set; }
        public string Phone { get; set; }
        public string Lang { get; set; }
        public SAPTCOCustomParameters SAPTCOCustomParameters { get; set; }
    }

    public class DTOSumbitPayment
    {
        public string CheckoutId { get; set; }
        public string ReturnURL { get; set; }
    }

    public class DTOReBillPayment
    {
        public string PaymentId { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentType { get; set; } = "PA";
    }

    public class DTOCredit
    {
        public string PaymentMethod { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string PaymentBrand { get; set; }
        public string PaymentType { get; set; } = "CD";
        public string CardNo { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string CardHolder { get; set; }
    }
}
