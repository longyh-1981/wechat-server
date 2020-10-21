using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.ProviderBase;
using System.Data.Common;
//using MySql.Data.MySqlClient;

namespace WCRMServer.Proc
{
    public class MyDbException : Exception
    {
        public bool IsConnError = false;
        public string Sql = string.Empty;
        public MyDbException(string message)
            : base(message)
        {

        }
        public MyDbException(string message, bool isConnError)
            : base(message)
        {
            IsConnError = isConnError;
        }
        public MyDbException(string message, string sql)
            : base(message)
        {
            Sql = sql;
        }
    }

    public class DbConnSettings
    {
        public string ConnName = string.Empty;
        public string ProviderName = string.Empty;
        public string ConnStr = string.Empty;
        public DbProviderFactory ProviderFactory = null;
    }

    public class DbConnManager
    {
        private static List<DbConnSettings> dbConnSettingsList = new List<DbConnSettings>();

        private static DbConnSettings GetDbConnSettings(string connName)
        {
            lock (dbConnSettingsList)
            {
                foreach (DbConnSettings mySettings in dbConnSettingsList)
                {
                    if (mySettings.ConnName.Equals(connName))
                    {
                        return mySettings;
                    }
                }
                ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[connName];
                if (settings == null)
                    throw new Exception("在配置文件中没找到名称为 " + connName + " 的数据库连接串配置");

                DbConnSettings mySettings2 = new DbConnSettings();
                dbConnSettingsList.Add(mySettings2);
                mySettings2.ConnName = connName;
                mySettings2.ProviderName = settings.ProviderName;
                mySettings2.ProviderFactory = DbProviderFactories.GetFactory(settings.ProviderName);
                mySettings2.ConnStr = settings.ConnectionString;// DecryptPasswordInDbConnStr(settings.ConnectionString);
                return mySettings2;
            }
        }
        private static string DecryptPasswordInDbConnStr(string connStr)
        {
            StringBuilder sb = new StringBuilder();
            string str = connStr.ToUpper();
            int inx1 = str.IndexOf("PASSWORD=");
            if (inx1 >= 0)
                inx1 += 9;
            else
            {
                inx1 = str.IndexOf("PWD=");
                if (inx1 >= 0)
                    inx1 += 4;
            }
            if (inx1 >= 0)
            {
                if ((inx1 < connStr.Length - 1) && (connStr[inx1] == '\''))
                    inx1++;
                int inx2 = str.IndexOf(";", inx1);
                string password = null;
                if (inx2 >= 0)
                {
                    if ((inx2 > 1) && (connStr[inx2 - 1] == '\''))
                    {
                        inx2--;
                    }
                    password = connStr.Substring(inx1, inx2 - inx1);
                }
                else
                {
                    if (connStr[connStr.Length - 1] == '\'')
                    {
                        inx2 = connStr.Length - 1;
                        password = connStr.Substring(inx1, inx2 - inx1);
                    }
                    else
                    {
                        password = connStr.Substring(inx1, connStr.Length - inx1);
                    }
                }
                password = PasswordEncryptUtils.PasswordDecrypt(password);

                sb.Append(connStr.Substring(0, inx1)).Append(password);
                if (inx2 >= 0)
                    sb.Append(connStr.Substring(inx2, connStr.Length - inx2));
            }
            return sb.ToString();
        }
        private static DbConnSettings GetDbConnSettings(string connName, string providerName, string connStr)
        {
            lock (dbConnSettingsList)
            {
                foreach (DbConnSettings mySettings in dbConnSettingsList)
                {
                    if (mySettings.ConnName.Equals(connName))
                    {
                        if (!mySettings.ProviderName.Equals(providerName))
                        {
                            mySettings.ProviderName = providerName;
                            mySettings.ProviderFactory = DbProviderFactories.GetFactory(providerName);
                        }
                        mySettings.ConnStr = connStr;
                        //mySettings.ConnStr = DecryptPasswordInDbConnStr(connStr);
                        return mySettings;
                    }
                }

                DbConnSettings mySettings2 = new DbConnSettings();
                dbConnSettingsList.Add(mySettings2);
                mySettings2.ConnName = connName;
                mySettings2.ProviderName = providerName;
                mySettings2.ProviderFactory = DbProviderFactories.GetFactory(providerName);
                mySettings2.ConnStr = connStr;
                //mySettings2.ConnStr = DecryptPasswordInDbConnStr(connStr);
                return mySettings2;
            }
        }
        /// <summary>
        /// 根据名字取数据库连接对象
        /// </summary>
        /// <param name="connName">连接名字</param>
        /// <returns>数据库连接对象DbConnection</returns>

