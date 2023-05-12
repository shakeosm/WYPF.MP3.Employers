using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.Models
{    
    public class MonthlyContributionBO
    {
        public string UserLoginID { get; set; }
        public string UserName { get; set; }
        public double employerID { get; set; }
        public string PaymentMonth { get; set; }
        public string employerName { get; set; }
        public double MemberContrib { get; set; }
        public double MemberContrib5050 { get; set; }
        public double MemberContribPOES { get; set; }
        public double MemberContribARC { get; set; }
        public double MemberContribAPC { get; set; }
        public double MembersTotal { get; set; }

        public double EmployerContrib { get; set; }
        public double EmployerContribAPC { get; set; }
        public double EmployersTotal { get; set; }
        public string ClientID { get; set; }
        public string payrollProviderID { get; set; }
        

        [Required(ErrorMessage = "Enter zero if nil amount")]
        public double DeficitRec { get; set; }
        public double YearEndBalanceRec { get; set; }
        public double FundedBenefitsRec { get; set; }
        public double Miscellaneous_Rec { get; set; }

        [Required(ErrorMessage = "Paid by date is required.")]
        public DateTime PaidByChequeDate { get; set; }
        
        public string PaidDirectlyWYPF { get; set; }

        public string AdditionalInfo { get; set; }

        public double EmployeePayMain { get; set; }
        public double EmployeePay5050 { get; set; }

        public double MemberContribSS { get; set; }
        public double MemberContrib5050SS { get; set; }
        public double MemberContribPOESSS { get; set; }
        public double MemberContribARCSS { get; set; }
        public double MemberContribAPCSS { get; set; }
        public double MembersTotalSS { get; set; }

        public double EmployerContribSS { get; set; }
        public double EmployerContribAPCSS { get; set; }
        public double EmployersTotalSS { get; set; }

        public string UploadedFileName { get; set; }
        public string payRollYear { get; set; }
    }
}