﻿namespace MCPhase3.Common
{
    public class ApiEndpoints
    {
        public string Environment { get; set; }
        public string WebApiBaseUrl { get; set; }
        public string PasswordChange { get; set; }
        public string ErrorAndWarningsSelection { get; set; }
        public string AlertDetailsPLNextSteps { get; set; }
        public string ErrorAndWarnings { get; set; }
        public string TotalRecordsInserted { get; set; }
        public string InsertRemitanceDetails { get; set; }
        public string AutoMatch { get; set; }

        public string ErrorAndWarningsApproval { get; set; }
        public string ApproveWarningsBulkList { get; set; }
        public string GetAlertDetailsInfo { get; set; }
        public string UpdateRecord { get; set; }
        public string EmployerName { get; set; }
        public string GetErrorWarningList { get; set; }
        public string GetRemittanceId { get; set; }
        public string GetRemittanceInfo { get; set; }
        public string GetEventDetails { get; set; }
        public string InsertEventDetails { get; set; }
        public string PayrollProvider { get; set; }
        public string SubPayrollProvider { get; set; }
        public string LoginCheck { get; set; }
        public string MFA_IsRequired { get; set; }
        public string MFA_SendToEmployer { get; set; }
        public string MFA_Verify { get; set; }


        public string MatchingRecordsUPM { get; set; }
        public string MatchingRecordsManual { get; set; }
        public string DashboardRecentSubmission { get; set; }
        public string DashboardEmployers { get; set; }
        public string DashboardScoreHist { get; set; }
        public string CheckFileIsUploaded { get; set; }
        public string DetailEmpList { get; set; }
        public string InitialiseProc { get; set; }
        public string ReturnCheckProc { get; set; }
        public string SubmitReturn { get; set; }
        public string InsertData { get; set; }
        public string InsertDataCounter { get; set; }        
        public string RecordReset { get; set; }
        public string DeleteRemittance { get; set; }
        
        public string ErrorLogCreate { get; set; }
        public string GetUserDetails { get; set; }
        public string VerifyUserRegistrationCode { get; set; }
        public string RegisterUserWithNewPassword { get; set; }
        public string SuperUserCheck { get; set; }
    }

}
