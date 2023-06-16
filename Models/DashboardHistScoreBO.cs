using System;

namespace MCPhase3.Models
{
    public class DashboardHistScoreBO
    {
      
            public int remittance_Id { get; set; }
            public string remittanceId_Encrypted { get; set; }
            public DateTime score_Date { get; set; }
            public int score_Employer { get; set; }
            public int score_Wypf { get; set; }
        
    }
}
