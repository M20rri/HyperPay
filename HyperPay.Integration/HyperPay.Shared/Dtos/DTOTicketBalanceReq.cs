namespace HyperPay.Shared.Dtos
{
    public class DTOTicketBalanceReq
    {
        public string EmployeeID { get; set; }
    }

    public class DTOTicketBalanceRes
    {
        public string EMP_NO { get; set; }
        public string EMP_NAME { get; set; }
        public double ticket_balance { get; set; }
    }
}
