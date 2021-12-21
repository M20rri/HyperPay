using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperPay.Shared.Models
{
    public class HyperPayContext : DbContext
    {
        public HyperPayContext() : base("name=HyperPayConstr")
        {

        }
    }
}
