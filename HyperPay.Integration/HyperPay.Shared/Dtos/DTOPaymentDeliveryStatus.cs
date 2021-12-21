using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperPay.Shared.Dtos
{
    public enum PaymentDeliveryStatus
    {
        New = 1,
        UnderProcessing = 2,
        Delivered = 3,
        Failed = 4,
        FailedAndStopped = 5
    }
}
