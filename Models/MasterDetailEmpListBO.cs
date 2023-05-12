using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.Models
{
    public class MasterDetailEmpListBO
    {
        public string L_USERID { get; set; }
        public string L_REMITTANCE_ID { get; set; }
        public string L_STATUSTYPE { get; set; }
        public int? REMITTANCE_ID { get; set; }
        public int? PAYLOCATION_ID { get; set; }
        public string PAYLOCATION_NAME { get; set; }
        public string PAYLOCATIONREF { get; set; }
        public int? RETURN_RECORDS { get; set; }
        public string RETURN_STATUS_TEXT { get; set; }
        public int? RETURN_SCORE_EMP { get; set; }
        public int? RETURN_SCORE_WYPF { get; set; }
        public string action_Button_Text { get; set; }
    }
   
}
