﻿using MCPhase3.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MCPhase3.CodeRepository
{
    public class SpreadSheetChecks
    {

        /// <summary>
        /// New function for sign check
        /// </summary>
        public static bool CheckSpeechMark(DataTable dt, string[] invalidSigns, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            string mySign = string.Empty;
            //string[] speechMark = System.Configuration.ConfigurationManager.AppSettings.Get("SignsToCheck")
            //    .Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

            int rowNums = 1;
            foreach (DataRow dr in dt.Rows)
            {
                rowNums++;
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    //Replace new line Code edit on Richard's request 03/03/2021
                    if (dr[i].ToString().Contains("\r") || dr[i].ToString().Contains("\n"))
                    {
                        CheckSpreadSheetErrorMsg += "<br /> Remove the Line space from row  " + rowNums + " of spreadsheet.<br />";
                        result = false;
                    }
                }
            }

            foreach (string checkSign1 in invalidSigns)
            {
                int rowNum = 1;
                foreach (DataRow dr in dt.Rows)
                {
                    rowNum++;
                    for (int i = 0; i < dt.Columns.Count; i++)
                    {
                        if (dr[i].ToString().ToUpper().Contains(checkSign1) && dt.Columns[i].ToString().ToUpper() != "NOTES")
                        {
                            //var idx = dt.Rows.IndexOf(dr);
                            CheckSpreadSheetErrorMsg += "<br /> You have added invalid sign in  row " + rowNum + " of " + dt.Columns[i] + " of spreadsheet:<br />";
                            result = false;
                        }
                    }
                }
            }
            return result;
        }


        public static bool CheckPayrollPeriod(ref DataTable dt, string month, string posting, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            string[] validMonths = { "JANUARY", "FEBRUARY", "MARCH", "APRIL", "MAY", "JUNE", "JULY", "AUGUST", "SEPTEMBER", "OCTOBER", "NOVEMBER", "DECEMBER", "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC" };


            int monthFromDropDown = DateTime.ParseExact(month, "MMMM", CultureInfo.CurrentCulture).Month;
            int[] emptyRowNumber;
            int inc = 0;
            int checkRows = 0;

            if (!IsColumnEmptyNewMethod1(ref dt, "PAYROLL_PD", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        checkRows = emptyRowNumber[i];
                        checkRows++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Payroll period 'PAYROLL_PD' can not be empty at row number:" + inc + " in spreadsheet.<br />If there is no PAYROLL_PD empty cell then please select few empty rows from bottom of apreadsheet and delete them.<br />";
                    }
                }
                result = false;
            }

            inc = 1;
            foreach (DataRow rw in dt.Rows)
            {
                string payrollPeriod = rw["PAYROLL_PD"].ToString();
                inc++;
                int monthNumber = 0;

                bool isAllString = payrollPeriod.All(Char.IsLetter);
                if (isAllString == false)
                {
                    CheckSpreadSheetErrorMsg += $"<br/>You have entered invalid payroll period 'PAYROLL_PD' in spreadsheet at row number:<b>{inc}</b> <br />'PAYROLL_PD' value can be 3 character value (e.g) JAN or JANUARY.<br />";
                    result = false;
                }
                else if (!validMonths.Contains(payrollPeriod, StringComparer.CurrentCultureIgnoreCase) && inc != checkRows)
                {
                    if (!CheckSpreadSheetErrorMsg.Equals(string.Empty))
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />";
                    }

                    CheckSpreadSheetErrorMsg += "<br />You have entered invalid payroll period 'PAYROLL_PD' in spreadsheet at row number:<B>" + inc + " </B> <br />'PAYROLL_PD' value can be 3 character value (e.g) JAN or JANUARY.</Br>";
                    result = false;
                }
                else
                {
                    string parseFormat = payrollPeriod.Length > 3 ? "MMMM" : "MMM";
                    monthNumber = DateTime.ParseExact(payrollPeriod, parseFormat, CultureInfo.CurrentCulture).Month;

                }

                //Qasid disable following functionality to allow files with multiple months return in same file.
                //check the dropdown selected month is same as file's month 
                if (posting.Equals("1"))    //TODO: Use Enum value for Posting
                {
                    if (monthNumber != monthFromDropDown && !string.IsNullOrEmpty(payrollPeriod))
                    {
                        if (!CheckSpreadSheetErrorMsg.Equals(string.Empty))
                        {
                            CheckSpreadSheetErrorMsg += "<br /> <br />";
                        }
                        CheckSpreadSheetErrorMsg += $"You might have added wrong 'PAYROLL_PD' at row number:<b>{inc} </b> of spreadsheet.<br/>";
                        result = false;
                    }
                }
            }

            return result;
        }


        public static bool CheckFireScheme(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            string[] validSchemes = { "1992", "2006", "2015" };

            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "SCHEME_NAME", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Scheme name 'SCHEME_NAME' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br /> Valid scheme names are: 1992, 2006, 2015";
                    }
                }
                return result;
            }

            int rowNum = 1;
            foreach (DataRow rw in dt.Rows)
            {
                string payrollPeriod = rw["SCHEME_NAME"].ToString();
                rowNum++;

                if (!validSchemes.Contains(payrollPeriod, StringComparer.CurrentCultureIgnoreCase))
                {
                    if (!CheckSpreadSheetErrorMsg.Equals(string.Empty))
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />";
                    }

                    CheckSpreadSheetErrorMsg += "You have entered invalid scheme name 'SCHEME_NAME', at row number: <B>" + rowNum + "</B> in spreadsheet.<br />";
                    result = false;
                }
            }

            return result;
        }

        public static bool CheckPayrollYear(ref DataTable dt, string posting, string validYears, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "PAYROLL_YR", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Payroll year 'PAYROLL_YR' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }
                result = false;
            }

            int row = 1;

            foreach (DataRow rw in dt.Rows)
            {
                row++;
                string payrollYear = rw["PAYROLL_YR"].ToString();

                //## First check whether the content is valid? matching with '2023/24' pattern?
                string financialYearPattern = "20[1-9][0-9][/][1-9][0-9]";  //## this will force to Match a year starting with '20' and and then 2 more digits, 1 '/' and then 2 more digits...                
                Match m = Regex.Match(payrollYear, financialYearPattern, RegexOptions.IgnoreCase);
                result = m.Success;

                if (result == false)
                {
                    CheckSpreadSheetErrorMsg += "<br /> <br />You have entered an invalid payroll year 'PAYROLL_YR' in spreadsheet at row number:<b>" + (row - 1) + " </b> <br />'PAYROLL_YR' value should be like '2023/24'. Or you may have selected the wrong payroll year from the options above.<br />";
                }
                else if (posting.Equals("1"))
                {
                    //## For the 1st Posting- Excelsheet Year value must exist in the predefined YearList in the App.Settings file
                    if (!validYears.Contains(payrollYear))
                    {
                        result = false; //## it does match the pattern, however- it doesn't match the Year selected Vs Year in the ExcelSheet
                        CheckSpreadSheetErrorMsg += "<br /> <br />You have entered an invalid payroll year 'PAYROLL_YR' in spreadsheet at row number:<b>" + (row - 1) + " </b> <br />You may have selected the wrong payroll year from the options above.<br />";
                    }
                }

            }

            return result;
        }

        /// <summary>
        /// disabled on Richard's request at 17/10/22
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="validTitles"></param>
        /// <param name="CheckSpreadSheetErrorMsg"></param>
        /// <returns></returns>
        public static bool CheckTitle(ref DataTable dt, string[] validTitles, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            bool invalidLetterFound = false;

            int[] emptyRowNumber;
            int inc = 1;
            int inc1 = 0;

            if (!IsColumnEmptyNewMethod1(ref dt, "MEMBERS_TITLE", out emptyRowNumber))
            {

                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's title 'MEMBERS_TITLE' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }
                result = false;
            }
            inc = 1;
            foreach (DataRow dr in dt.Rows)
            {
                string title = dr["MEMBERS_TITLE"].ToString().ToUpper();

                //## Title - data cleansing- remove '.' and any blank spaces..
                title = title.Replace(".", "").Replace(" ", "");

                dr["MEMBERS_TITLE"] = title;

                inc++;

                if (!validTitles.Contains(title, StringComparer.CurrentCultureIgnoreCase))
                {
                    //var idx = dt.Rows.IndexOf(dr);
                    CheckSpreadSheetErrorMsg += "<br /> You have added invalid member title in  row <b>" + inc + "</b> of spreadsheet:";
                    invalidLetterFound = true;
                    result = false;
                }
            }

            if (invalidLetterFound)
            {  //## avoid adding this to at the end of each error..
                CheckSpreadSheetErrorMsg += "<div class='h4 text-danger mt-2 mb-2'>Please add right member title to upload file.<span>";
            }

            return result;
        }



        public static bool CheckRankRole(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "RANK_ROLE", out emptyRowNumber))
            {

                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Rank Role 'RANK_ROLE' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }
                result = false;
            }

            return result;
        }

        public static bool CheckEmployerName(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int inc = 0;

            int[] emptyRowNumber;
            if (!IsColumnEmptyNewMethod1(ref dt, "EMPLOYER_NAME", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Employer's name 'EMPLOYER_NAME' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";

                    }
                }
                result = false;
            }

            return result;
        }


        // Q - Amended code to get all the line numbers of Excel sheet where is some error

        public static bool CheckSurname(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int inc = 0;

            int[] emptyRowNumber;
            if (!IsColumnEmptyNewMethod1(ref dt, "SURNAME", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's surname can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";

                    }
                }

                result = false;
            }

            return result;
        }

        // Q - Amended code to get all the line numbers of Excel sheet where is some error

        public static bool CheckForename(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int inc = 0;
            int[] emptyRowNumber;

            if (!IsColumnEmptyNewMethod1(ref dt, "FORENAMES", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's forename 'FORENAMES' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br /> ";

                    }
                }
                result = false;
            }

            return result;
        }

        // Q - Amended code to get all the line numbers of Excel sheet where is some error

        public static bool CheckAddress1(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int inc = 0;
            int[] emptyRowNumber;

            if (!IsColumnEmptyNewMethod1(ref dt, "ADDRESS1", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's address1 'ADDRESS1' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";

                    }

                }
                result = false;
            }

            if (!IsAddressLineCharactersOK(dt, "ADDRESS1", out emptyRowNumber))
            {
                int inc1 = 0;

                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc1 = emptyRowNumber[i];
                        inc1++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's 'ADDRESS1' is too short, add more than 4 characters in row:<B>" + inc1 + " </B> in spreadsheet.<br />";

                    }
                }
                result = false;
            }

            return result;
        }

        public static bool CheckAddress2(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            int[] emptyRowNumber;
            bool result = true;
            if (!IsColumnEmptyNewMethod1(ref dt, "ADDRESS2", out emptyRowNumber))
            {
                int inc1 = 0;

                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc1 = emptyRowNumber[i];
                        inc1++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's address2 'ADDRESS2' can not be empty at row number:<B>" + inc1 + " </B> in spreadsheet.<br />";

                    }
                }
                result = false;
            }

            return result;
        }

        public static bool CheckPostcode(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "POSTCODE", out emptyRowNumber))
            {

                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's postcode 'POSTCODE' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            if (!IsPostCodeOK(dt, "POSTCODE", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's postcode 'POSTCODE' incorrectly formated at row number:<B>" + inc + " </B> in spreadsheet. <br /> It must be a valid postcode format inc. the space eg ‘AA1 1AA’ <br />";
                    }
                }

                result = false;
            }

            return result;
        }

        public static bool CheckNINO(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "NI_NUMBER", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's NI number 'NI_NUMBER' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            if (!IsNINOFormatOK(dt, "NI_NUMBER", out emptyRowNumber))
            {
                inc = 0;
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's NI number 'NI_NUMBER' formated incorrectly at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            return result;
        }

        public static bool CheckPayRef(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "PAYREF", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's pay ref. 'PAYREF' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            return result;
        }

        public static bool CheckDOB(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "DOB", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's date of birth 'DOB' can not be empty at row number:<B>" + inc + " </B> or incorrect format in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            string wrongDateInFormat = string.Empty;
            if (!IsValidDate(dt, "DOB", ref wrongDateInFormat))
            {
                CheckSpreadSheetErrorMsg += "<br /> <br />You have entered invalid date of birth 'DOB'. value should be like '30/10/2014'.<br />";
                result = false;
            }
            //## check minimun ad maximum date for a member record

            return result;
        }

        public static bool CheckFullTimePartTimeFlag(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            string[] validValues = { "FT", "PT", "RT" };
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "FT_PT_RT_FLAG", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Full time part time flag 'FT_PT_RT_FLAG' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";

                    }
                }
                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidColumnValues(dt, "FT_PT_RT_FLAG", validValues, ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }


        public static bool CheckFullTimePartTimeFlagForLG(ref DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            string[] validValues = { "FT", "PT", "CS" };
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "FT_PT_CS_FLAG", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Full time part time flag 'FT_PT_CS_FLAG' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }
                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidColumnValues(dt, "FT_PT_CS_FLAG", validValues, ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }

        public static bool CheckStandardHours(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "STD_HOURS", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's pay ref. 'STD_HOURS' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "STD_HOURS", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }

        public static bool CheckAnnualRateOfPenPay(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "ANNUAL_RATE_OF_PENSIONABLE", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's pay ref. 'ANNUAL_RATE_OF_PENSIONABLE' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }
            inc = 1;
            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "ANNUAL_RATE_OF_PENSIONABLE", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }
        public static bool CheckAvgPenPay(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "AVERAGE_PENSIONABLE_PAY", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's pay ref. 'AVERAGE_PENSIONABLE_PAY' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "AVERAGE_PENSIONABLE_PAY", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }

        public static bool CheckEmployeeContribution(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "EE_CONTS", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's contribution 'EE_CONTS' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "EE_CONTS", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }
        public static bool CheckContractualHours(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "CONTRACTUAL_HRS", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's pay ref. 'CONTRACTUAL_HRS' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "CONTRACTUAL_HRS", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            string errorMsgAgainstFlag = string.Empty;
            if (!CheckContractualHoursAgainstFlagTypeLG(dt, ref errorMsgAgainstFlag))
            {
                CheckSpreadSheetErrorMsg += errorMsgAgainstFlag;
                result = false;
            }



            return result;
        }

        public static bool CheckPensionablePay1992And2006(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int emptyRowNumber = 1;

            foreach (DataRow rw in dt.Rows)
            {
                emptyRowNumber++;
                string schemeName = rw["SCHEME_NAME"].ToString();
                double schemeValue;
                if (schemeName == "1992" || schemeName == "2006")
                {
                    if (!double.TryParse(rw["PENSIONABLE_PAY_1992_2006_SCHEME"].ToString(), out schemeValue))
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />Number value for column 'PENSIONABLE_PAY_1992_2006_SCHEME' required at row number:<B>" + emptyRowNumber.ToString() + "</B> in spreadsheet.<br />";
                        result = false;
                    }

                }
                else if (schemeName == "2015")
                {
                    string temPensionValue = rw["PENSIONABLE_PAY_1992_2006_SCHEME"].ToString();
                    if (temPensionValue != string.Empty)
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />Data can only be present if '1992 or 2006 scheme' selected in column 'SCHEME_NAME' at row number:<B>" + emptyRowNumber.ToString() + "</B> in spreadsheet.<br />";
                        result = false;
                    }
                }
            }

            return result;
        }

        public static bool CheckTempPromotionContrib1992And2006(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int emptyRowNumber = 1;

            foreach (DataRow rw in dt.Rows)
            {
                emptyRowNumber++;
                string schemeName = rw["SCHEME_NAME"].ToString();
                double schemeValue;
                if (schemeName == "1992" || schemeName == "2006")
                {
                    if (rw["APB_FOR_TEMP_PROMOTION_CONTS"].ToString() != string.Empty)
                    {
                        if (!double.TryParse(rw["APB_FOR_TEMP_PROMOTION_CONTS"].ToString(), out schemeValue))
                        {
                            CheckSpreadSheetErrorMsg += "<br /> <br />Number value for column 'APB_FOR_TEMP_PROMOTION_CONTS' required at row number:" + emptyRowNumber.ToString() + " in spreadsheet.";
                            result = false;
                        }
                    }

                }
                else if (schemeName == "2015")
                {
                    string temPensionValue = rw["APB_FOR_TEMP_PROMOTION_CONTS"].ToString();
                    if (temPensionValue != string.Empty)
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />Data can only be present in column 'APB_FOR_TEMP_PROMOTION_CONTS' if '1992 or 2006 scheme' selected in column 'SCHEME_NAME' at row number:" + emptyRowNumber.ToString() + " in spreadsheet.";
                        result = false;
                    }

                }


            }

            return result;
        }

        public static bool CheckCPDContrib1992And2006(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int emptyRowNumber = 1;

            foreach (DataRow rw in dt.Rows)
            {
                emptyRowNumber++;
                string schemeName = rw["SCHEME_NAME"].ToString();
                double schemeValue;
                if (schemeName == "1992" || schemeName == "2006")
                {
                    if (rw["CPD_TOT_PEN_EE_CONTS"].ToString() != string.Empty)
                    {
                        if (!double.TryParse(rw["CPD_TOT_PEN_EE_CONTS"].ToString(), out schemeValue))
                        {
                            CheckSpreadSheetErrorMsg += "<br /> <br />Number value for column 'CPD_TOT_PEN_EE_CONTS' required at row number:" + emptyRowNumber.ToString() + " in spreadsheet.";
                            result = false;
                        }
                    }

                }
                else if (schemeName == "2015")
                {
                    string temPensionValue = rw["CPD_TOT_PEN_EE_CONTS"].ToString();
                    if (temPensionValue != string.Empty)
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />Data can only be present in column 'CPD_TOT_PEN_EE_CONTS' if '1992 or 2006 scheme' selected in column 'SCHEME_NAME' at row number:" + emptyRowNumber.ToString() + " in spreadsheet.";
                        result = false;
                    }

                }


            }

            return result;
        }

        public static bool CheckPurchaseOf60Contrib1992And2006(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int emptyRowNumber = 1;

            foreach (DataRow rw in dt.Rows)
            {
                emptyRowNumber++;
                string schemeName = rw["SCHEME_NAME"].ToString();
                double schemeValue;
                if (schemeName == "1992" || schemeName == "2006")
                {
                    if (rw["PRCHS_OF_60TH"].ToString() != string.Empty)
                    {
                        if (!double.TryParse(rw["PRCHS_OF_60TH"].ToString(), out schemeValue))
                        {
                            CheckSpreadSheetErrorMsg += "<br /> <br />Number value for column 'PRCHS_OF_60TH' required at row number:" + emptyRowNumber.ToString() + " in spreadsheet.";
                            result = false;
                        }
                    }

                }
            }

            return result;
        }


        public static bool CheckPensionablePay2015(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int emptyRowNumber = 1;

            foreach (DataRow rw in dt.Rows)
            {
                emptyRowNumber++;
                string schemeName = rw["SCHEME_NAME"].ToString();
                double schemeValue;
                if (schemeName == "2015")
                {
                    if (!double.TryParse(rw["PENSIONABLE_PAY_2015_CARE_SCHEME"].ToString(), out schemeValue))
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />Number value for column 'PENSIONABLE_PAY_2015_CARE_SCHEME' required at row number:" + emptyRowNumber.ToString() + " in spreadsheet.";
                        result = false;
                    }

                }

            }

            return result;
        }

        public static bool CheckAddedPensionContributionFor2015(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int emptyRowNumber = 1;

            foreach (DataRow rw in dt.Rows)
            {
                emptyRowNumber++;
                string schemeName = rw["SCHEME_NAME"].ToString();
                double schemeValue;

                if (schemeName == "1992")
                {
                    if (rw["ADDED_PENSION_CONTRIBUTIONS"].ToString().Trim() != string.Empty)
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />Data can only be present in 'ADDED_PENSION_CONTRIBUTIONS' column, if '2015 or 2006 scheme' selected in column 'SCHEME_NAME' at row number:" + emptyRowNumber.ToString() + " in spreadsheet.";
                        result = false;
                    }

                }
            }

            return result;
        }

        public static bool CheckRetainedFireDOB(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int emptyRowNumber = 1;

            foreach (DataRow rw in dt.Rows)
            {
                emptyRowNumber++;
                string schemeName = rw["SCHEME_NAME"].ToString().Trim();
                string fulltime_parttime_retained_flage = rw["FT_PT_RT_FLAG"].ToString();
                double schemeValue;
                if (schemeName.Equals("1992"))
                {
                    if (fulltime_parttime_retained_flage.Equals("RT"))
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />Retained 'RT' can only be present in column 'FT_PT_RT_FLAG' when scheme name is 2006 or 2015 in column 'SCHEME_NAME':" + emptyRowNumber.ToString() + " in spreadsheet.";
                        result = false;
                    }

                }
            }

            return result;
        }


        public static bool CheckSchmeNameAgainstDOB(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int emptyRowNumber = 1;

            foreach (DataRow rw in dt.Rows)
            {
                emptyRowNumber++;
                string schemeName = rw["SCHEME_NAME"].ToString().Trim();
                string dob = rw["DOB"].ToString();

                dob = dob.Replace("00:00:00", "").Trim();

                string DateFor1992 = "01/04/1971";
                DateTime ParsedDateFor1992;

                string DateFor2006 = "01/04/1966";
                DateTime ParsedDateFor2006;

                DateTime parsedDOB;
                if (!DateTime.TryParseExact(dob, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsedDOB))
                {
                    CheckSpreadSheetErrorMsg += "Date of Birth format is wrong";
                    result = false;
                }

                if (!DateTime.TryParseExact(DateFor1992, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out ParsedDateFor1992))
                {
                    CheckSpreadSheetErrorMsg += "Date of Birth format is worng .";
                    result = false;
                }

                if (!DateTime.TryParseExact(DateFor2006, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out ParsedDateFor2006))
                {
                    CheckSpreadSheetErrorMsg += "Date of Birth format is worng .";
                    result = false;
                }


                if (schemeName.Equals("1992"))
                {
                    if (parsedDOB > ParsedDateFor1992)
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />DOB = 01/04/71 or earlier when scheme is 1992 at row:" + emptyRowNumber.ToString() + " in spreadsheet.";
                        result = false;
                    }
                }


                if (schemeName.Equals("2006"))
                {
                    if (parsedDOB > ParsedDateFor1992)
                    {
                        CheckSpreadSheetErrorMsg += "<br /> <br />DOB = 01/04/71 or earlier when scheme is 2006 at row:" + emptyRowNumber.ToString() + " in spreadsheet.";
                        result = false;
                    }

                }


            }

            return result;
        }


        public static bool CheckContractualHoursFire(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "CONTRACTUAL_HRS", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Member's pay ref. 'CONTRACTUAL_HRS' can not be empty at row number:<B>" + inc + " </B> in spreadsheet.<br />";
                    }
                }

                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "CONTRACTUAL_HRS", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            string errorMsgAgainstFlag = string.Empty;
            if (!CheckContractualHoursAgainstFlagTypeFire(dt, ref errorMsgAgainstFlag))
            {
                CheckSpreadSheetErrorMsg += errorMsgAgainstFlag;
                result = false;
            }



            return result;
        }

        public static bool CheckContractualHoursAgainstFlagTypeFire(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            int ctr = 1;
            foreach (DataRow row in dt.Rows)
            {
                object contractualHours = row["CONTRACTUAL_HRS"];
                object flagTypes = row["FT_PT_RT_FLAG"];


                ctr++;
                double contractualHoursD;
                if (!double.TryParse(contractualHours.ToString(), out contractualHoursD))
                {
                    result = false;
                }

                if (contractualHoursD == 0 && !flagTypes.ToString().Trim().Equals("RT"))
                {
                    ctr++;
                    CheckSpreadSheetErrorMsg += "<br /> <br />Should show 0.00 hours if ‘RT’ flag present in FT_PT_RT_FLAG column at <B>" + ctr + "</B>";
                    result = false;
                }

                if (contractualHoursD != 0 && flagTypes.ToString().Trim().Equals("RT"))
                {
                    ++ctr;
                    CheckSpreadSheetErrorMsg += "<br /> <br />Should show 0.00 hours if ‘RT’ flag present in FT_PT_RT_FLAG column at <B>" + ctr + "</B>";
                    result = false;
                }
            }

            return result;
        }

        public static bool CheckContractualHoursAgainstFlagTypeLG(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            DataTable dtSelected;

            int ctr = 1;
            foreach (DataRow row in dt.Rows)
            {
                object contractualHours = row["CONTRACTUAL_HRS"];
                object flagTypes = row["FT_PT_CS_FLAG"];

                ctr++;

                double contractualHoursD;
                if (!double.TryParse(contractualHours.ToString(), out contractualHoursD))
                {
                    result = false;
                }

                if (contractualHoursD == 0 && !flagTypes.ToString().Trim().Equals("CS"))
                {

                    CheckSpreadSheetErrorMsg += "<br /> <br />Should show 0.00 hours if ‘CS’ flag present in FT_PT_CS_FLAG column at <B>" + ctr + "</B>";
                    result = false;

                }

                if (contractualHoursD != 0 && flagTypes.ToString().Trim().Equals("CS"))
                {

                    CheckSpreadSheetErrorMsg += "<br /> <br />Should show 0.00 hours if ‘CS’ flag present in FT_PT_CS_FLAG column at <B>" + ctr + "</B>";
                    result = false;
                }
            }

            return result;
        }

        public static bool CheckDateJS(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            string wrongDateInFormat = string.Empty;
            if (!IsValidDate(dt, "DATE_JOINED_SCHEME", ref wrongDateInFormat))
            {
                CheckSpreadSheetErrorMsg += "You have entered invalid date joined scheme 'DATE_JOINED_SCHEME' in spreadsheet.<br />'DATE_JOINED_SCHEME' value should be like '30/10/2014'.";
                result = false;
            }

            return result;
        }

        public static bool CheckDateLS(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            string wrongDateInFormat = string.Empty;
            if (!IsValidDate(dt, "DATE_OF_LEAVING_SCHEME", ref wrongDateInFormat))
            {
                CheckSpreadSheetErrorMsg += "You have entered invalid date of leaving scheme 'DATE_OF_LEAVING_SCHEME' in spreadsheet.<br />'DATE_OF_LEAVING_SCHEME' value should be like '30/10/2014'.";
                result = false;
            }

            return result;
        }

        public static bool CheckDateOptOut(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            string wrongDateInFormat = string.Empty;
            if (!IsValidDate(dt, "OPTOUT_DATE", ref wrongDateInFormat))
            {
                CheckSpreadSheetErrorMsg += "<br /> <br />You have entered invalid opt out date 'OPTOUT_DATE' in spreadsheet.<br />'OPTOUT_DATE' value should be like '30/10/2014'.";
                result = false;
            }

            return result;
        }

        public static bool CheckDate505StartDate(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            string wrongDateInFormat = string.Empty;
            if (!IsValidDate(dt, "50_50_START_DATE", ref wrongDateInFormat))
            {
                CheckSpreadSheetErrorMsg += "<br /> <br />You have entered invalid '50_50_START_DATE' in spreadsheet.<br />'50_50_START_DATE' value should be like '30/10/2014'.";
                result = false;
            }

            return result;
        }

        public static bool CheckDate505EndDate(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            string wrongDateInFormat = string.Empty;
            if (!IsValidDate(dt, "50_50_END_DATE", ref wrongDateInFormat))
            {
                CheckSpreadSheetErrorMsg += "<br /> <br />You have entered invalid '50_50_END_DATE' in spreadsheet.<br />'50_50_END_DATE' value should be like '30/10/2014'.";
                result = false;
            }

            return result;
        }

        public static bool CheckEnrolmentType(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            string[] validEnrolmentTypes = { "AUTO", "CONTRACTUAL" };
            string errorMsg = string.Empty;
            if (!IsValidColumnValues(dt, "ENROLMENT_TYPE", validEnrolmentTypes, ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }

        public static bool CheckOptFlage(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            string[] validEnrolmentTypes = { "AUTO", "CONTRACTUAL" };
            string errorMsg = string.Empty;
            if (!IsValidColumnValues(dt, "OPTOUT_FLAG", validEnrolmentTypes, ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }


        public static bool CheckEmployerContrib(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "ER_CONTS", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;
                        CheckSpreadSheetErrorMsg += "<br /> <br />Employer's contribution can not be empty at row number:<B>" + inc + "</B> in spreadsheet.";
                    }
                }

                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "ER_CONTS", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Checks rate of pay - in march this column should have a value greater than 0 and rest of year it can have anything.
        /// 29/09/22
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="month"></param>
        /// <param name="CheckSpreadSheetErrorMsg"></param>
        /// <returns></returns>
        public static bool CheckAnnualRateOfpay(DataTable dt, string month, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int[] emptyRowNumber;
            int inc = 0;
            int monthFromDropDown = DateTime.ParseExact(month, "MMMM", CultureInfo.CurrentCulture).Month;
            if (!IsColumnEmptyNewMethod1(ref dt, "ANNUAL_RATE_OF_PAY", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (monthFromDropDown == 3)
                    {
                        if (emptyRowNumber[i] != 0)
                        {
                            inc = emptyRowNumber[i];
                            inc++;
                            CheckSpreadSheetErrorMsg += "<br /> <br />Annual rate of pay 'ANNUAL_RATE_OF_PAY' can not be empty at row number:<B>" + inc + "</B> in spreadsheet.";
                        }
                    }
                }
                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "ANNUAL_RATE_OF_PAY", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }

        public static bool CheckPayMain(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            ///////////////////////////////////////////////////////////////////////////
            ///////// Check PAY_MAIN and PAY_50_50  both should not be null////////////
            ///////////////////////////////////////////////////////////////////////////
            DataTable dtSelected1;
            var dr1 = from row in dt.AsEnumerable()
                      where row.Field<string>("PAY_MAIN") == null || row.Field<string>("PAY_MAIN") == string.Empty
                      select row;

            if (dr1.Any())
            {
                dtSelected1 = dr1.CopyToDataTable();
                if (dtSelected1.Rows.Count > 0)
                {
                    foreach (DataRow drWithEmptyEmployeeContrib1 in dtSelected1.Rows)
                    {
                        if ((drWithEmptyEmployeeContrib1["PAY_50_50"].ToString().Equals(string.Empty) && drWithEmptyEmployeeContrib1["EE_CONT_50_50"].ToString().Equals(string.Empty)) && drWithEmptyEmployeeContrib1["PAY_MAIN"].ToString().Equals(string.Empty))
                        {
                            CheckSpreadSheetErrorMsg += "<br /> <br />'PAY_MAIN', 'PAY_50_50' and EE_CONT_50_50 cannot be empty in one row of the spreadsheet.";
                            result = false;
                        }
                    }
                }
            }


            ///////////////////////////////////////////////////////////////////////////
            ///////// Check PAY_MAIN and PAY_50_50  both cannot have value in one row//
            ///////////////////////////////////////////////////////////////////////////
            DataTable dtSelected2;
            var dr2 = from row in dt.AsEnumerable()
                      where row.Field<string>("PAY_MAIN") != null
                      select row;

            if (dr2.Any())
            {
                dtSelected2 = dr2.CopyToDataTable();
                if (dtSelected2.Rows.Count > 0)
                {
                    foreach (DataRow drWithEmptyEmployeeContrib2 in dtSelected2.Rows)
                    {
                        if ((drWithEmptyEmployeeContrib2["PAY_50_50"].ToString() != string.Empty || drWithEmptyEmployeeContrib2["EE_CONT_50_50"].ToString() != string.Empty) && drWithEmptyEmployeeContrib2["PAY_MAIN"].ToString() != string.Empty)
                        {
                            CheckSpreadSheetErrorMsg += "<br /> <br />'PAY_MAIN', 'PAY_50_50' and EE_CONT_50_50  cannot be present in one row of the spreadsheet.";
                            result = false;
                        }
                    }
                }
            }


            ///////////////////////////////////////////////////////////////////////////////////////////////
            ///////// PAY_MAIN and EE_CONT_MAIN both should have value if any of the column has value //
            ///////////////////////////////////////////////////////////////////////////////////////////////

            DataTable dtSelected3;
            var dr3 = from row in dt.AsEnumerable()
                      where (
                      !string.IsNullOrEmpty(row.Field<string>("PAY_MAIN")) && string.IsNullOrEmpty(row.Field<string>("EE_CONT_MAIN"))
                            //((row.Field<string>("PAY_MAIN") != null || row.Field<string>("PAY_MAIN") != string.Empty) && (row.Field<string>("EE_CONT_MAIN") == null || row.Field<string>("EE_CONT_MAIN") == string.Empty))
                            // ||         ( (row.Field<string>("EE_CONT_MAIN") != null || row.Field<string>("EE_CONT_MAIN") != string.Empty) && (row.Field<string>("PAY_MAIN") == null || row.Field<string>("PAY_MAIN") == string.Empty) )
                            )

                      select row;

            var dr3_1 = from row in dt.AsEnumerable()
                        where (
                        !string.IsNullOrEmpty(row.Field<string>("EE_CONT_MAIN")) && string.IsNullOrEmpty(row.Field<string>("PAY_MAIN"))
                              //((row.Field<string>("EE_CONT_MAIN") != null || row.Field<string>("EE_CONT_MAIN") != string.Empty) && (row.Field<string>("PAY_MAIN") == null || row.Field<string>("PAY_MAIN") == string.Empty))
                              )

                        select row;

            if (dr3.Any() || dr3_1.Any())
            {
                dtSelected3 = dr3.CopyToDataTable();
                CheckSpreadSheetErrorMsg += "<br /> <br />if any of column (PAY_MAIN  or EE_CONT_MAIN both)  has value then both cannot be empty in the spreadsheet.";
                result = false;
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////


            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "PAY_50_50", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            if (!IsValidDoubleType(dt, "EE_CONT_50_50", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }


            return result;
        }


        public static bool CheckEmployeeContrib(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            DataTable dtSelected1;
            var dr1 = from row in dt.AsEnumerable()
                      where row.Field<string>("EE_CONT_MAIN") == null || row.Field<string>("EE_CONT_MAIN") == string.Empty
                      select row;

            if (dr1.Any())
            {
                dtSelected1 = dr1.CopyToDataTable();
                if (dtSelected1.Rows.Count > 0)
                {
                    foreach (DataRow drWithEmptyEmployeeContrib1 in dtSelected1.Rows)
                    {
                        if ((drWithEmptyEmployeeContrib1["PAY_50_50"].ToString().Equals(string.Empty) && drWithEmptyEmployeeContrib1["EE_CONT_50_50"].ToString().Equals(string.Empty)) && drWithEmptyEmployeeContrib1["EE_CONT_MAIN"].ToString().Equals(string.Empty))
                        {
                            CheckSpreadSheetErrorMsg += "<br /> <br />'EE_CONT_MAIN', 'PAY_50_50' and EE_CONT_50_50 cannot be empty in one row of the spreadsheet.";
                            result = false;
                        }
                    }
                }
            }

            DataTable dtSelected2;
            var dr2 = from row in dt.AsEnumerable()
                      where row.Field<string>("EE_CONT_MAIN") != null
                      select row;

            if (dr2.Any())
            {
                dtSelected2 = dr2.CopyToDataTable();
                if (dtSelected2.Rows.Count > 0)
                {
                    foreach (DataRow drWithEmptyEmployeeContrib2 in dtSelected2.Rows)
                    {
                        if ((drWithEmptyEmployeeContrib2["PAY_50_50"].ToString() != string.Empty || drWithEmptyEmployeeContrib2["EE_CONT_50_50"].ToString() != string.Empty) && drWithEmptyEmployeeContrib2["EE_CONT_MAIN"].ToString() != string.Empty)
                        {
                            CheckSpreadSheetErrorMsg += "<br /> <br />'EE_CONT_MAIN', 'PAY_50_50' and EE_CONT_50_50  can notbe present in one row of the spreadsheet.";
                            result = false;
                        }
                    }
                }
            }

            string errorMsg = string.Empty;
            if (!IsValidDoubleType(dt, "EE_CONT_50_50", ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }

        public static bool CheckEmployeePayandContrib(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;

            int ctr = 1;
            foreach (DataRow row in dt.Rows)
            {
                object employee_pay_main = row["PAY_MAIN"];
                object employee_contribution_main = row["EE_CONT_MAIN"];
                object employee_pay_5050 = row["PAY_50_50"];
                object employee_contribution_5050 = row["EE_CONT_50_50"];

                ctr++;

                // PAY_MAIN and PAY_50_50 both cannot be null in one row
                if (
                    (employee_pay_main == DBNull.Value || employee_pay_main.ToString().Equals(string.Empty)) &&
                    (employee_pay_5050 == DBNull.Value || employee_pay_5050.ToString().Equals(string.Empty))
                   )
                {
                    CheckSpreadSheetErrorMsg += "<br /> <br />PAY_MAIN and PAY_50_50 cannot be empty in row number: <B>" + ctr + "</B>";
                    result = false;
                }
                /////////////////////////////////////////////////////////////////

                // PAY_MAIN and PAY_50_50 both cannot have value in one row
                if (
                    (employee_pay_main != DBNull.Value && !employee_pay_main.ToString().Equals(string.Empty)) &&
                    (employee_pay_5050 != DBNull.Value && !employee_pay_5050.ToString().Equals(string.Empty))
                   )
                {
                    CheckSpreadSheetErrorMsg += "<br /> <br />PAY_MAIN and PAY_50_50 cannot have values in row number: " + ctr;
                    result = false;
                }
                /////////////////////////////////////////////////////////////////

                // if any of column (PAY_MAIN and EE_CONT_MAIN) has value then both cannot be null at the same time
                if (
                    (employee_pay_main != DBNull.Value && !employee_pay_main.ToString().Equals(string.Empty)) &&
                    (employee_contribution_main == DBNull.Value || employee_contribution_main.ToString().Equals(string.Empty))
                   )
                {
                    CheckSpreadSheetErrorMsg += "<br /> <br />if any of column (PAY_MAIN  or EE_CONT_MAIN)  has value then both cannot be empty in row number: " + ctr;
                    result = false;
                }

                if (
                    (employee_contribution_main != DBNull.Value && !employee_contribution_main.ToString().Equals(string.Empty)) &&
                    (employee_pay_main == DBNull.Value || employee_pay_main.ToString().Equals(string.Empty))
                   )
                {
                    CheckSpreadSheetErrorMsg += "<br /> <br />if any of column (PAY_MAIN  or EE_CONT_MAIN)  has value then both cannot be empty in row number: " + ctr;
                    result = false;
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////////////


                // if any of column (PAY_50_50 and EE_CONT_50_50) has value then both cannot be null at the same time
                if (
                    (employee_pay_5050 != DBNull.Value && !employee_pay_5050.ToString().Equals(string.Empty)) &&
                    (employee_contribution_5050 == DBNull.Value || employee_contribution_5050.ToString().Equals(string.Empty))
                   )
                {
                    CheckSpreadSheetErrorMsg += "<br /> <br />if any of column (PAY_50_50  or EE_CONT_50_50)  has value then both cannot be empty in row number: " + ctr;
                    result = false;

                }

                if (
                    (employee_contribution_5050 != DBNull.Value && !employee_contribution_5050.ToString().Equals(string.Empty)) &&
                    (employee_pay_5050 == DBNull.Value || employee_pay_5050.ToString().Equals(string.Empty))
                   )
                {
                    CheckSpreadSheetErrorMsg += "<br /> <br />if any of column (PAY_50_50  or EE_CONT_50_50)  has value then both cannot be empty in row number: " + ctr;
                    result = false;

                }
                /////////////////////////////////////////////////////////////////////////////////////////////////////////

            }

            return result;
        }


        public static bool CheckGender(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            string[] validValues = { "F", "M" };
            int[] emptyRowNumber;
            int inc = 0;
            if (!IsColumnEmptyNewMethod1(ref dt, "GENDER", out emptyRowNumber))
            {
                for (int i = 0; i < emptyRowNumber.Length; i++)
                {
                    if (emptyRowNumber[i] != 0)
                    {
                        inc = emptyRowNumber[i];
                        inc++;

                        CheckSpreadSheetErrorMsg += "<br /> <br />Gender 'GENDER' can not be empty at row number:<B>" + inc + "</B> in spreadsheet.";
                    }
                }

                result = false;
            }

            string errorMsg = string.Empty;
            if (!IsValidColumnValues(dt, "GENDER", validValues, ref errorMsg))
            {
                CheckSpreadSheetErrorMsg += errorMsg;
                result = false;
            }

            return result;
        }


        /// <summary>
        /// Qasid added new param: rowRef to find out the row number of error column.
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="columnName"></param>
        /// <param name="wrongDateFormat"></param>
        /// <returns></returns>
        public static bool IsValidDate(DataTable dt, string columnName, ref string wrongDateFormat)
        {
            int rowRef = 1;
            bool result = true;
            foreach (DataRow rw in dt.Rows)
            {
                rowRef++;
                string dateColumnValue = rw[columnName].ToString();
                //Qasid changed this code because date format was converted as 44074
                if (dateColumnValue.Equals("44074"))
                {
                    double value = double.Parse(dateColumnValue);
                    dateColumnValue = DateTime.FromOADate(value).ToString();
                }
                dateColumnValue = dateColumnValue.Replace("00:00:00", "");
                dateColumnValue = dateColumnValue.Trim();


                string updatedColumnValue = dateColumnValue.Replace(".", "/");
                // do not check empty columns
                if (dateColumnValue.Equals(string.Empty))
                {
                    continue;
                }

                DateTime parsed;
                if (!DateTime.TryParseExact(updatedColumnValue, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out parsed))
                {
                    wrongDateFormat = " at row Number: " + rowRef;
                    result = false;

                }
            }

            return result;
        }


        public static bool IsColumnEmpty(DataTable dt, string columnName)
        {
            bool result = true;

            DataTable dtSelected;
            var dr = from row in dt.AsEnumerable()
                     where row.Field<string>(columnName).Trim() == null || (row.Field<string>(columnName)).Trim() == string.Empty
                     select row;

            if (dr.Any())
            {
                dtSelected = dr.CopyToDataTable();
                if (dtSelected.Rows.Count > 0)
                {
                    result = false;
                }
            }

            return result;
        }


        // public static bool IsColumnEmptyNewMethod(DataTable dt, string columnName, out int rowNumber)

        public static bool IsColumnEmptyNewMethod(DataTable dt, string columnName, out int rowNumber)
        {
            bool result = true;
            rowNumber = 0;

            int ctr = 0;
            foreach (DataRow row in dt.Rows)
            {
                object value = row[columnName];

                ctr++;
                if (value == DBNull.Value || value.ToString().Equals(""))
                {
                    result = false;
                    rowNumber = ctr;
                    break;
                }

            }

            return result;
        }

        //Q - Amended function to show all the empty rows
        /// <summary>
        /// 
        /// </summary>this function returns all the values using an array.
        /// <param name="dt"></param>
        /// <param name="columnName"></param>
        /// <param name="rowNumber">contains all the row numbers of datatable</param>
        /// <returns></returns>

        public static bool IsColumnEmptyNewMethod1(ref DataTable dt, string columnName, out int[] rowNumber)
        {
            bool result = true;
            rowNumber = new int[dt.Rows.Count];

            int ctr = 0;
            int i = 0;
            foreach (DataRow row in dt.Rows)
            {
                object value = row[columnName];

                ctr++;
                if (value == DBNull.Value || value.ToString().Equals(""))
                {
                    result = false;
                    rowNumber[i] = ctr;
                    i++;

                }
                //## Data cleansing will be carried out in API level..

            }

            return result;

        }


        //Q changed this code to check address line no characters should be more than 4
        public static bool IsAddressLineCharactersOK(DataTable dt, string columnName, out int[] rowNumber)
        {
            bool result = true;
            rowNumber = new int[dt.Rows.Count];

            int ctr = 0;
            int i = 0;

            foreach (DataRow row in dt.Rows)
            {
                object value = row[columnName];
                ctr++;
                if (value != DBNull.Value)
                {
                    string newValue = value.ToString();
                    //.Where(x=>!string.IsNullOrEmpty(x)).ToArray();                   
                    string addressChars = new string(newValue.Where(Char.IsLetter).ToArray());

                    if (addressChars.Length < 4)
                    {
                        result = false;
                        rowNumber[i] = ctr;
                        i++;
                    }
                }
            }
            return result;
        }

        public static bool IsPostCodeOK(DataTable dt, string columnName, out int[] rowNumber)
        {
            bool result = true;
            rowNumber = new int[dt.Rows.Count];

            int ctr = 0;
            int i = 0;
            foreach (DataRow row in dt.Rows)
            {
                object value = row[columnName];

                ctr++;
                if (value != DBNull.Value)
                {

                    string postcode = value.ToString();
                    if (postcode.Length < 6)
                    {
                        result = false;
                        rowNumber[i] = ctr;
                    }

                    if (!IsPostCode(postcode))
                    {
                        result = false;
                        rowNumber[i] = ctr;

                    }

                    if (!IsPostCodeSpaceOK(postcode))
                    {
                        result = false;
                        rowNumber[i] = ctr;
                    }

                    i++;
                }
            }

            return result;
        }


        static public bool IsPostCodeSpaceOK(string postcode)
        {
            bool result = true;
            int postcodeLength = postcode.Length;

            if (postcodeLength >= 4)
            {
                char tempStr = postcode[postcode.Length - 4];


                if (tempStr != ' ')
                {
                    result = false;
                }
            }

            return result;
        }

        static public bool IsPostCode(string postcode)
        {
            return (
                System.Text.RegularExpressions.Regex.IsMatch(postcode, "(^[A-PR-UWYZa-pr-uwyz][0-9][ ]*[0-9][ABD-HJLNP-UW-Zabd-hjlnp-uw-z]{2}$)") ||
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
            bool result = false;
            result = System.Text.RegularExpressions.Regex.IsMatch(NINO, "^[A-CEGHJ-PR-TW-Z]{1}[A-CEGHJ-NPR-TW-Z]{1}[0-9]{6}[A-D]{0,1}$");//"[ABCEGHJKLMNPRSTWXYZ][0-9]{6}[A-D ]");

            return result;
        }


        public static bool IsNINOFormatOK(DataTable dt, string columnName, out int[] rowNumber)
        {
            bool result = true;
            rowNumber = new int[dt.Rows.Count];
            DataTable dtSelected;

            int ctr = 0;
            int i = 0;
            foreach (DataRow row in dt.Rows)
            {
                object value = row[columnName];

                ctr++;
                if (value != DBNull.Value)
                {

                    string nino = value.ToString();
                    if (!IsNINO(nino))
                    {
                        result = false;
                        rowNumber[i] = ctr;
                        i++;
                    }
                }
            }

            return result;
        }
        public static bool IsValidColumnValues(DataTable dt, string columnName, string[] validValues, ref string errorMsg)
        {
            bool result = true;
            int rowRef = 1;

            string allValiedValues = string.Empty;

            foreach (string validValue in validValues)
            {
                allValiedValues += "<br />" + validValue;
            }

            // get distinct column values
            DataTable newDt = dt.DefaultView.ToTable(true, new String[] { columnName });

            foreach (DataRow rw in dt.Rows)
            {
                rowRef++;
                string columnValue = rw[columnName].ToString();
                columnValue = columnValue.Trim();

                if (columnValue.Trim().Equals(string.Empty))
                {
                    continue;
                }

                if (!validValues.Contains(columnValue.Trim(), StringComparer.CurrentCultureIgnoreCase))
                {
                    errorMsg += "<br /> <br />You have entered invalid '" + columnName + "' in spreadsheet.<br />'" + columnName + "' value can be " + allValiedValues + "at row number: " + rowRef + " of spreadsheet.";
                    result = false;
                }
            }

            return result;
        }

        public static bool IsValidDoubleType(DataTable dt, string columnName, ref string errorMsg)
        {
            bool result = true;
            int inc = 1;
            foreach (DataRow rw in dt.Rows)
            {
                inc++;
                string columnValue = rw[columnName].ToString();
                columnValue = columnValue.Trim();
                // do not check empty columns
                if (columnValue.Equals(string.Empty))
                {
                    continue;
                }

                double parsed;
                if (!double.TryParse(columnValue, out parsed))
                {
                    errorMsg += "<br /> <br />You have entered invalid '" + columnName + " value' in spreadsheet at row: <B>" + inc + "</B>";
                    result = false;
                }
            }

            return result;
        }


        public static bool CheckEmployerLocCode(DataTable dt, List<PayrollProvidersBO> validPayLocList, ref string CheckSpreadSheetErrorMsg)
        {
            bool result = true;
            int inc = 1;

            bool invalidPayLocationCodeFound = false;   //## an extra variable- to customize the error message.. to add a 'Solution info' at the end of all same errors..            

            foreach (DataRow rw in dt.Rows)
            {
                inc++;
                string payLocID = rw["EMPLOYER_LOC_CODE"].ToString();

                // if payloc is empty string then goto next payloc to process. Some time employer add empty rows in spreadsheet
                if (!payLocID.Equals(string.Empty))
                {
                    if (validPayLocList.Any(pl => pl.pay_location_ID == payLocID))
                    {
                        //## all good...
                    }
                    else
                    {
                        CheckSpreadSheetErrorMsg += "<br />You have entered invalid 'Employer Location Code: in spreadsheet at row number: " + inc;
                        invalidPayLocationCodeFound = true;     //## that's it- we know there is a criminal.. 
                        result = false;
                    }
                }

            }

            if (invalidPayLocationCodeFound)
            {   //## avoid adding 'message/tips' to at the end of each error..
                CheckSpreadSheetErrorMsg += "<br/><div class='h4 text-danger mt-2 mb-2'>Tips: Valid 'Employer Location Code' is shown above in 'Payroll Provider' drop down list.</div>";
            }

            return result;

        }
    }
}
