using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class RemittanceItemVM
    {
        public string userId { get; set; }
        public int remittance_Id { get; set; }
        public string remittance_IdEnc { get; set; }
        public string statusType { get; set; }
        public string statusCode { get; set; }
        public string year_From { get; set; }
        public string year_To { get; set; }
        public string return_Year_Month { get; set; }
        public string return_Year { get; set; }
        public int return_Month { get; set; }
        public string return_Month_Name { get; set; }
        public DateTime return_Received_Date { get; set; }

        //public int paylocation_Id { get; set; }
        public string paylocationRef { get; set; }
        public string paylocation_Name { get; set; }
        public int? return_Records { get; set; }
        public string return_Status_Text { get; set; }
        public string return_Status_Code { get; set; }
        public int? return_Score_Emp { get; set; }
        public int? return_Score_Wypf { get; set; }
        public string action_Button_Text { get; set; }
        public string L_PAYROLL_PROVIDER { get; set; }

        public string EMPLOYER_REF { get; set; }
        public string EMPLOYER_NAME { get; set; }
        public string RETURN_PERIOD { get; set; }

    }

    public class RemittanceFileNameUpdateVM
    {
        [Required]
        public int RemittanceId { get; set; }
        [Required]
        public string CustomerFileName { get; set; }

    }

    public class DashboardWrapperVM
    {
        public RemittanceItemVM BO { get; set; }
        public List<RemittanceItemVM> RemittanceList { get; set; }
    }
}
