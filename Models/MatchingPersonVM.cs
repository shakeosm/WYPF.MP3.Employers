using System;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class MatchingPersonVM
    {
        public int dataRowId { get; set; }
        public string userId { get; set; }
        public int? personId { get; set; }
        public string personMatch { get; set; }
        public string personMatchType { get; set; }
        public string folderMatch { get; set; }


        public string upperSurName { get; set; }
        public string upperForeNames { get; set; }
        public string NINO { get; set; }
       [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd-MM-yyyy}")]
        public DateTime DOB { get; set; }
        public string postCode { get; set; }
        public string folderId { get; set; }
        public string folderRef { get; set; }
        public string payLocationName { get; set; }
        public string statusDesc { get; set; }
        public string payRef { get; set; }
        public string postRef { get; set; }
        [DataType(DataType.Date)]
        public DateTime? dateJoined { get; set; }
        [DataType(DataType.Date)]
        public DateTime? dateLeft { get; set; }
        public string serviceId { get; set; }
        public string serviceTypeFG { get; set; }
        public double partTimeHours { get; set; }
        public double STANDARDHOURS { get; set; }
        public string jobTitle { get; set; }
        public string HOURS_CONCATENATED { get; set; }
        public string note { get; set; }
        public string isActive { get; set; }
        public string Matched_Flag { get; set; }

    }
    public class MatchingRecordQueryVM
    {
        public int dataRowId { get; set; }
        public string userId { get; set; }
    }


}
