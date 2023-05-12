using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.Models
{
    public class GetRemittanceStatusByUserBO
    {
        public int remittance_ID { get; set; }
        public int event_Type_ID { get; set; }
        public DateTime event_DateTime { get; set; }
        public string contribution_Month { get; set; }
        public string contribution_Year { get; set; }
        public int remittance_Status { get; set; }
        public int payloc_File_ID { get; set; }
        public string event_Notes { get; set; }
    }
    public class DashboardViewModel
    {
        public GetRemittanceStatusByUserBO BO { get; set; }
        public List<GetRemittanceStatusByUserBO> dashboardBO { get; set; }
        public IEnumerable<MasterDetailEmpListBO> details { get; set; }
    }
}
