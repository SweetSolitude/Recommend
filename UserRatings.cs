using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOBM.Models
{
    public class UserRatings
    {
        public int Id { get; set; }
        public string EntityId { get; set; }
        public double Interest {get; set;}
        public DateTime LastModifiedDate { get; set; }
    }
}
