using MCPhase3.Models;
using System.Collections.Generic;
using System.Data;

namespace MCPhase3.CodeRepository
{
    public class CheckSpreadsheetValuesSample
    {
        CommonRepo repo = new CommonRepo();

        /// <summary>This is the actual Field/Data validation on the Excel file- which is now in a DataSet.
        /// This will generate respective error message based on the defined validation rules on each field.</summary>
        /// <param name="dt"></param>
        /// <param name="month"></param>
        /// <param name="posting"></param>
        /// <param name="validPayrollYr"></param>
        /// <param name="validPayLocations"></param>
        /// <param name="validTitles"></param>
        /// <param name="invalidSigns"></param>
        /// <param name="CheckSpreadSheetErrorMsg"></param>
        /// <returns></returns>
        public string CheckSpreadsheetValues(DataTable dt,string month,string posting, string validPayrollYr, List<PayrollProvidersBO> validPayLocations,string[] validTitles,string[] invalidSigns, ref string CheckSpreadSheetErrorMsg)
        {
            //bool result = true;
            string result = string.Empty;
            bool CheckPayrollPeriodStatus = true;
            bool CheckPayrollYearStatus = true;
            bool CheckEmployerLocCodeStatus = true;
            bool CheckMemberTitleStatus = true;
            bool CheckMemberSurnameStatus = true;
            bool CheckMemberForenameStatus = true;
            bool CheckMemberAddress1Status = true;
            bool CheckMemberAddress2Status = true;
            bool CheckMemberPostcodeStatus = true;
            bool CheckMemberNINOStatus = true;
            bool CheckMemberPayrefStatus = true;
            bool CheckDOBStatus = true;
            bool CheckFTPTFlagStatus = true;
            bool CheckStdHoursStatus = true;
            bool CheckContractualHoursStatus = true;
            bool CheckDateJSSatus = true;
            bool CheckDateLSSatus = true;

            bool CheckDateOptOuttus = true;
            bool CheckDate505StartSatus = true;
            bool CheckDate505EndSatus = true;

            bool CheckEnrolmentTypeSatus = true;
            bool CheckEmployerContribSatus = true;
            bool CheckAnnualRateOfPaySatus = true;
            bool CheckPayMainSatus = true;

            bool CheckEmployeeContribSatus = true;

            bool CheckGenderSatus = true;
            bool CheckOptOutFlageStatus = true;
            //Q - Error messages
            bool CheckEmployerLocCodeMsg = true;
            //New validations - stop upload if a member's age over 75 and check employer contribution rate



            string payrollPeriodErrorMsg = string.Empty;
            string payrollYearErrorMsg = string.Empty;
            string employerLocCodeErrorMsg = string.Empty;
            string memberTitleErrorMsg = string.Empty;
            string memberSurnameErrorMsg = string.Empty;
            string memberForenameErrorMsg = string.Empty;
            string memberAddress1ErrorMsg = string.Empty;
            string memberAddress2ErrorMsg = string.Empty;
            string memberPostcodeErrorMsg = string.Empty;
            string memberNINOErrorMsg = string.Empty;
            string memberPayrefErrorMsg = string.Empty;
            string memberDOBErrorMsg = string.Empty;
            string memberFTPTFlagErrorMsg = string.Empty;
            string memberStdHoursErrorMsg = string.Empty;
            string memberContractualHoursErrorMsg = string.Empty;
            string memberDateJSErrorMsg = string.Empty;
            string memberDateLSErrorMsg = string.Empty;

            string memberDateOptOutErrorMsg = string.Empty;
            string memberDate505StartErrorMsg = string.Empty;
            string memberDate505EndErrorMsg = string.Empty;

            string memberEnrolmentTypeErrorMsg = string.Empty;
            string employerContribErrorMsg = string.Empty;
            string annualRateOfPayErrorMsg = string.Empty;
            string payMainErrorMsg = string.Empty;

            string payEmployeeContribErrorMsg = string.Empty;

            string genderErrorMsg = string.Empty;
            string optOutFlagErrorMsg = string.Empty;
            string speechMarkErrorMsg = string.Empty;

            //Q - Error messages

            string employerLocErrorMsg = string.Empty;

            CheckPayrollPeriodStatus = CodeRepository.SpreadSheetChecks.CheckPayrollPeriod(ref dt, month,posting, ref payrollPeriodErrorMsg);
            CheckPayrollPeriodStatus = CodeRepository.SpreadSheetChecks.CheckSpeechMark(dt, invalidSigns, ref speechMarkErrorMsg);
            CheckPayrollYearStatus = CodeRepository.SpreadSheetChecks.CheckPayrollYear(ref dt, posting, validPayrollYr, ref payrollYearErrorMsg);
            CheckEmployerLocCodeStatus = CodeRepository.SpreadSheetChecks.CheckEmployerLocCode(dt, validPayLocations, ref employerLocCodeErrorMsg);
          //richard asked to disable it 17/10/22
             CheckMemberTitleStatus = CodeRepository.SpreadSheetChecks.CheckTitle(ref dt,validTitles, ref memberTitleErrorMsg);

            CheckMemberSurnameStatus = CodeRepository.SpreadSheetChecks.CheckSurname(ref dt, ref memberSurnameErrorMsg);
            CheckMemberForenameStatus = CodeRepository.SpreadSheetChecks.CheckForename(ref dt, ref memberForenameErrorMsg);
            CheckMemberAddress1Status = CodeRepository.SpreadSheetChecks.CheckAddress1(ref dt, ref memberAddress1ErrorMsg);
            CheckMemberAddress2Status = CodeRepository.SpreadSheetChecks.CheckAddress2(ref dt, ref memberAddress2ErrorMsg);
            CheckMemberPostcodeStatus = CodeRepository.SpreadSheetChecks.CheckPostcode(ref dt, ref memberPostcodeErrorMsg);
            CheckMemberNINOStatus = CodeRepository.SpreadSheetChecks.CheckNINO(ref dt, ref memberNINOErrorMsg);
            CheckMemberPayrefStatus = CodeRepository.SpreadSheetChecks.CheckPayRef(ref dt, ref memberPayrefErrorMsg);
            CheckDOBStatus = CodeRepository.SpreadSheetChecks.CheckDOB(dt, ref memberDOBErrorMsg);
            CheckFTPTFlagStatus = CodeRepository.SpreadSheetChecks.CheckFullTimePartTimeFlagForLG(ref dt, ref memberFTPTFlagErrorMsg);
            CheckStdHoursStatus = CodeRepository.SpreadSheetChecks.CheckStandardHours(dt, ref memberStdHoursErrorMsg);
            CheckContractualHoursStatus = CodeRepository.SpreadSheetChecks.CheckContractualHours(dt, ref memberContractualHoursErrorMsg);
            CheckDateJSSatus = CodeRepository.SpreadSheetChecks.CheckDateJS(dt, ref memberDateJSErrorMsg);
            CheckDateLSSatus = CodeRepository.SpreadSheetChecks.CheckDateLS(dt, ref memberDateLSErrorMsg);

            CheckDateOptOuttus = CodeRepository.SpreadSheetChecks.CheckDateOptOut(dt, ref memberDateOptOutErrorMsg);
            CheckDate505StartSatus = CodeRepository.SpreadSheetChecks.CheckDate505StartDate(dt, ref memberDate505StartErrorMsg);
            CheckDate505EndSatus = CodeRepository.SpreadSheetChecks.CheckDate505EndDate(dt, ref memberDate505EndErrorMsg);
            CheckEnrolmentTypeSatus = CodeRepository.SpreadSheetChecks.CheckEnrolmentType(dt, ref memberEnrolmentTypeErrorMsg);
            CheckEmployerContribSatus = CodeRepository.SpreadSheetChecks.CheckEmployerContrib(dt, ref employerContribErrorMsg);
            //  CheckEmployerLocCodeMsg = CodeRepository.SpreadSheetChecks.CheckEmployerLocCode1(dt, ref employerLocErrorMsg);

            // that should only be for march
            CheckAnnualRateOfPaySatus = CodeRepository.SpreadSheetChecks.CheckAnnualRateOfpay(dt,month, ref annualRateOfPayErrorMsg);

            CheckPayMainSatus = CodeRepository.SpreadSheetChecks.CheckPayMain(dt, ref payMainErrorMsg);

            //CheckPayMainSatus = CodeRepository.SpreadSheetChecks.CheckEmployeePayandContrib(dt, ref payMainErrorMsg);

            CheckGenderSatus = CodeRepository.SpreadSheetChecks.CheckGender(dt, ref genderErrorMsg);
            CheckOptOutFlageStatus = CodeRepository.SpreadSheetChecks.CheckOptFlage(dt, ref optOutFlagErrorMsg);
            //  CheckEmployeeContribSatus = CodeRepository.SpreadSheetChecks.CheckEmployeeContrib(dt, ref payEmployeeContribErrorMsg);


            CheckSpreadSheetErrorMsg += payrollPeriodErrorMsg;
            CheckSpreadSheetErrorMsg += speechMarkErrorMsg;
            CheckSpreadSheetErrorMsg += payrollYearErrorMsg;
            CheckSpreadSheetErrorMsg += employerLocCodeErrorMsg;
            CheckSpreadSheetErrorMsg += memberTitleErrorMsg;

            CheckSpreadSheetErrorMsg += memberSurnameErrorMsg;
            CheckSpreadSheetErrorMsg += memberForenameErrorMsg;
            CheckSpreadSheetErrorMsg += memberAddress1ErrorMsg;
            //CheckSpreadSheetErrorMsg += memberAddress2ErrorMsg;
            CheckSpreadSheetErrorMsg += memberPostcodeErrorMsg;
            CheckSpreadSheetErrorMsg += memberNINOErrorMsg;
            CheckSpreadSheetErrorMsg += memberPayrefErrorMsg;
            CheckSpreadSheetErrorMsg += memberDOBErrorMsg;
            CheckSpreadSheetErrorMsg += memberFTPTFlagErrorMsg;
            CheckSpreadSheetErrorMsg += memberStdHoursErrorMsg;
            CheckSpreadSheetErrorMsg += memberContractualHoursErrorMsg;
            CheckSpreadSheetErrorMsg += memberDateJSErrorMsg;
            CheckSpreadSheetErrorMsg += memberDateLSErrorMsg;

            CheckSpreadSheetErrorMsg += memberDateOptOutErrorMsg;
            CheckSpreadSheetErrorMsg += memberDate505StartErrorMsg;
            CheckSpreadSheetErrorMsg += memberDate505EndErrorMsg;

            CheckSpreadSheetErrorMsg += memberEnrolmentTypeErrorMsg;
            CheckSpreadSheetErrorMsg += employerContribErrorMsg;
            CheckSpreadSheetErrorMsg += annualRateOfPayErrorMsg;
            CheckSpreadSheetErrorMsg += payMainErrorMsg;
            CheckSpreadSheetErrorMsg += genderErrorMsg;
            CheckSpreadSheetErrorMsg += optOutFlagErrorMsg;
            CheckSpreadSheetErrorMsg += payEmployeeContribErrorMsg;
            CheckSpreadSheetErrorMsg += employerContribErrorMsg;
            CheckSpreadSheetErrorMsg += employerLocErrorMsg;

            // AllSpreadsheetErrors.Add(CheckSpreadSheetErrorMsg);
            //Label3.Text = CheckSpreadSheetErrorMsg;
           

            //Q-Amended code
            result = CheckSpreadSheetErrorMsg;

            // if any check false then return false to calling method
            if (CheckPayrollPeriodStatus == false || CheckEmployerLocCodeStatus == false
                || CheckPayrollYearStatus == false || CheckMemberTitleStatus == false
                || CheckMemberSurnameStatus == false || CheckMemberForenameStatus == false
                || CheckMemberAddress1Status == false
                || CheckMemberPostcodeStatus == false || CheckMemberNINOStatus == false
                || CheckMemberPayrefStatus == false || CheckDOBStatus == false
                || CheckFTPTFlagStatus == false || CheckStdHoursStatus == false
                || CheckContractualHoursStatus == false || CheckDateJSSatus == false
                || CheckDateLSSatus == false || CheckDateOptOuttus == false
                || CheckDate505StartSatus == false || CheckDate505EndSatus == false
                || CheckEnrolmentTypeSatus == false || CheckEmployerContribSatus == false
                || CheckAnnualRateOfPaySatus == false 
                || CheckPayMainSatus == false || CheckGenderSatus == false
                || CheckOptOutFlageStatus == false || CheckEmployeeContribSatus == false
                || CheckEmployerLocCodeMsg == false

               )
            {
                // Change result to true then file with errors will be downloaded
                 result = CheckSpreadSheetErrorMsg;
                //result = false;

            }

            return result;

        }


