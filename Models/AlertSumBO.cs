namespace MCPhase3.Models
{
    public class AlertSumBO
    {
        public string RemittanceId { get; set; }
        public int? L_PAYLOC_FILE_ID { get; set; }
        public string L_PAYLOC_CODE{ get; set; }

        public string L_USERID { get; set; }
        public string AlertType { get; set; }
        public bool? ShowAlertsNotCleared { get; set; }
    }

    public class AlertQueryVM
    {
        public string RemittanceId { get; set; }
        public string L_USERID { get; set; }
        public string AlertType { get; set; }
        public string EmployerCode { get; set; }
        public string Status { get; set; }
        public string Total { get; set; }

    }
}
