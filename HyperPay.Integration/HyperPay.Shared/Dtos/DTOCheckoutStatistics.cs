using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperPay.Shared.Dtos
{
    public class DTOCheckoutStatistics
    {
        public int CheckoutCount { get; set; }
        public decimal CheckoutSUM { get; set; }
    }
}