namespace MCPhase3.Models
{
    public class UserDetailsVM
    {
        public string UserId { get; set; }
        /// <summary>LoginName from UPM3, some user don't have same UserId and LoginName.</summary>
        public string LoginName { get; set; }
        public string FullName { get; set; }
        public string JobTitle { get; set; }
        public string Type { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }

        /* Followings will be inserted from another API call.. "_apiEndpoints.PayrollProvider -> PayrollProvidersBO" */
        public string Pay_Location_Name { get; set; }
        public string Pay_Location_Ref { get; set; }
        public string Pay_Location_ID { get; set; }
        public string Client_Id { get; set; }
        public bool IsSuperUser { get; set; }
    }
}
