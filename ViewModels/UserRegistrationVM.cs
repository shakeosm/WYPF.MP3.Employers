using System.ComponentModel.DataAnnotations;
using MCPhase3.Models;

namespace MCPhase3.ViewModels
{
    public class UserRegistrationVM
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[`!@$%^&*(){}[\];'#:@~<>?/|\-\=\+]).{12,}$", ErrorMessage = "Password doesn't match the criteria.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }


        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Confirm password doesn't match.")]
        public string ConFirmPassword { get; set; }

        public UserDetailsVM UserDetails { get; set; }

        public string RegistationMessage { get; set; }
    }

    public class RegisterUserWithNewPasswordVM
    {
        public string UserId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
        
        public string SessionToken { get; set; }


    }

}
