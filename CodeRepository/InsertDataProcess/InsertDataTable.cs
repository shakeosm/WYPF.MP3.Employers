using DocumentFormat.OpenXml.Spreadsheet;
using MCPhase3.CodeRepository.HelperClasses;
using MCPhase3.Common;
using NPOI.Util;
using System;
using System.Collections.Generic;
using System.Data;

namespace MCPhase3.CodeRepository.InsertDataProcess
{
    public class InsertDataTable : IInsertDataTable
    {
        public DataTable dtInsert;
        XmlReader helper = new XmlReader();
        DTColumnDataChecks dtCheck = new DTColumnDataChecks();
        private readonly IRedisCache _cache;

        public InsertDataTable(IRedisCache Cache)
        {
            _cache = Cache;
        }

        public DataTable ReadAndSaveXML(DataTable excelData, string xmlPath, string userId)
        {
            var columnDefinations = new List<ColumnDefinations>(helper.readXMLFile(xmlPath));
            //columnDefinations = helper.readXMLFile(path);
            dtInsert = dtCheck.AllColumnExistInExSheetCheck(excelData, columnDefinations);
            _cache.Set($"{userId}_{Constants.Excel_DataTableToInsert}", dtInsert);
            //_cache.Set($"{userId}_{Constants.Excel_XML_ConfigPath}", xmlPath);
            
            Console.WriteLine($"{userId} > DataTable ReadAndSaveXML() => xmlPath: {xmlPath}, excelData.Rows: {excelData.Rows.Count}");

            return dtInsert;
        }

        /// <summary>
        /// This will insert new column in the DataTable, and add values in all rows, ie: UserName, ClientId, RemittanceId, MODDATE, PostDate
        /// </summary>
        /// <param name="row"></param>
        /// <param name="userName"></param>
        /// <param name="schemeName"></param>
        /// <param name="clientID"></param>
        /// <param name="remittanceID"></param>
        /// <param name="newData"></param>
        /// <param name="path"></param>
        public DataTable ProcessDataTable(int row, string userName, string schemeName, string clientID, string remittanceID, DataTable dtInsert, string path)
        {            

            //newData table does not have rem and client id in start but when it was called 1st time I added both in datatable
            //so If I needed to call again this function I do not need to add them and only need to add their values.
            if (string.IsNullOrEmpty(remittanceID))
            {
                Console.WriteLine($"{userName} > DataTable PassDt(), first run.. no Remittance created yet...");
                var result = ReadAndSaveXML(dtInsert, path, userName);
                return result;
            }
            else
            {
                dtInsert = _cache.Get<DataTable>($"{userName}_{Constants.Excel_DataTableToInsert}");
                Console.WriteLine($"{userName} > DataTable PassDt() => path: {path}, remittanceID: {remittanceID}, Rows: {row}, dtInsert.Rows: {dtInsert?.Rows?.Count}");

                //## These are extra columns will be added to the Excel sheet preparing for Database- which already has many other columns.                
                try
                {
                    dtInsert.Columns.Add("REMITTANCE_ID");
                    dtInsert.Columns.Add("DATAROWID_RECD", typeof(int));
                    dtInsert.Columns.Add("CLIENTID");
                    dtInsert.Columns.Add("SCHEMENAME");
                    dtInsert.Columns.Add("MODUSER");
                    dtInsert.Columns.Add("MODDATE", typeof(DateTime));
                    dtInsert.Columns.Add("MODTYPE");
                    dtInsert.Columns.Add("POSTDATE", typeof(DateTime));
                }
                catch (DuplicateNameException ex)
                {
                    Console.WriteLine($"{DateTime.Now.ToLongTimeString()}, {userName} > DuplicateNameException: {ex.ToString()}\r\nremittanceID: {remittanceID}") ;
                    //## if a 'DuplicateNameException' is found- then just ignore and carry on..
                }

                int total = dtInsert.Rows.Count;
                for (int i = 0; i < total; i++)
                {
                    dtInsert.Rows[i]["REMITTANCE_ID"] = remittanceID;
                    dtInsert.Rows[i]["DATAROWID_RECD"] = row++;

                    dtInsert.Rows[i]["CLIENTID"] = clientID;
                    dtInsert.Rows[i]["SCHEMENAME"] = schemeName;

                    dtInsert.Rows[i]["MODUSER"] = userName;
                    dtInsert.Rows[i]["MODTYPE"] = "A";
                    dtInsert.Rows[i]["MODDATE"] = Convert.ToDateTime(DateTime.Now.ToString("dd/MM/yyyy"));
                    dtInsert.Rows[i]["POSTDATE"] = Convert.ToDateTime(DateTime.Now.ToString("dd/MM/yyyy"));
                }
                dtInsert.AcceptChanges();
                //newData.Dispose();

                return dtInsert;
            }
        }

        public DataTable Get(int row, string userName, string schemeName, string clientID, string remittanceID)
        {
            dtInsert = _cache.Get<DataTable>($"{userName}_{Constants.Excel_DataTableToInsert}");

            //PassDT will have already xml document path so I keep it empty here.
            dtInsert = ProcessDataTable(row, userName, schemeName, clientID, remittanceID, dtInsert, path:"");

            Console.WriteLine($"{userName} > DataTable Get() => schemeName: {schemeName}, remittanceID: {remittanceID}");

            return dtInsert;
        }

    }

    public interface IInsertDataTable
    {
        DataTable ReadAndSaveXML(DataTable newData, string path, string userId);

        /// <summary>
        /// This will insert new column in the DataTable, and add values in all rows, ie: UserName, ClientId, RemittanceId, MODDATE, PostDate
        /// </summary>
        /// <param name="row"></param>
        /// <param name="userName"></param>
        /// <param name="schemeName"></param>
        /// <param name="clientID"></param>
        /// <param name="remittanceID"></param>
        /// <param name="newData"></param>
        /// <param name="path"></param>
        DataTable ProcessDataTable(int row, string userName, string schemeName, string clientID, string remittanceID, DataTable newData, string path);

        DataTable Get(int row, string userName, string schemeName, string clientID, string remittanceID);
    }
}
