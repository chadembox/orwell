using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;

namespace Orwell.Models
{

    public class DataAccessSql
    {
        public static string ConnectionString;

        private static DataSet RetrieveDataSet(string CommandText, CommandType CommandType, SqlParameterCollection Parameters)
        {
            try
            {
                DataSet dataSet = new DataSet();
                SqlConnection connection = DataAccessSql.GetConnection();
                connection.Open();
                SqlDataAdapter dataAdapter = DataAccessSql.GetDataAdapter(CommandText, connection);
                SqlCommand command = DataAccessSql.GetCommand();
                command.CommandText = CommandText;
                command.CommandType = CommandType;
                command.Connection = connection;
                dataAdapter.SelectCommand = command;
                if (Parameters != null)
                {
                    foreach (SqlParameter parameter in (DbParameterCollection)Parameters)
                        dataAdapter.SelectCommand.Parameters.Add(new SqlParameter(parameter.ParameterName, parameter.SqlDbType, parameter.Size, parameter.Direction, parameter.Precision, parameter.Scale, parameter.SourceColumn, parameter.SourceVersion, parameter.SourceColumnNullMapping, parameter.Value, parameter.XmlSchemaCollectionDatabase, parameter.XmlSchemaCollectionOwningSchema, parameter.XmlSchemaCollectionName));
                }
                dataAdapter.Fill(dataSet);
                connection.Close();
                return dataSet;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static SqlDataReader RetrieveDataReader(string CommandText, CommandType CommandType, SqlParameterCollection Parameters)
        {
            try
            {
                SqlConnection connection = DataAccessSql.GetConnection();
                connection.Open();
                SqlCommand command = DataAccessSql.GetCommand();
                command.CommandText = CommandText;
                command.CommandType = CommandType;
                command.Connection = connection;
                if (Parameters != null)
                {
                    foreach (SqlParameter parameter in (DbParameterCollection)Parameters)
                        command.Parameters.Add(new SqlParameter(parameter.ParameterName, parameter.SqlDbType, parameter.Size, parameter.Direction, parameter.Precision, parameter.Scale, parameter.SourceColumn, parameter.SourceVersion, parameter.SourceColumnNullMapping, parameter.Value, parameter.XmlSchemaCollectionDatabase, parameter.XmlSchemaCollectionOwningSchema, parameter.XmlSchemaCollectionName));
                }
                return command.ExecuteReader();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static int PrivateExecuteNonQuery(string CommandText, CommandType CommandType, SqlParameterCollection Parameters)
        {
            try
            {
                SqlConnection connection = DataAccessSql.GetConnection();
                connection.Open();
                SqlCommand command = DataAccessSql.GetCommand();
                command.CommandText = CommandText;
                command.CommandType = CommandType;
                command.Connection = connection;
                if (Parameters != null)
                {
                    foreach (SqlParameter parameter in (DbParameterCollection)Parameters)
                        command.Parameters.Add(new SqlParameter(parameter.ParameterName, parameter.SqlDbType, parameter.Size, parameter.Direction, parameter.Precision, parameter.Scale, parameter.SourceColumn, parameter.SourceVersion, parameter.SourceColumnNullMapping, parameter.Value, parameter.XmlSchemaCollectionDatabase, parameter.XmlSchemaCollectionOwningSchema, parameter.XmlSchemaCollectionName));
                }
                int num = command.ExecuteNonQuery();
                connection.Close();
                return num;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static object PrivateExecuteScalar(string CommandText, CommandType CommandType, SqlParameterCollection Parameters)
        {
            try
            {
                SqlConnection connection = DataAccessSql.GetConnection();
                connection.Open();
                SqlCommand command = DataAccessSql.GetCommand();
                command.CommandText = CommandText;
                command.CommandType = CommandType;
                command.Connection = connection;
                if (Parameters != null)
                {
                    foreach (SqlParameter parameter in (DbParameterCollection)Parameters)
                        command.Parameters.Add(new SqlParameter(parameter.ParameterName, parameter.SqlDbType, parameter.Size, parameter.Direction, parameter.Precision, parameter.Scale, parameter.SourceColumn, parameter.SourceVersion, parameter.SourceColumnNullMapping, parameter.Value, parameter.XmlSchemaCollectionDatabase, parameter.XmlSchemaCollectionOwningSchema, parameter.XmlSchemaCollectionName));
                }
                object obj = command.ExecuteScalar();
                connection.Close();
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static SqlDataAdapter GetDataAdapter(string SQL, SqlConnection Connection)
        {
            try
            {
                return new SqlDataAdapter(SQL, Connection);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static SqlCommand GetCommand()
        {
            try
            {
                return new SqlCommand();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private static SqlConnection GetConnection()
        {
            try
            {
                return new SqlConnection(DataAccessSql.ConnectionString);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static DataSet GetDataSet(string CommandText, CommandType CommandType)
        {
            try
            {
                return DataAccessSql.RetrieveDataSet(CommandText, CommandType, (SqlParameterCollection)null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static DataSet GetDataSet(string StoredProcName, SqlParameterCollection Parameters)
        {
            try
            {
                return DataAccessSql.RetrieveDataSet(StoredProcName, CommandType.StoredProcedure, Parameters);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static DataSet GetDataSet(string StoredProcName)
        {
            try
            {
                return DataAccessSql.RetrieveDataSet(StoredProcName, CommandType.StoredProcedure, (SqlParameterCollection)null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlDataReader GetDataReader(string CommandText, CommandType CommandType)
        {
            try
            {
                return DataAccessSql.RetrieveDataReader(CommandText, CommandType, (SqlParameterCollection)null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlDataReader GetDataReader(string StoredProcName, SqlParameterCollection Parameters)
        {
            try
            {
                return DataAccessSql.RetrieveDataReader(StoredProcName, CommandType.StoredProcedure, Parameters);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static SqlDataReader GetDataReader(string StoredProcName)
        {
            try
            {
                return DataAccessSql.RetrieveDataReader(StoredProcName, CommandType.StoredProcedure, (SqlParameterCollection)null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static int ExecuteNonQuery(string CommandText, CommandType CommandType)
        {
            try
            {
                return DataAccessSql.PrivateExecuteNonQuery(CommandText, CommandType, (SqlParameterCollection)null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static int ExecuteNonQuery(string StoredProcName, SqlParameterCollection Parameters)
        {
            try
            {
                return DataAccessSql.PrivateExecuteNonQuery(StoredProcName, CommandType.StoredProcedure, Parameters);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static int ExecuteNonQuery(string StoredProcName)
        {
            try
            {
                return DataAccessSql.PrivateExecuteNonQuery(StoredProcName, CommandType.StoredProcedure, (SqlParameterCollection)null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static object ExecuteScalar(string CommandText, CommandType CommandType)
        {
            try
            {
                return DataAccessSql.PrivateExecuteScalar(CommandText, CommandType, (SqlParameterCollection)null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static object ExecuteScalar(string StoredProcName, SqlParameterCollection Parameters)
        {
            try
            {
                return DataAccessSql.PrivateExecuteScalar(StoredProcName, CommandType.StoredProcedure, Parameters);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static object ExecuteScalar(string StoredProcName)
        {
            try
            {
                return DataAccessSql.PrivateExecuteScalar(StoredProcName, CommandType.StoredProcedure, (SqlParameterCollection)null);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static DataSet GetTableSchema(string TableName)
        {
            try
            {
                using (SqlConnection connection = DataAccessSql.GetConnection())
                {
                    DataSet dataSet = new DataSet();
                    string selectCommandText;
                    if (TableName.Contains("."))
                    {
                        string str = TableName.Split('.')[0];
                        TableName = TableName.Replace(str + ".", "");
                        selectCommandText = "SELECT TOP 1 * FROM " + str + ".[" + TableName + "]";
                    }
                    else
                        selectCommandText = "SELECT TOP 1 * FROM [" + TableName + "]";
                    new SqlDataAdapter(selectCommandText, connection).FillSchema(dataSet, SchemaType.Source, TableName);
                    return dataSet;
                }
            }
            catch
            {
                return (DataSet)null;
            }
        }

        public static string[] GetTableNamesFromDatabase()
        {
            SqlConnection connection = DataAccessSql.GetConnection();
            string selectCommandText = "SELECT sys.objects.name AS TableNameOnly, sys.schemas.name AS SchemaName, sys.schemas.name + '.' + sys.objects.name AS TableName FROM sys.objects INNER JOIN sys.schemas ON sys.objects.schema_id = sys.schemas.schema_id WHERE sys.objects.type = 'U'";
            DataSet dataSet1 = new DataSet();
            try
            {
                new SqlDataAdapter(selectCommandText, connection).Fill(dataSet1);
            }
            catch (Exception ex)
            {
                SqlDataAdapter sqlDataAdapter = new SqlDataAdapter("SELECT [name] AS TableName FROM sysobjects WHERE xtype='U' AND [name] <> 'dtproperties' ORDER BY [name]", connection);
                dataSet1 = new DataSet();
                DataSet dataSet2 = dataSet1;
                sqlDataAdapter.Fill(dataSet2);
            }
            if (dataSet1 == null || dataSet1.Tables.Count == 0)
                return (string[])null;
            int count = dataSet1.Tables[0].Rows.Count;
            string[] strArray = new string[count];
            for (int index = 0; index < count; ++index)
                strArray[index] = dataSet1.Tables[0].Rows[index]["TableName"].ToString();
            return strArray;
        }

        [DebuggerStepThrough]
        public static SqlParameterCollection GetParametersCollection()
        {
            return DataAccessSql.GetCommand().Parameters;
        }
    }
}