        /// <summary>
        /// checks errors in Fire file
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="CheckSpreadSheetErrorMsg"></param>
        /// <returns></returns>
        public string CheckSpreadsheetValuesFire(DataTable dt,string month, string posting, string validPayrollYr, string[] validTitles,string[] invalidSigns, ref string CheckSpreadSheetErrorMsg)
        {

            string result = string.Empty;
            bool CheckPayrollPeriodStatus = true;
            bool CheckPayrollYearStatus = true;
            bool CheckEmployerLocCodeStatus = true;
            bool CheckMemberTitleStatus = true;
            bool CheckEmployerName = true;
            bool CheckMemberSurnameStatus = true;
            bool CheckMemberForenameStatus = true;
            bool CheckMemberAddress1Status = true;
            bool CheckMemberAddress2Status = true;
            bool CheckMemberPostcodeStatus = true;
            bool CheckMemberNINOStatus = true;
            bool CheckMemberPayrefStatus = true;
            bool CheckDOBStatus = true;
            bool CheckFTPTFlagStatus = true;
            bool CheckStdHoursStatus = true;
            bool CheckContractualHoursStatus = true;
            bool CheckDateJSSatus = true;
            bool CheckDateLSSatus = true;

            bool CheckDateOptOuttus = true;
            bool CheckDate505StartSatus = true;
            bool CheckDate505EndSatus = true;

            bool CheckEnrolmentTypeSatus = true;
            bool CheckEmployerContribSatus = true;
            bool CheckAnnualRateOfPaySatus = true;
            bool CheckPayMainSatus = true;

            bool CheckEmployeeContribSatus = true;

            bool CheckGenderSatus = true;
            bool CheckOptOutFlageStatus = true;
            bool CheckSchemeName = true;
            bool CheckRangRole = true;
            bool CheckStdHours = true;
            bool CheckScheme1992And2006Value = true;
            bool CheckScheme2015Value = true;
            bool CheckEmployeeContributionValue = true;
            bool CheckTempPromForScheme1992And2006Value = true;
            bool CheckCPDForScheme1992And2006Value = true;
            bool CheckPurchase60ForScheme1992And2006Value = true;
            bool CheckAddedPensionContribFor2015Value = true;
            bool CheckAnnualRateOfPayValue = true;
            bool CheckAvgPenPayValue = true;
            bool CheckEmployerLocCodeMsg = true;

            bool CheckRTFlageValue = true;
            bool CheckSchemeAndDOBFlageValue = true;

            string payrollPeriodErrorMsg = string.Empty;
            string payrollYearErrorMsg = string.Empty;
            string employerLocCodeErrorMsg = string.Empty;
            string memberTitleErrorMsg = string.Empty;
            string memberSurnameErrorMsg = string.Empty;
            string memberForenameErrorMsg = string.Empty;
            string memberAddress1ErrorMsg = string.Empty;
            string memberAddress2ErrorMsg = string.Empty;
            string memberPostcodeErrorMsg = string.Empty;
            string memberNINOErrorMsg = string.Empty;
            string memberPayrefErrorMsg = string.Empty;
            string memberDOBErrorMsg = string.Empty;
            string memberFTPTFlagErrorMsg = string.Empty;
            string memberStdHoursErrorMsg = string.Empty;
            string memberContractualHoursErrorMsg = string.Empty;
            string memberDateJSErrorMsg = string.Empty;
            string memberDateLSErrorMsg = string.Empty;

            string memberDateOptOutErrorMsg = string.Empty;
            string memberDate505StartErrorMsg = string.Empty;
            string memberDate505EndErrorMsg = string.Empty;

            string memberEnrolmentTypeErrorMsg = string.Empty;
            string employerContribErrorMsg = string.Empty;
            string annualRateOfPayErrorMsg = string.Empty;
            string payMainErrorMsg = string.Empty;

            string payEmployeeContribErrorMsg = string.Empty;

            string genderErrorMsg = string.Empty;
            string optOutFlagErrorMsg = string.Empty;

            string employerNameErrorMsg = string.Empty;
            string schemeNameErrorMsg = string.Empty;
            string ranRoleErrorMsg = string.Empty;
            string stdHoursErrorMsg = string.Empty;
            string schemeValue1992And2006ErrorMsg = string.Empty;
            string schemeValue2015ErrorMsg = string.Empty;
            string employeeContributionErrorMsg = string.Empty;
            string tempPromotionFor2992And2006ErrorMsg = string.Empty;
            string CPDFor2992And2006ErrorMsg = string.Empty;
            string purchase60For2992And2006ErrorMsg = string.Empty;
            string addedPensionContribFor2015ErrorMsg = string.Empty;
            string added_RT_flage_for_scheme_Msg = string.Empty;
            string annualRateOfPensionErrorMsg = string.Empty;
            string avgPenPayErrorMsg = string.Empty;
            string schemeNameAndDOB = string.Empty;
            string employerLocErrorMsg = string.Empty;
            string speechMarkErrorMsg = string.Empty;

            CheckPayrollPeriodStatus = CodeRepository.SpreadSheetChecks.CheckPayrollPeriod(ref dt, month, posting, ref payrollPeriodErrorMsg);
            CheckPayrollPeriodStatus = CodeRepository.SpreadSheetChecks.CheckSpeechMark(dt, invalidSigns, ref speechMarkErrorMsg);
            CheckPayrollYearStatus = CodeRepository.SpreadSheetChecks.CheckPayrollYear(ref dt,posting, validPayrollYr, ref payrollYearErrorMsg);
            CheckEmployerLocCodeStatus = repo.CheckEmployerLocCode(dt, ref employerLocCodeErrorMsg);
            
            CheckEmployerName = CodeRepository.SpreadSheetChecks.CheckTitle(ref dt,validTitles, ref employerNameErrorMsg);
            CheckMemberTitleStatus = CodeRepository.SpreadSheetChecks.CheckEmployerName(ref dt, ref memberTitleErrorMsg);

            CheckSchemeName = CodeRepository.SpreadSheetChecks.CheckFireScheme(ref dt, ref schemeNameErrorMsg);
            CheckRangRole = CodeRepository.SpreadSheetChecks.CheckRankRole(dt, ref ranRoleErrorMsg);
            CheckStdHours = CodeRepository.SpreadSheetChecks.CheckStandardHours(dt, ref stdHoursErrorMsg);

            CheckMemberSurnameStatus = CodeRepository.SpreadSheetChecks.CheckSurname(ref dt, ref memberSurnameErrorMsg);
            CheckMemberForenameStatus = CodeRepository.SpreadSheetChecks.CheckForename(ref dt, ref memberForenameErrorMsg);
            CheckMemberAddress1Status = CodeRepository.SpreadSheetChecks.CheckAddress1(ref dt, ref memberAddress1ErrorMsg);
            CheckMemberAddress2Status = CodeRepository.SpreadSheetChecks.CheckAddress2(ref dt, ref memberAddress2ErrorMsg);
            CheckMemberPostcodeStatus = CodeRepository.SpreadSheetChecks.CheckPostcode(ref dt, ref memberPostcodeErrorMsg);
            CheckMemberNINOStatus = CodeRepository.SpreadSheetChecks.CheckNINO(ref dt, ref memberNINOErrorMsg);
            CheckMemberPayrefStatus = CodeRepository.SpreadSheetChecks.CheckPayRef(ref dt, ref memberPayrefErrorMsg);
            CheckDOBStatus = CodeRepository.SpreadSheetChecks.CheckDOB(dt, ref memberDOBErrorMsg);
            CheckFTPTFlagStatus = CodeRepository.SpreadSheetChecks.CheckFullTimePartTimeFlag(ref dt, ref memberFTPTFlagErrorMsg);
            //CheckStdHoursStatus = App_Code.SpreadSheetChecks.CheckStandardHours(dt, ref memberStdHoursErrorMsg);

            CheckScheme1992And2006Value = CodeRepository.SpreadSheetChecks.CheckPensionablePay1992And2006(dt, ref schemeValue1992And2006ErrorMsg);
            CheckTempPromForScheme1992And2006Value = CodeRepository.SpreadSheetChecks.CheckTempPromotionContrib1992And2006(dt, ref tempPromotionFor2992And2006ErrorMsg);
            CheckCPDForScheme1992And2006Value = CodeRepository.SpreadSheetChecks.CheckCPDContrib1992And2006(dt, ref CPDFor2992And2006ErrorMsg);
            CheckPurchase60ForScheme1992And2006Value = CodeRepository.SpreadSheetChecks.CheckPurchaseOf60Contrib1992And2006(dt, ref purchase60For2992And2006ErrorMsg);
            CheckAddedPensionContribFor2015Value = CodeRepository.SpreadSheetChecks.CheckAddedPensionContributionFor2015(dt, ref addedPensionContribFor2015ErrorMsg);

            CheckRTFlageValue = CodeRepository.SpreadSheetChecks.CheckRetainedFireDOB(dt, ref added_RT_flage_for_scheme_Msg);
            CheckSchemeAndDOBFlageValue = CodeRepository.SpreadSheetChecks.CheckSchmeNameAgainstDOB(dt, ref schemeNameAndDOB);



            CheckAnnualRateOfPayValue = CodeRepository.SpreadSheetChecks.CheckAnnualRateOfPenPay(dt, ref annualRateOfPensionErrorMsg);
            CheckAvgPenPayValue = CodeRepository.SpreadSheetChecks.CheckAvgPenPay(dt, ref avgPenPayErrorMsg);

            CheckScheme2015Value = CodeRepository.SpreadSheetChecks.CheckPensionablePay2015(dt, ref schemeValue2015ErrorMsg);
            CheckEmployeeContributionValue = CodeRepository.SpreadSheetChecks.CheckEmployeeContribution(dt, ref employeeContributionErrorMsg);


            CheckContractualHoursStatus = CodeRepository.SpreadSheetChecks.CheckContractualHoursFire(dt, ref memberContractualHoursErrorMsg);
            CheckDateJSSatus = CodeRepository.SpreadSheetChecks.CheckDateJS(dt, ref memberDateJSErrorMsg);
            CheckDateLSSatus = CodeRepository.SpreadSheetChecks.CheckDateLS(dt, ref memberDateLSErrorMsg);

            CheckDateOptOuttus = CodeRepository.SpreadSheetChecks.CheckDateOptOut(dt, ref memberDateOptOutErrorMsg);
            //CheckDate505StartSatus = App_Code.SpreadSheetChecks.CheckDate505StartDate(dt, ref memberDate505StartErrorMsg);
            //CheckDate505EndSatus = App_Code.SpreadSheetChecks.CheckDate505EndDate(dt, ref memberDate505EndErrorMsg);
            CheckEnrolmentTypeSatus = CodeRepository.SpreadSheetChecks.CheckEnrolmentType(dt, ref memberEnrolmentTypeErrorMsg);
            //CheckEmployerContribSatus = App_Code.SpreadSheetChecks.CheckEmployerContrib(dt, ref employerContribErrorMsg);

            // that should only be for march
            //CheckAnnualRateOfPaySatus = App_Code.SpreadSheetChecks.CheckAnnualRateOfpay(dt, ref annualRateOfPayErrorMsg);

            CheckPayMainSatus = CodeRepository.SpreadSheetChecks.CheckPayMain(dt, ref payMainErrorMsg);
            CheckGenderSatus = CodeRepository.SpreadSheetChecks.CheckGender(dt, ref genderErrorMsg);
            CheckOptOutFlageStatus = CodeRepository.SpreadSheetChecks.CheckOptFlage(dt, ref optOutFlagErrorMsg);
            //CheckEmployeeContribSatus = App_Code.SpreadSheetChecks.CheckEmployeeContrib(dt, ref payEmployeeContribErrorMsg);

            //   CheckEmployerLocCodeMsg = App_Code.SpreadSheetChecks.CheckEmployerLocCode1(dt, ref employerLocErrorMsg);


            CheckSpreadSheetErrorMsg = payrollPeriodErrorMsg;
            CheckSpreadSheetErrorMsg += speechMarkErrorMsg;
            CheckSpreadSheetErrorMsg += payrollYearErrorMsg;
            CheckSpreadSheetErrorMsg += employerLocCodeErrorMsg;
            CheckSpreadSheetErrorMsg += memberTitleErrorMsg;

            CheckSpreadSheetErrorMsg += employerNameErrorMsg;

            CheckSpreadSheetErrorMsg += schemeNameErrorMsg;
            CheckSpreadSheetErrorMsg += ranRoleErrorMsg;
            CheckSpreadSheetErrorMsg += stdHoursErrorMsg;

            CheckSpreadSheetErrorMsg += memberSurnameErrorMsg;
            CheckSpreadSheetErrorMsg += memberForenameErrorMsg;
            CheckSpreadSheetErrorMsg += memberAddress1ErrorMsg;
            //CheckSpreadSheetErrorMsg += memberAddress2ErrorMsg;
            CheckSpreadSheetErrorMsg += memberPostcodeErrorMsg;
            CheckSpreadSheetErrorMsg += memberNINOErrorMsg;
            CheckSpreadSheetErrorMsg += memberPayrefErrorMsg;
            CheckSpreadSheetErrorMsg += memberDOBErrorMsg;
            CheckSpreadSheetErrorMsg += memberFTPTFlagErrorMsg;
            CheckSpreadSheetErrorMsg += memberStdHoursErrorMsg;
            CheckSpreadSheetErrorMsg += memberContractualHoursErrorMsg;
            CheckSpreadSheetErrorMsg += memberDateJSErrorMsg;
            CheckSpreadSheetErrorMsg += memberDateLSErrorMsg;

            CheckSpreadSheetErrorMsg += memberDateOptOutErrorMsg;
            CheckSpreadSheetErrorMsg += memberDate505StartErrorMsg;
            CheckSpreadSheetErrorMsg += memberDate505EndErrorMsg;

            CheckSpreadSheetErrorMsg += memberEnrolmentTypeErrorMsg;
            CheckSpreadSheetErrorMsg += employerContribErrorMsg;
            //CheckSpreadSheetErrorMsg += annualRateOfPayErrorMsg;
            CheckSpreadSheetErrorMsg += payMainErrorMsg;
            CheckSpreadSheetErrorMsg += genderErrorMsg;
            CheckSpreadSheetErrorMsg += optOutFlagErrorMsg;
            CheckSpreadSheetErrorMsg += payEmployeeContribErrorMsg;
            CheckSpreadSheetErrorMsg += schemeValue1992And2006ErrorMsg;
            CheckSpreadSheetErrorMsg += schemeValue2015ErrorMsg;
            CheckSpreadSheetErrorMsg += employeeContributionErrorMsg;
            CheckSpreadSheetErrorMsg += tempPromotionFor2992And2006ErrorMsg;
            CheckSpreadSheetErrorMsg += CPDFor2992And2006ErrorMsg;
            CheckSpreadSheetErrorMsg += purchase60For2992And2006ErrorMsg;
            CheckSpreadSheetErrorMsg += addedPensionContribFor2015ErrorMsg;
            CheckSpreadSheetErrorMsg += annualRateOfPensionErrorMsg;
            CheckSpreadSheetErrorMsg += avgPenPayErrorMsg;
            CheckSpreadSheetErrorMsg += added_RT_flage_for_scheme_Msg;
            CheckSpreadSheetErrorMsg += schemeNameAndDOB;
            CheckSpreadSheetErrorMsg += employerLocErrorMsg;

            // AllSpreadsheetErrors.Add(CheckSpreadSheetErrorMsg);
           // Label3.Text = CheckSpreadSheetErrorMsg;

            //Q-Amended code
            result = CheckSpreadSheetErrorMsg;


            // if any check false then return false to calling method
            if (CheckPayrollPeriodStatus == false || CheckEmployerLocCodeStatus == false
                || CheckPayrollYearStatus == false || CheckMemberTitleStatus == false
                || CheckMemberSurnameStatus == false || CheckMemberForenameStatus == false
                || CheckMemberAddress1Status == false
                || CheckMemberPostcodeStatus == false || CheckMemberNINOStatus == false
                || CheckMemberPayrefStatus == false || CheckDOBStatus == false
                || CheckFTPTFlagStatus == false || CheckStdHoursStatus == false
                || CheckContractualHoursStatus == false || CheckDateJSSatus == false
                || CheckDateLSSatus == false || CheckDateOptOuttus == false
                || CheckDate505StartSatus == false || CheckDate505EndSatus == false
                || CheckEnrolmentTypeSatus == false || CheckEmployerContribSatus == false
                //|| CheckAnnualRateOfPaySatus == false 
                || CheckPayMainSatus == false || CheckGenderSatus == false
                || CheckOptOutFlageStatus == false || CheckEmployeeContribSatus == false
                || CheckEmployerName == false || CheckSchemeName == false
                || CheckRangRole == false || CheckStdHours == false
                || CheckScheme1992And2006Value == false || CheckScheme2015Value == false
                || CheckEmployeeContributionValue == false || CheckTempPromForScheme1992And2006Value == false
                || CheckCPDForScheme1992And2006Value == false || CheckPurchase60ForScheme1992And2006Value == false
                || CheckAddedPensionContribFor2015Value == false || CheckAnnualRateOfPayValue == false
                || CheckAvgPenPayValue == false || CheckRTFlageValue == false
                || CheckSchemeAndDOBFlageValue == false

               )
            {
                result = CheckSpreadSheetErrorMsg;
            }

            return result;

        }

    }
}