        public static DbConnection GetDbConnection(string connName)
        {
            DbConnSettings settings = GetDbConnSettings(connName);
            DbConnection conn = settings.ProviderFactory.CreateConnection();
            conn.ConnectionString = settings.ConnStr;
            return conn;
        }
        public static DbConnection GetDbConnection(string connName, string providerName, string connStr)
        {
            DbConnSettings settings = GetDbConnSettings(connName, providerName, connStr);
            DbConnection conn = settings.ProviderFactory.CreateConnection();
            conn.ConnectionString = settings.ConnStr;
            return conn;
        }
        public static DbDataAdapter CreateDbDataAdapter(string connName)
        {
            DbConnSettings settings = GetDbConnSettings(connName);
            DbDataAdapter adapter = settings.ProviderFactory.CreateDataAdapter();
            return adapter;
        }
        public static DbDataAdapter CreateDbDataAdapter(string connName, string providerName, string connStr)
        {
            DbConnSettings settings = GetDbConnSettings(connName, providerName, connStr);
            DbDataAdapter adapter = settings.ProviderFactory.CreateDataAdapter();
            return adapter;
        }
    }

    public class DbUtils
    {
        public const string OracleDbSystemName = "ORACLE";
        public const string SybaseDbSystemName = "SYBASE";
        public const string DB2DbSystemName = "DB2";
        public const string MySQLDbSystemName = "MYSQL";
        //public const byte OracleDbSystem = 1;
        //public const byte SybaseDbSystem = 2;
        //public const byte DB2DbSystem = 3;

        private static Encoding gbkEncoding = Encoding.GetEncoding(936);

        public static string GetDbSystemName(DbConnection conn)
        {
            string str = conn.GetType().FullName.ToUpper();
            if (str.Contains("ASECONNECTION"))
                return SybaseDbSystemName;
            if (str.Contains(OracleDbSystemName))
                return OracleDbSystemName;
            if (str.Contains(MySQLDbSystemName))
                return MySQLDbSystemName;
            str = conn.ConnectionString.ToUpper();
            if (str.Contains(SybaseDbSystemName))
                return SybaseDbSystemName;
            else if (str.Contains(OracleDbSystemName))
                return OracleDbSystemName;
            else if (str.Contains(DB2DbSystemName))
                return DB2DbSystemName;
            else
                throw new Exception("Only support Syabse or Oracle or DB2");
        }
        public static DateTime GetDbServerTime(DbCommand cmd)
        {
            switch (GetDbSystemName(cmd.Connection))
            {
                case SybaseDbSystemName:
                    cmd.CommandText = "select getdate() ";
                    break;
                case OracleDbSystemName:
                    cmd.CommandText = "select sysdate from dual ";
                    break;
                case DB2DbSystemName:
                    cmd.CommandText = "select current timestamp from SYSIBM.SYSDUMMY1 ";
                    break;
                case MySQLDbSystemName:
                    cmd.CommandText = "select now() ";
                    break;
            }
            return (DateTime)cmd.ExecuteScalar();
        }
        public static string GetDbServerTimeStr(DbCommand cmd)
        {
            return FormatUtils.DatetimeToString(GetDbServerTime(cmd));
        }

