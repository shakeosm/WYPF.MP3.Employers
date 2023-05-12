using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository.HelperClasses
{
    public class DTColumnDataChecks
    {
        // ErrorChecks errorMessage = new ErrorChecks();
        /// <summary>
        /// This function will check datatypes of all column in datatable
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="columnDefinations"></param>
        /// <returns></returns>
        public DataTable AllColumnExistInExSheetCheck(DataTable dataTable, List<ColumnDefinations> columnDefinations)
        {
            DataColumn colName = new DataColumn();
            DataTable newDt = new DataTable();
            Type type = null;

            //columnDefinations = new List<ColumnDefination>();
            bool result = true;

            try
            {
                for (int i = 0; i < columnDefinations.Count; i++)
                {
                    if (result == true)
                    {
                        colName = dataTable.Columns[i];
                        type = dataTable.Columns[i].DataType;

                        foreach (var name in columnDefinations)
                        {
                            if (colName.ColumnName == name.fileColumnName && type.ToString() == name.columnDataType)
                            {
                                result = true;
                                newDt.Columns.Add(name.dBColumnName);
                                newDt.Columns[name.dBColumnName].DataType = System.Type.GetType(name.columnDataType);
                                colName.ColumnName = name.dBColumnName;
                                break;
                            }
                            else if (colName.ColumnName == name.fileColumnName && type.ToString() != name.columnDataType)
                            {
                                newDt.Columns.Add(name.dBColumnName);
                                //assign a datatype to datatable
                                newDt.Columns[name.dBColumnName].DataType = System.Type.GetType(name.columnDataType);
                                colName.ColumnName = name.dBColumnName;

                                // dataTable.Columns[name.dBColumnName].DataType = System.Type.GetType(name.columnDataType);
                                result = true;
                                break;
                            }
                            else
                            {
                                result = false;
                                //    errorMessage.ErrorsCheck("\n Column name is changed at column: " + i);
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }


                // datatabe is checked and all the column datatypes are set and then datacolumn columns are customized according to 
                //data base table columns
                return AddDataToNewDT(newDt, dataTable);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// This function copies all the data rows from datatable to datacolumn where datacolumn has already same column names with
        /// new datatypes with according to xml file.
        /// </summary>
        /// <param name="dataColumn"> empty datacolumn but I assigned it dbColumn and datatypes according to xml 
        /// now need to add data from datatable into dataColumn</param>
        /// <param name="dataTable">existing datatable but the calling function already changed its column names to dbColumn names 
        /// as specified in xml</param>
        /// <returns></returns>
        public DataTable AddDataToNewDT(DataTable dataColumn, DataTable dataTable)
        {
            object valueForList = null;
            foreach (DataRow rw in dataTable.Rows)
            {
                foreach (DataColumn col in dataColumn.Columns)
                {
                    Type type = col.DataType;
                    string val = rw[col.ColumnName].ToString();
                    if (string.IsNullOrEmpty(val) || string.IsNullOrWhiteSpace(val))
                    {
                        if (col.DataType.FullName.Equals("System.Double"))
                        {
                            rw[col.ColumnName] = null;
                        }
                        if (col.DataType.FullName.Equals("System.DateTime"))
                        {
                            rw[col.ColumnName] = null;
                        }                        
                    }
                    else if (col.DataType.FullName.Equals("System.DateTime"))
                    {
                        if (!val.Equals(""))
                        {
                            rw[col.ColumnName] = Convert.ToDateTime(val); //Convert.ToDateTime(val, "dd/MM/yyyy");
                        }

                    }
                }                   
                

                dataColumn.ImportRow(rw);
            }

            return dataColumn;
        }
        /// <summary>
        /// This function checks datatable col column name against the xml and if column exists in datatable col
        /// then it assigns Oracle datatype to dbType.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="columnDefinations"></param>
        /// <returns></returns>
        public OracleDbType ColumnDataTypeCheck(DataColumn col, List<ColumnDefinations> columnDefinations)
        {
            OracleDbType dbType = new OracleDbType();
            bool result = false;

            for (int i = 0; i < columnDefinations.Count; i++)
            {
                if (result == true) break;

                foreach (var name in columnDefinations)
                {
                    if (result == true) break;

                    if (col.ToString() == name.dBColumnName)
                    {

                        switch (name.columnDataType)
                        {
                            case "System.Double":
                                dbType = OracleDbType.Double;
                                result = true;
                                break;

                            case "System.DateTime":
                                dbType = OracleDbType.Date;
                                result = true;
                                break;

                            case "System.String":
                                dbType = OracleDbType.Varchar2;
                                result = true;
                                break;
                        }
                    }
                }
            }
            return dbType;
        }
    }
}
