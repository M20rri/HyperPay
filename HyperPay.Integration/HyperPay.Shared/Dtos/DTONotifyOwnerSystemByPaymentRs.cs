using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperPay.Shared.Dtos
{
    public class DTONotifyOwnerSystemByPaymentRs
    {
        public bool IsDelivered { get; set; }
        public string OwnerSystemResponse { get; set; }
    }

    public class DTONotifyOwnerChannelByPaymentRs
    {
        public bool IsDelivered { get; set; }
        public string OwnerChannelResponse { get; set; }
    }
}
