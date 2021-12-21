using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperPay.Entities.Models
{
    [Table("UserRoute")]
    public class UserRoute
    {
        public int Id { get; set; }
        public Nullable<int> UserId { get; set; }
        public virtual User User { get; set; }

        public Nullable<int> RouteId { get; set; }
        public virtual Route Route { get; set; }
    }
}
