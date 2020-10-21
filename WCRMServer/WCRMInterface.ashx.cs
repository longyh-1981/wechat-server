using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.IO;
using System.Data.Common;
using WCRMServer.Proc;

namespace WCRMServer.Web
{
    /// <summary>
    /// CRMInterface 的摘要说明
    /// </summary>
    public class WCRMInterface : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json; charset=utf-8";
            context.Request.ContentType = "application/json; charset=utf-8";
            context.Response.Buffer = true;
            context.Response.ExpiresAbsolute = System.DateTime.Now.AddDays(-1);
            context.Response.Expires = 0;
            //GET
            if (context.Request.HttpMethod == "GET")
            {
                #region GET
                context.Response.Write("<br> WCRMInterface.ashx 已可响应请求");
                context.Response.Write("<br>");
                return;
                #endregion
            }
            if (!context.Request.HttpMethod.Equals("POST"))
            {
                return;
            }

            DateTime timeBegin = DateTime.Now;
            string url = context.Request.Url.ToString();
            string method = context.Request.QueryString["method"];
            string src = context.Request.QueryString["src"];
            Stream reqStream = context.Request.InputStream;
            int reqSize = context.Request.ContentLength;
            byte[] reqBytes = new byte[reqSize];
            int totalReadNum = 0;
            while (totalReadNum < reqSize)
            {
                int readNum = reqStream.Read(reqBytes, totalReadNum, reqSize - totalReadNum);
                totalReadNum = totalReadNum + readNum;
            }
            string reqJsonStr = Encoding.UTF8.GetString(reqBytes);
            DateTime timeRead = DateTime.Now;
            String respJsonStr = string.Empty;
            string errorMsg = string.Empty;
            string errorLog = string.Empty;
            bool dbConnError = false;
            try
            {
                WebInterface.InterFace(out respJsonStr, reqJsonStr, src, method);
            }
            catch (Exception e)
            {
                ServerPlatform.ParseException(out errorMsg, out errorLog, out dbConnError, e);
            }
            if (dbConnError && ServerPlatform.Config.LoadBalance)
            {
                context.Response.StatusCode = 503;
                context.Response.StatusDescription = "数据库连接失败";
            }
            else
            {
                if (errorMsg.Length > 0)
                {
                    AppRespone err = new AppRespone();
                    err.Code = "1";
                    err.Message = errorMsg;
                    respJsonStr = ServerPlatform.Json_ToShortDateString(err).Replace("\r\n", "");
                }
                byte[] respBytes = System.Text.Encoding.UTF8.GetBytes(respJsonStr);
                Stream respStream = context.Response.OutputStream;
                respStream.Write(respBytes, 0, respBytes.Length);
                respStream.Flush();
            }
            DateTime timeEnd = DateTime.Now;
            StringBuilder logStr = new StringBuilder();
            logStr.Append("\r\n begin Log ").Append(timeBegin.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append(", ").Append(context.Request.UserHostAddress);
            logStr.Append("\r\n url:").Append(url);
            logStr.Append("\r\n Request:");
            logStr.Append("\r\n").Append(reqJsonStr);
            logStr.Append("\r\n Response:");
            logStr.Append("\r\n").Append(respJsonStr);
            if (errorLog.Length > 0)
            {
                logStr.Append("\r\n error detail:\r\n ").Append(errorLog);
            }
            logStr.Append("\r\n end Log ").Append(timeEnd.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            logStr.Append(", ").Append(timeRead.Subtract(timeBegin).TotalMilliseconds.ToString("f0"));
            logStr.Append(", ").Append(timeEnd.Subtract(timeBegin).TotalMilliseconds.ToString("f0")).Append(" ms");
            logStr.Append("\r\n");
            ServerPlatform.WriteLog(timeBegin.ToString("yyyy-MM-dd"), logStr.ToString());
            //if (errorLog.Length > 0)
            //{
            //    logStr.Length = 0;
            //    logStr.Append("\r\n request ").Append(timeBegin.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append(", ").Append(context.Request.UserHostAddress);
            //    logStr.Append("\r\n").Append(reqJsonStr);
            //    logStr.Append("\r\n error ").Append(timeEnd.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            //    logStr.Append(", ").Append(timeRead.Subtract(timeBegin).TotalMilliseconds.ToString("f0"));
            //    logStr.Append(", ").Append(timeEnd.Subtract(timeBegin).TotalMilliseconds.ToString("f0")).Append(" ms");
            //    logStr.Append("\r\n").Append(errorLog);
            //    ServerPlatform.WriteErrorLog(logStr.ToString());
            //}
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}