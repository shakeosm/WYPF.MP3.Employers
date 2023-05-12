using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
    public class ReturnCheckBO
    {
        public int P_PAYLOC_FILE_ID { get; set; }
        public string P_USERID { get; set; }
        public int p_REMITTANCE_ID { get; set; }
        public string L_STATUSTEXT { get; set; }
        public int? L_STATUSCODE { get; set; }
    }
}
