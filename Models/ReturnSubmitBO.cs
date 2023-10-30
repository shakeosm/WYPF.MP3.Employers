namespace MCPhase3.Models
{
    public class ReturnSubmitBO
    {
        public int P_PAYLOC_FILE_ID { get; set; }
        public string P_USERID { get; set; }
        public int P_STATUSCODE { get; set; }
        public string p_REMITTANCE_ID { get; set; }
        public string L_STATUSTEXT { get; set; }
        public int? L_STATUSCODE { get; set; }

        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
