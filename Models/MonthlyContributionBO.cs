﻿using System;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class MonthlyContributionBO
    {
        public string UserLoginID { get; set; }
        public string UserName { get; set; }
        public string employerID { get; set; }
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
        public double DeficitTotal { get; set; }

        public string UploadedFileName { get; set; }
        public string payrollYear { get; set; }

        public double EmployeeTotal () => MemberContribSS + MemberContrib5050SS + MemberContribAPCSS + MemberContribPOESSS + MemberContribARCSS;
        public double EmployersEmployeeTotalValue() => EmployeeTotal() + EmployersTotalSS;
    }

    public class MonthlyContributionPostVM
    {
        public string UserLoginID { get; set; }
        public string UserName { get; set; }
        public double employerID { get; set; }
        public string PaymentMonth { get; set; }
        public string EmployerName { get; set; }
        public string ClientID { get; set; }
        public string PayrollProviderID { get; set; }


        [Required(ErrorMessage = "Paid by date is required.")]
        public DateTime PaidByChequeDate { get; set; }

        public double MemberContribSS { get; set; }
        public double MemberContrib5050SS { get; set; }
        public double MemberContribPOESSS { get; set; }
        public double MemberContribARCSS { get; set; }
        public double MemberContribAPCSS { get; set; }

        public string UploadedFileName { get; set; }
        public string payrollYear { get; set; }
    }

    public class PreviousMonthMissingInfoVM
    {
        public double EmployerID { get; set; }
        public string EmployerName { get; set; }
        public string SubmissionPeriodName { get; set; }                
        
        public string MissingPeriodName { get; set; }
        /// <summary>Missing period status- either: Missing or Pending.. We are not dealing with 'Completed' cases here</summary>
        public string MissingPeriodStatus { get; set; }

        public int TotalRecordsInFile { get; set; }

    }
}