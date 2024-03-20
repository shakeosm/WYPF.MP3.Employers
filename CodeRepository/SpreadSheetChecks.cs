using MCPhase3.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static MCPhase3.Common.Constants;

namespace MCPhase3.CodeRepository
{
    public class SpreadSheetChecks
    {
        
        #region Methods to validate and verify new Excel data in Class Object

        public static void CheckPayrollPeriod(List<string> payrollPeriod, string selectedMonth, string selectedPosting, string validMonthNames, ref StringBuilder errorMsg)
        {
            var validMonths = validMonthNames.Split(",").ToList();
            CheckEmptyValues("PAYROLL_PD", payrollPeriod, ref errorMsg);

            CheckInvalidValues("PAYROLL_PD", payrollPeriod, validMonths, ref errorMsg);     
        }

        public static void CheckPayrollYear(List<string> payrollYear, string validYears, ref StringBuilder errorMsg)
        {
            var validYearList = validYears.Split(",").ToList();
            CheckEmptyValues("PAYROLL_YR", payrollYear, ref errorMsg);

            CheckInvalidValues("PAYROLL_YR", payrollYear, validYearList, ref errorMsg);
        }


        public static void CheckFireScheme(List<string> schemeName, ref StringBuilder errorMsg)
        {
            string[] validSchemes = { "1992", "2006", "2015" };

            CheckEmptyValues("SCHEME_NAME", schemeName, ref errorMsg);

            CheckInvalidValues("SCHEME_NAME", schemeName, validSchemes.ToList(), ref errorMsg);
        }

        public static void CheckTitle(List<string> titleList, List<string> validTitles, ref StringBuilder errorMsg)
        {
            CheckEmptyValues("MEMBERS_TITLE", titleList, ref errorMsg);

            CheckInvalidValues("MEMBERS_TITLE", titleList, validTitles, ref errorMsg);
        }

        public static void CheckEmployerLocCode(List<string> employerLocCodeList, List<string> validPayLocList, ref StringBuilder errorMsg)
        {

            CheckEmptyValues("EMPLOYER_LOC_CODE", employerLocCodeList, ref errorMsg);

            CheckInvalidValues("EMPLOYER_LOC_CODE", employerLocCodeList, validPayLocList, ref errorMsg);

        }

        public static void CheckAddress_Line_1(List<string> addressList, ref StringBuilder errorMsg)
        {

            CheckEmptyValues("ADDRESS1", addressList, ref errorMsg);

            List<int> invalidValueRowNumber = new();

            if (addressList.Any(a=> a.Length < 5))
            {
                int rowNumber = 1;
                foreach (var item in addressList)
                {
                    if (item.Length < 5) {
                        invalidValueRowNumber.Add(rowNumber);
                    }
                    rowNumber++;
                }
                if (invalidValueRowNumber.Any()) {
                    errorMsg.Append("<hr /> <h4>'ADDRESS1' is too short, add more than 4 characters, at row number:</h4>");
                    errorMsg.Append(string.Join(", ", invalidValueRowNumber));
                }

            }
        }

        public static void CheckPostcode(List<string> postcodeList, ref StringBuilder errorMsg)
        {            
            CheckEmptyValues("POSTCODE", postcodeList, ref errorMsg);

            List<int> invalidValueRowNumber = new();
            int rowNumber = 1;
            foreach (var item in postcodeList)
            {
                if (!IsPostCode(item))
                {
                    invalidValueRowNumber.Add(rowNumber);
                }

                rowNumber++;
            }

            if (invalidValueRowNumber.Any())
            {
                errorMsg.Append("<hr/><h4>'POSTCODE' incorrectly formated, at row number: </h4>");
                errorMsg.Append(string.Join(", ", invalidValueRowNumber));
            }

        }

