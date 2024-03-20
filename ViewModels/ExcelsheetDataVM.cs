using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.ViewModels
{
    public class ExcelsheetDataVM
    {
        [Ignore]
        public long REMITTANCE_ID { get; set; }
        [Ignore] public long DATAROWID_RECD { get; set; }
        [Ignore] public string CLIENTID { get; set; }
        [Ignore] public string SCHEMENAME { get; set; }
        [Ignore] public string MODUSER { get; set; }

        public string PAYROLL_PD { get; set; }
        public string PAYROLL_YR { get; set; }
        public string EMPLOYER_LOC_CODE { get; set; }
        public string EMPLOYER_NAME { get; set; }
        public string MEMBERS_TITLE { get; set; }
        public string SURNAME { get; set; }
        public string FORENAMES { get; set; }
        [StringLength(1)]
        public string GENDER { get; set; }
        public string DOB { get; set; }
        public string JOBTITLES { get; set; }
        public string ADDRESS1 { get; set; }
        public string ADDRESS2 { get; set; }
        public string ADDRESS3 { get; set; }
        public string ADDRESS4 { get; set; }
        public string ADDRESS5 { get; set; }
        public string POSTCODE { get; set; }
        public string COSTCODE { get; set; }
        public string MEMBER_NO { get; set; }
        public string NI_NUMBER { get; set; }
        public string PAYREF { get; set; }
        public string POSTREF { get; set; }
        public string FT_PT_CS_FLAG { get; set; }
        public string FT_PT_HOURS_WORKED { get; set; }
        public string STD_HOURS { get; set; }
        public string CONTRACTUAL_HRS { get; set; }
        public string DATE_JOINED_SCHEME { get; set; }
        public string ENROLMENT_TYPE { get; set; }
        public string DATE_OF_LEAVING_SCHEME { get; set; }
        public string OPTOUT_FLAG { get; set; }
        public string OPTOUT_DATE { get; set; }
        public string PAY_MAIN { get; set; }
        public string EE_CONT_MAIN { get; set; }
        public string PAY_50_50 { get; set; }
        public string EE_CONT_50_50 { get; set; }
        public string START_DATE_50_50 { get; set; }
        public string END_DATE_50_50 { get; set; }
        public string PRCHS_OF_SRV { get; set; }
        public string ARC_CONTS { get; set; }
        public string EE_APC_CONTS { get; set; }
        public string ER_APC_CONTS { get; set; }
        public string ER_CONTS { get; set; }
        public string ANNUAL_RATE_OF_PAY { get; set; }
        public string TOTAL_AVC_CONTRIBUTIONS_PAID { get; set; }
        public string NOTES { get; set; }

    }
}
