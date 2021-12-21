namespace HyperPay.Shared.Dtos
{
    public class DTOUserMaster
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
        public int SystemId { get; set; }
        public int DailyCheckoutAllowedCount { get; set; }
        public decimal DailyCheckoutAllowedAmount { get; set; }
        public int DailyInvoicesAllowedCount { get; set; }
        public decimal DailyInvoicesAllowedAmount { get; set; }
        public int InvoiceExpirationInMinutes { get; set; }
    }
}