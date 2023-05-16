namespace MCPhase3.Common
{
    public static class Constants
    {
        public const string SessionKeyUserID = "_UserName";
        //Paylocation and Employer both are same.
        public const string SessionKeyPayLocName = "_PayLocName";
        public const string SessionKeyPayLocId = "_Id";
        public const string SessionKeyPaylocFileID = "_PaylocFileID";
        public const string SessionKeyPassword = "_Password";
        public const string SessionKeyClientId = "_clientId";
        public const string SessionKeyClientType = "_clientType";
        public const string SessionKeyEmployerName = "_employerName";
        public const string SessionKeyPayrollProvider = "_payrollProvider";


        public const string SessionKeyMonth = "_month";
        public const string SessionKeyYears = "_years";
        public const string SessionKeyFileName = "_fileName";
        public const string SessionKeyPosting = "_posting";
        public const string SessionKeySchemeName = "_scheme";
        public const string SessionKeyTotalRecords = "_totalRecords";
        public const string SessionKeyRemittanceID = "_remittanceID";


        //public const string PasswordChangedKeyName = "_passwordChanged";
        //public const string CurrentPasswordKeyName = "_currentPassword";
        
        public const string SessionInfoKeyName = "SessionInfo";    //## for Redis use
        public const string SessionGuidKeyName = "SessionGUID";    //## for Redis cross check
        public const string UserIdKeyName = "LoggedInAs";    //## for Redis cross check

    }
}