        public static void CheckNINO(List<string> niList, ref StringBuilder errorMsg)
        {
            CheckEmptyValues("NI_NUMBER", niList, ref errorMsg);

            List<int> invalidValueRowNumber = new();

            int rowNumber = 1;
            foreach (var item in niList)
            {
                if (!IsEmpty(item) && !IsNINO(item))
                {
                    invalidValueRowNumber.Add(rowNumber);
                }
                rowNumber++;
            }
            if (invalidValueRowNumber.Any())
            {
                errorMsg.Append("<hr/><h4>'NI_NUMBER' formated incorrectly, at row number:</h4>");
                errorMsg.Append(string.Join(", ", invalidValueRowNumber));
            }

        }

        /// <summary>
        /// Checks rate of pay - in march this column should have a value greater than 0 and rest of year it can have anything.
        /// 29/09/22
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="selectedMonth"></param>
        /// <param name="CheckSpreadSheetErrorMsg"></param>
        /// <returns></returns>
        public static void CheckAnnualRateOfpay(List<string> annualRateOfpayList, string selectedMonth, ref StringBuilder errorMsg)
        {
            int monthFromDropDown = DateTime.ParseExact(selectedMonth, "MMMM", CultureInfo.CurrentCulture).Month;

            //## if not 'March' and all fields are empty.. then great.. no need to do anything..            
            if (monthFromDropDown != EndOfYear_March && annualRateOfpayList.All(v => IsEmpty(v)))
            {
                return; //## do nothing.. not mandatory and doesn't have any values
            }
            
            if (monthFromDropDown == EndOfYear_March) { 
                CheckNumberValues("ANNUAL_RATE_OF_PAY", annualRateOfpayList, ref errorMsg, isMandatory: true);  //## isMandatory: true => only for March, each year    
            }
        }

        public static void CheckGender(List<string> genderList, ref StringBuilder errorMsg)
        {            
            CheckEmptyValues("GENDER", genderList, ref errorMsg);

            string[] validValues = { "F", "M" };
            CheckInvalidValues("GENDER", genderList, validValues.ToList(), ref errorMsg);
        }


        public static void CheckPayMain(List<ExcelsheetDataVM> excelData, ref StringBuilder errorMsg)
        {
            ///////////////////////////////////////////////////////////////////////////
            /////////// Check PAY_MAIN and PAY_50_50 both should not be null///////////
            ///////////////////////////////////////////////////////////////////////////
            int rowCounter = 1;
            List<int> emptyRowsList = new();

            if (excelData.Any(e => IsEmptyOrZero(e.PAY_MAIN) && IsEmptyOrZero(e.PAY_50_50)) ) {
                foreach (var item in excelData)
                {
                    var payMainValue = RemoveCurrency(item.PAY_MAIN);
                    var pay50_50_Value = RemoveCurrency(item.PAY_50_50);

                    if (IsEmptyOrZero(payMainValue) && IsEmptyOrZero(pay50_50_Value) ) {
                        //## and if the FT_PT_CS Flag == 'CS' - then exception.. PayMain and Pay_50/50 both can be Zero... OR if the 'DATE_OF_LEAVING_SCHEME' field has a Date- then YES, too
                        if (item.FT_PT_CS_FLAG != "CS" && ( IsEmpty(item.DATE_OF_LEAVING_SCHEME) && IsEmpty(item.DATE_JOINED_SCHEME) && IsEmpty(item.OPTOUT_DATE) ) ) {  //## for FT/PT scenario- when any of those dates have value- then can have Zero values
                            //## OptOut_Date if in future- then allow Zero.. if that is in Past- then Error.. 
                            emptyRowsList.Add(rowCounter);
                            //Console.WriteLine($"Member: '{item.SURNAME}', FT_PT_CS_FLAG: '{item.FT_PT_CS_FLAG}', DATE_OF_LEAVING_SCHEME: '{item.DATE_OF_LEAVING_SCHEME}', DATE_JOINED_SCHEME: '{item.DATE_JOINED_SCHEME}'");
                        }
                    }
                    rowCounter++;
                }

                if (emptyRowsList.Any()) {
                    errorMsg.Append("<hr/><h4>'PAY_MAIN' and 'PAY_50_50' both cannot be empty in the same record of the member, at row number:</h4>");                    
                    errorMsg.Append(string.Join(", ", emptyRowsList));
                }
            }



            ///////////////////////////////////////////////////////////////////////////
            ///////// Check PAY_MAIN and PAY_50_50  both cannot have value in one row//
            ///////////////////////////////////////////////////////////////////////////
            if (excelData.Any(e => HasNonZeroValue(e.PAY_50_50) ) )
            {
                rowCounter = 1;
                emptyRowsList = new();

                foreach (var item in excelData)
                {
                    _ = double.TryParse(item.PAY_MAIN, out double payMain);
                    _ = double.TryParse(item.PAY_50_50, out double pay_50_50);

                    if (payMain > 0 && pay_50_50 > 0 )
                    {
                        emptyRowsList.Add(rowCounter);
                    }
                    rowCounter++;
                }

                if (emptyRowsList.Any())
                {
                    errorMsg.Append("<hr/><h4>'PAY_MAIN' and 'PAY_50_50' cannot be present in the same record of the member, at row number:</h4>");
                    errorMsg.Append(string.Join(", ", emptyRowsList));
                }

            }



            ///////////////////////////////////////////////////////////////////////////////////////////////
            ///////// PAY_MAIN and EE_CONT_MAIN both should have values if any of the column has value //
            ///////////////////////////////////////////////////////////////////////////////////////////////
            bool bothValuesExist = false;
            rowCounter = 1;
            emptyRowsList = new();
            foreach (var item in excelData)
            {
                _ = double.TryParse(item.PAY_MAIN, out double payMain);
                _ = double.TryParse(item.EE_CONT_MAIN, out double ee_CONT_MAIN);

                if ( (payMain > 0 && ee_CONT_MAIN < 1)    ||
                     (payMain < 1 && ee_CONT_MAIN > 0) )
                {
                    emptyRowsList.Add(rowCounter);
                    
                }

                rowCounter++;

            }
            if (bothValuesExist) {
                errorMsg.Append("<hr/><h4>Both 'PAY_MAIN' and 'EE_CONT_MAIN'- should have values in the member's record, at row number:</h4>");
                errorMsg.Append(string.Join(", ", emptyRowsList));
            }
        }

