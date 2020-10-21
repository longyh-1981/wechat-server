using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using System.Threading;

namespace WCRMServer.Proc
{
    public  class SendHttpRequest
    {
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }

        //public static bool uploadDataToRemoteServer(out string msg, out string respJsonStr, string reqJsonStr, string appCode, string managedThreadId = "")
        //{
        //    msg = string.Empty;
        //    respJsonStr = string.Empty;
        //    string serviceUrl = ServerPlatform.Config.url;
        //    if ((serviceUrl == null) || (serviceUrl.Length == 0))
        //    {
        //        msg = "未设置url";
        //        return false;
        //    }
        //    DateTime timeBegin = DateTime.Now;
        //    serviceUrl = serviceUrl + appCode;
        //    //string tempString = string.Format("{0}&appId={1}&signKey={2}", reqJsonStr, ServerPlatform.Config.appId, ServerPlatform.Config.signKey + timeBegin.ToString("yyyyMMddHHmmss"));
        //    //string signStr = PasswordEncryptUtils.MD5Encrypt(tempString).ToLower();
        //    string errorLog = string.Empty;
        //    string respStr = string.Empty;
        //    //StringBuilder sb = new StringBuilder();
        //    try
        //    {
        //        HttpWebRequest req = null;
        //        if (serviceUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase))
        //        {
        //            req = WebRequest.Create(serviceUrl) as HttpWebRequest;
        //            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
        //            req.ProtocolVersion = HttpVersion.Version11;
        //            // 这里设置了协议类型。
        //            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;// SecurityProtocolType.Tls1.2; 
        //            //req.KeepAlive = false;
        //            req.KeepAlive = true;
        //            ServicePointManager.CheckCertificateRevocationList = true;
        //            ServicePointManager.DefaultConnectionLimit = 100;
        //            ServicePointManager.Expect100Continue = false;
        //        }
        //        else
        //        {
        //            req = (HttpWebRequest)WebRequest.Create(serviceUrl);
        //        }
        //        req.Method = "POST";
        //        req.Timeout = 60000;
        //        req.ContentType = "text/plain";//application/json;charset=utf8
        //        //sb.Append("data=" + reqJsonStr);
        //        //sb.Append("&signType=MD5");
        //        //sb.Append("&sign=" + signStr);
        //        byte[] reqBytes = Encoding.UTF8.GetBytes(reqJsonStr);
        //        Stream reqStream = req.GetRequestStream();
        //        reqStream.Write(reqBytes, 0, reqBytes.Length);
        //        WebResponse resp = req.GetResponse();
        //        Stream stream = resp.GetResponseStream();
        //        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        //        respStr = reader.ReadToEnd();
        //        respData myResp = JsonConvert.DeserializeObject<respData>(respStr);
        //        if (myResp != null)
        //        {
        //            if (myResp.code != 10000)
        //            {
        //                msg = myResp.code + "," + PubUtils.getError(myResp.code);
        //                errorLog = myResp.code + "," + PubUtils.getError(myResp.code);
        //            }
        //            else
        //            {
        //                if (myResp.data != null)
        //                    respJsonStr = myResp.data.ToString();
        //            }
        //        }
        //        else
        //        {
        //            msg = "传输出错";
        //            errorLog = respStr + "," + msg;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        errorLog = string.Format("访问服务失败 {0},{1}", serviceUrl, e.Message);
        //        msg = "访问会员卡系统失败";
        //    }
        //    DateTime timeEnd = DateTime.Now;
        //    StringBuilder logStr = new StringBuilder();
        //    logStr.Append("\r\n begin ").Append(appCode).Append(",").Append(timeBegin.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        //    logStr.Append("\r\n url:").Append(serviceUrl);
        //    logStr.Append("\r\n req:\r\n ").Append(reqJsonStr);
        //    logStr.Append("\r\n resp:\r\n ").Append(respStr);
        //    logStr.Append("\r\n managedThreadId:\r\n ").Append(managedThreadId);
        //    logStr.Append("\r\n end ").Append(timeEnd.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        //    logStr.Append(", ").Append(timeEnd.Subtract(timeBegin).TotalMilliseconds.ToString("f0")).Append(" ms");
        //    logStr.Append("\r\n");
        //    ServerPlatform.WriteLog(timeBegin.ToString("yyyy-MM-dd"), logStr.ToString());
        //    if (errorLog.Length > 0)
        //    {
        //        logStr.Length = 0;
        //        logStr.Append("\r\n ").Append(appCode).Append(",").Append(timeBegin.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        //        logStr.Append("\r\n").Append(reqJsonStr);
        //        logStr.Append("\r\n error ").Append(timeEnd.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        //        logStr.Append(", ").Append(timeEnd.Subtract(timeBegin).TotalMilliseconds.ToString("f0")).Append(" ms");
        //        logStr.Append("\r\n").Append(errorLog);
        //        ServerPlatform.WriteErrorLog(logStr.ToString());
        //    }
        //    return (msg.Length == 0);
        //}

        public static string SendHttpPostRequest(out string msg, string url, string date)
        {
            msg = "";
            try
            {
                WebRequest request = WebRequest.Create(url);

                request.Method = "POST";
                byte[] byteArray = Encoding.UTF8.GetBytes(date);
                request.ContentType = "application/x-gzip";
                request.ContentLength = byteArray.Length;

                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();

                WebResponse response = request.GetResponse();
                dataStream = response.GetResponseStream();

                StreamReader reader = new StreamReader(dataStream);

                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
                return responseFromServer.ToString();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                return "-1";
            }
        }
        public static string SendHttpGetRequest(out string msg, string url)
        {
            msg = "";
            try
            {
                WebRequest request = WebRequest.Create(url);
                request.Method = "GET";
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                reader.Close();
                dataStream.Close();
                response.Close();
                return responseFromServer.ToString();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
                return "-1";
            }
        }

    }
}
