using MCPhase3.Models;
using System.Collections.Generic;

namespace MCPhase3.ViewModels
{

    public class HomeFileUploadVM
    {
        public List<NameOfMonths> MonthList { get; set; }
        public List<YearsBO>  YearList { get; set; }
        public List<NameOfMonths> OptionList { get; set; }
        public List<PayrollProvidersBO> PayLocationList { get; set; }

    }
}
