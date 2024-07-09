using MCPhase3.Common;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class LoginBO
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter the user name")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[`¬!@$%^&*(){}[\];'#:@~<>?/|\-\=\+]).{12,}$", ErrorMessage = "Password doesn't match the criteria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        
        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Confirm password doesn't match.")]
        public string ConFirmPassword { get; set; }

        [Required(ErrorMessage = "Old password is required")]
        [StringLength(255, ErrorMessage = "Must be between 9 and 255 characters", MinimumLength = 9)]
        [DataType(DataType.Password)]
        public string OldPassword { get; set; }

        public List<string> SuggestedPasswords { get; set; }
    }

    public class LoginPostVM
    {
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please enter the user name")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        public int Result { get; set; }

        public string PortalName { get; internal set; } = Constants.ThisPortalName;
    }

}
