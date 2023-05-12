using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.Models
{
    public class GetMatchesViewModel
    {
      
        public string note { get; set; }
        public string activeProcess { get; set; }
        public List<GetMatchesBO> Matches { get; set; }
        
    }
}
