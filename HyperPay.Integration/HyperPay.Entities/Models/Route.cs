using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperPay.Entities.Models
{
    [Table("Route")]
    public class Route
    {
        public Route()
        {
            this.UserRoutes = new HashSet<UserRoute>();
        }
        public int Id { get; set; }
        public string Prefix { get; set; }
        public virtual ICollection<UserRoute> UserRoutes { get; set; }
    }
}
