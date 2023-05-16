using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class LoginBO
    {
        [Required(ErrorMessage = "Username is required")]
        public string userName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [RegularExpression(@"/^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{9,}$/", ErrorMessage ="Password doesn't match the criteria")]        
        [DataType(DataType.Password)]
        public string password { get; set; }

        [Required(ErrorMessage = "Old password is required")]
        [StringLength(255, ErrorMessage = "Must be between 9 and 255 characters", MinimumLength = 9)]
        [DataType(DataType.Password)]
        public string oldPassword { get; set; }

       // public int isStaff { get; set; }
        public int result { get; set; }
    }
}
