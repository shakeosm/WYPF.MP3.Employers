using System.Collections.Generic;

namespace MCPhase3.Models
{
    public class GetMatchesViewModel
    {
        public string EmployersName { get; set; }
        public string CurrentAlertDescription { get; set; }
        public string Note { get; set; }
        public string ActiveProcess { get; set; }
        public string DataRowEncryptedId { get; set; }

        public List<MatchingPersonVM> MatchingPersonList { get; set; }

        /// <summary>This is for 'Contributions Data Received' section...</summary>
        public MemberUpdateRecordBO MemberRecord{ get; set; }
    }

    public class MemberFolderMatchingVM
    {
        public string ActiveProcess { get; set; }
        public string Note { get; set; }

    }
}
