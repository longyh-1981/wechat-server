using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Configuration;
using System.Threading;
using Newtonsoft.Json;

namespace WCRMServer.Proc
{
    public class CrmConfig
    {
        //public bool DbCharSetIsNotChinese = false;
        public bool ShowErrorDetail = false;
        public bool LoadBalance = false;
        public int OpenCardTypeId = 0;
        public int OpenCardStoreId = 0;
        public int CardGradeId = 0;
        public bool DbCharSetIsNotChinese = false;
    }

    public class ServerPlatform
    {
        private static Object sync = new Object();
        private static CrmConfig config = new CrmConfig();
        public static CrmConfig Config
        {
            get
            {
                InitiateData();
                return config;
            }
        }
        private static LogFile LogFileWriter = null;
        private static ErrorFileWriter ErrorLogFileWriter = null;
        private static Thread AutoBackProcThread = null;
        public static bool isInitiated = false;
        public static void InitiateData()
        {
            if (!isInitiated)
            {
                try
                {
                    string str = ConfigurationManager.AppSettings["open.card.type.id"];
                    if (str != null)
                    {
                        config.OpenCardTypeId = int.Parse(str);
                    }
                    str = ConfigurationManager.AppSettings["open.card.store.id"];
                    if (str != null)
                    {
                        config.OpenCardStoreId = int.Parse(str);
                    }
                    str = ConfigurationManager.AppSettings["card.grade.id"];
                    if (str != null)
                    {
                        config.CardGradeId = int.Parse(str);
                    }
                    str = ConfigurationManager.AppSettings["CrmDbCharSetIsNotChinese"];
                    config.DbCharSetIsNotChinese = ((str != null) && (str.ToLower().Equals("true")));
                    string logPath = ConfigurationManager.AppSettings["log.path"];
                    LogFileWriter = new LogFile(logPath, "wcrmlog");
                    //ErrorLogFileWriter = new ErrorFileWriter(logPath);
                    //if (!ErrorLogFileWriter.SetFileName("error.log"))
                    //{
                    //    for (int i = 1; i < 100; i++)
                    //    {
                    //        if (ErrorLogFileWriter.SetFileName(string.Format("error_{0}.log", i.ToString().PadLeft(2, '0'))))
                    //            break;
                    //    }
                    //}
                }
                catch (Exception e)
                {
                    ServerPlatform.WriteErrorLog("\r\n" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\r\n Error: " + e.Message.ToString());
                }

                BackProc backgroundProc = new BackProc();
                AutoBackProcThread = new Thread(new ThreadStart(backgroundProc.AutoBackProc));
                AutoBackProcThread.IsBackground = true;
                AutoBackProcThread.Start();

                isInitiated = true;
            }
        }

        public static void FinalizeData()
        {
            if (LogFileWriter != null)
            {
                LogFileWriter.Close();
            }
            if (ErrorLogFileWriter != null)
            {
                ErrorLogFileWriter.Close();
            }
        }

        public static void WriteLog(String dateStr, String logText)
        {
            if (LogFileWriter != null)
                LogFileWriter.Write(dateStr, logText);
        }

        public static void WriteErrorLog(String logText)
        {
            if (ErrorLogFileWriter != null)
                ErrorLogFileWriter.Write(logText);
        }

        public static string Json_ToShortDateString(object json)
        {
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            timeConverter.DateTimeFormat = "yyyy'-'MM'-'dd";
            return JsonConvert.SerializeObject(json, Formatting.Indented, timeConverter);
        }

        public static void ParseException(out string errorMsg, out string errorLog, out bool dbConnError, Exception e)
        {
            errorMsg = string.Empty;
            errorLog = string.Empty;
            dbConnError = false;
            if (e is MyDbException)
            {
                dbConnError = (e as MyDbException).IsConnError;
                if (dbConnError)
                    errorLog = "WCRM 数据库连接出错 " + e.Message;
                else
                {
                    errorLog = "WCRM 数据库操作出错 " + e.Message;
                    //if (CrmServerPlatform.Config.ShowErrorDetail && ((e as MyDbException).Sql.Length > 0)) longyh 2013-12-18
                    if ((e as MyDbException).Sql.Length > 0)
                        errorLog = errorLog + "\r\n " + (e as MyDbException).Sql;
                }
            }
            else
                errorLog = "WCRM 服务处理出错 " + e.Message;
            if (ServerPlatform.Config.ShowErrorDetail)
                errorMsg = errorLog;
            else
            {
                if (e is MyDbException)
                {
                    if ((e as MyDbException).IsConnError)
                        errorMsg = "WCRM 数据库连接出错";
                    else
                        errorMsg = "WCRM 数据库操作出错";
                }
                else
                    errorMsg = "WCRM 服务处理出错 ";
            }
        }

        public static bool getAppSecret(out string msg, out string appSecret, string appKey)
        {
            msg = string.Empty;
            appSecret = string.Empty;
            if (appKey == null)
            {
                msg = "APPID 不能为空";
                return false;
            }
            DbConnection conn = DbConnManager.GetDbConnection("MYDB");
            DbCommand cmd = conn.CreateCommand();
            StringBuilder sql = new StringBuilder();
            try
            {
                try
                {
                    conn.Open();
                }
                catch (Exception e)
                {
                    throw new MyDbException(e.Message, true);
                }
                try
                {
                    bool isFound = false;
                    //if ((Config.appKey != null) && (Config.appKey.Equals(appKey)))
                    //{
                    //    appSecret = Config.appSecret;
                    //    isFound = true;
                    //}
                    if (!isFound)
                    {
                        sql.Length = 0;
                        sql.Append("select APPSECRET from CRM_USER where APPID = ").Append(DbUtils.SpellSqlParameter(conn, "APPID"));
                        cmd.CommandText = sql.ToString();
                        DbUtils.AddStrInputParameterAndValue(cmd, 30, "APPID", appKey);
                        DbDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            appSecret = DbUtils.GetString(reader, 0);
                            isFound = true;
                        }
                        reader.Close();
                        cmd.Parameters.Clear();
                    }
                    if (!isFound)
                        msg = "APPID不存在";
                }
                catch (Exception e)
                {
                    if (e is MyDbException)
                        throw e;
                    else
                        throw new MyDbException(e.Message, cmd.CommandText);
                }
            }
            finally
            {
                conn.Close();
            }
            return (msg.Length == 0);
        }
    }
}