        public static string GetDbServerTimeFuncSql(DbCommand cmd)
        {
            switch (GetDbSystemName(cmd.Connection))
            {
                case SybaseDbSystemName:
                    return " getdate() ";
                case OracleDbSystemName:
                    return " sysdate ";
                case DB2DbSystemName:
                    return " current timestamp ";
                case MySQLDbSystemName:
                    return " now() ";
            }
            return string.Empty;
        }

        public static string GetIsNullFuncName(string dbSysName)
        {
            switch (dbSysName)
            {
                case SybaseDbSystemName:
                    return "isnull";
                case OracleDbSystemName:
                    return "nvl";
                case DB2DbSystemName:
                    return "value";
                case MySQLDbSystemName:
                    return " IFNULL ";
            }
            return string.Empty;
        }

        public static string SpellSqlParameter(DbConnection conn, string paramName)
        {
            string str = conn.GetType().FullName.ToUpper();
            if (str.Contains("ASECONNECTION"))
                return "@" + paramName;
            else if (str.Contains(OracleDbSystemName))
                return ":" + paramName;
            else
                return "?" + paramName;
        }

        public static void SpellSqlParameter(DbConnection conn, StringBuilder sql, string prefix, string fieldName, string operationSymbol)
        {
            sql.Append(prefix);
            string str = conn.GetType().FullName.ToUpper();
            if (str.Contains("ASECONNECTION"))
            {
                if (operationSymbol.Length > 0)
                    sql.Append(fieldName).Append(operationSymbol).Append("@").Append(fieldName);
                else
                    sql.Append("@").Append(fieldName);
            }
            else if (str.Contains(OracleDbSystemName))
            {
                if (operationSymbol.Length > 0)
                    sql.Append(fieldName).Append(operationSymbol).Append(":").Append(fieldName);
                else
                    sql.Append(":").Append(fieldName);
            }
            else
            {
                if (operationSymbol.Length > 0)
                    sql.Append(fieldName).Append(operationSymbol).Append("?");
                else
                    sql.Append("?");
            }
        }
        public static void SpellSqlParameter(DbConnection conn, StringBuilder sql, string prefix, string fieldName, string operationSymbol, string paramName)
        {
            if (paramName.Length == 0)
                paramName = fieldName;
            sql.Append(prefix);
            string str = conn.GetType().FullName.ToUpper();
            if (str.Contains("ASECONNECTION"))
            {
                if (operationSymbol.Length > 0)
                    sql.Append(fieldName).Append(operationSymbol).Append("@").Append(paramName);
                else
                    sql.Append("@").Append(paramName);
            }
            else if (str.Contains(OracleDbSystemName))
            {
                if (operationSymbol.Length > 0)
                    sql.Append(fieldName).Append(operationSymbol).Append(":").Append(paramName);
                else
                    sql.Append(":").Append(paramName);
            }
            else
            {
                if (operationSymbol.Length > 0)
                    sql.Append(fieldName).Append(operationSymbol).Append("?");
                else
                    sql.Append("?");
            }
        }
        public static DbParameter AddParameter(DbCommand cmd, ParameterDirection paramDirection, DbType paramType, int paramSize, string paramName, Object paramValue)
        {
            DbParameter param = cmd.CreateParameter();
            param.Direction = paramDirection;
            param.DbType = paramType;
            param.Size = paramSize;
            if (cmd.Connection.GetType().FullName.ToUpper().Contains("ASECONNECTION"))
                param.ParameterName = "@" + paramName;
            else
                param.ParameterName = paramName;
            param.Value = paramValue;
            cmd.Parameters.Add(param);
            return param;
        }
        public static DbParameter AddIntInputParameter(DbCommand cmd, string paramName)
        {
            return AddParameter(cmd, ParameterDirection.Input, DbType.Int32, 0, paramName, null);
        }
        public static DbParameter AddIntInputParameterAndValue(DbCommand cmd, string paramName, string paramValue)
        {
            return AddParameter(cmd, ParameterDirection.Input, DbType.Int32, 0, paramName, paramValue);
        }
        public static DbParameter AddStrInputParameter(DbCommand cmd, int paramSize, string paramName)
        {
            return AddParameter(cmd, ParameterDirection.Input, DbType.String, paramSize, paramName, null);
        }
        public static DbParameter AddStrInputParameter(DbCommand cmd, int paramSize, string paramName,bool isChinese)
        {
            return AddParameter(cmd, ParameterDirection.Input, isChinese?DbType.Binary:DbType.String, paramSize, paramName, null);
        }
        public static DbParameter AddStrInputParameterAndValue(DbCommand cmd, int paramSize, string paramName, string paramValue)
        {
            return AddParameter(cmd, ParameterDirection.Input, DbType.String, paramSize, paramName, paramValue);
        }

