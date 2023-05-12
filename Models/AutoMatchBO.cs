using System;

namespace MCPhase3.Models
{
    [Serializable()]
    public class AutoMatchBO
    {
        public double totalRecordCount { get; set; }
        public double personMatchCount { get; set; }
        public double folderMatchCount { get; set; }
        public double L_STATUS_CODE { get; set; }
        public string L_STATUS_TEXT { get; set; }

    }
}
