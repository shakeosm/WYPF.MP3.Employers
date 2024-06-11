using System.ComponentModel.DataAnnotations;

namespace MCPhase3.Models
{
    public class LoginViewModel
    {
        /// <summary>This will be the 'UPM Login ID' for this User</summary>
        [Required(ErrorMessage = "User Id is required")]
        public string UserId { get; set; }

        //[RegularExpression(@"/^(?=.*?[A-Z])(?=.*?[a-z])(?=.*?[0-9])(?=.*?[#?!@$%^&*-]).{9,}$/", ErrorMessage = "Password doesn't match the criteria")]
        //[DataType(DataType.Password)]
        [Required(ErrorMessage = "Password is required")]
        public string Password {  get; set; }   

        public string ClientId {  get; set; }

        public string EmployerName { get; set; }

        public string BrowserId { get; set; } = "SpecialBrowser112";
        public string WindowsId { get; set; } = "Windows95";
        public string LoginErrorMessage { get; set; }
    }
}
