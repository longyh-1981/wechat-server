using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Web;
using System.Threading;
using Newtonsoft.Json;

namespace WCRMServer.Proc
{
    public class BackProc
    {
        public void AutoBackProc()
        {
            while (true)
            {
                string msg = string.Empty;
                try
                {
                    DateTime time1 = DateTime.Now;
                    //if ((ServerPlatform.Config.AESKey == null) || (ServerPlatform.Config.AESKey.Length == 0))
                    //{
                    //    msg = "aes_key未设置";
                    //}
                    #region 传输
                    if (msg.Length == 0)
                    {
                        DateTime timeBegin = DateTime.MinValue;
                        DateTime timeEnd = DateTime.MinValue;
                        StringBuilder logStr = new StringBuilder();
                        //int uploadNumber = 0;

                        //DbConnection conn = null;
                        //DbCommand cmd = null;
                        //try
                        //{
                        //    conn = DbConnManager.GetDbConnection("MYDB");
                        //    try
                        //    {
                        //        conn.Open();
                        //    }
                        //    catch (Exception e)
                        //    {
                        //        throw new MyDbException(e.Message, true);
                        //    }
                        //    try
                        //    {
                        //        cmd = conn.CreateCommand();
                        //        GetVipCard(cmd, "0000000002");
                        //    }
                        //    catch (Exception e)
                        //    {
                        //        if (cmd == null)
                        //            ServerPlatform.WriteErrorLog("\r\n UploadDetpInfo error: " + e.Message);
                        //        else
                        //            ServerPlatform.WriteErrorLog("\r\n UploadDetpInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
                        //    }
                        //}
                        //finally
                        //{
                        //    if (conn != null) conn.Close();
                        //}
                        
                        //timeBegin = DateTime.Now;
                        //logStr.Append("\r\n开始上传非会员非使用优惠券订单信息,").Append(timeBegin.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        //UploadNonMemberTradeInfo(out uploadNumber);/
                        //timeEnd = DateTime.Now;
                        //logStr.Append("\r\n上传非会员非使用优惠券订单信息结束，共传" + uploadNumber.ToString() + "条,").Append(timeEnd.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                        //logStr.Append(", ").Append(timeEnd.Subtract(timeBegin).TotalMilliseconds.ToString("f0")).Append(" ms");
                        //ServerPlatform.WriteLog(timeBegin.ToString("yyyy-MM-dd"), logStr.ToString());
                    }
                    #endregion
                    DateTime time2 = DateTime.Now;
                    int interval = 30000 - PubUtils.Truncate(time2.Subtract(time1).TotalMilliseconds);//180000
                    if (interval < 1000)
                        interval = 1000;
                    Thread.Sleep(interval);   //3分钟后再来,2019.11.18 从3分钟调整为30秒
                }
                catch (Exception e)
                {
                    msg = e.Message;
                    ServerPlatform.WriteErrorLog("\r\n AutoBackProc error: " + msg);
                }
            }
        }

        private int GetVipCard(DbCommand cmd, string CardCode)
        {
            int tm = 0;
            StringBuilder sql = new StringBuilder();
            sql.Length = 0;
            sql.Append("select VipID,CardNo,OptId,OptName from cardinfo where CardNo = ").Append(DbUtils.SpellSqlParameter(cmd.Connection, "CardNo"));
            cmd.CommandText = sql.ToString();
            DbUtils.AddStrInputParameterAndValue(cmd, 30, "CardNo", CardCode);
            DbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                tm = DbUtils.GetInt(reader, 0);
                string code = DbUtils.GetString(reader, 1);
                string name = DbUtils.GetString(reader, 3);
            }
            reader.Close();
            cmd.Parameters.Clear();
            return tm;
        }

        //private void UpdateTblTM(DbCommand cmd, string tblName, int tm)
        //{
        //    DbTransaction dbTrans = cmd.Connection.BeginTransaction();
        //    try
        //    {
        //        cmd.Transaction = dbTrans;
        //        StringBuilder sql = new StringBuilder();
        //        sql.Length = 0;
        //        sql.Append("update TRANS_TABLE set TM = ").Append(tm).Append(" where TBLNAME = ").Append(DbUtils.SpellSqlParameter(cmd.Connection, "TBLNAME"));
        //        cmd.CommandText = sql.ToString();
        //        DbUtils.AddStrInputParameterAndValue(cmd, 30, "TBLNAME", tblName);
        //        if (cmd.ExecuteNonQuery() == 0)
        //        {
        //            cmd.Parameters.Clear();
        //            sql.Length = 0;
        //            sql.Append("insert into TRANS_TABLE(TBLNAME,TM) ");
        //            sql.Append(" values(").Append(DbUtils.SpellSqlParameter(cmd.Connection, "TBLNAME"));
        //            sql.Append(",").Append(tm);
        //            sql.Append(")");
        //            cmd.CommandText = sql.ToString();
        //            DbUtils.AddStrInputParameterAndValue(cmd, 30, "TBLNAME", tblName);
        //            cmd.ExecuteNonQuery();
        //        }
        //        cmd.Parameters.Clear();

        //        dbTrans.Commit();
        //    }
        //    catch (Exception e)
        //    {
        //        dbTrans.Rollback();
        //        throw e;
        //    }
        //}

        //public void UploadDeptInfo(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            TDeptInfo detpInfo = new TDeptInfo();
        //            int tm = GetTblTM(cmd, "SHBM_CRM");//取上次传的最大TM值
        //            int lastTm = 0;
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select BMDM,BMMC,BMJB,PARENT_BMDM,TM from");
        //            sql.Append(" (select BMDM,BMMC,BMJB,PARENT_BMDM,TM  ");
        //            sql.Append(" from SHBM_CRM  ");
        //            sql.Append(" where TM > ").Append(tm);
        //            sql.Append(" order by TM) A where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TDeptItemInfo item = new TDeptItemInfo();
        //                detpInfo.orgList.Add(item);
        //                item.orgID = DbUtils.GetString(reader, 0);
        //                item.orgName = DbUtils.GetString(reader, 1, ServerPlatform.Config.DbCharSetIsNotChinese, 100).Trim();
        //                item.currentLevelID = DbUtils.GetInt(reader, 2);
        //                item.parentOrgID = DbUtils.GetString(reader, 3);
        //                lastTm = DbUtils.GetInt(reader, 4);
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();