        public static DbParameter AddStrInputParameterAndValue(DbCommand cmd, int paramSize, string paramName, string paramValue, bool isChinese)
        {
            if (isChinese)
            {
                DbParameter param = AddParameter(cmd, ParameterDirection.Input, DbType.Binary, paramSize, paramName, null);
                param.Value = Encoding.Convert(Encoding.Unicode, gbkEncoding, Encoding.Unicode.GetBytes(paramValue));
                return param;
            }
            else
            {
                return AddParameter(cmd, ParameterDirection.Input, DbType.String, paramSize, paramName, paramValue);
            }
        }

        public static DbParameter AddDoubleInputParameter(DbCommand cmd, string paramName)
        {
            return AddParameter(cmd, ParameterDirection.Input, DbType.Double, 0, paramName, null);
        }
        public static DbParameter AddDoubleInputParameterAndValue(DbCommand cmd, string paramName, string paramValue)
        {
            return AddParameter(cmd, ParameterDirection.Input, DbType.Double, 0, paramName, paramValue);
        }

        public static DbParameter AddDatetimeInputParameter(DbCommand cmd, string paramName)
        {
            return AddParameter(cmd, ParameterDirection.Input, DbType.DateTime, 0, paramName, null);
        }
        public static DbParameter AddDatetimeInputParameterAndValue(DbCommand cmd, string paramName, DateTime paramValue)
        {
            return AddParameter(cmd, ParameterDirection.Input, DbType.DateTime, 0, paramName, paramValue);
        }
        public static DbParameter AddBytesInputParameterAndValue(DbCommand cmd, string paramName, byte[] paramValue)
        {
            return AddParameter(cmd, ParameterDirection.Input, DbType.Binary, 0, paramName, paramValue);
        }

        public static int GetInt(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0;
            else
                return int.Parse(reader.GetValue(index).ToString());
        }
        public static double GetDouble(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return 0;
            else
                return double.Parse(reader.GetValue(index).ToString());
        }
        public static string GetString(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return string.Empty;
            else
                return reader.GetString(index);
        }
        public static string GetString(DbDataReader reader, int index,bool isChinese,int dataSize)
        {
            if (reader.IsDBNull(index))
                return string.Empty;
            else if (isChinese)
            {
                byte[] gbkBytes = new byte[dataSize];
                reader.GetBytes(index, 0, gbkBytes, 0, dataSize);
                if (gbkBytes[0] == 0)
                {
                    return string.Empty;
                }
                else
                {
                    int dataSize2 = dataSize;
                    for (int i = dataSize - 1; i >= 0; i--)
                    {
                        if (gbkBytes[i] != 0)
                        {
                            dataSize2 = i + 1;
                            break;
                        }
                    }
                    byte[] gbkBytes2 = new byte[dataSize2];
                    Array.Copy(gbkBytes, gbkBytes2, dataSize2);
                    return Encoding.Unicode.GetString(Encoding.Convert(gbkEncoding, Encoding.Unicode, gbkBytes2)).Trim();
                }
            }
            else
                return reader.GetString(index);
        }

        public static bool GetBool(DbDataReader reader, int index)
        {
            return (GetInt(reader,index) != 0);
        }
        public static DateTime GetDateTime(DbDataReader reader, int index)
        {
            if (reader.IsDBNull(index))
                return DateTime.MinValue;
            else
                return reader.GetDateTime(index);
        }
    }
}
