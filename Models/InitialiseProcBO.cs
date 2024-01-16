using DocumentFormat.OpenXml.Wordprocessing;
using MC_WService;

namespace MCPhase3.Models
{
    public class InitialiseProcBO
    {
        public int P_REMITTANCE_ID { get; set; }
        public string P_USERID { get; set; }
        public int P_STATUSCODE { get; set; }
        public string P_STATUSTEXT { get; set; }
        public int P_EMPLOYERS_PROCESSED { get; set; }
        public int P_RECORDS_PROCESSED { get; set; }
    }

    public class InitialiseProcessResultVM
    {
        public string EncryptedRemittanceId { get; set; }
        public string EmployeeName { get; set; }
        public string ErrorMessage { get; set; }
        public string ShowProcessedInfo { get; set; }
        public string ShowMatchingResult{ get; set; }

        public string TotalRecordsInFile { get; set; }
        public string TotalRecordsInDatabase { get; set; }
        public string EmployersProcessedRecords { get; set; }

        /// <summary>This is to show which Process is Current in the View page- so user can take action on the next one</summary>
        public string CurrentStep { get; set; }
    }
}