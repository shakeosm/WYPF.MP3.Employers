using MCPhase3.Models;
using MCPhase3.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace MCPhase3.CodeRepository
{
    /// <summary>
    /// following class will show totals value on form from uploaded excel file.
    /// </summary>
    public class CheckTotalsService : ICheckTotalsService
    {

        public MonthlyContributionBO GetSpreadsheetValues(List<ExcelsheetDataVM> excelData)
        {
            MonthlyContributionBO contributionVM = new();

            if (excelData.Any())
            {
                contributionVM.MemberContribSS = GetTotal(excelData.Select(e => e.EE_CONT_MAIN).ToList());
                contributionVM.MemberContrib5050SS = GetTotal(excelData.Select(e => e.EE_CONT_50_50).ToList());
                contributionVM.MemberContribPOESSS = GetTotal(excelData.Select(e => e.PRCHS_OF_SRV).ToList()); 
                contributionVM.MemberContribARCSS = GetTotal(excelData.Select(e => e.ARC_CONTS).ToList());
                contributionVM.MemberContribAPCSS = GetTotal(excelData.Select(e => e.EE_APC_CONTS).ToList());

                contributionVM.MembersTotalSS = contributionVM.MemberContribSS +
                                                            contributionVM.MemberContrib5050SS +
                                                            contributionVM.MemberContribPOESSS +
                                                            contributionVM.MemberContribARCSS +
                                                            contributionVM.MemberContribAPCSS;

                contributionVM.EmployerContribAPCSS = GetTotal(excelData.Select(e => e.ER_APC_CONTS).ToList());
                contributionVM.EmployerContribSS = GetTotal(excelData.Select(e => e.ER_CONTS).ToList());
                contributionVM.EmployersTotalSS = contributionVM.EmployerContribAPCSS + contributionVM.EmployerContribSS;

                contributionVM.DeficitTotal = contributionVM.DeficitRec +
                                                 contributionVM.YearEndBalanceRec +
                                                 contributionVM.FundedBenefitsRec +
                                                 contributionVM.Miscellaneous_Rec;

                contributionVM.EmployeePayMain = GetTotal(excelData.Select(e => e.PAY_MAIN).ToList());
                contributionVM.EmployeePay5050 = GetTotal(excelData.Select(e => e.PAY_50_50).ToList());

                return contributionVM;

            }
            return contributionVM;

        }

        private double GetTotal(List<string> valueList)
        {
            double totalValue = 0;
            double parsedValue = 0;
            foreach (var item in valueList)
            {
                if (!string.IsNullOrEmpty(item)) { 
                    _ = double.TryParse(item.Replace("£", ""), out parsedValue);
                    totalValue += parsedValue;
                }
            }
            return totalValue;
        }     
    }

    public interface ICheckTotalsService
    {
        MonthlyContributionBO GetSpreadsheetValues(List<ExcelsheetDataVM> excelData);
    }
}
