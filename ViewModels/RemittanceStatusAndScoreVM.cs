namespace MC3StaffAdmin.Models
{
    public class RemittanceStatusAndScoreVM
    {
        public int RemittanceId { get; set; }
        public string Payroll_Provider_Id { get; set; }
        public string Payroll_Provider_Name { get; set; }
        public string Contribution_Year { get; set; }
        public string Contribution_Month { get; set; }
        public int StatusCode { get; set; }
        public int Count_Records { get; set; }
        public int Count_Employers { get; set; }
        public int WypfScore { get; set; }
        public int EmployerScore { get; set; }
        public string ButtonActionText { get; set; }

    }
}
