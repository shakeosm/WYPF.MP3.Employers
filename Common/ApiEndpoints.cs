namespace MCPhase3.Common
{
    public class ApiEndpoints
    {
        public string Environment { get; set; }
        public string WebApiBaseUrl { get; set; }
        
        public string PasswodResetLinkRequest { get; set; }        
        public string PasswodResetVerifyRequest { get; set; }        
        public string EmployerForgottenPasswordupdate { get; set; }        
        public string PasswordChange { get; set; }        
        public string ValidatePassword { get; set; }
        public string GeneratePassword { get; set; }

        public string ErrorAndWarningsSelection { get; set; }
        public string AlertDetailsPLNextSteps { get; set; }
        public string ErrorAndWarnings { get; set; }
        public string TotalRecordsInserted { get; set; }
        public string InsertRemitanceDetails { get; set; }
        
        /// <summary>This call waits for the process to finish and then comes back with result/status</summary>
        public string AutoMatch { get; set; }
        /// <summary>this call will initiate the DB Procedure but will not wait for that to finish and just come back.. like kick and hide.. </summary>
        public string AutoMatch_V2 { get; set; }
        /// <summary>this API will be called periodically to check the current status of AutoMatch process</summary>
        public string AutoMatch_CheckProgress_Periodically { get; set; }

        public string ErrorAndWarningsApproval { get; set; }
        public string ApproveWarningsBulkList { get; set; }
        public string GetAlertDetailsInfo { get; set; }
        public string UpdateRecord { get; set; }
        public string EmployerName { get; set; }
        public string GetErrorWarningList { get; set; }
        public string GetRemittanceId { get; set; }
        public string GetRemittanceInfo { get; set; }
        public string GetPayLocation_With_Finance_Business_Partner { get; set; }
        public string GetFinanceBusinessPartnerByPayLocation { get; set; }

        public string SubmissionStatusForPreviousMonth { get; set; }
        public string SubmissionNotification_CreateNew { get; set; }
        public string SubmissionNotification_GetByUser { get; set; }

        public string GetEventDetails { get; set; }
        public string InsertEventDetails { get; set; }
        public string PayrollProvider { get; set; }
        public string SubPayrollProvider { get; set; }
        public string LoginCheck { get; set; }
        public string MFA_IsRequiredForUser { get; set; }
        public string Is_MfaEnabled { get; set; }
        public string MFA_SendToEmployer { get; set; }
        public string MFA_Verify { get; set; }


        public string MatchingRecordsUPM { get; set; }
        public string MatchingRecordsManual { get; set; }
        public string DashboardRecentSubmission { get; set; }
        public string DashboardEmployers { get; set; }
        public string DashboardScoreHist { get; set; }
        public string CheckFileIsUploaded { get; set; }
        public string DetailEmpList { get; set; }

        /// <summary>This call waits for the process to finish and then comes back with result/status</summary>
        public string InitialiseProc { get; set; }
        /// <summary>This call will initiate the DB Procedure but will not wait for that to finish and just come back.. like kick and hide.. </summary>
        public string InitialiseProc_v2 { get; set; }
        /// <summary>This is to call both Return_Initialise and Check_Return in a single API request- while the user can exit from MP3 UI and the Process will still continue in background</summary>
        public string InitialiseAndCheckReturn { get; set; }
        /// <summary>this API will be called periodically to check the current status of InitialiseProc process</summary>
        public string GetProgressForReturnInitialise { get; set; }
        public string GetProgressForAutoMatch { get; set; }

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
