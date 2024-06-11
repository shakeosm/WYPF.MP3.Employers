using MCPhase3.Common;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class PasswordUpdateVM
    {

        public string UserName { get; internal set; }
        public string Password { get; set; }
        public string OldPassword { get; set; }
        internal int Result;
        public string PortalName  { get; internal set; } = Constants.ThisPortalName;

    }

    public class ForgottenPasswordResetVM
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Confirm password doesn't match.")]
        public string ConfirmPassword { get; set; }

        //public string SessionToken { get; set; }
        public int Result { get; set; }
    }

    public class ForgottenPasswordResetPostVM
    {
        public string UserId { get; set; }
        
        /// <summary>This is for API adoptability. API has been using UserName for UPM LoginId.. crazy stuff</summary>
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        public string SessionToken { get; set; }
        public int Result { get; set; }
        public string PortalName { get; set; } = "Employer-reset-pwd";
    }

}
