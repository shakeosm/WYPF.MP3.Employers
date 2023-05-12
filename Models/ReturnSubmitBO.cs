using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.Models
{
    public class ReturnSubmitBO
    {
        public int P_PAYLOC_FILE_ID { get; set; }
        public string P_USERID { get; set; }
        public int P_STATUSCODE { get; set; }
        public string p_REMITTANCE_ID { get; set; }
        public string RETURN_STATUSTEXT { get; set; }
        public int? RETURN_STATUSCODE { get; set; }
    }
}
