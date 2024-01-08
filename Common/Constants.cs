namespace MCPhase3.Common
{
    public static class Constants
    {
        public const int EmployersPortal = 2;    //## API=1, Employers = 2; Admin = 3
        public const string SessionKeyUserID = "_UserName"; //## used in the 'UserSessionCheckActionFilter' for Authentication
        //Paylocation and Employer both are same.
        public const string SessionKeyPayLocName = "_PayLocName";       //## used in "MCPhase3.CodeRepository.LoginCheck : ViewComponent"
        //public const string SessionKeyPayLocId = "_Id";
        public const string SessionKeyPaylocFileID = "_PaylocFileID";
        public const string SessionKeyPassword = "_Password";
        public const string SessionKeyClientId = "_clientId";
        public const string SessionKeyClientType = "_clientType";
        public const string SessionKeyEmployerName = "_employerName";
        //public const string SessionKeyPayrollProvider = "_payrollProvider";


        public const string SessionKeyMonth = "_month";
        public const string SessionKeyYears = "_years";
        public const string SessionKeyFileName = "_fileName";
        public const string SessionKeyPosting = "_posting";
        public const string SessionKeySchemeName = "_scheme";
        public const string SessionSchemeNameValue = "LGPS";

        public const string SessionKeyTotalRecords = "_totalRecords";
        public const string SessionKeyRemittanceID = "remittanceID";
        
        public const string SessionInfoKeyName = "SessionInfo";    //## for Redis use
        public const string SessionGuidKeyName = "SessionGUID";    //## for Redis cross check
        public const string LoggedInAsKeyName = "LoggedInAs";    
        public const string BrowserId = "BrowserId";    
        public const string WindowsId = "WindowsId";    


        public const string ErrorWarningSummaryKeyName = "ErrorAndWarningSummaryVM";    
        public const string BulkApprovalAlertIdList = "BulkApprovalRecordIdList";    
        
        /// <summary>The complete File path and name of the file uploaded by the Customer</summary>
        public const string UploadedFilePathKey = "UploadedFilePathName";    
        public const string FileUploadErrorMessage = "FileUploadErrorMessage";    
        public const string CustomErrorDetails = "CustomErrorDetails";      //# to show the user a custom error message in a Error500 page

        public const string Error403_Page = @"~/Views/Errors/Errror403.cshtml";
        public const string AccountLockedMessage = "Your account is temporarily locked to prevent unauthorized use. Please try again later in 30 minutes, and if you still have trouble, contact WYPF.";
        public const string AccountFailedLoginMessage = "Username or password not correct, please try again";
        public const string AccountGenericErrorMessage = "Account error! Please contact Helpdesk.";
        public const string SessionExpiredMessage = "Session is expired. Please log in again.";
        
        public const string StatusType_EMPLOYER = "EMPLOYER";
        public const string StatusType_COMPLETE = "COMPLETE";
        public const string StatusType_WYPF = "WYPF";

        public static string MemberMatchingList = "MemberMatchingList";
        public static string UserPayLocationInfo = "UserPayLocationInfo";
        public static string AppUserDetails = "AppUserDetails";
        
        public static string ExcelDataAsString = "ExcelDataAsString";
        public static string CurrentAlertDescription = "CurrentAlertDescription";
        public static string Excel_DataTableToInsert = "Excel_DataTableToInsert";        
        //public static string Excel_XML_ConfigPath = "Excel_XML_ConfigPath";

        public static string RedisKeyList()
        {
            return $"{SessionKeyRemittanceID},{SessionInfoKeyName},{SessionGuidKeyName},{LoggedInAsKeyName},{ErrorWarningSummaryKeyName},{UploadedFilePathKey},{UserPayLocationInfo},{MemberMatchingList},{AppUserDetails},{ExcelDataAsString},{Excel_DataTableToInsert},{Excel_DataTableToInsert}";
        }

        public enum PostingType
        {
            First= 1,
            Second= 2,
            PreviousMonth= 3,
        }

        public enum LoginStatus
        {
            Failed = 0,
            Valid = 1,
            Locked = 2,
        }

        public enum Password
        {
            Updated = 1,
            Invalid = 2,
            IncorrectOldPassword = 3,
        }
    }
}
