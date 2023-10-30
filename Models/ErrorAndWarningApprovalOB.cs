namespace MCPhase3.Models
{
    public class ErrorAndWarningApprovalOB
    {
            public int alertID { get; set; }
            public string userID { get; set; }
            public double returnStatus { get; set; }
            public string returnStatusTxt { get; set; }

        
    }

    public class ApproveWarningsInBulkVM
    {
        public string AlertIdList { get; set; }
        public string UserID { get; set; }
        public double returnStatus { get; set; }
        public string returnStatusTxt { get; set; }

    }
}
