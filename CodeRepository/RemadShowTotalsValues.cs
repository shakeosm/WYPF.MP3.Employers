using MCPhase3.Models;
using System;
using System.Data;

namespace MCPhase3.CodeRepository
{
    /// <summary>
    /// following class will show totals value on form from uploaded excel file.
    /// </summary>
    public class RemadShowTotalsValues
    {

        private MonthlyContributionBO contributionVM = new();

        public MonthlyContributionBO GetSpreadsheetValues(DataTable excelData)
        {
            //bool answer = true;

            if (excelData != null)
            {
                contributionVM.MemberContribSS = GetSum(excelData, "EE_CONT_MAIN");
                //string testStr = excelDt.Compute("Sum(EE_CONT_MAIN)", "").ToString();
                contributionVM.MemberContrib5050SS = GetSum(excelData, "EE_CONT_50_50");
                contributionVM.MemberContribPOESSS = GetSum(excelData, "PRCHS_OF_SRV");
                contributionVM.MemberContribARCSS = GetSum(excelData, "ARC_CONTS");
                contributionVM.MemberContribAPCSS = GetSum(excelData, "EE_APC_CONTS");

                contributionVM.MembersTotalSS = contributionVM.MemberContribSS +
                                                            contributionVM.MemberContrib5050SS +
                                                            contributionVM.MemberContribPOESSS +
                                                            contributionVM.MemberContribARCSS +
                                                            contributionVM.MemberContribAPCSS;

                contributionVM.EmployerContribAPCSS = GetSum(excelData, "ER_APC_CONTS");
                contributionVM.EmployerContribSS = GetSum(excelData, "ER_CONTS");
                contributionVM.EmployersTotalSS = contributionVM.EmployerContribAPCSS +
                                                              contributionVM.EmployerContribSS;

                contributionVM.EmployeePayMain = GetSum(excelData, "PAY_MAIN");
                contributionVM.EmployeePay5050 = GetSum(excelData, "PAY_50_50");

            }

            return contributionVM;
        }

        public double GetSum(DataTable dt, string columnName)
        {
            double sum = 0;
           
            foreach (DataRow row in dt.Rows)
            {
                string str = string.Empty;
                try
                {
                    str = row[columnName].ToString();
                }
                catch (Exception ex)
                {
                    str = "0";
                }
                double tempDouble;
                if (double.TryParse(str, out tempDouble))
                {
                    sum += tempDouble;
                }
            }



            return Math.Round(sum, 2);

        }
    }
}
