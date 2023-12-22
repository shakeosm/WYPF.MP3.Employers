using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class MailDataVM
    {
        public string UserId { get; set; }
        public string EmailTo { get; set; }
        public string FullName { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }        
        public string CommunicationMethod { get; set; }        
    }

    public class TokenDataVerifyVM
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "You must enter the Session Token to verify login")]
        [StringLength(20, MinimumLength = 6)]
        public string SessionToken { get; set; }
        public string Email { get; set; }
        public string VerificationMessage { get; set; }

    }
}
