using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.Models
{
    public class LoginBO
    {
        public string userName { get; set; }
        public string password { get; set; }
        public string oldPassword { get; set; }
       // public int isStaff { get; set; }
        public int result { get; set; }
    }
}
