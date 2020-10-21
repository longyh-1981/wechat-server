using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Text;
using System.Configuration;

namespace WCRMServer.Web
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            // 在应用程序启动时运行的代码
            WCRMServer.Proc.ServerPlatform.InitiateData();

            string str = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            WCRMServer.Proc.ServerPlatform.WriteLog(str.Substring(0, 10), "\r\n" + str + " WCRMServer.Interface Start \r\n");
            
        }

        protected void Application_End(object sender, EventArgs e)
        {
            //  在应用程序关闭时运行的代码
            string str = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            //string url = WCRMServer.Proc.ServerPlatform.Config.test_url;
            WCRMServer.Proc.ServerPlatform.WriteLog(str.Substring(0, 10), "\r\n" + str + " WCRMServer.Interface Stop \r\n");
            WCRMServer.Proc.ServerPlatform.FinalizeData();
            //System.Threading.Thread.Sleep(5000);
            //System.Net.HttpWebRequest _HttpWebRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            //System.Net.HttpWebResponse _HttpWebResponse = (System.Net.HttpWebResponse)_HttpWebRequest.GetResponse();
            //System.IO.Stream _Stream = _HttpWebResponse.GetResponseStream();//得到回写的字节流 
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        
    }
}