        //            if (detpInfo.orgList.Count > 0)
        //            {
        //                reqData req = new reqData();
        //                req.data = detpInfo;
        //                string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                req.sign = PubUtils.GetSign(timestamp);// "3FFCA3B5D096BC3DBB5B85BFC52F87E4";//签名
        //                string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                string appCode = "/api/cy_erp/send_org_infor";
        //                string respJsonStr = string.Empty;
        //                bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode);
        //                if (ok)
        //                {
        //                    uploadNumber += detpInfo.orgList.Count;
        //                    UpdateTblTM(cmd, "SHBM_CRM", lastTm);
        //                }
        //                else
        //                {
        //                    ServerPlatform.WriteErrorLog("\r\n UploadDetpInfo error: " + msg);
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadDetpInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadDetpInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadCategoryInfo(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            TCategoryInfo categoryInfo = new TCategoryInfo();
        //            int tm = GetTblTM(cmd, "SHSPFL_CRM");//取上次传的最大TM值
        //            int lastTm = 0;
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select SPFLDM,SPFLMC,FLJB,PARENT_SPFLDM,TM from");
        //            sql.Append(" (select SPFLDM,SPFLMC,FLJB,PARENT_SPFLDM,TM  ");
        //            sql.Append(" from SHSPFL_CRM  ");
        //            sql.Append(" where TM > ").Append(tm);
        //            sql.Append(" order by TM) A where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TCategoryItemInfo item = new TCategoryItemInfo();
        //                categoryInfo.productClassifyList.Add(item);
        //                item.classifyID = DbUtils.GetString(reader, 0);
        //                item.classifyName = DbUtils.GetString(reader, 1, ServerPlatform.Config.DbCharSetIsNotChinese, 30).Trim();
        //                item.currentLevelID = DbUtils.GetInt(reader, 2);
        //                item.parentID = DbUtils.GetString(reader, 3);
        //                lastTm = DbUtils.GetInt(reader, 4);
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();

        //            if (categoryInfo.productClassifyList.Count > 0)
        //            {
        //                reqData req = new reqData();
        //                req.data = categoryInfo;
        //                string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                req.sign = PubUtils.GetSign(timestamp);// "3FFCA3B5D096BC3DBB5B85BFC52F87E4";//签名
        //                string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                string appCode = "/api/cy_erp/send_product_classify_infor";
        //                string respJsonStr = string.Empty;
        //                bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode);
        //                if (ok)
        //                {
        //                    uploadNumber += categoryInfo.productClassifyList.Count;
        //                    UpdateTblTM(cmd, "SHSPFL_CRM", lastTm);
        //                }
        //                else
        //                {
        //                    ServerPlatform.WriteErrorLog("\r\n UploadCategoryInfo error: " + msg);
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadCategoryInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadCategoryInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadBrandInfo(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            TBrandInfo brandInfo = new TBrandInfo();
        //            int tm = GetTblTM(cmd, "SHSPSB_CRM");//取上次传的最大TM值
        //            int lastTm = 0;
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select SBDM,SBMC,TM from");
        //            sql.Append(" (select SBDM,SBMC,TM  ");
        //            sql.Append(" from SHSPSB_CRM  ");
        //            sql.Append(" where TM > ").Append(tm);
        //            sql.Append(" order by TM) A where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TBrandItemInfo item = new TBrandItemInfo();
        //                brandInfo.productBrandList.Add(item);
        //                item.brandID = DbUtils.GetString(reader, 0);
        //                item.brandName = DbUtils.GetString(reader, 1, ServerPlatform.Config.DbCharSetIsNotChinese, 50).Trim();
        //                lastTm = DbUtils.GetInt(reader, 2);
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();

        //            if (brandInfo.productBrandList.Count > 0)
        //            {
        //                reqData req = new reqData();
        //                req.data = brandInfo;
        //                string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                req.sign = PubUtils.GetSign(timestamp);// "3FFCA3B5D096BC3DBB5B85BFC52F87E4";//签名
        //                string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                string appCode = "/api/cy_erp/send_product_brand_infor";
        //                string respJsonStr = string.Empty;
        //                bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode);
        //                if (ok)
        //                {
        //                    uploadNumber += brandInfo.productBrandList.Count;
        //                    UpdateTblTM(cmd, "SHSPSB_CRM", lastTm);
        //                }
        //                else
        //                {
        //                    ServerPlatform.WriteErrorLog("\r\n UploadBrandInfo error: " + msg);
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadBrandInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadBrandInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadArticleInfo(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            TArticleInfo articleInfo = new TArticleInfo();
        //            int tm = GetTblTM(cmd, "SHSPXX_CRM");//取上次传的最大TM值
        //            int lastTm = 0;
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select SPFLDM,SBDM,SPDM,SPMC,STATUS,SPDJ,TM,JXSL,TAXCODE from");
        //            sql.Append(" (select SPFLDM,SBDM,SPDM,SPMC,STATUS,SPDJ,TM,JXSL,TAXCODE  ");
        //            sql.Append(" from SHSPXX_CRM  ");
        //            sql.Append(" where TM > ").Append(tm);
        //            sql.Append(" order by TM) A where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TArticleItemInfo item = new TArticleItemInfo();
        //                articleInfo.productList.Add(item);
        //                item.productClassifyID = DbUtils.GetString(reader, 0);
        //                item.productBrandID = DbUtils.GetString(reader, 1);
        //                item.productID = DbUtils.GetString(reader, 2);
        //                item.productName = DbUtils.GetString(reader, 3, ServerPlatform.Config.DbCharSetIsNotChinese, 70).Trim();
        //                item.productStatus = DbUtils.GetInt(reader, 4);
        //                item.productCharge = DbUtils.GetDouble(reader, 5).ToString("f2");
        //                lastTm = DbUtils.GetInt(reader, 6);
        //                item.taxRate = DbUtils.GetDouble(reader, 7).ToString("f2");
        //                item.taxCode = DbUtils.GetString(reader, 8);
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();

        //            if (articleInfo.productList.Count > 0)
        //            {
        //                reqData req = new reqData();
        //                req.data = articleInfo;
        //                string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                req.sign = PubUtils.GetSign(timestamp);// "3FFCA3B5D096BC3DBB5B85BFC52F87E4";//签名
        //                string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                string appCode = "/api/cy_erp/send_product_infor";
        //                string respJsonStr = string.Empty;
        //                bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode);
        //                if (ok)
        //                {
        //                    uploadNumber += articleInfo.productList.Count;
        //                    UpdateTblTM(cmd, "SHSPXX_CRM", lastTm);
        //                }
        //                else
        //                {
        //                    ServerPlatform.WriteErrorLog("\r\n UploadArticleInfo error: " + msg);
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadArticleInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadArticleInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadDeptArticleInfo1(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            TArticleInfo articleInfo = new TArticleInfo();
        //            int tm = GetTblTM(cmd, "BMSP_BH");//取上次传的最大TM值
        //            int lastTm = 0;
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select SPFLDM,SBDM,SPDM,SPMC,STATUS,SPDJ,TM,BMDM from");
        //            sql.Append(" (select b.SPFLDM,b.SBDM,b.SPDM,b.SPMC,b.STATUS,b.SPDJ,a.TM,a.BMDM  ");
        //            sql.Append(" from BMSP_BH a,SHSPXX_CRM b  ");
        //            sql.Append(" where  a.SPDM = b.SPDM and a.TM > ").Append(tm);
        //            sql.Append(" order by a.TM) tbl where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TArticleItemInfo item = new TArticleItemInfo();
        //                articleInfo.productList.Add(item);
        //                item.productClassifyID = DbUtils.GetString(reader, 0);
        //                item.productBrandID = DbUtils.GetString(reader, 1);
        //                item.productID = DbUtils.GetString(reader, 2);
        //                item.productName = DbUtils.GetString(reader, 3, ServerPlatform.Config.DbCharSetIsNotChinese, 70).Trim();
        //                item.productStatus = DbUtils.GetInt(reader, 4);
        //                item.productCharge = DbUtils.GetDouble(reader, 5).ToString("f2");
        //                lastTm = DbUtils.GetInt(reader, 6);
        //                item.orgID = DbUtils.GetString(reader, 7);
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();

        //            if (articleInfo.productList.Count > 0)
        //            {
        //                reqData req = new reqData();
        //                req.data = articleInfo;
        //                string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                req.sign = PubUtils.GetSign(timestamp);// "3FFCA3B5D096BC3DBB5B85BFC52F87E4";//签名
        //                string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                string appCode = "/api/cy_erp/send_product_infor";
        //                string respJsonStr = string.Empty;
        //                bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode);
        //                if (ok)
        //                {
        //                    uploadNumber += articleInfo.productList.Count;
        //                    UpdateTblTM(cmd, "BMSP_BH", lastTm);
        //                }
        //                else
        //                {
        //                    ServerPlatform.WriteErrorLog("\r\n UploadArticleInfo error: " + msg);
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadArticleInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadArticleInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadDeptArticleInfo2(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            TArticleInfo articleInfo = new TArticleInfo();
        //            int tm = GetTblTM(cmd, "BMSP_CS");//取上次传的最大TM值
        //            int lastTm = 0;
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select SPFLDM,SBDM,SPDM,SPMC,STATUS,SPDJ,TM,BMDM from");
        //            sql.Append(" (select b.SPFLDM,b.SBDM,b.SPDM,b.SPMC,b.STATUS,b.SPDJ,a.TM,a.BMDM  ");
        //            sql.Append(" from BMSP_CS a,SHSPXX_CRM b  ");
        //            sql.Append(" where  a.SPDM = b.SPDM and a.TM > ").Append(tm);
        //            sql.Append(" order by a.TM) tbl where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TArticleItemInfo item = new TArticleItemInfo();
        //                articleInfo.productList.Add(item);
        //                item.productClassifyID = DbUtils.GetString(reader, 0);
        //                item.productBrandID = DbUtils.GetString(reader, 1);
        //                item.productID = DbUtils.GetString(reader, 2);
        //                item.productName = DbUtils.GetString(reader, 3, ServerPlatform.Config.DbCharSetIsNotChinese, 70).Trim();
        //                item.productStatus = DbUtils.GetInt(reader, 4);
        //                item.productCharge = DbUtils.GetDouble(reader, 5).ToString("f2");
        //                lastTm = DbUtils.GetInt(reader, 6);
        //                item.orgID = DbUtils.GetString(reader, 7);
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();

        //            if (articleInfo.productList.Count > 0)
        //            {
        //                reqData req = new reqData();
        //                req.data = articleInfo;
        //                string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                req.sign = PubUtils.GetSign(timestamp);// "3FFCA3B5D096BC3DBB5B85BFC52F87E4";//签名
        //                string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                string appCode = "/api/cy_erp/send_product_infor";
        //                string respJsonStr = string.Empty;
        //                bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode);
        //                if (ok)
        //                {
        //                    uploadNumber += articleInfo.productList.Count;
        //                    UpdateTblTM(cmd, "BMSP_CS", lastTm);
        //                }
        //                else
        //                {
        //                    ServerPlatform.WriteErrorLog("\r\n UploadArticleInfo error: " + msg);
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadArticleInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadArticleInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadPaymentInfo(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            TPaymentInfo paymentInfo = new TPaymentInfo();
        //            int tm = GetTblTM(cmd, "SHZFFS_CRM");//取上次传的最大TM值
        //            int lastTm = 0;
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select ZFFSDM,ZFFSMC,ZFFSJB,PARENT_ZFFSDM,TM from");
        //            sql.Append(" (select ZFFSDM,ZFFSMC,ZFFSJB,PARENT_ZFFSDM,TM  ");
        //            sql.Append(" from SHZFFS_CRM  ");
        //            sql.Append(" where TM > ").Append(tm);
        //            sql.Append(" order by TM) A where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TPaymentItemInfo item = new TPaymentItemInfo();
        //                paymentInfo.payInforList.Add(item);
        //                item.payID = DbUtils.GetString(reader, 0);
        //                item.payName = DbUtils.GetString(reader, 1, ServerPlatform.Config.DbCharSetIsNotChinese, 30).Trim();
        //                item.currentLevel = DbUtils.GetInt(reader, 2);
        //                item.parentPayId = DbUtils.GetString(reader, 3);
        //                lastTm = DbUtils.GetInt(reader, 4);
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();

        //            if (paymentInfo.payInforList.Count > 0)
        //            {
        //                reqData req = new reqData();
        //                req.data = paymentInfo;
        //                string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                req.sign = PubUtils.GetSign(timestamp);// "3FFCA3B5D096BC3DBB5B85BFC52F87E4";//签名
        //                string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                string appCode = "/api/cy_erp/send_pay_infor";
        //                string respJsonStr = string.Empty;
        //                bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode);
        //                if (ok)
        //                {
        //                    uploadNumber += paymentInfo.payInforList.Count;
        //                    UpdateTblTM(cmd, "SHZFFS_CRM", lastTm);
        //                }
        //                else
        //                {
        //                    ServerPlatform.WriteErrorLog("\r\n UploadBrandInfo error: " + msg);
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadPaymentInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadPaymentInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadTradeInfo(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            //int tm = GetTblTM(cmd, "SHZFFS_CRM");//取上次传的最大TM值
        //            List<int> Ids = new List<int>();
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select XFJLID,INTIME from");
        //            sql.Append(" (select XFJLID,INTIME  ");
        //            sql.Append(" from HYK_XFJL_XFJLID  ");
        //            sql.Append(" order by INTIME) A where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                Ids.Add(DbUtils.GetInt(reader, 0));
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();
        //            if (Ids.Count > 0)
        //            {
        //                foreach (int serverBillId in Ids)
        //                {
        //                    TUploadTradeInfo tradeInfo = new TUploadTradeInfo();
        //                    bool fromBack = false;
        //                    bool isFound = false;
        //                    int originalServerBillId = 0;
        //                    #region 查实时表
        //                    sql.Length = 0;
        //                    sql.Append("select b.MDDM,b.MDMC,c.MEMBER_ID,a.SKTNO,a.JLBH,a.XFJLID_OLD,a.XFSJ,a.JZRQ,a.CZJE,a.JE  ");
        //                    sql.Append(" from HYK_XFJL a,MDDY b,HYK_HYXX c");
        //                    sql.Append(" where a.MDID = b.MDID and a.HYID = c.HYID(+) ");
        //                    sql.Append(" and a.XFJLID = ").Append(serverBillId);
        //                    cmd.CommandText = sql.ToString();
        //                    reader = cmd.ExecuteReader();
        //                    if (reader.Read())
        //                    {
        //                        isFound = true;
        //                        tradeInfo.storeCode = DbUtils.GetString(reader, 0);
        //                        tradeInfo.storeName = DbUtils.GetString(reader, 1);
        //                        tradeInfo.memberID = DbUtils.GetString(reader, 2);
        //                        tradeInfo.machineID = DbUtils.GetString(reader, 3);
        //                        tradeInfo.orderID = DbUtils.GetInt(reader, 4).ToString();
        //                        originalServerBillId = DbUtils.GetInt(reader, 5);
        //                        tradeInfo.orderOccurTime = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 6));
        //                        tradeInfo.orderBelongTime = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 7));
        //                        tradeInfo.cardCharge = DbUtils.GetDouble(reader, 8);
        //                        if (DbUtils.GetDouble(reader, 9) < 0)
        //                            tradeInfo.type = 2;
        //                    }
        //                    reader.Close();
        //                    #endregion

        //                    if (!isFound)
        //                    {
        //                        #region 查历史表
        //                        sql.Length = 0;
        //                        sql.Append("select b.MDDM,b.MDMC,c.MEMBER_ID,a.SKTNO,a.JLBH,a.XFJLID_OLD,a.XFSJ,a.JZRQ,a.CZJE,a.JE  ");
        //                        sql.Append(" from HYXFJL a,MDDY b,HYK_HYXX c");
        //                        sql.Append(" where a.MDID = b.MDID and a.HYID = c.HYID(+) ");
        //                        sql.Append(" and a.XFJLID = ").Append(serverBillId);
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        if (reader.Read())
        //                        {
        //                            isFound = true;
        //                            fromBack = true;
        //                            tradeInfo.storeCode = DbUtils.GetString(reader, 0);
        //                            tradeInfo.storeName = DbUtils.GetString(reader, 1);
        //                            tradeInfo.memberID = DbUtils.GetString(reader, 2);
        //                            tradeInfo.machineID = DbUtils.GetString(reader, 3);
        //                            tradeInfo.orderID = DbUtils.GetInt(reader, 4).ToString();
        //                            originalServerBillId = DbUtils.GetInt(reader, 5);
        //                            tradeInfo.orderOccurTime = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 6));
        //                            tradeInfo.orderBelongTime = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 7));
        //                            tradeInfo.cardCharge = DbUtils.GetDouble(reader, 8);
        //                            if (DbUtils.GetDouble(reader, 9) < 0)
        //                                tradeInfo.type = 2;
        //                        }
        //                        reader.Close();
        //                        #endregion
        //                    }
        //                    if (originalServerBillId > 0)
        //                    {
        //                        #region 找退货记录
        //                        sql.Length = 0;
        //                        sql.Append("select SKTNO,JLBH from HYK_XFJL where XFJLID = ").Append(originalServerBillId);
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        if (reader.Read())
        //                        {
        //                            tradeInfo.oldMachineID = DbUtils.GetString(reader, 0);
        //                            tradeInfo.oldOrderID = DbUtils.GetInt(reader, 1).ToString();
        //                        }
        //                        else
        //                        {
        //                            sql.Length = 0;
        //                            sql.Append("select SKTNO,JLBH from HYXFJL where XFJLID = ").Append(originalServerBillId);
        //                            cmd.CommandText = sql.ToString();
        //                            reader = cmd.ExecuteReader();
        //                            if (reader.Read())
        //                            {
        //                                tradeInfo.oldMachineID = DbUtils.GetString(reader, 0);
        //                                tradeInfo.oldOrderID = DbUtils.GetInt(reader, 1).ToString();
        //                            }
        //                        }
        //                        reader.Close();
        //                        #endregion
        //                    }
        //                    if (isFound)
        //                    {
        //                        string tableName = "HYK_XFJL";
        //                        if (fromBack)
        //                            tableName = "HYXFJL";
        //                        sql.Length = 0;
        //                        sql.Append("select a.INX,a.SPDM,a.XSSL,a.XSJE,a.XSJE_JF from ").Append(tableName + "_SP a where XFJLID = ").Append(serverBillId);
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        while (reader.Read())
        //                        {
        //                            TUploadTradeProductInfo itemInfo = new TUploadTradeProductInfo();
        //                            itemInfo.Index = DbUtils.GetInt(reader, 0);
        //                            itemInfo.productID = DbUtils.GetString(reader, 1);
        //                            itemInfo.productNum = DbUtils.GetDouble(reader, 2);
        //                            itemInfo.charge = DbUtils.GetDouble(reader, 3);
        //                            itemInfo.scoreCharge = DbUtils.GetDouble(reader, 4);
        //                            tradeInfo.productList.Add(itemInfo);
        //                        }
        //                        reader.Close();

        //                        sql.Length = 0;
        //                        sql.Append("select a.ZFFSDM,b.ZFFSMC,a.JE ");
        //                        //sql.Append(",(select max(BANK_KH) from ").Append(tableName + "_YHKZF c where a.XFJLID = XFJLID and a.ZFFSID = SHZFFSID) as BANK_KH");
        //                        sql.Append(" from ").Append(tableName + "_ZFFS a,SHZFFS b");
        //                        sql.Append("  where a.ZFFSID = b.SHZFFSID and a.XFJLID = ").Append(serverBillId);
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        while (reader.Read())
        //                        {
        //                            TUploadTradePayInfo itemInfo = new TUploadTradePayInfo();
        //                            itemInfo.payID = DbUtils.GetString(reader, 0);
        //                            itemInfo.payName = DbUtils.GetString(reader, 1);
        //                            itemInfo.payCharge = DbUtils.GetDouble(reader, 2);
        //                            //itemInfo.BankCardID = DbUtils.GetString(reader, 3);
        //                            tradeInfo.payList.Add(itemInfo);
        //                        }
        //                        reader.Close();

        //                        List<TUploadTradePayInfo> bankCardList = new List<TUploadTradePayInfo>();
        //                        string barkCardID = string.Empty;
        //                        sql.Length = 0;
        //                        sql.Append("select c.INX,b.ZFFSDM,c.BANK_KH from ").Append(tableName + "_YHKZF c,SHZFFS b where c.SHZFFSID = b.SHZFFSID and c.XFJLID = ").Append(serverBillId);
        //                        sql.Append(" order by c.INX ");
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        while (reader.Read())
        //                        {
        //                            bool IsFound = false;
        //                            string payId = DbUtils.GetString(reader, 1);
        //                            string bankCardID = DbUtils.GetString(reader, 2);
        //                            foreach (TUploadTradePayInfo bankCard in bankCardList)
        //                            {
        //                                if (bankCard.payID.Equals(payId))
        //                                {
        //                                    bankCard.BankCardID = bankCard.BankCardID + ";" + bankCardID;
        //                                    IsFound = true;
        //                                    break;
        //                                }
        //                            }
        //                            if (!IsFound)
        //                            {
        //                                TUploadTradePayInfo bankCard = new TUploadTradePayInfo();
        //                                bankCard.payID = payId;
        //                                bankCard.BankCardID = bankCardID;
        //                                bankCardList.Add(bankCard);
        //                            }
        //                        }
        //                        reader.Close();

        //                        foreach (TUploadTradePayInfo itemInfo in tradeInfo.payList)
        //                        {
        //                            foreach (TUploadTradePayInfo bankCard in bankCardList)
        //                            {
        //                                if (itemInfo.payID.Equals(bankCard.payID))
        //                                {
        //                                    itemInfo.BankCardID = bankCard.BankCardID;
        //                                    break;
        //                                }
        //                            }
        //                        }

        //                        sql.Length = 0;
        //                        sql.Append("select a.YHQCODE,c.BJ_WECHAT from HYK_JYCLITEM_YHQDM a,HYK_JYCL b,YHQDEF c where a.JYID = b.JYID and a.YHQID = c.YHQID and b.STATUS = 2 and b.XFJLID = ").Append(serverBillId);
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        while (reader.Read())
        //                        {
        //                            TUploadTradeCouponInfo itemInfo = new TUploadTradeCouponInfo();
        //                            itemInfo.couponID = DbUtils.GetString(reader, 0);
        //                            int couponKind = DbUtils.GetInt(reader, 1);
        //                            if (couponKind == 0)
        //                                couponKind = 2;
        //                            else
        //                                couponKind = 1;
        //                            itemInfo.couponKind = couponKind;
        //                            tradeInfo.couponList.Add(itemInfo);
        //                        }
        //                        reader.Close();

        //                        reqData req = new reqData();
        //                        req.data = tradeInfo;
        //                        string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                        req.sign = PubUtils.GetSign(timestamp);
        //                        string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                        string appCode = "/api/cy_erp/send_order_infor";
        //                        string respJsonStr = string.Empty;
        //                        bool ok = false;
        //                        int tmpErrorNumber = 0;
        //                        int errorNumber = ServerPlatform.Config.errorNumber;
        //                        if (errorNumber < 0)
        //                            errorNumber = 0;
        //                        while (errorNumber >= 0)
        //                        {
        //                            ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //                            if (ok)
        //                            {
        //                                errorNumber = 0;
        //                                tmpErrorNumber = 0;
        //                                break;
        //                            }
        //                            else
        //                            {
        //                                ServerPlatform.WriteErrorLog("\r\n UploadTradeInfo error: " + msg);
        //                                if (errorNumber > 0)
        //                                {
        //                                    errorNumber -= 1;
        //                                }
        //                                tmpErrorNumber += 1;
        //                                Thread.Sleep(1000);  
        //                            }
        //                            if (errorNumber < 0)
        //                                errorNumber = 0;
        //                            if (errorNumber == 0)
        //                                break;
        //                        }
        //                        if (ok)
        //                        {
        //                            uploadNumber += 1;
        //                        }
        //                        DbTransaction dbTrans = cmd.Connection.BeginTransaction();
        //                        try
        //                        {
        //                            cmd.Transaction = dbTrans;
        //                            sql.Length = 0;
        //                            sql.Append("delete from HYK_XFJL_XFJLID where XFJLID = ").Append(serverBillId);
        //                            cmd.CommandText = sql.ToString();
        //                            cmd.ExecuteNonQuery();
        //                            if (!ok)
        //                            {
        //                                sql.Length = 0;
        //                                sql.Append("update HYK_XFJL_XFJLID_ERR set ERROR_NUM = ").Append(tmpErrorNumber);
        //                                sql.Append(" where XFJLID = ").Append(serverBillId);
        //                                cmd.CommandText = sql.ToString();
        //                                if (cmd.ExecuteNonQuery() == 0)
        //                                {
        //                                    sql.Length = 0;
        //                                    sql.Append("insert into HYK_XFJL_XFJLID_ERR(XFJLID,INTIME,ERROR_NUM)");
        //                                    sql.Append(" values(").Append(serverBillId);
        //                                    sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "INTIME"));
        //                                    sql.Append(",").Append(tmpErrorNumber);
        //                                    sql.Append(")");
        //                                    cmd.CommandText = sql.ToString();
        //                                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "INTIME", serverTime);
        //                                    cmd.ExecuteNonQuery();
        //                                    cmd.Parameters.Clear();
        //                                }
        //                            }
        //                            dbTrans.Commit();
        //                        }
        //                        catch (Exception e)
        //                        {
        //                            dbTrans.Rollback();
        //                            throw e;
        //                        }

        //                        //if (ok)
        //                        //{
        //                        //    uploadNumber += 1;
        //                        //    DbTransaction dbTrans = cmd.Connection.BeginTransaction();
        //                        //    try
        //                        //    {
        //                        //        cmd.Transaction = dbTrans;
        //                        //        sql.Length = 0;
        //                        //        sql.Append("delete from HYK_XFJL_XFJLID where XFJLID = ").Append(serverBillId);
        //                        //        cmd.CommandText = sql.ToString();
        //                        //        cmd.ExecuteNonQuery();
        //                        //        dbTrans.Commit();
        //                        //    }
        //                        //    catch (Exception e)
        //                        //    {
        //                        //        dbTrans.Rollback();
        //                        //        throw e;
        //                        //    }
        //                        //}
        //                        //else
        //                        //{
        //                        //    ServerPlatform.WriteErrorLog("\r\n UploadTradeInfo error: " + msg);
        //                        //    break; 
        //                        //}
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadTradeInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadTradeInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadCouponInfo(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            TUploadCouponInfo couponInfo = new TUploadCouponInfo();
        //            List<TUploadCouponInfo> couponInfoList = new List<TUploadCouponInfo>();
        //            int tm = GetTblTM(cmd, "HYKYQDYD");//取上次传的最大TM值
        //            int lastTm = 0;
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select distinct YHQID,YHQMC,YHQSYGZMC,RQ1,RQ2,ZFFSDM,BJ_WECHAT,TM,ZQBZ1,ZQBZ2 from");
        //            sql.Append(" (select distinct YHQID,YHQMC,YHQSYGZMC,RQ1,RQ2,ZFFSDM,BJ_WECHAT,TM,ZQBZ1,ZQBZ2 from");
        //            sql.Append(" (select distinct a.YHQID,b.YHQMC,c.YHQSYGZMC,a.RQ1,a.RQ2,b.BJ_WECHAT,a.TM  ");
        //            sql.Append(",(select ZQBZ1 from YHQDEF_CXHD where a.SHDM=SHDM and a.YHQID=YHQID and a.CXHDBH=CXHDBH) as ZQBZ1");
        //            sql.Append(",(select ZQBZ2 from YHQDEF_CXHD where a.SHDM=SHDM and a.YHQID=YHQID and a.CXHDBH=CXHDBH) as ZQBZ2");
        //            sql.Append(",(select ZFFSDM from SHZFFS where a.SHDM=SHDM and a.YHQID=YHQID) as ZFFSDM");
        //            sql.Append(" from HYKYQDYD a,HYKYQDYD_GZSD m,YHQDEF b,YHQSYGZ c ");
        //            sql.Append(" where a.YHQID = b.YHQID and a.JLBH = m.JLBH and m.YQGZID = c.YHQSYGZID");
        //            sql.Append(" and a.STATUS = 2 and a.TM > ").Append(tm);
        //            sql.Append(" union ");
        //            sql.Append(" select distinct a.YHQID,b.YHQMC,c.YHQSYGZMC,a.RQ1,a.RQ2,b.BJ_WECHAT,a.TM  ");
        //            sql.Append(",(select ZQBZ1 from YHQDEF_CXHD where a.SHDM=SHDM and a.YHQID=YHQID and a.CXHDBH=CXHDBH) as ZQBZ1");
        //            sql.Append(",(select ZQBZ2 from YHQDEF_CXHD where a.SHDM=SHDM and a.YHQID=YHQID and a.CXHDBH=CXHDBH) as ZQBZ2");
        //            sql.Append(",(select ZFFSDM from SHZFFS where a.SHDM=SHDM and a.YHQID=YHQID) as ZFFSDM");
        //            sql.Append(" from HYKYQDYD a,HYKYQDYD_SP m,YHQDEF b,YHQSYGZ c ");
        //            sql.Append(" where a.YHQID = b.YHQID and a.JLBH = m.JLBH and m.YQGZID = c.YHQSYGZID");
        //            sql.Append(" and a.STATUS = 2 and a.TM > ").Append(tm);
        //            sql.Append(" ) where TM > ").Append(tm).Append(" order by TM) tbl where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                int couponId = DbUtils.GetInt(reader, 0);
        //                string couponName = DbUtils.GetString(reader, 1);
        //                string ruleName = DbUtils.GetString(reader, 2);
        //                string validDate1 = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 3));
        //                string validDate2 = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 4));
        //                string payId = DbUtils.GetString(reader, 5);
        //                int Flag = DbUtils.GetInt(reader, 6);
        //                int couponKind = 0;
        //                if (Flag == 0)
        //                    couponKind = 2;
        //                else
        //                    couponKind = 1;
        //                int tm1 = DbUtils.GetInt(reader, 7);
        //                if (tm1 > lastTm)
        //                    lastTm = tm1;
        //                string brief1 = DbUtils.GetString(reader, 8);
        //                string brief2 = DbUtils.GetString(reader, 9);
                        
        //                bool isFound = false;
        //                foreach (TUploadCouponInfo info in couponInfoList)
        //                {
        //                    if (couponKind == info.couponKind)
        //                    {
        //                        TUploadCouponItemInfo item = new TUploadCouponItemInfo();
        //                        item.couponType = couponId;
        //                        item.couponName = couponName;
        //                        item.couponRule = ruleName;
        //                        item.couponValidTime = validDate1 + ";" + validDate2;
        //                        item.payID = payId;
        //                        item.couponDesInfor1 = brief1;
        //                        item.couponDesInfor2 = brief2;
        //                        info.couponList.Add(item);
        //                        isFound = true;
        //                        break;
        //                    }
        //                }
        //                if (!isFound)
        //                {
        //                    TUploadCouponInfo info = new TUploadCouponInfo();
        //                    info.pageNum = 1;
        //                    info.curPageNum = 1;
        //                    info.curNum = 1;
        //                    info.couponKind = couponKind;
        //                    couponInfoList.Add(info);
        //                    TUploadCouponItemInfo item = new TUploadCouponItemInfo();
        //                    item.couponType = couponId;
        //                    item.couponName = couponName;
        //                    item.couponRule = ruleName;
        //                    item.couponValidTime = validDate1 + ";" + validDate2;
        //                    item.payID = payId;
        //                    item.couponDesInfor1 = brief1;
        //                    item.couponDesInfor2 = brief2;
        //                    info.couponList.Add(item);
        //                }
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();
                    

        //            if (couponInfoList.Count > 0)
        //            {
        //                foreach (TUploadCouponInfo info in couponInfoList)
        //                {
        //                    reqData req = new reqData();
        //                    req.data = info;
        //                    string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                    req.sign = PubUtils.GetSign(timestamp);
        //                    string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                    string appCode = "/api/cy_erp/send_coupon_attr";
        //                    string respJsonStr = string.Empty;
        //                    bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //                    if (ok)
        //                    {
        //                        uploadNumber += info.couponList.Count;
        //                        UpdateTblTM(cmd, "HYKYQDYD", lastTm);
        //                    }
        //                    else
        //                    {
        //                        ServerPlatform.WriteErrorLog("\r\n UploadTradeInfo error: " + msg);
        //                        break;
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadCouponInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadCouponInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadTradeInfoOfCoupon(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            List<TUploadTradeInfo2> TradeInfoList = new List<TUploadTradeInfo2>();
        //            int tm = GetTblTM(cmd, "HYXFJL");//取上次传的最大TM值
        //            int lastTm = 0;
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select XFJLID,SKTNO,JLBH,TM from");//TM值太大 改用XFJLID
        //            sql.Append(" (select XFJLID,SKTNO,JLBH,XFJLID as TM from HYXFJL a ");
        //            sql.Append(" where XFJLID > ").Append(tm);
        //            sql.Append(" and exists(select 1 from HYXFJL_SP_YQFT b where a.XFJLID = b.XFJLID)");
        //            sql.Append(" order by XFJLID) tbl where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TUploadTradeInfo2 info = new TUploadTradeInfo2();
        //                info.id = DbUtils.GetInt(reader, 0);
        //                info.machineID = DbUtils.GetString(reader, 1);
        //                info.orderID = DbUtils.GetInt(reader, 2).ToString();
        //                info.tm = DbUtils.GetInt(reader, 3);
        //                TradeInfoList.Add(info);
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();
        //            int iCount = 0;
        //            if (TradeInfoList.Count > 0)
        //            {
        //                foreach (TUploadTradeInfo2 info in TradeInfoList)
        //                {
        //                    TUploadTradeInfoOfCoupon trandInfo = null;
        //                    sql.Length = 0;
        //                    sql.Append("select b.BJ_WECHAT,a.YHQID,a.INX,c.SPDM,a.YQJE,d.RQ1,d.RQ2 from HYXFJL_SP_YQFT a,YHQDEF b,SHSPXX c,HYKYQDYD d");
        //                    sql.Append(" where a.YHQID = b.YHQID and a.SHSPID = c.SHSPID and a.YHQSYDBH = d.JLBH(+)");
        //                    sql.Append(" and a.XFJLID = ").Append(info.id);
        //                    cmd.CommandText = sql.ToString();
        //                    reader = cmd.ExecuteReader();
        //                    while (reader.Read())
        //                    {
        //                        if (trandInfo == null)
        //                            trandInfo = new TUploadTradeInfoOfCoupon();
        //                        TUploadTradeCouponItem item = new TUploadTradeCouponItem();
        //                        int couponKind = DbUtils.GetInt(reader, 0);
        //                        if (couponKind == 0)
        //                            couponKind = 2;
        //                        else
        //                            couponKind = 1;
        //                        item.couponKind = couponKind;
        //                        item.couponType = DbUtils.GetInt(reader, 1);
        //                        item.Index = DbUtils.GetInt(reader, 2);
        //                        item.productID = DbUtils.GetString(reader, 3);
        //                        item.couponCharge = Math.Round(DbUtils.GetDouble(reader, 4), 2);
        //                        if (!reader.IsDBNull(5))
        //                            item.couponValidTime = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 5))+";"+FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 6));
        //                        trandInfo.couponUsedList.Add(item);
        //                    }
        //                    reader.Close();
        //                    if (trandInfo != null)
        //                    {
        //                        trandInfo.machineID = info.machineID;
        //                        trandInfo.orderID = info.orderID;
        //                        reqData req = new reqData();
        //                        req.data = trandInfo;
        //                        string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                        req.sign = PubUtils.GetSign(timestamp);
        //                        string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                        string appCode = "/api/cy_erp/send_coupon_used_infor";
        //                        string respJsonStr = string.Empty;
        //                        bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //                        if (ok)
        //                        {
        //                            uploadNumber += 1;
        //                            iCount += 1;
        //                            if (info.tm > lastTm)
        //                                lastTm = info.tm;
        //                            if (iCount == 10)
        //                            {
        //                                UpdateTblTM(cmd, "HYXFJL", lastTm);
        //                                iCount = 0;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            ServerPlatform.WriteErrorLog("\r\n UploadTradeInfoOfCoupon error: " + msg);
        //                            break;
        //                        }
        //                    }
        //                }
        //            }
        //            if (iCount > 0)
        //            {
        //                UpdateTblTM(cmd, "HYXFJL", lastTm);
        //                iCount = 0;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadCouponInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadCouponInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadVipDiscInfo(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("CRMDB");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            List<TVipDiscBillId> billIds = new List<TVipDiscBillId>();
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select ID,JLBH,CLLX,INTIME from");
        //            sql.Append(" (select ID,JLBH,CLLX,INTIME  ");
        //            sql.Append(" from HYKZKDYD_JLBH  ");
        //            sql.Append(" order by ID) A where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TVipDiscBillId billIdInfo = new TVipDiscBillId();
        //                billIds.Add(billIdInfo);
        //                billIdInfo.id = DbUtils.GetInt(reader, 0);
        //                billIdInfo.billId = DbUtils.GetInt(reader, 1);
        //                billIdInfo.type = DbUtils.GetInt(reader, 2);
        //                billIdInfo.updateTime = DbUtils.GetDateTime(reader, 3);
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();
        //            if (billIds.Count > 0)
        //            {
        //                foreach (TVipDiscBillId billIdInfo in billIds)
        //                {
        //                    TVipDiscBillInfo tradeInfo = new TVipDiscBillInfo();
        //                    tradeInfo.type = billIdInfo.type;

        //                    sql.Length = 0;
        //                    sql.Append("select HYKTYPE,SHBMDM,RQ1,RQ2,YXCLBJ  ");
        //                    sql.Append(" from HYKZKDYD ");
        //                    sql.Append(" where JLBH = ").Append(billIdInfo.billId);
        //                    cmd.CommandText = sql.ToString();
        //                    reader = cmd.ExecuteReader();
        //                    if (reader.Read())
        //                    {
        //                        tradeInfo.id = billIdInfo.billId;
        //                        tradeInfo.gradeLevel = DbUtils.GetInt(reader, 0);
        //                        tradeInfo.deptCode = DbUtils.GetString(reader, 1);
        //                        tradeInfo.validStartTime = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 2));
        //                        tradeInfo.validEndTime = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 3));
        //                        tradeInfo.bIsPriority = DbUtils.GetBool(reader, 4);
        //                    }
        //                    reader.Close();
        //                    bool ok = false;
        //                    int tmpErrorNumber = 0;
        //                    if (tradeInfo.id > 0)
        //                    {
        //                        #region 折扣单明细
        //                        sql.Length = 0;
        //                        sql.Append("select a.JLBH,a.INX,a.GZBH,a.CLFS_BM,a.CLFS_SPFL,a.CLFS_SPSB,a.CLFS_SP,b.SJLX,b.SJNR,c.BMDM as SJDM,a.BJ_CJ,a.ZKL ");
        //                        sql.Append(" from HYKZKDYD_GZSD a,HYKZKDYD_GZITEM b,SHBM c ");
        //                        sql.Append(" where a.JLBH = b.JLBH ");
        //                        sql.Append(" and a.INX = b.INX ");
        //                        sql.Append(" and a.GZBH = b.GZBH ");
        //                        sql.Append(" and b.SJNR = c.SHBMID ");
        //                        sql.Append(" and b.SJLX = 1 ");
        //                        sql.Append(" and a.JLBH = ").Append(tradeInfo.id);
        //                        sql.Append("union ");
        //                        sql.Append("select a.JLBH,a.INX,a.GZBH,a.CLFS_BM,a.CLFS_SPFL,a.CLFS_SPSB,a.CLFS_SP,b.SJLX,b.SJNR,c.SPFLDM as SJDM,a.BJ_CJ,a.ZKL ");
        //                        sql.Append(" from HYKZKDYD_GZSD a,HYKZKDYD_GZITEM b,SHSPFL c ");
        //                        sql.Append(" where a.JLBH = b.JLBH ");
        //                        sql.Append(" and a.INX = b.INX ");
        //                        sql.Append(" and a.GZBH = b.GZBH ");
        //                        sql.Append(" and b.SJNR = c.SHSPFLID ");
        //                        sql.Append(" and b.SJLX = 3 ");
        //                        sql.Append(" and a.JLBH = ").Append(tradeInfo.id);
        //                        sql.Append(" union ");
        //                        sql.Append("select a.JLBH,a.INX,a.GZBH,a.CLFS_BM,a.CLFS_SPFL,a.CLFS_SPSB,a.CLFS_SP,b.SJLX,b.SJNR,c.SBDM as SJDM,a.BJ_CJ,a.ZKL ");
        //                        sql.Append(" from HYKZKDYD_GZSD a,HYKZKDYD_GZITEM b,SHSPSB c ");
        //                        sql.Append(" where a.JLBH = b.JLBH ");
        //                        sql.Append(" and a.INX = b.INX ");
        //                        sql.Append(" and a.GZBH = b.GZBH ");
        //                        sql.Append(" and b.SJNR = c.SHSBID ");
        //                        sql.Append(" and b.SJLX = 4 ");
        //                        sql.Append(" and a.JLBH = ").Append(tradeInfo.id);
        //                        sql.Append(" union ");
        //                        sql.Append("select a.JLBH,a.INX,a.GZBH,a.CLFS_BM,a.CLFS_SPFL,a.CLFS_SPSB,a.CLFS_SP,b.SJLX,b.SJNR,c.SPDM as SJDM,a.BJ_CJ,a.ZKL ");
        //                        sql.Append(" from HYKZKDYD_GZSD a,HYKZKDYD_GZITEM b,SHSPXX c ");
        //                        sql.Append(" where a.JLBH = b.JLBH ");
        //                        sql.Append(" and a.INX = b.INX ");
        //                        sql.Append(" and a.GZBH = b.GZBH ");
        //                        sql.Append(" and b.SJNR = c.SHSPID ");
        //                        sql.Append(" and b.SJLX = 6");
        //                        sql.Append(" and a.JLBH = ").Append(tradeInfo.id);
        //                        sql.Append(" order by INX,GZBH,SJLX,SJNR");
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        while (reader.Read())
        //                        {
        //                            int ruleNo = DbUtils.GetInt(reader, 2);
        //                            int dataType = DbUtils.GetInt(reader, 7);
        //                            if (dataType == 3)
        //                                dataType = 2;
        //                            else if (dataType == 4)
        //                                dataType = 3;
        //                            else if (dataType == 6)
        //                                dataType = 4;
        //                            bool isFound = false;
        //                            foreach (TVipDiscRuleInfo ruleInfo in tradeInfo.disRuleInforList)
        //                            {
        //                                if (ruleInfo.ruleNo == ruleNo)
        //                                {
        //                                    isFound = true;
        //                                    bool isFound2 = false;
        //                                    foreach (TVipDiscItemInfo item in ruleInfo.discountInforList)
        //                                    {
        //                                        if (item.type == dataType)
        //                                        {
        //                                            TCodeInfo codeInfo = new TCodeInfo();
        //                                            item.inforList.Add(codeInfo);
        //                                            codeInfo.code = DbUtils.GetString(reader, 9);
        //                                            isFound2 = true;
        //                                            break;
        //                                        }
        //                                    }
        //                                    if (!isFound2)
        //                                    {
        //                                        TVipDiscItemInfo item = new TVipDiscItemInfo();
        //                                        ruleInfo.discountInforList.Add(item);
        //                                        item.type = dataType;
        //                                        TCodeInfo codeInfo = new TCodeInfo();
        //                                        item.inforList.Add(codeInfo);
        //                                        codeInfo.code = DbUtils.GetString(reader, 9);
        //                                    }
        //                                    break;
        //                                }
        //                            }
        //                            if (!isFound)
        //                            {
        //                                TVipDiscRuleInfo ruleInfo = new TVipDiscRuleInfo();
        //                                tradeInfo.disRuleInforList.Add(ruleInfo);
        //                                ruleInfo.ruleNo = ruleNo;
        //                                ruleInfo.joinFlag = DbUtils.GetInt(reader, 10);
        //                                ruleInfo.discRate = DbUtils.GetDouble(reader, 11);
        //                                TVipDiscItemInfo item = new TVipDiscItemInfo();
        //                                ruleInfo.discountInforList.Add(item);
        //                                item.type = dataType;
        //                                TCodeInfo codeInfo = new TCodeInfo();
        //                                item.inforList.Add(codeInfo);
        //                                codeInfo.code = DbUtils.GetString(reader, 9);
        //                            }
        //                        }
        //                        reader.Close();
        //                        #endregion
        //                        reqData req = new reqData();
        //                        req.data = tradeInfo;
        //                        string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                        req.sign = PubUtils.GetSign(timestamp);
        //                        string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                        string appCode = "/api/cy_erp/send_discount_info";
        //                        string respJsonStr = string.Empty;
        //                        ok = false;
        //                        tmpErrorNumber = 0;
        //                        int errorNumber = ServerPlatform.Config.errorNumber;
        //                        if (errorNumber < 0)
        //                            errorNumber = 0;
        //                        while (errorNumber >= 0)
        //                        {
        //                            ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //                            if (ok)
        //                            {
        //                                errorNumber = 0;
        //                                tmpErrorNumber = 0;
        //                                break;
        //                            }
        //                            else
        //                            {
        //                                ServerPlatform.WriteErrorLog("\r\n UploadVipDiscInfo error: " + msg);
        //                                if (errorNumber > 0)
        //                                {
        //                                    errorNumber -= 1;
        //                                }
        //                                tmpErrorNumber += 1;
        //                                Thread.Sleep(1000);
        //                            }
        //                            if (errorNumber < 0)
        //                                errorNumber = 0;
        //                            if (errorNumber == 0)
        //                                break;
        //                        }
        //                        if (ok)
        //                        {
        //                            uploadNumber += 1;
        //                        }
        //                        //--
        //                    }
        //                    DbTransaction dbTrans = cmd.Connection.BeginTransaction();
        //                    try
        //                    {
        //                        cmd.Transaction = dbTrans;
        //                        sql.Length = 0;
        //                        sql.Append("delete from HYKZKDYD_JLBH where ID = ").Append(billIdInfo.id);
        //                        cmd.CommandText = sql.ToString();
        //                        cmd.ExecuteNonQuery();
        //                        if (!ok)
        //                        {
        //                            sql.Length = 0;
        //                            sql.Append("update HYKZKDYD_JLBH_ERR set ERROR_NUM = ").Append(tmpErrorNumber);
        //                            sql.Append(" where ID = ").Append(billIdInfo.id);
        //                            cmd.CommandText = sql.ToString();
        //                            if (cmd.ExecuteNonQuery() == 0)
        //                            {
        //                                sql.Length = 0;
        //                                sql.Append("insert into HYKZKDYD_JLBH_ERR(ID,JLBH,CLLX,INTIME,ERROR_NUM)");
        //                                sql.Append(" values(").Append(billIdInfo.id);
        //                                sql.Append(",").Append(billIdInfo.billId);
        //                                sql.Append(",").Append(billIdInfo.type);
        //                                sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "INTIME"));
        //                                sql.Append(",").Append(tmpErrorNumber);
        //                                sql.Append(")");
        //                                cmd.CommandText = sql.ToString();
        //                                DbUtils.AddDatetimeInputParameterAndValue(cmd, "INTIME", serverTime);
        //                                cmd.ExecuteNonQuery();
        //                                cmd.Parameters.Clear();
        //                            }
        //                        }
        //                        dbTrans.Commit();
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        dbTrans.Rollback();
        //                        throw e;
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadVipDiscInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadVipDiscInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}

        //public void UploadNonMemberTradeInfo(out int uploadNumber)
        //{
        //    string msg = string.Empty;
        //    uploadNumber = 0;
        //    DbConnection conn = null;
        //    DbCommand cmd = null;
        //    try
        //    {
        //        conn = DbConnManager.GetDbConnection("ERP");
        //        try
        //        {
        //            conn.Open();
        //        }
        //        catch (Exception e)
        //        {
        //            throw new MyDbException(e.Message, true);
        //        }
        //        try
        //        {
        //            string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //            string dbSysName = DbUtils.GetDbSystemName(conn);
        //            cmd = conn.CreateCommand();
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            List<TNONMemberJLBH> JLBHList = new List<TNONMemberJLBH>();
        //            StringBuilder sql = new StringBuilder();
        //            sql.Append("select ID,SKTNO,JLBH from");
        //            sql.Append(" (select ID,SKTNO,JLBH,DJSJ  ");
        //            sql.Append(" from UPLOAD_XSJL_JLBH  ");
        //            sql.Append(" order by DJSJ) A where rownum <= ").Append(ServerPlatform.Config.OnceUploadNumber);
        //            cmd.CommandText = sql.ToString();
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                int id = DbUtils.GetInt(reader, 0);
        //                string posId = DbUtils.GetString(reader, 1);
        //                int billId = DbUtils.GetInt(reader, 2);
        //                bool isFound = false;
        //                foreach (TNONMemberJLBH info in JLBHList)
        //                {
        //                    if ((info.posId.Equals(posId)) && (info.billId == billId))
        //                    {
        //                        info.Ids.Add(id);
        //                        isFound = true;
        //                        break;
        //                    }
        //                }
        //                if (!isFound)
        //                {
        //                    TNONMemberJLBH info = new TNONMemberJLBH();
        //                    JLBHList.Add(info);
        //                    info.posId = posId;
        //                    info.billId = billId;
        //                    info.Ids.Add(id);
        //                }
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();

        //            if (JLBHList.Count > 0)
        //            {
        //                string NonCodeCouponPayment = ServerPlatform.Config.NonCodeCouponPayment;
        //                if ((NonCodeCouponPayment != null) && (NonCodeCouponPayment.Length > 0) && (!NonCodeCouponPayment.Substring(NonCodeCouponPayment.Length - 1, 1).Equals(",")))
        //                    NonCodeCouponPayment = NonCodeCouponPayment + ",";
        //                foreach (TNONMemberJLBH info in JLBHList)
        //                {
        //                    TUploadNonMemberTradeInfo tradeInfo = new TUploadNonMemberTradeInfo();
        //                    bool fromBack = false;
        //                    bool isFound = false;
        //                    #region 查实时表
        //                    sql.Length = 0;
        //                    sql.Append("select a.SKTNO,a.JLBH,a.JYSJ,a.JZRQ,a.XSJE,b.RYDM,a.SKTNO_OLD,a.JLBH_OLD  ");
        //                    sql.Append(" from XSJL a,RYXX b where a.SKY=b.PERSON_ID(+)");
        //                    sql.Append(" and a.SKTNO not like '5%' ");
        //                    sql.Append(" and a.SKTNO = '").Append(info.posId).Append("'");
        //                    sql.Append(" and a.JLBH = ").Append(info.billId);
        //                    cmd.CommandText = sql.ToString();
        //                    reader = cmd.ExecuteReader();
        //                    if (reader.Read())
        //                    {
        //                        isFound = true;
        //                        tradeInfo.storeCode = "BH01";
        //                        tradeInfo.storeName = "杭州大厦购物城";
        //                        tradeInfo.machineID = DbUtils.GetString(reader, 0);
        //                        if ((tradeInfo.machineID != null) && (tradeInfo.machineID.Length == 6) && (tradeInfo.machineID.Substring(0,1).Equals("0")))
        //                        {
        //                            tradeInfo.storeCode = "BH11";
        //                            tradeInfo.storeName = "杭州大厦超市新A店";
        //                        }
        //                        tradeInfo.orderID = DbUtils.GetInt(reader, 1).ToString();
        //                        DateTime TimeShopping = DbUtils.GetDateTime(reader, 2);
        //                        tradeInfo.orderOccurTime = FormatUtils.DatetimeToString(TimeShopping);
        //                        tradeInfo.orderBelongTime = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 3));
        //                        tradeInfo.oldMachineID = DbUtils.GetString(reader, 6);
        //                        tradeInfo.oldOrderID = DbUtils.GetInt(reader, 7).ToString();
        //                        if (DbUtils.GetDouble(reader, 4) < 0)
        //                            tradeInfo.type = 2;
        //                    }
        //                    reader.Close();
        //                    #endregion

        //                    if (!isFound)
        //                    {
        //                        #region 查历史表
        //                        sql.Length = 0;
        //                        sql.Append("select a.SKTNO,a.JLBH,a.JYSJ,a.JZRQ,a.XSJE,b.RYDM,a.SKTNO_OLD,a.JLBH_OLD  ");
        //                        sql.Append(" from SKTXSJL a,RYXX b where a.SKY=b.PERSON_ID(+)");
        //                        sql.Append(" and a.SKTNO not like '5%' ");
        //                        sql.Append(" and a.SKTNO = '").Append(info.posId).Append("'");
        //                        sql.Append(" and a.JLBH = ").Append(info.billId);
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        if (reader.Read())
        //                        {
        //                            isFound = true;
        //                            fromBack = true;
        //                            tradeInfo.storeCode = "BH01";
        //                            tradeInfo.storeName = "杭州大厦购物城";
        //                            tradeInfo.machineID = DbUtils.GetString(reader, 0);
        //                            if ((tradeInfo.machineID != null) && (tradeInfo.machineID.Length == 6) && (tradeInfo.machineID.Substring(0, 1).Equals("0")))
        //                            {
        //                                tradeInfo.storeCode = "BH11";
        //                                tradeInfo.storeName = "杭州大厦超市新A店";
        //                            }
        //                            tradeInfo.orderID = DbUtils.GetInt(reader, 1).ToString();
        //                            DateTime TimeShopping = DbUtils.GetDateTime(reader, 2);
        //                            tradeInfo.orderOccurTime = FormatUtils.DatetimeToString(TimeShopping);
        //                            tradeInfo.orderBelongTime = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 3));
        //                            tradeInfo.oldMachineID = DbUtils.GetString(reader, 6);
        //                            tradeInfo.oldOrderID = DbUtils.GetInt(reader, 7).ToString();
        //                            if (DbUtils.GetDouble(reader, 4) < 0)
        //                                tradeInfo.type = 2;
        //                        }
        //                        reader.Close();
        //                        #endregion
        //                    }
        //                    if (isFound)
        //                    {
        //                        bool isCodeCouponPay = false;
        //                        #region 商品和付款方式
        //                        string tableName = "XSJL";
        //                        if (fromBack)
        //                            tableName = "SKTXSJL";
        //                        sql.Length = 0;
        //                        sql.Length = 0;
        //                        sql.Append("select a.INX,b.SPCODE,a.XSSL,a.XSJE ");
        //                        sql.Append(" from ").Append(tableName + "C a,SPXX b where a.SP_ID = b.SP_ID ");
        //                        sql.Append(" and a.SKTNO = '").Append(info.posId).Append("'");
        //                        sql.Append(" and a.JLBH = ").Append(info.billId);
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        while (reader.Read())
        //                        {
        //                            TUploadNonMemberTradeProductInfo itemInfo = new TUploadNonMemberTradeProductInfo();
        //                            itemInfo.Index = DbUtils.GetInt(reader, 0);
        //                            itemInfo.productID = DbUtils.GetString(reader, 1);
        //                            itemInfo.productNum = DbUtils.GetDouble(reader, 2);
        //                            itemInfo.charge = DbUtils.GetDouble(reader, 3);
        //                            tradeInfo.productList.Add(itemInfo);
        //                        }
        //                        reader.Close();
        //                        double cardCharge = 0; 
        //                        sql.Length = 0;
        //                        sql.Append("select a.SKFS,b.NAME,a.SKJE,b.TYPE ");
        //                        sql.Append(" from ").Append(tableName + "M_JB a,SKFS_JB b where a.SKFS=b.CODE");
        //                        sql.Append(" and a.SKTNO = '").Append(info.posId).Append("'");
        //                        sql.Append(" and a.JLBH = ").Append(info.billId);
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        while (reader.Read())
        //                        {
        //                            TUploadNonMemberTradePayInfo itemInfo = new TUploadNonMemberTradePayInfo();
        //                            itemInfo.payID = DbUtils.GetInt(reader, 0).ToString();
        //                            itemInfo.payName = DbUtils.GetString(reader, 1);
        //                            itemInfo.payCharge = DbUtils.GetDouble(reader, 2);
        //                            int payType = DbUtils.GetInt(reader, 3);
        //                            if (payType == 2)
        //                                cardCharge += itemInfo.payCharge;
        //                            else if (payType == 3)
        //                            {
        //                                if ((NonCodeCouponPayment == null) || (NonCodeCouponPayment.Length == 0) || (!NonCodeCouponPayment.Contains(itemInfo.payID+",")))
        //                                {
        //                                    isCodeCouponPay = true;
        //                                }
        //                            }
        //                            if (payType == 1)
        //                                itemInfo.BankCardID = "1";
        //                            tradeInfo.payList.Add(itemInfo);
        //                        }
        //                        reader.Close();

        //                        tradeInfo.cardCharge = cardCharge;
        //                        string barkCardID = string.Empty;
        //                        sql.Length = 0;
        //                        sql.Append("select INX,KH from XSJL_XYKJL_ONLINE a ");
        //                        sql.Append(" where a.SKTNO = '").Append(info.posId).Append("'");
        //                        sql.Append(" and a.JLBH = ").Append(info.billId);
        //                        sql.Append(" order by INX ");
        //                        cmd.CommandText = sql.ToString();
        //                        reader = cmd.ExecuteReader();
        //                        while (reader.Read())
        //                        {
        //                            if (barkCardID.Length == 0)
        //                                barkCardID = DbUtils.GetString(reader, 1);
        //                            else
        //                                barkCardID = barkCardID + ";" + DbUtils.GetString(reader, 1);
        //                        }
        //                        reader.Close();

        //                        foreach (TUploadNonMemberTradePayInfo itemInfo in tradeInfo.payList)
        //                        {
        //                            if ((itemInfo.BankCardID != null) && (itemInfo.BankCardID.Equals("1")))
        //                            {
        //                                itemInfo.BankCardID = barkCardID;
        //                            }
        //                        }
        //                        #endregion
        //                        bool ok = false;
        //                        int tmpErrorNumber = 0;
        //                        if (!isCodeCouponPay)
        //                        {
        //                            #region 上传数据
        //                            reqData req = new reqData();
        //                            req.data = tradeInfo;
        //                            string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //                            req.sign = PubUtils.GetSign(timestamp);
        //                            string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //                            string appCode = "/api/cy_erp/send_non_viporder_infor";
        //                            string respJsonStr = string.Empty;

        //                            int errorNumber = ServerPlatform.Config.errorNumber;
        //                            if (errorNumber < 0)
        //                                errorNumber = 0;
        //                            while (errorNumber >= 0)
        //                            {
        //                                ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //                                if (ok)
        //                                {
        //                                    errorNumber = 0;
        //                                    tmpErrorNumber = 0;
        //                                    break;
        //                                }
        //                                else
        //                                {
        //                                    ServerPlatform.WriteErrorLog("\r\n UploadNonMemberTradeInfo error: " + msg);
        //                                    if (errorNumber > 0)
        //                                    {
        //                                        errorNumber -= 1;
        //                                    }
        //                                    tmpErrorNumber += 1;
        //                                    Thread.Sleep(1000);
        //                                }
        //                                if (errorNumber < 0)
        //                                    errorNumber = 0;
        //                                if (errorNumber == 0)
        //                                    break;
        //                            }
        //                            if (ok)
        //                            {
        //                                uploadNumber += 1;
        //                            }
        //                            #endregion
        //                        }
        //                        else
        //                        {
        //                            ok = true;//编码券支付时直接删除
        //                        }

        //                        if ((info.Ids != null) && (info.Ids.Count > 0))
        //                        {
        //                            DbTransaction dbTrans = cmd.Connection.BeginTransaction();
        //                            try
        //                            {
        //                                cmd.Transaction = dbTrans;
        //                                sql.Length = 0;
        //                                sql.Append("delete from UPLOAD_XSJL_JLBH where ID in (").Append(info.Ids[0]);
        //                                for (int i = 1; i < info.Ids.Count; i++)
        //                                {
        //                                    sql.Append(",").Append(info.Ids[i]);
        //                                }
        //                                sql.Append(")");
        //                                cmd.CommandText = sql.ToString();
        //                                cmd.ExecuteNonQuery();
        //                                if (!ok)
        //                                {
        //                                    sql.Length = 0;
        //                                    sql.Append("update UPLOAD_XSJL_JLBH_ERR set ERROR_NUM = ").Append(tmpErrorNumber);
        //                                    sql.Append(" where ID = ").Append(info.Ids[0]);
        //                                    cmd.CommandText = sql.ToString();
        //                                    if (cmd.ExecuteNonQuery() == 0)
        //                                    {
        //                                        sql.Length = 0;
        //                                        sql.Append("insert into UPLOAD_XSJL_JLBH_ERR(ID,SKTNO,JLBH,DJSJ,ERROR_NUM)");
        //                                        sql.Append(" values(").Append(info.Ids[0]);
        //                                        sql.Append(",'").Append(info.posId).Append("'");
        //                                        sql.Append(",").Append(info.billId);
        //                                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "DJSJ"));
        //                                        sql.Append(",").Append(tmpErrorNumber);
        //                                        sql.Append(")");
        //                                        cmd.CommandText = sql.ToString();
        //                                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "DJSJ", serverTime);
        //                                        cmd.ExecuteNonQuery();
        //                                        cmd.Parameters.Clear();
        //                                    }
        //                                }
        //                                dbTrans.Commit();
        //                            }
        //                            catch (Exception e)
        //                            {
        //                                dbTrans.Rollback();
        //                                throw e;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (cmd == null)
        //                ServerPlatform.WriteErrorLog("\r\n UploadNonMemberTradeInfo error: " + e.Message);
        //            else
        //                ServerPlatform.WriteErrorLog("\r\n UploadNonMemberTradeInfo, sql：" + cmd.CommandText + "\r\n error: " + e.Message);
        //        }
        //    }
        //    finally
        //    {
        //        if (conn != null) conn.Close();
        //    }
        //}
    }
}
