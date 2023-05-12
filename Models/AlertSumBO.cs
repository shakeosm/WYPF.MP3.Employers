using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.Models
{
    public class AlertSumBO
    {
        public string remittanceId { get; set; }
        public int? L_PAYLOC_FILE_ID { get; set; }

        public string L_USERID { get; set; }
    }
}
