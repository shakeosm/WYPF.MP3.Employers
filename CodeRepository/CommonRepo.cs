using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace MCPhase3.CodeRepository
{
    public class CommonRepo
    {
        const string SessionKeyMonth = "_month";
        const string SessionKeyFileName = "_fileName";

        public static DataTable valuePassDT;
        public static DataTable nameOfMonthDT = new DataTable();

        //copy file to another folder
        public static bool CopyFileToFolder(string sourceFile, string destinationFile, string fileName)
        {
            bool result = true;
            try
            {
                if(!Directory.Exists(destinationFile))
                    Directory.CreateDirectory(destinationFile); 

                destinationFile = Path.Combine(destinationFile, fileName);
                 File.Copy(sourceFile, destinationFile, true);
            }
            catch (IOException ex)
            {
                result = false;
            }
            return result;
        }

        public static DataTable ExcelToDT(string fileName, out string errorMessage)
        {
            OleDbConnection con = null;
            DataTable retData;
            DataTable schemaTable = null;
            DataTable myTable = new DataTable();
            DataTable dtUpdate = new DataTable();
            DataSet myDataSet = new DataSet();
            string dtXMLStr = string.Empty;
            IWorkbook xssWorkbook = null;

            errorMessage = string.Empty;
            bool result = true;
            string sheetNameEx = string.Empty;
            DataTable dtTable = new DataTable();
            List<string> rowList = new List<string>();
            ISheet sheet;
            
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                if (Path.GetExtension(fileName) == ".xlsx")
                    xssWorkbook = new XSSFWorkbook(stream);
                else if (Path.GetExtension(fileName) == ".xls")
                    xssWorkbook = new HSSFWorkbook(stream);

                sheet = xssWorkbook.GetSheetAt(0);
                //get the name of sheet from Excel.
                sheetNameEx = xssWorkbook.GetSheetAt(0).SheetName.ToUpper();
                bool tableFound = false;
                if (sheetNameEx.Contains("MC_Data".Trim().ToUpper()) || sheetNameEx.Contains("MC_Data_Fire".Trim().ToUpper()))
                {
                    tableFound = true;                    
                }              
                if (!tableFound)
                {
                    errorMessage = " Excel spreadsheet 'MC_Data' or 'MC_Data_Fire' not found within Excel file.";
                    //Logger.Write(LogLevel.ERROR, "Excel spreadsheet not found with in excel file");
                    return null;
                }

                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;
                for (int j = 0; j < cellCount; j++)
                {
                    ICell cell = headerRow.GetCell(j);
                    if (cell == null || string.IsNullOrWhiteSpace(cell.ToString())) continue;
                    {
                        //Created a dtTable according to database columns and directly inserting data inside from datatable.
                        // AllColumnExistInExSheetCheck(dtTable, xmlDT, cell.ToString());
                        dtTable.Columns.Add(cell.ToString());
                    }
                }

                for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;

                    DataRow dataRow = dtTable.NewRow();
                    for (int j = row.FirstCellNum; j < cellCount; ++j)
                    {
                        ICell cell = row.GetCell(j);
                        // ICell cell = cell.GetCell(j);
                        if (cell == null)
                        {
                            try
                            {
                                dataRow[j] = "";
                            }
                            catch (Exception)
                            {
                                dataRow[j] = DBNull.Value;
                            }
                        }
                        else
                        {
                            try{
                                switch (cell.CellType)
                                {
                                    case CellType.Blank:
                                        try
                                        {
                                            dataRow[j] = "";
                                        }
                                        catch (Exception)
                                        {
                                            dataRow[j] = DBNull.Value;
                                        }
                                        break;
                                    case CellType.Numeric:
                                        short format = cell.CellStyle.DataFormat;
                                        //Processing time format (2015.12.5, 2015/12/5, 2015-12-5, etc.)
                                        // if (format == 14 || format == 31 || format == 57 || format == 58)
                                        if (DateUtil.IsCellDateFormatted(cell))
                                        {
                                            DateTime date = cell.DateCellValue;
                                            dataRow[j] = date.ToString("dd/MM/yyyy");
                                        }
                                        else
                                            dataRow[j] = cell.NumericCellValue;
                                        break;
                                    case CellType.String:
                                        dataRow[j] = cell.StringCellValue;
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                errorMessage = "Data in row" + i + "is not in readable fromat.";
                            }
                            //CellType(Unknown = -1,Numeric = 0,String = 1,Formula = 2,Blank = 3,Boolean = 4,Error = 5,)
                            
                        }
                    }
                    dtTable.Rows.Add(dataRow);
                }
            }
            // retData = OpenOleDBConnection(fileName, ref con);
            if (dtTable is null)
            {
                errorMessage = " System could not open uploaded file.";
                return null;
            }
          

            return dtTable;
        }

            //try
            //{
            //    // Retrieve schema information about tables.
            //    //Because tables include tables, views, and other objects,
            //    //restrict to just TABLE in the Object array of restrictions.
            //    //schemaTable = con.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
            //    //new object[] { null, null, null, null });
            //    schemaTable = dtTable;
            //}
            //catch (Exception ex)
            //{
            //    errorMessage = ex.Message;
            //    //Logger.Write(LogLevel.ERROR, ex.Message);
            //    //con.Close();
            //    //con.Dispose();
            //    return null;
            //}

            
            //string sheetNameInDT = string.Empty;
            ////List the table name from each row in the schema table.
            //string drData = "";
            //try
            //{
            //    foreach (DataColumn dc in schemaTable.Columns)
            //        drData = drData + (drData.Length == 0 ? "" : ",") + dc.ColumnName;

            //    // check sheet name within excel file exist or not
            //    foreach (DataRow dr in schemaTable.Rows)
            //    {
            //        drData = "";
            //        foreach (DataColumn dc in schemaTable.Columns)
            //            drData = drData + (drData.Length == 0 ? "" : ",") + dr[dc.ColumnName].ToString();

            //       // sheetNameInDT = dr["TABLE_NAME"].ToString().Trim().ToUpper();//dr["TABLE_NAME"].ToString().Trim().ToUpper();
            //        if (sheetNameEx.Contains(sheetName.Trim().ToUpper()))
            //        {
            //            tableFound = true;
            //            break;
            //        }
            //    }

            //    if (!tableFound)
            //    {
            //        errorMessage = " Excel spreadsheet 'MC_Data' not found with in Excel file.";
            //        //Logger.Write(LogLevel.ERROR, "Excel spreadsheet not found with in excel file");
            //        return null;
            //    }

                //OleDbDataAdapter objda = new OleDbDataAdapter("Select * from [" + sheetNameInDT + "] ", con);

        //        //objda.Fill(myTable);
        //        myTable = schemaTable;

        //    }
        //    catch (Exception ex)
        //    {
        //        errorMessage = ex.Message;
        //        //Logger.Write(LogLevel.ERROR, ex.Message);
        //        //con.Close();
        //        //con.Dispose();
        //        return null;
        //    }
        //    finally
        //    {
        //        //con.Close();
        //        //con.Dispose();
        //    }

        //    // delete top rows from dataTable

        //    try
        //    {
        //        if (IgnoreTopNoOfLines < myTable.Rows.Count)
        //        {
        //            for (int ctr = 0; ctr < IgnoreTopNoOfLines; ctr++)
        //            {
        //                myTable.Rows[ctr].Delete();
        //            }

        //            myTable.AcceptChanges();
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        errorMessage = ex.Message;
        //        //Logger.Write(LogLevel.ERROR, ex.Message);
        //        return null;
        //    }

        //    return myTable;
        //}
      

        //public static bool OpenOleDBConnection(string fileName, ref OleDbConnection con)
        //{
        //    bool result = true;

        //    string xlsCon = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + fileName + "; Extended Properties=\"Excel 12.0;  HDR=Yes; IMEX=1;\"  ";
        //    con = new OleDbConnection(xlsCon);
        //    try
        //    {
        //        if (con.State != ConnectionState.Open)
        //            con.Open();
        //    }
        //    catch (Exception ex)
        //    {
        //        result = false;
        //    }

        //    return result;
        //}
        public static DataTable OpenOleDBConnection(string fileName, ref OleDbConnection con)
        {
            bool result = true;
            string sheetName = string.Empty;
            DataTable dtTable = new DataTable();
            List<string> rowList = new List<string>();
            ISheet sheet;

            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                stream.Position = 0;
                XSSFWorkbook xssWorkbook = new XSSFWorkbook(stream);
                sheet = xssWorkbook.GetSheetAt(0);
                //get the name of sheet from Excel.
                sheetName = xssWorkbook.GetSheetAt(0).SheetName;
                IRow headerRow = sheet.GetRow(0);
                int cellCount = headerRow.LastCellNum;
                for (int j = 0; j < cellCount; j++)
                {
                    ICell cell = headerRow.GetCell(j);
                    if (cell == null || string.IsNullOrWhiteSpace(cell.ToString())) continue;
                    {
                        dtTable.Columns.Add(cell.ToString());
                    }
                }
                for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    if (row == null) continue;
                    if (row.Cells.All(d => d.CellType == CellType.Blank)) continue;
                    for (int j = row.FirstCellNum; j < cellCount; j++)
                    {
                        if (row.GetCell(j) != null)
                        {
                            if (!string.IsNullOrEmpty(row.GetCell(j).ToString()) && !string.IsNullOrWhiteSpace(row.GetCell(j).ToString()))
                            {
                                rowList.Add(row.GetCell(j).ToString());
                            }
                        }
                    }
                    if (rowList.Count > 0)
                        dtTable.Rows.Add(rowList.ToArray());
                    rowList.Clear();
                }
            }
            return dtTable; 
        }
    

    public bool CheckFileIsExcel(string fileName)
    {
        bool result = true;

        OleDbConnection con = null;
        string xlsCon = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" + fileName + "; Extended Properties=\"Excel 12.0;  HDR=Yes; IMEX=1;\"  ";
        con = new OleDbConnection(xlsCon);
        try
        {
            if (con.State != ConnectionState.Open)
            {
                con.Open();
                con.Close();
            }
        }
        catch (Exception ex)
        {
            result = false;
        }

        return result;
    }
    /// <summary>
    /// Function is seperated below this function for Fire becaurse Fire has more column headings.
    /// </summary>
    /// <param name="dt"></param>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    public static bool ChangeColumnHeadings(DataTable dt, out string errorMessage)
    {
        errorMessage = string.Empty;          

        bool result = true; 
         string[] arr = { "PAYROLL_PD", "PAYROLL_YR", "EMPLOYER_LOC_CODE", "EMPLOYER_NAME", "MEMBERS_TITLE", "SURNAME", "FORENAMES", "GENDER", "DOB", "JOBTITLES", "ADDRESS1", "ADDRESS2", "ADDRESS3", "ADDRESS4", "ADDRESS5", "POSTCODE", "COSTCODE", "MEMBER_NO", "NI_NUMBER", "PAYREF", "POSTREF", "FT_PT_CS_FLAG", "FT_PT_HOURS_WORKED", "STD_HOURS", "CONTRACTUAL_HRS", "DATE_JOINED_SCHEME", "ENROLMENT_TYPE", "DATE_OF_LEAVING_SCHEME", "OPTOUT_FLAG", "OPTOUT_DATE", "PAY_MAIN", "EE_CONT_MAIN", "PAY_50_50", "EE_CONT_50_50", "50_50_START_DATE", "50_50_END_DATE", "PRCHS_OF_SRV", "ARC_CONTS", "EE_APC_CONTS", "ER_APC_CONTS", "ER_CONTS", "ANNUAL_RATE_OF_PAY", "TOTAL_AVC_CONTRIBUTIONS_PAID", "NOTES" };
         
        try
        {
            for (int ctr = 0; ctr < arr.Length; ctr++)
            {
                dt.Columns[ctr].ColumnName = arr[ctr];
            }
        }
        catch (Exception ex)
        {
            errorMessage = " Spreadsheet column headings are wrong.";
            result = false;
            //Logger.Write(LogLevel.ERROR, ex.Message);
        }

        return result;
    }
        public static bool ChangeColumnHeadingsFire(DataTable dt, out string errorMessage)
        {
            errorMessage = string.Empty;          

            bool result = true;
           
                string[] arr = { "PAYROLL_PD", "PAYROLL_YR", "EMPLOYER_LOC_CODE", "EMPLOYER_NAME", "SCHEME_NAME", "MEMBERS_TITLE", "SURNAME", "FORENAMES", "GENDER", "DOB", "RANK_ROLE", "ADDRESS1", "ADDRESS2", "ADDRESS3", "ADDRESS4", "ADDRESS5", "POSTCODE", "COSTCODE", "MEMBER_NO", "NI_NUMBER", "PAYREF", "POSTREF", "FT_PT_RT_FLAG", "STD_HOURS", "CONTRACTUAL_HRS", "DATE_JOINED_SCHEME", "ENROLMENT_TYPE", "DATE_OF_LEAVING_SCHEME", "OPTOUT_FLAG", "OPTOUT_DATE", "PENSIONABLE_PAY_1992_2006_SCHEME", "PENSIONABLE_PAY_2015_CARE_SCHEME", "EE_CONTS", "APB_FOR_TEMP_PROMOTION_CONTS", "CPD_TOT_PEN_EE_CONTS", "PRCHS_OF_60TH", "ADDED_PENSION_CONTRIBUTIONS", "ANNUAL_RATE_OF_PENSIONABLE", "AVERAGE_PENSIONABLE_PAY", "NOTES" };
          
            try
            {
                for (int ctr = 0; ctr < arr.Length; ctr++)
                {
                    dt.Columns[ctr].ColumnName = arr[ctr];
                }
            }
            catch (Exception ex)
            {
                errorMessage = " Spreadsheet column headings are wrong.";
                result = false;
                //Logger.Write(LogLevel.ERROR, ex.Message);
            }

            return result;
        }

        public bool DeleteUploadedFile(string fileFullName, out string errorMsg)
    {
        bool answer = true;
        errorMsg = string.Empty;
        try
        {
            System.IO.File.Delete(fileFullName);
        }
        catch (Exception ex)
        {
            errorMsg = "File delete exception from server. " + ex.Message;
            answer = false;
        }

        return answer;
    }

    public static DataTable ConvertAllFieldsToString(DataTable dt)
    {
            //nameOfMonthDt I am using to save values that I want to use on different views.
            //if (nameOfMonthDT.Columns.Count > 0)
            //{
              //  nameOfMonthDT.Clear();

            //}

            ///Session I am using to keep the values that I needed to use on different pages/views     
           // HttpContext.Session.SetString(SessionKeyMonth, nameOfMonth.ToString());
            //nameOfMonthDT.Columns.Add(nameOfMonth.ToString());
            //nameOfMonthDT.Columns.Add(fileName);
           
           // int numberOfRows = dt.Rows.Count;
            //nameOfMonthDT.Columns.Add((numberOfRows - 1).ToString());

            DataTable dtClone = dt.Clone(); //just copy structure, no data
        for (int i = 0; i < dtClone.Columns.Count; i++)
        {
            if (dtClone.Columns[i].DataType != typeof(string))
            {
                dtClone.Columns[i].DataType = typeof(string);
            }
        }

        // copy data from one table to another
        foreach (DataRow dr in dt.Rows)
        {
            dtClone.ImportRow(dr);
        }

        // remove pound £ sign from fields
        foreach (DataRow dr in dtClone.Rows)
        {
            for (int ctr = 0; ctr < dtClone.Columns.Count; ctr++)
            {
                if (
                    dtClone.Columns[ctr].ColumnName.Equals("PAY_MAIN", StringComparison.InvariantCultureIgnoreCase) ||
                    dtClone.Columns[ctr].ColumnName.Equals("EE_CONT_MAIN", StringComparison.InvariantCultureIgnoreCase) ||
                    dtClone.Columns[ctr].ColumnName.Equals("PAY_50_50", StringComparison.InvariantCultureIgnoreCase) ||
                    dtClone.Columns[ctr].ColumnName.Equals("EE_CONT_50_50", StringComparison.InvariantCultureIgnoreCase) ||
                    dtClone.Columns[ctr].ColumnName.Equals("PRCHS_OF_SRV", StringComparison.InvariantCultureIgnoreCase) ||
                    dtClone.Columns[ctr].ColumnName.Equals("ARC_CONTS", StringComparison.InvariantCultureIgnoreCase) ||
                    dtClone.Columns[ctr].ColumnName.Equals("EE_APC_CONTS", StringComparison.InvariantCultureIgnoreCase) ||
                    dtClone.Columns[ctr].ColumnName.Equals("ER_APC_CONTS", StringComparison.InvariantCultureIgnoreCase) ||
                    dtClone.Columns[ctr].ColumnName.Equals("ER_CONTS", StringComparison.InvariantCultureIgnoreCase) ||
                    dtClone.Columns[ctr].ColumnName.Equals("ANNUAL_RATE_OF_PAY", StringComparison.InvariantCultureIgnoreCase)
                    )
                {
                    dr[ctr] = dr[ctr].ToString().Replace("£", string.Empty);
                    dr[ctr] = dr[ctr].ToString().Replace(",", string.Empty);
                }
            }

        }

            //List<System.Data.DataRow> removeRowIndex = new List<System.Data.DataRow>();
            //foreach (DataRow dr in dtClone.Rows)
            //{
            //    if (CheckRowEmpty(dr))
            //    {
            //        removeRowIndex.Add(dr);
            //    }

            //}
            //// Remove all blank of in-valid rows
            //foreach (System.Data.DataRow rowIndex in removeRowIndex)
            //{
            //    dtClone.Rows.Remove(rowIndex);
            //}
            valuePassDT = dtClone.Copy();
        return dtClone;
    }

        public static DataTable PassDataTableToViews()
        {

            return valuePassDT;
        }

       // public static DataTable MonthandPayrollDT()
        //{
          //  return nameOfMonthDT;
        //}
        public bool CheckEmployerLocCode(DataTable dt, ref string CheckSpreadSheetErrorMsg)
        {
        bool result = true;



        // get paylocations from spreadsheet
        DataTable newDt = dt;//dt.DefaultView.ToTable(true, new String[] { "EMPLOYER_LOC_CODE" });
        int inc = 1;

        //int Emp_Loc = 0;

        foreach (DataRow rw in newDt.Rows)
        {

            string payLocID;
            payLocID = rw["EMPLOYER_LOC_CODE"].ToString();
            inc++;

            if (payLocID.Equals(string.Empty))
            {

                CheckSpreadSheetErrorMsg += "<BR />'Employer Location Code' can not be empty in spreadsheet at row number:" + inc + ".<BR />And if there is any empty row, please delete that row.";
                result = false;
            }
        }

        bool validPalocInSpreadsheet = false;
        string[] payrolls = { "480", "490", "550" };
        //initialize inc 
        inc = 1;
        foreach (DataRow rw in newDt.Rows)
        {
            inc++;

            validPalocInSpreadsheet = false;
            string payLocID = rw["EMPLOYER_LOC_CODE"].ToString();

            // if payloc is empty string then goto next payloc to process. Some time employer add empty rows in spreadsheet
            if (!payLocID.Equals(string.Empty))
            {
                // foreach (var item in this.dListPayrollPrivider.Items)
                foreach (var item in payrolls)
                {
                    // if (item.Value.Trim().Equals(payLocID.Trim()))
                    if (item.Equals(payLocID.Trim()))
                    {
                        validPalocInSpreadsheet = true;

                        //Q-comment break point
                        break;                            
                    }
                }
            }

            //Q - added condition to check if pay loc is not empty as well
            if (!validPalocInSpreadsheet && !payLocID.Equals(string.Empty))
            {
                //if (!CheckSpreadSheetErrorMsg.Equals(string.Empty))
                //{
                //    CheckSpreadSheetErrorMsg = "<BR /> <BR />";
                //}

                CheckSpreadSheetErrorMsg += "<BR />You have entered invalid 'Employer Location Code:<B>"
                        + payLocID + "</B> in spreadsheet at row number: " + inc + ".<BR />Valid 'Employer Location Code' shown above in 'Payroll Provider' drop down list.";
                result = false;

                //        //Q-comment break point
                //     break;
            }

        }

        return result;

    }
}


    
}
