namespace MCPhase3.ViewModels
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public int ApplicationId { get; set; }
        public string UserId { get; set; }

        public string ErrorPath { get; set; }
        public string Message { get; set; }
        public string StackTrace { get; set; }
    }
}
