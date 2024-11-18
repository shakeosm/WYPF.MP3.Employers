using MCPhase3.CodeRepository.HelperClasses;
using MCPhase3.Common;
using MCPhase3.ViewModels;
using System;
using System.Collections.Generic;
using System.Data;

namespace MCPhase3.CodeRepository.InsertDataProcess
{
    public class ExcelData : IExcelData
    {
        public DataTable dtInsert;
        XmlReader helper = new XmlReader();
        DTColumnDataChecks dtCheck = new DTColumnDataChecks();
        private readonly IRedisCache _cache;

        public ExcelData(IRedisCache Cache)
        {
            _cache = Cache;
        }


        /// <summary>
        /// This will add values in all rows, ie: UserName, ClientId, RemittanceId, MODDATE, PostDate
        /// </summary>
        /// <param name="remittanceId">Remittance Id</param>
        /// <param name="newDataRowRecordId">DataRowRecord Id to start from</param>
        /// <param name="clientId">Client Id</param>
        /// <param name="schemeName">Scheme name</param>
        /// <param name="userName">User Id</param>
        public void AddRemittanceInfo(long remittanceId, int newDataRowRecordId, string clientId, string schemeName, string userName)
        {
            string cacheKeyName = $"{userName}_{Constants.ExcelData_ToInsert}";
            var excelData = _cache.Get<List<ExcelsheetDataVM>>(cacheKeyName);
            foreach (var item in excelData)
            {
                item.REMITTANCE_ID= remittanceId;
                item.DATAROWID_RECD = newDataRowRecordId++;
                item.CLIENTID = clientId;
                item.SCHEMENAME = schemeName;
                item.MODUSER = userName;
                //item.MODTYPE = Constants.DATA_MODIFY_ADD;
                //item.MODDATE = DateTime.Now;
                
                //item.EMPLOYER_NAME = userName;
            }

            _cache.Set(cacheKeyName, excelData);
        }

        public List<ExcelsheetDataVM> Get(string userName)
        {
            var result = _cache.Get<List<ExcelsheetDataVM>>($"{userName}_{Constants.ExcelData_ToInsert}");

            return result;
        }

    }

    public interface IExcelData
    {
        //DataTable ReadAndSaveXML(DataTable newData, string path, string userId);

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
        //DataTable ProcessDataTable(int row, string userName, string schemeName, string clientID, string remittanceID, DataTable newData, string path);

        /// <summary>
        /// This will add RemittanceInfo in all rows, ie: UserName, ClientId, SchemeName, RemittanceId, MODDATE, PostDate
        /// </summary>
        /// <param name="remittanceId">Remittance Id</param>
        /// <param name="newDataRowRecordId">DataRowRecord Id to start from</param>
        /// <param name="clientId">Client Id</param>
        /// <param name="schemeName">Scheme name</param>
        /// <param name="userName">User Id</param>
        void AddRemittanceInfo(long remittanceId, int newDataRowRecordId, string clientId, string schemeName, string userName);

        //DataTable Get(int row, string userName, string schemeName, string clientID, string remittanceID);
        List<ExcelsheetDataVM> Get(string userName);
    }
}