        /// <summary>This will check whether the parameter is a Non Zero value. If Zero or Empty- it will return false. And TRUE for any Negative or positive Non-zero value</summary>
        /// <param name="value">Parameter to assess the value</param>
        /// <returns>True/False</returns>
        private static bool HasNonZeroValue(string value)
        {
            if (IsEmpty(value)) return false;

            var numberValue = RemoveCurrency(value);    //## if not replacing the currency symbol- then double.TryParse() fails.
            bool isParsed = double.TryParse(numberValue, CultureInfo.CurrentCulture, out double parsedValue);
            if (!IsEmpty(numberValue) && isParsed && parsedValue != 0)
            {
                return true;
            }

            return false;
        }

        private static string RemoveCurrency(string currencyValue) => currencyValue;    // currencyValue.Replace("£", "");
        

        public static void CheckOptFlag(List<string> flagValueList, ref StringBuilder errorMsg)
        {
            string[] validEnrolmentTypes = { "AUTO", "CONTRACTUAL" };
            CheckInvalidValues("OPTOUT_FLAG", flagValueList, validEnrolmentTypes.ToList(), ref errorMsg);            
        }

        #endregion
       
        public static void CheckContractualHoursAgainstFlagTypeLG(List<ExcelsheetDataVM> excelData, ref StringBuilder result)
        {

            int rowNumber = 1;
            List <int> invalidRows = new();
            
            foreach (var row in excelData)
            {
                if (!IsEmpty(row.CONTRACTUAL_HRS)) { 
                    double.TryParse(row.CONTRACTUAL_HRS, out double contractualHours);
                    string flagTypes = row.FT_PT_CS_FLAG;

                    if (    (contractualHours == 0 && !flagTypes.Trim().ToLower().Equals("cs")    )
                         || (contractualHours != 0 && flagTypes.Trim().ToLower().Equals("cs")))
                    {
                        invalidRows.Add(rowNumber);                    

                    }
                
                }

                rowNumber++;
            }

            if (invalidRows.Any()) {
                result.Append("<hr/><h4>Invalid 'CONTRACTUAL_HRS', should be 0.00 hour if ‘CS’ flag present in 'FT_PT_CS_FLAG', at row number:</h4> ");
                result.Append(string.Join(", ", rowNumber));
            }
        }

