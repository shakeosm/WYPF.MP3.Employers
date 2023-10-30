using MCPhase3.CodeRepository.HelperClasses;
using System;
using System.Collections.Generic;
using System.Data;

namespace MCPhase3.CodeRepository.InsertDataProcess
{
    public class InsertDataTable
    {
        public static DataTable dtInsert;
        XmlReader helper = new XmlReader();
        DTColumnDataChecks dtCheck = new DTColumnDataChecks();
        public void readAndSaveXML(DataTable newData, string path)
        {
            List<ColumnDefinations> columnDefinations = new List<ColumnDefinations>(helper.readXMLFile(path));
            //columnDefinations = helper.readXMLFile(path);
            dtInsert = dtCheck.AllColumnExistInExSheetCheck(newData, columnDefinations);
        }

        public void PassDt(int row, string userName, string schemeName, string clientID, string remittanceID, DataTable newData, string path)
        {
            //DataTable newData;
            //newData table does not have rem and client id in start but when it was called 1st time I added both in datatable
            //so If I needed to call again this function I do not need to add them and only need to add their values.
            if (string.IsNullOrEmpty(remittanceID))
            {
                readAndSaveXML(newData, path);
            }
            else
            {
                //## These are extra columns will be added to the Excel sheet preparing for Database- which already has many other columns.
                int total = dtInsert.Rows.Count;
                dtInsert.Columns.Add("REMITTANCE_ID");
                dtInsert.Columns.Add("DATAROWID_RECD", typeof(int));
                dtInsert.Columns.Add("CLIENTID");
                dtInsert.Columns.Add("SCHEMENAME");
                dtInsert.Columns.Add("MODUSER");
                dtInsert.Columns.Add("MODDATE", typeof(DateTime));
                dtInsert.Columns.Add("MODTYPE");
                dtInsert.Columns.Add("POSTDATE", typeof(DateTime));

                for (int i = 0; i < total; i++)
                {
                    dtInsert.Rows[i]["REMITTANCE_ID"] = remittanceID;
                    dtInsert.Rows[i]["DATAROWID_RECD"] = row++;

                    dtInsert.Rows[i]["CLIENTID"] = clientID;
                    dtInsert.Rows[i]["SCHEMENAME"] = schemeName;

                    dtInsert.Rows[i]["MODUSER"] = userName;
                    dtInsert.Rows[i]["MODDATE"] = Convert.ToDateTime(DateTime.Now.ToString("dd/MM/yyyy"));
                    dtInsert.Rows[i]["MODTYPE"] = "A";
                    dtInsert.Rows[i]["POSTDATE"] = Convert.ToDateTime(DateTime.Now.ToString("dd/MM/yyyy"));
                }
                dtInsert.AcceptChanges();
                newData.Dispose();
            }
        }

        public DataTable KeepDataTable(int row, string userName, string schemeName, string clientID, string remittanceID)
        {

            //PassDT will have already xml document path so I keep it empty here.
            PassDt(row, userName, schemeName, clientID, remittanceID, dtInsert, "");
            return dtInsert;


        }

    }
}
