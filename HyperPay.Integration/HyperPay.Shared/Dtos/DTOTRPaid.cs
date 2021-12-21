using System;

namespace HyperPay.Shared.Dtos
{
    public class DTOTRPaid
    {
        public string p_channel { get; set; }
        public long p_usr_id { get; set; }
        public string p_lang_id { get; set; }
        public long p_sdp_id { get; set; }
        public long p_saptco_pnr { get; set; }
        public long p_payment_method_id { get; set; } //  (2 for credit card, 13 for mada card)
        public decimal p_total_paid_amount { get; set; } // (total paid amount in Hyper pay)
        public string p_transaction_id { get; set; } // (can be the unique transaction id in Hyper pay)
        public string p_payment_process_date { get; set; } //  (payment datetime in Hyper pay)
        public int p_is_paying { get; set; }
        public string p_expiry_date { get; set; }
    }

    public class DTOTRPaidResult
    {
        public string p_is_paid { get; set; }
        public string p_excp_ticket_numbers { get; set; }
        public string p_rtrn_cd { get; set; }
        public string p_rtrn_desc { get; set; }
    }

    public class DTOTRPaymentRes
    {
        public string p_payment_process_date { get; set; }
        public string p_transaction_id { get; set; }
        public int p_total_paid_amount { get; set; }
        public string p_payment_method_id { get; set; }
        public int p_saptco_pnr { get; set; }
    }
}