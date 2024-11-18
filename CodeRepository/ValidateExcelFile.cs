using MCPhase3.Models;
using MCPhase3.ViewModels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MCPhase3.CodeRepository
{
    public class ValidateExcelFile : IValidateExcelFile
    {
        private readonly ICommonRepo _commonRepo;
        private readonly IConfiguration _configuration;
        private readonly string[] _financeMonthNames;

        public ValidateExcelFile(ICommonRepo CommonRepo, IConfiguration Configuration)
        {
            _commonRepo = CommonRepo;
            _configuration = Configuration;
            _financeMonthNames = _configuration["FinanceMonthNames"].Split(',').ToArray();   //## All Financial Month names are kept in a Zero index based Array.
        }

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
        public string Validate(DataTable dt,string month,string posting, string validPayrollYr, List<PayrollProvidersBO> validPayLocations, ref string CheckSpreadSheetErrorMsg)
        {
            return "";

        }


        public string Validate(List<ExcelsheetDataVM> excelData, string month, string posting, string validPayrollYr, List<PayrollProvidersBO> validPayLocations)
        {

            //Member titles coming from config - 
            string[] validTitles = _configuration["ValidMemberTitles"].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //Check all invalid signs from file and show error to employer
            string[] invalidSigns = _configuration["SignToCheck"].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            var validMonths = _configuration["ValidMonths"];

            var errorMessage = new StringBuilder();

            //List<ExcelsheetDataVM> excelData, string selectedMonth, string selectedPosting, string validMonthNames, ref StringBuilder CheckSpreadSheetErrorMsg
            SpreadSheetChecks.CheckPayrollPeriod(excelData.Select(e=> e.PAYROLL_PD).ToList(), month, posting, validMonths, ref errorMessage);
            SpreadSheetChecks.CheckPayrollYear(excelData.Select(e => e.PAYROLL_YR).ToList(), validPayrollYr, ref errorMessage);            
            SpreadSheetChecks.CheckEmployerLocCode(excelData.Select(e => e.EMPLOYER_LOC_CODE).ToList(), validPayLocations.Select(e => e.Pay_Location_Ref).ToList(), ref errorMessage);
                        
            //## Title - data cleansing- remove '.' and any blank spaces..            
            foreach (var item in excelData)
            {
                if (item.MEMBERS_TITLE.Contains('.') || item.MEMBERS_TITLE.Contains(' ')) { 
                    item.MEMBERS_TITLE = item.MEMBERS_TITLE.Replace(".", "").Replace(" ", "");                
                }
            }
            SpreadSheetChecks.CheckTitle(excelData.Select(e => e.MEMBERS_TITLE).ToList(), validTitles.ToList(), ref errorMessage);
            SpreadSheetChecks.CheckEmptyValues("Forename", excelData.Select(e => e.FORENAMES).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckEmptyValues("Surname", excelData.Select(e => e.SURNAME).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckDateValues("DOB", excelData.Select(e => e.DOB).ToList(), ref errorMessage, isMandatoryField: true);
            SpreadSheetChecks.CheckGender(excelData.Select(e => e.GENDER).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckAddress_Line_1(excelData.Select(e => e.ADDRESS1).ToList(), ref errorMessage);                        
            //SpreadSheetChecks.CheckEmptyValues("Address4", excelData.Select(e => e.ADDRESS4).ToList(), ref errorMessage);   //## Address_Line_4 = City
            SpreadSheetChecks.CheckPostcode(excelData.Select(e => e.POSTCODE).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckNINO(excelData.Select(e => e.NI_NUMBER).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckEmptyValues("PAYREF", excelData.Select(e => e.PAYREF).ToList(), ref errorMessage);
            
            SpreadSheetChecks.CheckEmptyValues("FT_PT_CS_FLAG", excelData.Select(e => e.FT_PT_CS_FLAG).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckNumberValues("FT_PT_HOURS_WORKED", excelData.Select(e => e.FT_PT_HOURS_WORKED).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckNumberValues("STD_HOURS", excelData.Select(e => e.STD_HOURS).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckNumberValues("CONTRACTUAL_HRS", excelData.Select(e => e.CONTRACTUAL_HRS).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckContractualHoursAgainstFlagTypeLG(excelData, ref errorMessage);            
            SpreadSheetChecks.CheckDateValues("DATE_JOINED_SCHEME", excelData.Select(e => e.DATE_JOINED_SCHEME).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckDateValues("DATE_OF_LEAVING_SCHEME", excelData.Select(e => e.DATE_OF_LEAVING_SCHEME).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckOptoutFlag(excelData.Select(e => e.OPTOUT_FLAG).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckDateValues("OPTOUT_DATE", excelData.Select(e => e.OPTOUT_DATE).ToList(), ref errorMessage);            
            SpreadSheetChecks.CheckLeaveDateVsOptoutDate(excelData, ref errorMessage);            
            SpreadSheetChecks.CheckDateValues("START_DATE_50_50", excelData.Select(e => e.START_DATE_50_50).ToList(), ref errorMessage);            
            SpreadSheetChecks.CheckDateValues("END_DATE_50_50", excelData.Select(e => e.END_DATE_50_50).ToList(), ref errorMessage);            
            var validEnrollmentTypeValues = _configuration["EnrollmentType_Valid_Values"].Split(",").ToList();            
            SpreadSheetChecks.CheckInvalidValues("ENROLMENT_TYPE", excelData.Select(e => e.ENROLMENT_TYPE).ToList(), validEnrollmentTypeValues, ref errorMessage);
            //SpreadSheetChecks.CheckPayMain(excelData, month, ref errorMessage);    //## 'PAY_MAIN'
            SpreadSheetChecks.CheckNumberValues("EE_CONT_MAIN", excelData.Select(e => e.EE_CONT_MAIN).ToList(), ref errorMessage);            
            SpreadSheetChecks.CheckNumberValues("PAY_50_50", excelData.Select(e => e.PAY_50_50).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckNumberValues("EE_CONT_50_50", excelData.Select(e => e.EE_CONT_50_50).ToList(), ref errorMessage);            
            SpreadSheetChecks.CheckNumberValues("PRCHS_OF_SRV", excelData.Select(e => e.PRCHS_OF_SRV).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckNumberValues("ARC_CONTS", excelData.Select(e => e.ARC_CONTS).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckNumberValues("EE_APC_CONTS", excelData.Select(e => e.EE_APC_CONTS).ToList(), ref errorMessage);
            SpreadSheetChecks.CheckNumberValues("ER_CONTS", excelData.Select(e => e.ER_CONTS).ToList(), ref errorMessage, isMandatory: true);
            SpreadSheetChecks.CheckNumberValues("ANNUAL_RATE_OF_PAY", excelData.Select(e => e.ANNUAL_RATE_OF_PAY).ToList(), ref errorMessage);
            //// that should only be for the last month of the Year, ie: March
            SpreadSheetChecks.CheckAnnualRateOfpay(excelData.Select(e => e.ANNUAL_RATE_OF_PAY).ToList(), month, ref errorMessage);

            SpreadSheetChecks.CheckNumberValues("TOTAL_AVC_CONTRIBUTIONS_PAID", excelData.Select(e => e.TOTAL_AVC_CONTRIBUTIONS_PAID).ToList(), ref errorMessage);            

            return errorMessage.ToString();
        }



        public string GetPreviousPeriodName(string financialYear, string financialMonth)
        {            
            /* Get the Previous Month Name */
            int currentMonthNumber = FinancialMonthNameToNumber(financialMonth);
            string previousMonthName = _financeMonthNames[currentMonthNumber - 1];   //## For any other month rather than April- just minus 1 from that month will give u previous finance month for that year

            financialYear = GetPreviousYearPeriod(currentMonthNumber, financialYear);

            return $"{previousMonthName} - {financialYear}".ToUpper();
        }


        public TaskResults FutureMonthRecordFound(List<ExcelsheetDataVM> excelSheetData, string selectedFinancialYear, string selectedMonth)
        {
            //public TaskResults FutureMonthRecordFound(List<ExcelsheetDataVM> excelSheetData, int selectedFinancialYear, int monthNumber)
            StringBuilder invalidRecords = new();
            var result = new TaskResults();
            int selectedYear = GetYear(selectedFinancialYear);
            int monthNumber = FinancialMonthNameToNumber(selectedMonth);

            int rowNumber = 1;

            try
            {
                foreach (var item in excelSheetData)
                {
                    if (GetYear(item.PAYROLL_YR) > selectedYear)
                    {
                        invalidRecords.Append($"{rowNumber}, ");
                    }
                    else if (GetYear(item.PAYROLL_YR) == selectedYear && FinancialMonthNameToNumber(item.PAYROLL_PD) > monthNumber)
                    {
                        invalidRecords.Append($"{rowNumber}, ");
                    }

                    rowNumber++;
                }
            }
            catch (Exception ex)
            {
                result.Message = $"<h5>Invalid Month/Year values found at row: {rowNumber}. File validation aborted! Please check all Month/Year values are valid and try again.</h5>";                
                return result;
            }

            result.IsSuccess = invalidRecords.Length < 1;

            if (!result.IsSuccess)
            {                
                result.Message = $"<h5>Invalid records found. Submissions for a future month is not allowed.</h5>Rows at: {invalidRecords}";
            }

            return result;
        }

        public string GetPreviousYearPeriod(int currentMonthNumber, string currentFinancialYear)
        {
            if (currentMonthNumber == 1)
            {
                //## If Current Finance Month is April- then Previous month will be March, but for the previous Finance Year, which will make the month number to 12, and Previous year
                int financeYearPart = GetYear(currentFinancialYear);    //## take the first part of the Fin-Year

                return $"{financeYearPart - 1}/{financeYearPart - 2000}"; //## For 'April 2023/24' -> Previous month will be 'March 2022/23'- Year will change, too
            }


            return currentFinancialYear;
        }


        public int FinancialMonthNameToNumber(string financialMonth)
        {
            int currentMonthNumber = Array.IndexOf(_financeMonthNames, financialMonth[..3].ToLower());
            return currentMonthNumber;
        }


        /// <summary>This will return the Year Part from the Financial Year value</summary>
        /// <param name="financialYear">Financial Year value, ie: 2023/24</param>
        /// <returns>Returns the First part, ie: 2023</returns>
        private int GetYear(string financialYear)
        {
            //int yearPart = Convert.ToInt16(financialYear.Split("/")[1]) + 2000;
            int financeYearPart = Convert.ToInt16(financialYear.Split("/")[1]) + 2000;    //## take the 4 letters from Left, ie: '2024'
            return financeYearPart;
        }



        /// <summary>
        /// checks errors in Fire file
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="CheckSpreadSheetErrorMsg"></param>
        /// <returns></returns>
        public string CheckSpreadsheetValuesFire(DataTable dt, string month, string posting, string validPayrollYr, string[] validTitles, string[] invalidSigns, ref string CheckSpreadSheetErrorMsg)
        {

            // string result = string.Empty;
            // bool CheckPayrollPeriodStatus = true;
            // bool CheckPayrollYearStatus = true;
            // bool CheckEmployerLocCodeStatus = true;
            // bool CheckMemberTitleStatus = true;
            // bool CheckEmployerName = true;
            // bool CheckMemberSurnameStatus = true;
            // bool CheckMemberForenameStatus = true;
            // bool CheckMemberAddress1Status = true;
            // bool CheckMemberAddress2Status = true;
            // bool CheckMemberPostcodeStatus = true;
            // bool CheckMemberNINOStatus = true;
            // bool CheckMemberPayrefStatus = true;
            // bool CheckDOBStatus = true;
            // bool CheckFTPTFlagStatus = true;
            // bool CheckStdHoursStatus = true;
            // bool CheckContractualHoursStatus = true;
            // bool CheckDateJSSatus = true;
            // bool CheckDateLSSatus = true;

            // bool CheckDateOptOuttus = true;
            // bool CheckDate505StartSatus = true;
            // bool CheckDate505EndSatus = true;

            // bool CheckEnrolmentTypeSatus = true;
            // bool CheckEmployerContribSatus = true;
            // bool CheckAnnualRateOfPaySatus = true;
            // bool CheckPayMainSatus = true;

            // bool CheckEmployeeContribSatus = true;

            // bool CheckGenderSatus = true;
            // bool CheckOptOutFlageStatus = true;
            // bool CheckSchemeName = true;
            // bool CheckRangRole = true;
            // bool CheckStdHours = true;
            // bool CheckScheme1992And2006Value = true;
            // bool CheckScheme2015Value = true;
            // bool CheckEmployeeContributionValue = true;
            // bool CheckTempPromForScheme1992And2006Value = true;
            // bool CheckCPDForScheme1992And2006Value = true;
            // bool CheckPurchase60ForScheme1992And2006Value = true;
            // bool CheckAddedPensionContribFor2015Value = true;
            // bool CheckAnnualRateOfPayValue = true;
            // bool CheckAvgPenPayValue = true;
            // bool CheckEmployerLocCodeMsg = true;

            // bool CheckRTFlageValue = true;
            // bool CheckSchemeAndDOBFlageValue = true;

            // string payrollPeriodErrorMsg = string.Empty;
            // string payrollYearErrorMsg = string.Empty;
            // string employerLocCodeErrorMsg = string.Empty;
            // string memberTitleErrorMsg = string.Empty;
            // string memberSurnameErrorMsg = string.Empty;
            // string memberForenameErrorMsg = string.Empty;
            // string memberAddress1ErrorMsg = string.Empty;
            // string memberAddress2ErrorMsg = string.Empty;
            // string memberPostcodeErrorMsg = string.Empty;
            // string memberNINOErrorMsg = string.Empty;
            // string memberPayrefErrorMsg = string.Empty;
            // string memberDOBErrorMsg = string.Empty;
            // string memberFTPTFlagErrorMsg = string.Empty;
            // string memberStdHoursErrorMsg = string.Empty;
            // string memberContractualHoursErrorMsg = string.Empty;
            // string memberDateJSErrorMsg = string.Empty;
            // string memberDateLSErrorMsg = string.Empty;

            // string memberDateOptOutErrorMsg = string.Empty;
            // string memberDate505StartErrorMsg = string.Empty;
            // string memberDate505EndErrorMsg = string.Empty;

            // string memberEnrolmentTypeErrorMsg = string.Empty;
            // string employerContribErrorMsg = string.Empty;
            // string annualRateOfPayErrorMsg = string.Empty;
            // string payMainErrorMsg = string.Empty;

            // string payEmployeeContribErrorMsg = string.Empty;

            // string genderErrorMsg = string.Empty;
            // string optOutFlagErrorMsg = string.Empty;

            // string employerNameErrorMsg = string.Empty;
            // string schemeNameErrorMsg = string.Empty;
            // string ranRoleErrorMsg = string.Empty;
            // string stdHoursErrorMsg = string.Empty;
            // string schemeValue1992And2006ErrorMsg = string.Empty;
            // string schemeValue2015ErrorMsg = string.Empty;
            // string employeeContributionErrorMsg = string.Empty;
            // string tempPromotionFor2992And2006ErrorMsg = string.Empty;
            // string CPDFor2992And2006ErrorMsg = string.Empty;
            // string purchase60For2992And2006ErrorMsg = string.Empty;
            // string addedPensionContribFor2015ErrorMsg = string.Empty;
            // string added_RT_flage_for_scheme_Msg = string.Empty;
            // string annualRateOfPensionErrorMsg = string.Empty;
            // string avgPenPayErrorMsg = string.Empty;
            // string schemeNameAndDOB = string.Empty;
            // string employerLocErrorMsg = string.Empty;
            // string speechMarkErrorMsg = string.Empty;

            // CheckPayrollPeriodStatus = CodeRepository.SpreadSheetChecks.CheckPayrollPeriod(ref dt, month, posting, ref payrollPeriodErrorMsg);
            // CheckPayrollPeriodStatus = CodeRepository.SpreadSheetChecks.CheckSpeechMark(dt, invalidSigns, ref speechMarkErrorMsg);
            // CheckPayrollYearStatus = CodeRepository.SpreadSheetChecks.CheckPayrollYear(ref dt,posting, validPayrollYr, ref payrollYearErrorMsg);
            // CheckEmployerLocCodeStatus = _commonRepo.CheckEmployerLocCode(dt, ref employerLocCodeErrorMsg);

            // CheckEmployerName = CodeRepository.SpreadSheetChecks.CheckTitle(ref dt,validTitles, ref employerNameErrorMsg);
            // CheckMemberTitleStatus = CodeRepository.SpreadSheetChecks.CheckEmployerName(ref dt, ref memberTitleErrorMsg);

            // CheckSchemeName = CodeRepository.SpreadSheetChecks.CheckFireScheme(ref dt, ref schemeNameErrorMsg);
            // CheckRangRole = CodeRepository.SpreadSheetChecks.CheckRankRole(dt, ref ranRoleErrorMsg);
            // CheckStdHours = CodeRepository.SpreadSheetChecks.CheckStandardHours(dt, ref stdHoursErrorMsg);

            // CheckMemberSurnameStatus = CodeRepository.SpreadSheetChecks.CheckSurname(ref dt, ref memberSurnameErrorMsg);
            // CheckMemberForenameStatus = CodeRepository.SpreadSheetChecks.CheckForename(ref dt, ref memberForenameErrorMsg);
            // CheckMemberAddress1Status = CodeRepository.SpreadSheetChecks.CheckAddress1(ref dt, ref memberAddress1ErrorMsg);
            // CheckMemberAddress2Status = CodeRepository.SpreadSheetChecks.CheckAddress2(ref dt, ref memberAddress2ErrorMsg);
            // CheckMemberPostcodeStatus = CodeRepository.SpreadSheetChecks.CheckPostcode(ref dt, ref memberPostcodeErrorMsg);
            // CheckMemberNINOStatus = CodeRepository.SpreadSheetChecks.CheckNINO(ref dt, ref memberNINOErrorMsg);
            // CheckMemberPayrefStatus = CodeRepository.SpreadSheetChecks.CheckPayRef(ref dt, ref memberPayrefErrorMsg);
            // CheckDOBStatus = CodeRepository.SpreadSheetChecks.CheckDOB(dt, ref memberDOBErrorMsg);
            // CheckFTPTFlagStatus = CodeRepository.SpreadSheetChecks.CheckFullTimePartTimeFlag(ref dt, ref memberFTPTFlagErrorMsg);
            // //CheckStdHoursStatus = App_Code.SpreadSheetChecks.CheckStandardHours(dt, ref memberStdHoursErrorMsg);

            // CheckScheme1992And2006Value = CodeRepository.SpreadSheetChecks.CheckPensionablePay1992And2006(dt, ref schemeValue1992And2006ErrorMsg);
            // CheckTempPromForScheme1992And2006Value = CodeRepository.SpreadSheetChecks.CheckTempPromotionContrib1992And2006(dt, ref tempPromotionFor2992And2006ErrorMsg);
            // CheckCPDForScheme1992And2006Value = CodeRepository.SpreadSheetChecks.CheckCPDContrib1992And2006(dt, ref CPDFor2992And2006ErrorMsg);
            // CheckPurchase60ForScheme1992And2006Value = CodeRepository.SpreadSheetChecks.CheckPurchaseOf60Contrib1992And2006(dt, ref purchase60For2992And2006ErrorMsg);
            // CheckAddedPensionContribFor2015Value = CodeRepository.SpreadSheetChecks.CheckAddedPensionContributionFor2015(dt, ref addedPensionContribFor2015ErrorMsg);

            // CheckRTFlageValue = CodeRepository.SpreadSheetChecks.CheckRetainedFireDOB(dt, ref added_RT_flage_for_scheme_Msg);
            // CheckSchemeAndDOBFlageValue = CodeRepository.SpreadSheetChecks.CheckSchmeNameAgainstDOB(dt, ref schemeNameAndDOB);



            // CheckAnnualRateOfPayValue = CodeRepository.SpreadSheetChecks.CheckAnnualRateOfPenPay(dt, ref annualRateOfPensionErrorMsg);
            // CheckAvgPenPayValue = CodeRepository.SpreadSheetChecks.CheckAvgPenPay(dt, ref avgPenPayErrorMsg);

            // CheckScheme2015Value = CodeRepository.SpreadSheetChecks.CheckPensionablePay2015(dt, ref schemeValue2015ErrorMsg);
            // CheckEmployeeContributionValue = CodeRepository.SpreadSheetChecks.CheckEmployeeContribution(dt, ref employeeContributionErrorMsg);


            // CheckContractualHoursStatus = CodeRepository.SpreadSheetChecks.CheckContractualHoursFire(dt, ref memberContractualHoursErrorMsg);
            // CheckDateJSSatus = CodeRepository.SpreadSheetChecks.CheckDateJS(dt, ref memberDateJSErrorMsg);
            // CheckDateLSSatus = CodeRepository.SpreadSheetChecks.CheckDateLS(dt, ref memberDateLSErrorMsg);

            // CheckDateOptOuttus = CodeRepository.SpreadSheetChecks.CheckDateOptOut(dt, ref memberDateOptOutErrorMsg);
            // //CheckDate505StartSatus = App_Code.SpreadSheetChecks.CheckDate505StartDate(dt, ref memberDate505StartErrorMsg);
            // //CheckDate505EndSatus = App_Code.SpreadSheetChecks.CheckDate505EndDate(dt, ref memberDate505EndErrorMsg);
            // CheckEnrolmentTypeSatus = CodeRepository.SpreadSheetChecks.CheckEnrolmentType(dt, ref memberEnrolmentTypeErrorMsg);
            // //CheckEmployerContribSatus = App_Code.SpreadSheetChecks.CheckEmployerContrib(dt, ref employerContribErrorMsg);

            // // that should only be for march
            // //CheckAnnualRateOfPaySatus = App_Code.SpreadSheetChecks.CheckAnnualRateOfpay(dt, ref annualRateOfPayErrorMsg);

            // CheckPayMainSatus = CodeRepository.SpreadSheetChecks.CheckPayMain(dt, ref payMainErrorMsg);
            // CheckGenderSatus = CodeRepository.SpreadSheetChecks.CheckGender(dt, ref genderErrorMsg);
            // CheckOptOutFlageStatus = CodeRepository.SpreadSheetChecks.CheckOptFlage(dt, ref optOutFlagErrorMsg);
            // //CheckEmployeeContribSatus = App_Code.SpreadSheetChecks.CheckEmployeeContrib(dt, ref payEmployeeContribErrorMsg);

            // //   CheckEmployerLocCodeMsg = App_Code.SpreadSheetChecks.CheckEmployerLocCode1(dt, ref employerLocErrorMsg);


            // CheckSpreadSheetErrorMsg = payrollPeriodErrorMsg;
            // CheckSpreadSheetErrorMsg += speechMarkErrorMsg;
            // CheckSpreadSheetErrorMsg += payrollYearErrorMsg;
            // CheckSpreadSheetErrorMsg += employerLocCodeErrorMsg;
            // CheckSpreadSheetErrorMsg += memberTitleErrorMsg;

            // CheckSpreadSheetErrorMsg += employerNameErrorMsg;

            // CheckSpreadSheetErrorMsg += schemeNameErrorMsg;
            // CheckSpreadSheetErrorMsg += ranRoleErrorMsg;
            // CheckSpreadSheetErrorMsg += stdHoursErrorMsg;

            // CheckSpreadSheetErrorMsg += memberSurnameErrorMsg;
            // CheckSpreadSheetErrorMsg += memberForenameErrorMsg;
            // CheckSpreadSheetErrorMsg += memberAddress1ErrorMsg;
            // //CheckSpreadSheetErrorMsg += memberAddress2ErrorMsg;
            // CheckSpreadSheetErrorMsg += memberPostcodeErrorMsg;
            // CheckSpreadSheetErrorMsg += memberNINOErrorMsg;
            // CheckSpreadSheetErrorMsg += memberPayrefErrorMsg;
            // CheckSpreadSheetErrorMsg += memberDOBErrorMsg;
            // CheckSpreadSheetErrorMsg += memberFTPTFlagErrorMsg;
            // CheckSpreadSheetErrorMsg += memberStdHoursErrorMsg;
            // CheckSpreadSheetErrorMsg += memberContractualHoursErrorMsg;
            // CheckSpreadSheetErrorMsg += memberDateJSErrorMsg;
            // CheckSpreadSheetErrorMsg += memberDateLSErrorMsg;

            // CheckSpreadSheetErrorMsg += memberDateOptOutErrorMsg;
            // CheckSpreadSheetErrorMsg += memberDate505StartErrorMsg;
            // CheckSpreadSheetErrorMsg += memberDate505EndErrorMsg;

            // CheckSpreadSheetErrorMsg += memberEnrolmentTypeErrorMsg;
            // CheckSpreadSheetErrorMsg += employerContribErrorMsg;
            // //CheckSpreadSheetErrorMsg += annualRateOfPayErrorMsg;
            // CheckSpreadSheetErrorMsg += payMainErrorMsg;
            // CheckSpreadSheetErrorMsg += genderErrorMsg;
            // CheckSpreadSheetErrorMsg += optOutFlagErrorMsg;
            // CheckSpreadSheetErrorMsg += payEmployeeContribErrorMsg;
            // CheckSpreadSheetErrorMsg += schemeValue1992And2006ErrorMsg;
            // CheckSpreadSheetErrorMsg += schemeValue2015ErrorMsg;
            // CheckSpreadSheetErrorMsg += employeeContributionErrorMsg;
            // CheckSpreadSheetErrorMsg += tempPromotionFor2992And2006ErrorMsg;
            // CheckSpreadSheetErrorMsg += CPDFor2992And2006ErrorMsg;
            // CheckSpreadSheetErrorMsg += purchase60For2992And2006ErrorMsg;
            // CheckSpreadSheetErrorMsg += addedPensionContribFor2015ErrorMsg;
            // CheckSpreadSheetErrorMsg += annualRateOfPensionErrorMsg;
            // CheckSpreadSheetErrorMsg += avgPenPayErrorMsg;
            // CheckSpreadSheetErrorMsg += added_RT_flage_for_scheme_Msg;
            // CheckSpreadSheetErrorMsg += schemeNameAndDOB;
            // CheckSpreadSheetErrorMsg += employerLocErrorMsg;

            // // AllSpreadsheetErrors.Add(CheckSpreadSheetErrorMsg);
            //// Label3.Text = CheckSpreadSheetErrorMsg;

            // //Q-Amended code
            // result = CheckSpreadSheetErrorMsg;


            // // if any check false then return false to calling method
            // if (CheckPayrollPeriodStatus == false || CheckEmployerLocCodeStatus == false
            //     || CheckPayrollYearStatus == false || CheckMemberTitleStatus == false
            //     || CheckMemberSurnameStatus == false || CheckMemberForenameStatus == false
            //     || CheckMemberAddress1Status == false
            //     || CheckMemberPostcodeStatus == false || CheckMemberNINOStatus == false
            //     || CheckMemberPayrefStatus == false || CheckDOBStatus == false
            //     || CheckFTPTFlagStatus == false || CheckStdHoursStatus == false
            //     || CheckContractualHoursStatus == false || CheckDateJSSatus == false
            //     || CheckDateLSSatus == false || CheckDateOptOuttus == false
            //     || CheckDate505StartSatus == false || CheckDate505EndSatus == false
            //     || CheckEnrolmentTypeSatus == false || CheckEmployerContribSatus == false
            //     //|| CheckAnnualRateOfPaySatus == false 
            //     || CheckPayMainSatus == false || CheckGenderSatus == false
            //     || CheckOptOutFlageStatus == false || CheckEmployeeContribSatus == false
            //     || CheckEmployerName == false || CheckSchemeName == false
            //     || CheckRangRole == false || CheckStdHours == false
            //     || CheckScheme1992And2006Value == false || CheckScheme2015Value == false
            //     || CheckEmployeeContributionValue == false || CheckTempPromForScheme1992And2006Value == false
            //     || CheckCPDForScheme1992And2006Value == false || CheckPurchase60ForScheme1992And2006Value == false
            //     || CheckAddedPensionContribFor2015Value == false || CheckAnnualRateOfPayValue == false
            //     || CheckAvgPenPayValue == false || CheckRTFlageValue == false
            //     || CheckSchemeAndDOBFlageValue == false

            //    )
            // {
            //     result = CheckSpreadSheetErrorMsg;
            // }

            //return result;
            return "";

        }

    }

    public interface IValidateExcelFile
    {
        string Validate(DataTable dt, string month, string posting, string validPayrollYr, List<PayrollProvidersBO> validPayLocations, ref string CheckSpreadSheetErrorMsg);
        string Validate(List<ExcelsheetDataVM> excelData, string month, string posting, string validPayrollYr, List<PayrollProvidersBO> validPayLocations);

        string CheckSpreadsheetValuesFire(DataTable dt, string month, string posting, string validPayrollYr, string[] validTitles, string[] invalidSigns, ref string CheckSpreadSheetErrorMsg);
        TaskResults FutureMonthRecordFound(List<ExcelsheetDataVM> excelSheetData, string selectedFinancialYear, string selectedMonth);
        string GetPreviousPeriodName(string financialYear, string financialMonth);
        int FinancialMonthNameToNumber(string financialMonth);
        string GetPreviousYearPeriod(int currentMonthNumber, string currentFinancialYear);
    }
}
