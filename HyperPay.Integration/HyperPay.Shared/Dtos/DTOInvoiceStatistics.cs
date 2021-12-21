using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperPay.Shared.Dtos
{
    public class DTOInvoiceStatistics
    {
        public int InvoiceCount { get; set; }
        public decimal InvoiceSUM { get; set; }
    }
}