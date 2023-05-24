using MCPhase3.CodeRepository;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MCPhase3.Models
{
    /// <summary>
    /// following View model class is used for all the alert and warning apis
    /// </summary>
    public class ErrorAndWarningViewModel
    {

        public string alertClass { get; set; }
        public string approvalFlag { get; set; }
        public string alertDesc { get; set; }
        public int countTotals { get; set; }
    }
    /// <summary>
    /// this view model class will take idRem and alertType from Error and warnings summary
    /// </summary>
    public class ErrorAndWarningToShowListViewModel
    {

        public double remittanceID { get; set; }
        public string alertType { get; set; }
        public string L_USERID { get; set; }

    }
    public class ErrorAndWarningViewModelWithRecords
    {
        public string MC_ALERT_ID { get; set; }
        public string EncryptedAlertid { get; set; }
        public string remittanceID { get; set; }
        public string DATAROWID_RECD { get; set; }
        public string DATAROWID_RECD_ENC { get; set; }
        public string ALERT_COUNT { get; set; }
        public int? L_PAYLOC_FILE_ID { get; set; }

        public string empCorrectionFG { get; set; }
        public string CLEARED_FG { get; set; }
        public string BULK_APPROVAL_FG { get; set; }
        public string ALERT_CLASS { get; set; }
        public string ALERT_DESC { get; set; }
        public string alert_Text { get; set; }

        public string ALERT_DESC_LONG { get; set; }
        public string ALERT_TYPE_REF { get; set; }
        public int COUNT { get; set; }
        public string FORENAMES { get; set; }
        public string JOBTITLE { get; set; }
        public string SURNAME { get; set; }
        [DataType(DataType.Date)]
        public DateTime? DOB { get; set; }
        public string NINUMBER { get; set; }
        [DataType(DataType.Date)]
        public DateTime? DATEJOINEDSCHEME { get; set; }
        public bool selected { get; set; }
        public string selectAll { get; set; }
        public string userId { get; set; }
        public string ALERT_CLEARING_METHOD { get; set; }
        public string ACTION_BY { get; set; }

        public string EncryptedRowRecordID { get; set; }
    }

    public class ErrorAndWarningViewModelLists
    {
        //[BindProperty]
        public IEnumerable<ErrorAndWarningViewModelWithRecords> errorsAndWarningsList { get; set; }
    }
}
