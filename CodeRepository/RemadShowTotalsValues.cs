using MCPhase3.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
    /// <summary>
    /// following class will show totals value on form from uploaded excel file.
    /// </summary>
    public class RemadShowTotalsValues
    {

        MonthlyContributionBO MonthlyContributionBObject = new MonthlyContributionBO();
        CheckSpreadsheetValuesSample CheckSpreadsheetValues = new CheckSpreadsheetValuesSample();
       // CommonRepo repo = new CommonRepo();
        public MonthlyContributionBO GetSpreadsheetValues(DataTable stringDT)
        {
            bool answer = true;

            if (stringDT != null)
            {
                this.MonthlyContributionBObject.MemberContribSS = GetSum(stringDT, "EE_CONT_MAIN");
                //string testStr = excelDt.Compute("Sum(EE_CONT_MAIN)", "").ToString();
                this.MonthlyContributionBObject.MemberContrib5050SS = GetSum(stringDT, "EE_CONT_50_50");
                this.MonthlyContributionBObject.MemberContribPOESSS = GetSum(stringDT, "PRCHS_OF_SRV");
                this.MonthlyContributionBObject.MemberContribARCSS = GetSum(stringDT, "ARC_CONTS");
                this.MonthlyContributionBObject.MemberContribAPCSS = GetSum(stringDT, "EE_APC_CONTS");

                this.MonthlyContributionBObject.MembersTotalSS = this.MonthlyContributionBObject.MemberContribSS +
                                                            this.MonthlyContributionBObject.MemberContrib5050SS +
                                                            this.MonthlyContributionBObject.MemberContribPOESSS +
                                                            this.MonthlyContributionBObject.MemberContribARCSS +
                                                            this.MonthlyContributionBObject.MemberContribAPCSS;

                this.MonthlyContributionBObject.EmployerContribAPCSS = GetSum(stringDT, "ER_APC_CONTS");
                this.MonthlyContributionBObject.EmployerContribSS = GetSum(stringDT, "ER_CONTS");
                this.MonthlyContributionBObject.EmployersTotalSS = this.MonthlyContributionBObject.EmployerContribAPCSS +
                                                              this.MonthlyContributionBObject.EmployerContribSS;

                this.MonthlyContributionBObject.EmployeePayMain = GetSum(stringDT, "PAY_MAIN");
                this.MonthlyContributionBObject.EmployeePay5050 = GetSum(stringDT, "PAY_50_50");

            }
            //else
            //{
            //    // spreadSheetReadStatusMsg = errorMessage;
            //    return MonthlyContributionBObject;
            //}

            return MonthlyContributionBObject;
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
