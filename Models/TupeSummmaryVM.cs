using System;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class TupeSummmaryVM
    {
        public string LocationName { get; set; }
        public string LocationCode { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime DateLeft { get; set; }
        public int TotalStarter { get; set; }
        public int TotalLeaver { get; set; }
        public int TotalRecords { get; set; }
        public string TupeType { get; set; }
        public int StarterPercent() => TotalStarter / TotalRecords * 100;

    }

    public class TupeSearchVM
    {
        public int RemittanceId { get; set; }
        public string PayLocationCode { get; set; }
        public DateTime TupeDate { get; set; }
        public string TupeType { get; set; }
    }

    public class TupeItemVM
    {
        /// <summary>DataRowRecord ID</summary>
        public int Id { get; set; }
        public string FullName { get; set; }
        public string DoB { get; set; }
        public string Gender { get; set; }
        public string JobTitle { get; set; }
        public string Address1 { get; set; }
        public string NINumber { get; set; }
        public string PayRef { get; set; }
        public int RemittanceId { get; set; }
        public string LocationCode { get; set; }
        public DateTime DateStarted { get; set; }
        public DateTime DateLeft { get; set; }

    }


    public class TupePayLocationAlertVM
    {
        public int RecordId { get; set; }
        public int RemittanceId { get; set; }
        public string LocationCode { get; set; }
        public DateTime TupeDate { get; set; }
        public string TupeType { get; set; }
        /// <summary>The Employer will be given option to select whether a potential Tupe records is genuinely a Tupe </summary>
        public bool IsTupe { get; set; }
        public string UserId { get; set; }
    }

    public class TupePayLocationAlertCreateVM
    {
        /// <summary>Comma seperated DataRowRecord Id</summary>
        [Required]
        public string RecordIdList { get; set; }
        public int RemittanceId { get; set; }
        [Required]
        public string LocationCode { get; set; }

        [Required] public DateTime TupeDate { get; set; }
        [Required] public string TupeType { get; set; }

        /// <summary>Are we doing Ack for All/None/Selected members?</summary>
        [Required] public string AcknowledgementType { get; set; }        
        public string UserId { get; set; }
    }
}
