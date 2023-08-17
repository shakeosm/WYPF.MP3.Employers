using System.Collections.Generic;

namespace MCPhase3.Models
{
    public class GetMatchesViewModel
    {
      
        public string Note { get; set; }
        public string ActiveProcess { get; set; }
        public string DataRowEncryptedId { get; set; }

        public List<GetMatchesBO> Matches { get; set; }
        
    }
}
