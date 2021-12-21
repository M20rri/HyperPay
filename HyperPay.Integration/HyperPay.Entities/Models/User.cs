using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HyperPay.Entities.Models
{
    [Table("User")]
    public class User
    {
        public User()
        {
            this.RequestResponseLogs = new HashSet<RequestResponseLog>();
            this.UserRoutes = new HashSet<UserRoute>();
        }

        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<RequestResponseLog> RequestResponseLogs { get; set; }
        public virtual ICollection<UserRoute> UserRoutes { get; set; }
    }
}
