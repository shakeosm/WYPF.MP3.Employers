using MCPhase3.Models;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MCPhase3.ViewModels
{

    public class HomeFileUploadVM
    {
        public List<NameOfMonths> MonthList { get; set; }

        [Required(ErrorMessage = "Payroll Month is required")]
        public string SelectedMonth { get; set; }

        public List<YearsBO>  YearList { get; set; }

        [Required(ErrorMessage = "Payroll Year is required")]
        public string SelectedYear{ get; set; }

        public List<NameOfMonths> OptionList { get; set; }
        [Required(ErrorMessage = "Post option is required")]
        public int SelectedPostType { get; set; }
        
        public List<PayrollProvidersBO> PayLocationList { get; set; }

        [Required(ErrorMessage = "Data file is required")]
        public IFormFile PaymentFile { get; set; }

    }
}
