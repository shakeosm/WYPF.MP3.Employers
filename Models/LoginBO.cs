using MCPhase3.Common;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class LoginBO
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter the user name")]
        public string userName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{9,}$", ErrorMessage = "Password doesn't match the criteria.")]
        [DataType(DataType.Password)]
        public string password { get; set; }

        
        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("password", ErrorMessage = "Confirm password doesn't match.")]
        public string ConFirmPassword { get; set; }

        [Required(ErrorMessage = "Old password is required")]
        [StringLength(255, ErrorMessage = "Must be between 9 and 255 characters", MinimumLength = 9)]
        [DataType(DataType.Password)]
        public string oldPassword { get; set; }

       // public int isStaff { get; set; }
        public int result { get; set; }

        public UserDetailsVM  UserDetails { get; set; }
        public string PortalName { get; set; } = Constants.ThisPortalName;
    }

}
