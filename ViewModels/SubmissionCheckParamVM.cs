namespace MCPhase3.ViewModels
{
    public class SubmissionCheckParamVM
    {
        public string PayLocationCode { get; set; }
        /// <summary>Financial Month number- which submission we need to check. April = 1, March=12</summary>
        public int MonthNumber { get; set; }
        /// <summary>Financial Year, eg: 2023/24, 2024/25</summary>
        public string FinancialYear { get; set; }        

    }
}
