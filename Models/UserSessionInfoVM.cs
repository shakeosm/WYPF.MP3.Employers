namespace MCPhase3.Models
{
    /// <summary>This will hold information regarding a user session.</summary>
    public class UserSessionInfoVM
    {
        public string UserId { get; set; }
        public string Password { get; set; }
        public string BrowserId { get; set; }
        public string WindowsId { get; set; }
        public string LastLoggedIn { get; set; }
        public bool HasExistingSession { get; set; } = false;
        public string SessionId { get; set; }
    }
}
