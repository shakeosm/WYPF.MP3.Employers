using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MCPhase3.CodeRepository
{
    public class BulkInsertToOra
    {
        /// <summary>
        /// in case windows bulk insert fails this routine will insert data direct to database.
        /// </summary>
        /// <param name="destTableName"></param>
        /// <param name="dt"></param>
        /// <param name="connectionString"></param>
        public void SaveUsingOracleBulkCopy(string destTableName, DataTable dt, string connectionString)
        {
            try
            {
                using (var connection = new Oracle.ManagedDataAccess.Client.OracleConnection(connectionString))
                {
                    connection.Open();
                    using (var bulkCopy = new Oracle.ManagedDataAccess.Client.OracleBulkCopy(connection, Oracle.ManagedDataAccess.Client.OracleBulkCopyOptions.UseInternalTransaction))
                    {
                        bulkCopy.DestinationTableName = destTableName;
                        bulkCopy.BulkCopyTimeout = 600;
                        bulkCopy.WriteToServer(dt);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
