using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class HelpForEAndAUpdateRecord
    {
        public string errorType { get; set; }
        public string error { get; set; }
        public string errorAndCorrection { get; set; }
        public string info { get; set; }
        public string acknowledged { get; set; }
    }

    public class UpdateStatusVM
    {
        public string Header { get; set; }
        public string DisplayMessage { get; set; }
        public bool IsSuccess { get; set; } = false;
    }

    public class MemberUpdateRecordBO
    {
        /// <summary>To be displayed in the View page- on top</summary>
        public string AlertDescription { get; set; }
        public string DataRowEncryptedId { get; set; }
        public double dataRowID { get; set; }
        public string checkedAfterMatch { get; set; }
        public string modUser { get; set; }
        public string schemeName { get; set; }
        public string paylocationName { get; set; }
        public string employerName { get; set; }
        public string rank { get; set; }
        [Required(ErrorMessage ="Forename can not left blank")]
        public string forenames { get; set; }
        public string lastName { get; set; }
        public string jobTitle { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public string address5 { get; set; }
        public string postCode { get; set; }

        public string notes { get; set; }
        public string title { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DOB { get; set; }
        [DataType(DataType.Date)]
        public DateTime? startDate { get; set; }
        public string gender { get; set; }
        public string NI { get; set; }
        public string memberNo { get; set; }
        public string payRef { get; set; }
        public string postRef { get; set; }
        public string costCode { get; set; }
        public string ptRTFlag { get; set; }
        public string ptCSFlag { get; set; }

        public double? hoursWorked { get; set; }
        public double? stdHours { get; set; }
        public double? contractHours { get; set; }
        [DataType(DataType.Date)]
        public DateTime? dateJoined { get; set; }
        public string enrolmentType { get; set; }
        [DataType(DataType.Date)]
        public DateTime? dateLeft { get; set; }
        public string optOutFlag { get; set; }
        [DataType(DataType.Date)]
        public DateTime? optOutDate { get; set; }
        public double? payMain { get; set; }
        public double? eeContsMain { get; set; }
        public double? pay5050 { get; set; }
        public double? eeConts5050 { get; set; }
        [DataType(DataType.Date)]
        public DateTime? startDate5050 { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? endDate5050 { get; set; }
        public double? purchService { get; set; }
        public double? arcConts { get; set; }
        public double? eeAPCConts { get; set; }
        public double? erAPCConts { get; set; }
        public double? erConts { get; set; }
        public double? annRateofPay { get; set; }
        public double? totalAVCContsPaid { get; set; }
        public double? pay19922006 { get; set; }
        public double? pay2015 { get; set; }
        public double? eeConts { get; set; }
        public double? purch60 { get; set; }
        public double? apbTempProm { get; set; }
        public double? cpdEEConts { get; set; }
        public double? avgPay { get; set; }
        public double? addedPenConts { get; set; }
        public string HOURS_CONCATENATED { get; set; }

        public double? statusCode { get; set; }
        public string statusTxt { get; set; }

        public string EncryptedID { get; set; }

        public List<ErrorAndWarningViewModelWithRecords> ErrorAndWarningList { get; set; }

        //List<HelpForEAndAUpdateRecord> helpList { get; set; }   
    }
}
