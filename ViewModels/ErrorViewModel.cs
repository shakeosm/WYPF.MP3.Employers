using System;

namespace MCPhase3.ViewModels
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public string DisplayMessage { get; set; }
        public string ErrorPath { get; set; }
    }
}
