using System;
using System.Data;
using System.IO;

namespace MCPhase3.CodeRepository
{
    public class CommonRepo : ICommonRepo
    {
        private readonly IRedisCache _cache;

        public CommonRepo(IRedisCache Cache)
        {
            _cache = Cache;
        }

        //copy file to another folder
        public bool CopyFileToFolder(string sourceFile, string destinationFile, string fileName)
        {
            bool result = true;
            try
            {
                if (!Directory.Exists(destinationFile))
                    Directory.CreateDirectory(destinationFile);

                destinationFile = Path.Combine(destinationFile, fileName);
                File.Move(sourceFile, destinationFile, true);
            }
            catch (IOException ex)
            {
                result = false;
            }
            return result;
        }
        
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

                    CheckSpreadSheetErrorMsg += "<br />'Employer Location Code' can not be empty in spreadsheet at row:" + inc + ".<br />And if there is any empty row, please delete that row.";
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
                    CheckSpreadSheetErrorMsg += $"<br/>Invalid 'Employer Location Code:<b>{payLocID}</b> in spreadsheet at row: {inc}";
                    result = false;  break;
                }

            }

            CheckSpreadSheetErrorMsg += ".<br />Valid 'Employer Location Code' shown above in 'Payroll Provider' drop down list.";

            return result;

        }
    }

    public interface ICommonRepo
    {
        bool CopyFileToFolder(string sourceFile, string destinationFile, string fileName);
       
        bool CheckEmployerLocCode(DataTable dt, ref string CheckSpreadSheetErrorMsg);
    }
}