        public static void CheckDateValues(string fieldName, List<string> dateList, ref StringBuilder result, bool isMandatoryField = false)
        {
            if (dateList.Count >= 10000) {
                Console.WriteLine($"{DateTime.Now}, CheckDateValues => '{fieldName}', Records: {dateList.Count()}, isMandatoryField: {isMandatoryField}");            
            }

            //## if not mandatory and all fields are empty.. then great.. no need to do anything..            
            if ( !isMandatoryField && dateList.All(v => IsEmpty(v))) {
                return; //## do nothing.. not mandatory and doesn't have any values
            }

            int rowNumber = 1;
            List<int> invalidRows = new();
            foreach (var item in dateList)
            {
                if (isMandatoryField && IsEmpty(item)){
                    //## create a list of Row numbers where values are empty.. 
                    invalidRows.Add(rowNumber);                    
                }
                else if (!IsEmpty(item) && DateTime.TryParse(item, out DateTime dateUK) == false) {
                    //## NOT Mandatory, but has values.. then check the Date is valid
                    invalidRows.Add(rowNumber);
                    Console.WriteLine($"Invalid date value, {fieldName}=> {item}, at row: {rowNumber}");
                }
    
                rowNumber++;
            }

            if (invalidRows.Any()) {
                result.Append($"<hr/><h4>Invalid '{fieldName}' found. The cell value should be like '30/10/2014', at row number:</h4>");
                result.Append(string.Join(", ", invalidRows));
            }

        }

        static public bool IsPostCode(string postcode)
        {
            if(postcode.Length < 6)
                return false;

            return (
                Regex.IsMatch(postcode, "(^[A-PR-UWYZa-pr-uwyz][0-9][ ]*[0-9][ABD-HJLNP-UW-Zabd-hjlnp-uw-z]{2}$)") ||
                Regex.IsMatch(postcode, "(^[A-PR-UWYZa-pr-uwyz][0-9][0-9][ ]*[0-9][ABD-HJLNP-UW-Zabd-hjlnp-uw-z]{2}$)") ||
                Regex.IsMatch(postcode, "(^[A-PR-UWYZa-pr-uwyz][A-HK-Ya-hk-y][0-9][ ]*[0-9][ABD-HJLNP-UW-Zabd-hjlnp-uw-z]{2}$)") ||
                Regex.IsMatch(postcode, "(^[A-PR-UWYZa-pr-uwyz][A-HK-Ya-hk-y][0-9][0-9][ ]*[0-9][ABD-HJLNP-UW-Zabd-hjlnp-uw-z]{2}$)") ||
                Regex.IsMatch(postcode, "(^[A-PR-UWYZa-pr-uwyz][0-9][A-HJKS-UWa-hjks-uw][ ]*[0-9][ABD-HJLNP-UW-Zabd-hjlnp-uw-z]{2}$)") ||
                Regex.IsMatch(postcode, "(^[A-PR-UWYZa-pr-uwyz][A-HK-Ya-hk-y][0-9][A-Za-z][ ]*[0-9][ABD-HJLNP-UW-Zabd-hjlnp-uw-z]{2}$)") ||
                Regex.IsMatch(postcode, "(^[Gg][Ii][Rr][]*0[Aa][Aa]$)")
                );
        }

        static public bool IsNINO(string NINO)
        {            
            bool result = Regex.IsMatch(NINO, "^[A-CEGHJ-PR-TW-Z]{1}[A-CEGHJ-NPR-TW-Z]{1}[0-9]{6}[A-D]{0,1}$");//"[ABCEGHJKLMNPRSTWXYZ][0-9]{6}[A-D ]");

            return result;
        }

        private static bool IsEmpty(string value) => string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value);
        private static bool NotEmpty(string value) => !string.IsNullOrEmpty(value);

        /// <summary>This will check a list of values if they are all Numerical. Will return list of their row position if non-numerical items found</summary>
        /// <param name="fieldName">Field name to populate the error message</param>
        /// <param name="values">List of values to process</param>
        /// <param name="result">Error message will be populated and returned</param>
        /// <param name="isMandatory">If Mandatory- then ignore the empty items and check items which has values.</param>
        public static void CheckNumberValues(string fieldName, List<string> values, ref StringBuilder result, bool isMandatory = false)
        {
            List<int> rowNumberList = new();
            int rowNumber = 1;

            foreach (var item in values)
            {
                var numberValue = RemoveCurrency(item);    //## if not replacing the currency symbol- then double.TryParse() fails.
                if (isMandatory && IsEmptyOrZero(numberValue))
                {
                    //## create a list of Row numbers where values are Mandatory, but empty.. 
                    rowNumberList.Add(rowNumber);
                }
                else if ( !IsEmpty(numberValue) && double.TryParse(numberValue, CultureInfo.CurrentCulture, out double value) == false)
                {
                    //## NOT Mandatory, but has values.. then check the value is valid
                    rowNumberList.Add(rowNumber);
                }

                rowNumber++;
            }

            if (rowNumberList.Any())
            {
                result.Append($"<hr/><h4>Invalid values for '{fieldName}', at row number: </h4>");
                result.Append(string.Join(", ", rowNumberList));
            }

        }


        /// <summary>This will validate a list of Values with a second set of values. The 2nd set will have the criteria values which must be present in all the 1st set.</summary>
        /// <param name="values">Values to Check</param>
        /// <param name="validValues">Values to Match</param>
        /// <returns>List of Rows which has non-matching values</returns>
        public static void CheckInvalidValues(string fieldName, List<string> values, List<string> validValues, ref StringBuilder result)
        {
            List<int> rowNumberList = new();
            int rowNumber = 1;

            if (values.Any(e => !validValues.Contains(e)))
            {
                rowNumber = 1;
                //## invalid values found.. list those 
                foreach (var item in values)
                {
                    if ( !IsEmpty(item) && !validValues.Contains(item, StringComparer.CurrentCultureIgnoreCase))
                    {
                        rowNumberList.Add(rowNumber);
                    }
                    rowNumber++;
                }
            }

            if (rowNumberList.Any())
            {
                result.Append($"<hr/><h4>Invalid '{fieldName}' values found, at row number: </h4>");
                result.Append(string.Join(", ", rowNumberList));
            }
            
        }

        /// <summary>This will be used to validate mandatory text fields</summary>
        /// <param name="fieldName">Field Name</param>
        /// <param name="values">String value</param>
        /// <param name="result">StringBuilder result with error details</param>
        public static void CheckEmptyValues(string fieldName, List<string> values, ref StringBuilder result)
        {
            List<int> rowNumberList = new();
            int rowNumber = 1;

            //## Check Empty values
            if (values.Any(e => IsEmpty(e)))
            {
                //## Empty value found.. list their row number
                foreach (var item in values)
                {
                    if (IsEmpty(item))
                    {
                        rowNumberList.Add(rowNumber);
                    }
                    rowNumber++;
                }
            }
            if (rowNumberList.Any()) {
                result.Append($"<hr/><h4>'{fieldName}' can not be empty, at row number: </h4>");
                result.Append(string.Join(", ", rowNumberList));
            }
        }

        /// <summary>This will be used for Double data type. But we will pass the value as string. We need to check whether it is Empty or Zero</summary>
        /// <param name="dataValue"></param>
        /// <returns></returns>
        private static bool IsEmptyOrZero(string dataValue) {
            bool parseDone = double.TryParse(dataValue, out double doubleValue);
            return IsEmpty(dataValue)|| !parseDone || doubleValue == 0;

        }
    }
}
