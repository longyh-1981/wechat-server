using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Web;
using Newtonsoft.Json;
using System.Linq;
using System.Xml;
using System.Net;
using System.IO;
using System.Configuration;
using System.Threading;

namespace WCRMServer.Proc
{
    public class CRMProc
    {
        public static bool Register(out AppRespone respData, string reqJsonStr)
        {
            respData = new AppRespone();
            //if ((reqData == null) || (reqData.Data == null) || (reqData.args.ToString().Length == 0))
            //{
            //    Random rd = new Random();
            //    string randomStr = rd.Next(1000000, 9999999).ToString();
            //    respData.responseId = PubUtils.GetResponseId(randomStr);
            //    respData.errCode = 1;
            //    respData.errMessage = "请求数据有误";
            //    return false;
            //}
            TRegisterReq reqArgs = JsonConvert.DeserializeObject<TRegisterReq>(reqJsonStr);
            if (reqArgs == null)
            {
                respData.Code = "1000";
                respData.Message = "请求数据有误";
                return false;
            }
            if ((reqArgs.Mobile == null) || (reqArgs.Mobile.Length == 0))
            {
                respData.Code = "1001";
                respData.Message = "手机号必须有值";
                return false;
            }
            if ((reqArgs.UnionId == null) || (reqArgs.UnionId.Length == 0))
            {
                respData.Code = "1001";
                respData.Message = "UnionId必须有值";
                return false;
            }
            if ((reqArgs.OpenId == null) || (reqArgs.OpenId.Length == 0))
            {
                respData.Code = "1001";
                respData.Message = "OpenId必须有值";
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
                    int vipId = 0;
                    string memberId = string.Empty;
                    bool IsRegister = false;
                    int VipCardTypeId = ServerPlatform.Config.OpenCardTypeId;
                    int RegStoreId = ServerPlatform.Config.OpenCardStoreId;
                    int CardGradeId = ServerPlatform.Config.CardGradeId;
                    string VipCardTypeName = string.Empty;
                    string CardGradeName = string.Empty;
                    DbDataReader reader = null;
                    sql.Length = 0;
                    sql.Append("select CARDTYPENAME from CARDTYPEINFO where CARDTYPEID = ").Append(VipCardTypeId);
                    cmd.CommandText = sql.ToString();
                    reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        reader.Close();
                        respData.Code = "1001";
                        respData.Message = string.Format("卡类型{0}未设置", VipCardTypeId.ToString());
                        return false;
                    }
                    VipCardTypeName = DbUtils.GetString(reader, 0);
                    reader.Close();
                    sql.Length = 0;
                    sql.Append("select GRADENAME from VIPGRADEINFO where GRADEID = ").Append(CardGradeId);
                    cmd.CommandText = sql.ToString();
                    reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        reader.Close();
                        respData.Code = "1001";
                        respData.Message = string.Format("卡级别{0}未设置", VipCardTypeId.ToString());
                        return false;
                    }
                    CardGradeName = DbUtils.GetString(reader, 0);
                    reader.Close();
                    if ((reqArgs.CardCode != null) && (reqArgs.CardCode.Length > 0))
                    {
                        sql.Length = 0;
                        sql.Append("select VIPID,MEMBERID from CARDINFO where CARDNO = ").Append(DbUtils.SpellSqlParameter(conn, "CARDNO"));
                        cmd.CommandText = sql.ToString();
                        DbUtils.AddStrInputParameterAndValue(cmd, 50, "CARDNO", reqArgs.CardCode);
                        reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            vipId = DbUtils.GetInt(reader, 0);
                            memberId = DbUtils.GetString(reader, 1);
                            IsRegister = true;
                        }
                        reader.Close();
                        cmd.Parameters.Clear();
                        if (!IsRegister)
                        {
                            respData.Code = "1001";
                            respData.Message = "会员卡号不存在";
                            return false;
                        }
                    }
                    if (!IsRegister)
                    {
                        sql.Length = 0;
                        sql.Append("select a.CARDNO,a.MEMBERID,a.VIPID from CARDINFO a,MEMBER_WECHAT b where a.MEMBERID=b.MEMBERID ");//MEMBER_WECHAT(MEMBERID,UNIONID,OPENID,BDTIME)
                        sql.Append(" and b.UNIONID = ").Append(DbUtils.SpellSqlParameter(conn, "UNIONID"));
                        cmd.CommandText = sql.ToString();
                        DbUtils.AddStrInputParameterAndValue(cmd, 20, "UNIONID", reqArgs.UnionId);
                        cmd.CommandText = sql.ToString();
                        reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            reqArgs.CardCode = DbUtils.GetString(reader, 0);
                            memberId = DbUtils.GetString(reader, 1);
                            vipId = DbUtils.GetInt(reader, 2);
                            IsRegister = true;
                        }
                        reader.Close();
                        cmd.Parameters.Clear();
                    }
                    string CardTrack = reqArgs.CardCode;
                    DateTime validDate = DateTime.MinValue;
                    if (!IsRegister)
                    {
                        vipId = PubUtils.GetSeq("MYDB", 0, "CARDINFO");
                        int memberId1 = PubUtils.GetSeq("MYDB", 0, "MEMBERINFO");
                        memberId = string.Format("GK{0}", memberId1.ToString().PadLeft(8, '0'));
                        string PrefixCode = string.Empty;
                        string SuffixCode = string.Empty;
                        int CodeLength = 0;
                        validDate = PubUtils.GetVipCardValidDate(out PrefixCode, out SuffixCode, out CodeLength, cmd, VipCardTypeId);
                        int seqCode = PubUtils.GetSeq("MYDB", 0, "CARDINFO_NO");
                        string CardNo = string.Format("{0}{1}{2}", PrefixCode, SuffixCode, seqCode.ToString().PadLeft(CodeLength - PrefixCode.Length - SuffixCode.Length, '0'));
                        CardTrack = CardNo;
                        reqArgs.CardCode = CardNo;
                    }
                    DateTime serverTime = DbUtils.GetDbServerTime(cmd);
                    DbTransaction dbTrans = conn.BeginTransaction();
                    try
                    {
                        cmd.Transaction = dbTrans;
                        if (!IsRegister)
                        {
                            sql.Length = 0;
                            sql.Append("insert into CARDINFO(VIPID,CARDNO,CARDTRACK,CARDTYPE,REGDATE,EXPDATE,STATUS,SALETYPE,OPENID,OPTID,OPTNAME,MARKETID,MEMBERID,REGSTOREID,GRADEID)");
                            sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "VIPID"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "CARDNO"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "CARDTRACK"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "CARDTYPE"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "REGDATE"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "EXPDATE"));
                            sql.Append(",0");
                            sql.Append(",0");
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "OPENID"));
                            sql.Append(",1");
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "OPTNAME"));
                            sql.Append(",1");
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "MEMBERID"));
                            sql.Append(",").Append(RegStoreId);
                            sql.Append(",").Append(CardGradeId);
                            sql.Append(")");
                            cmd.CommandText = sql.ToString();
                            DbUtils.AddIntInputParameterAndValue(cmd, "VIPID", vipId.ToString());
                            DbUtils.AddStrInputParameterAndValue(cmd, 50, "CARDNO", reqArgs.CardCode);
                            DbUtils.AddStrInputParameterAndValue(cmd, 60, "CARDTRACK", CardTrack);
                            DbUtils.AddIntInputParameterAndValue(cmd, "CARDTYPE", VipCardTypeId.ToString());
                            DbUtils.AddDatetimeInputParameterAndValue(cmd, "REGDATE", serverTime);
                            DbUtils.AddDatetimeInputParameterAndValue(cmd, "EXPDATE", validDate);
                            DbUtils.AddStrInputParameterAndValue(cmd, 60, "OPENID", reqArgs.OpenId);
                            DbUtils.AddStrInputParameterAndValue(cmd, 20, "OPTNAME", "微信注册");
                            DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERID", memberId);
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();

                            sql.Length = 0;
                            sql.Append("insert into MEMBERINFO(MEMBERID,MOBILE,MEMBERNAME");//,IDNO,ADRESS
                            if ((reqArgs.BirthDay != null) && (reqArgs.BirthDay.Length > 0))
                                sql.Append(",BIRTHDAY");
                            if ((reqArgs.Gender >= 0) && (reqArgs.Gender <= 1))
                                sql.Append(",GENDER");
                            sql.Append(",CREATETIME,CHANNEL_ID,OPTID,OPTNAME,MARKETID) ");
                            sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "MEMBERID"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "MOBILE"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "MEMBERNAME"));
                            if ((reqArgs.BirthDay != null) && (reqArgs.BirthDay.Length > 0))
                                sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "BIRTHDAY"));
                            //sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "IDNO"));
                            if ((reqArgs.Gender >= 0) && (reqArgs.Gender <= 1))
                                sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "GENDER"));
                            //sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "ADRESS"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "CREATETIME"));
                            sql.Append(",1");//0线下，1微信，2支付宝，3网站，4APP
                            sql.Append(",1");
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "OPTNAME"));
                            sql.Append(",1");
                            sql.Append(")");
                            cmd.CommandText = sql.ToString();
                            DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERID", memberId);
                            DbUtils.AddStrInputParameterAndValue(cmd, 15, "MOBILE", reqArgs.Mobile);
                            DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERNAME", reqArgs.MemberName);
                            if ((reqArgs.BirthDay != null) && (reqArgs.BirthDay.Length > 0))
                                DbUtils.AddDatetimeInputParameterAndValue(cmd, "BIRTHDAY", FormatUtils.ParseDateString(reqArgs.BirthDay));
                            //DbUtils.AddStrInputParameterAndValue(cmd, 18, "IDNO", reqArgs.IDNo);
                            if ((reqArgs.Gender >= 0) && (reqArgs.Gender <= 1))
                                DbUtils.AddIntInputParameterAndValue(cmd, "GENDER", reqArgs.Gender.ToString());
                            DbUtils.AddDatetimeInputParameterAndValue(cmd, "CREATETIME", serverTime);
                            DbUtils.AddStrInputParameterAndValue(cmd, 20, "OPTNAME", "微信注册");
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();

                            sql.Length = 0;
                            sql.Append("insert into MEMBER_WECHAT(MEMBERID,UNIONID,OPENID,BDTIME)");
                            sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "MEMBERID"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "UNIONID"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "OPENID"));
                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "BDTIME"));
                            sql.Append(")");
                            cmd.CommandText = sql.ToString();
                            DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERID", memberId);
                            DbUtils.AddStrInputParameterAndValue(cmd, 60, "UNIONID", reqArgs.UnionId);
                            DbUtils.AddStrInputParameterAndValue(cmd, 60, "OPENID", reqArgs.OpenId);
                            DbUtils.AddDatetimeInputParameterAndValue(cmd, "BDTIME", serverTime);
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                        }
                        else
                        {
                            sql.Length = 0;
                            sql.Append("update MEMBER_WECHAT set BDTIME = ").Append(DbUtils.SpellSqlParameter(conn, "BDTIME"));
                            sql.Append(" where MEMBERID = ").Append(DbUtils.SpellSqlParameter(conn, "MEMBERID"));
                            sql.Append(" and UNIONID = ").Append(DbUtils.SpellSqlParameter(conn, "UNIONID"));
                            sql.Append(" and OPENID = ").Append(DbUtils.SpellSqlParameter(conn, "OPENID"));
                            cmd.CommandText = sql.ToString();
                            DbUtils.AddDatetimeInputParameterAndValue(cmd, "BDTIME", serverTime);
                            DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERID", memberId);
                            DbUtils.AddStrInputParameterAndValue(cmd, 60, "UNIONID", reqArgs.UnionId);
                            DbUtils.AddStrInputParameterAndValue(cmd, 60, "OPENID", reqArgs.OpenId);
                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                cmd.Parameters.Clear();
                                sql.Length = 0;
                                sql.Append("insert into MEMBER_WECHAT(MEMBERID,UNIONID,OPENID,BDTIME)");
                                sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "MEMBERID"));
                                sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "UNIONID"));
                                sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "OPENID"));
                                sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "BDTIME"));
                                sql.Append(")");
                                cmd.CommandText = sql.ToString();
                                DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERID", memberId);
                                DbUtils.AddStrInputParameterAndValue(cmd, 60, "UNIONID", reqArgs.UnionId);
                                DbUtils.AddStrInputParameterAndValue(cmd, 60, "OPENID", reqArgs.OpenId);
                                DbUtils.AddDatetimeInputParameterAndValue(cmd, "BDTIME", serverTime);
                                cmd.ExecuteNonQuery();
                                cmd.Parameters.Clear();
                            }
                            cmd.Parameters.Clear();
                        }
                        dbTrans.Commit();
                        TRegisterResponse response = new TRegisterResponse();
                        response.MemberId = memberId;
                        response.CardId = vipId.ToString();
                        response.CardCode = reqArgs.CardCode;
                        response.CardTypeId = VipCardTypeId;
                        response.CardTypeName = VipCardTypeName;
                        response.GradeName = CardGradeName;
                        respData.Data = response;
                    }
                    catch (Exception e)
                    {
                        dbTrans.Rollback();
                        throw e;
                    }
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
            return (respData.Code.Equals("0"));
        }

        public static bool UploadVipCardInfo(out AppRespone respData, string reqJsonStr)
        {
            respData = new AppRespone();
            TUpdateVipCardInfoReq reqArgs = JsonConvert.DeserializeObject<TUpdateVipCardInfoReq>(reqJsonStr);
            if (reqArgs == null)
            {
                respData.Code = "1000";
                respData.Message = "请求数据有误";
                return false;
            }
            if ((reqArgs.MemberId == null) || (reqArgs.MemberId.Length == 0))
            {
                respData.Code = "1001";
                respData.Message = "顾客ID必须有值";
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
                    sql.Length = 0;
                    sql.Append("select 1 from MEMBERINFO where MEMBERID = ").Append(DbUtils.SpellSqlParameter(conn, "MEMBERID"));
                    cmd.CommandText = sql.ToString();
                    DbUtils.AddStrInputParameterAndValue(cmd, 50, "MEMBERID", reqArgs.MemberId);
                    DbDataReader reader = cmd.ExecuteReader();
                    if (!reader.Read())
                    {
                        reader.Close();
                        cmd.Parameters.Clear();
                        respData.Code = "1001";
                        respData.Message = string.Format("顾客ID{0}不存在", reqArgs.MemberId);
                        return false;
                    }
                    reader.Close();
                    cmd.Parameters.Clear();
                    DateTime serverTime = DbUtils.GetDbServerTime(cmd);
                    DbTransaction dbTrans = conn.BeginTransaction();
                    try
                    {
                        cmd.Transaction = dbTrans;
                        sql.Length = 0;
                        sql.Append("update MEMBERINFO set set UPDATETIME = ").Append(DbUtils.SpellSqlParameter(conn, "UPDATETIME"));//,IDNO,ADRESS (MEMBERID,MOBILE,MEMBERNAME
                        sql.Append(",UOPTID = 1,UOPTNAME = ").Append(DbUtils.SpellSqlParameter(conn, "UOPTNAME"));
                        if ((reqArgs.MemberName != null) && (reqArgs.MemberName.Length > 0))
                            sql.Append(",MEMBERNAME = ").Append(DbUtils.SpellSqlParameter(conn, "MEMBERNAME"));
                        if ((reqArgs.BirthDay != null) && (reqArgs.BirthDay.Length > 0))
                            sql.Append(",BIRTHDAY = ").Append(DbUtils.SpellSqlParameter(conn, "BIRTHDAY"));
                        if ((reqArgs.IDNo != null) && (reqArgs.IDNo.Length > 0))
                            sql.Append(",IDNO = ").Append(DbUtils.SpellSqlParameter(conn, "IDNO"));
                        if ((reqArgs.Adress != null) && (reqArgs.Adress.Length > 0))
                            sql.Append(",ADRESS = ").Append(DbUtils.SpellSqlParameter(conn, "ADRESS"));
                        //if ((reqArgs.MemberLabel != null) && (reqArgs.MemberLabel.Length > 0))
                        if ((reqArgs.Gender >= 0) && (reqArgs.Gender <= 1))
                            sql.Append(",GENDER = ").Append(reqArgs.Gender);
                        sql.Append(" where MEMBERID = ").Append(DbUtils.SpellSqlParameter(conn, "MEMBERID"));
                        cmd.CommandText = sql.ToString();
                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "UPDATETIME", serverTime);
                        DbUtils.AddStrInputParameterAndValue(cmd, 20, "UOPTNAME", "微信");
                        if ((reqArgs.MemberName != null) && (reqArgs.MemberName.Length > 0))
                            DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERNAME", reqArgs.MemberName);
                        if ((reqArgs.BirthDay != null) && (reqArgs.BirthDay.Length > 0))
                            DbUtils.AddDatetimeInputParameterAndValue(cmd, "BIRTHDAY", FormatUtils.ParseDateString(reqArgs.BirthDay));
                        if ((reqArgs.IDNo != null) && (reqArgs.IDNo.Length > 0))
                            DbUtils.AddStrInputParameterAndValue(cmd, 20, "IDNO", reqArgs.IDNo);
                        if ((reqArgs.Adress != null) && (reqArgs.Adress.Length > 0))
                            DbUtils.AddStrInputParameterAndValue(cmd, 100, "ADRESS", reqArgs.Adress);
                        DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERID", reqArgs.MemberId);
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                        dbTrans.Commit();
                        
                    }
                    catch (Exception e)
                    {
                        dbTrans.Rollback();
                        throw e;
                    }
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
            return (respData.Code.Equals("0"));
        }

        public static bool GetMemberInfo(out AppRespone respData, string reqJsonStr)
        {
            respData = new AppRespone();
            TGetMemberInfoReq reqArgs = JsonConvert.DeserializeObject<TGetMemberInfoReq>(reqJsonStr);
            if (reqArgs == null)
            {
                respData.Code = "1000";
                respData.Message = "请求数据有误";
                return false;
            }
            if (((reqArgs.Mobile == null) || (reqArgs.Mobile.Length == 0)) && ((reqArgs.OpenId == null) || (reqArgs.OpenId.Length == 0)))
            {
                respData.Code = "1001";
                respData.Message = "手机号或OpenId不能同时为空";
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
                    TGetMemberInfoResponse response = null;
                    bool IsMobileQuery = false;
                    DateTime serverTime = DbUtils.GetDbServerTime(cmd);
                    sql.Length = 0;
                    sql.Append("select a.MEMBERID,a.MOBILE,a.MEMBERNAME,a.GENDER,a.BIRTHDAY,a.IDNO,a.ADDRESS");
                    if ((reqArgs.Mobile != null) && (reqArgs.Mobile.Length > 0))
                    {
                        sql.Append(" from MEMBERINFO a where MOBILE = ").Append(DbUtils.SpellSqlParameter(conn, "MOBILE"));
                        IsMobileQuery = true;
                    }
                    else
                    {
                        sql.Append(",b.OPENID,b.UNIONID from MEMBERINFO a,MEMBER_WECHAT b where a.MEMBERID=b.MEMBERID ");
                        sql.Append(" and OPENID = ").Append(DbUtils.SpellSqlParameter(conn, "OPENID"));
                    }
                    cmd.CommandText = sql.ToString();
                    if ((reqArgs.Mobile != null) && (reqArgs.Mobile.Length > 0))
                        DbUtils.AddStrInputParameterAndValue(cmd, 15, "MOBILE", reqArgs.Mobile);
                    else
                        DbUtils.AddStrInputParameterAndValue(cmd, 60, "OPENID", reqArgs.OpenId);
                    DbDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        response = new TGetMemberInfoResponse();
                        response.MemberId = DbUtils.GetString(reader, 0);
                        response.Mobile = DbUtils.GetString(reader, 1);
                        response.MemberName = DbUtils.GetString(reader, 2,ServerPlatform.Config.DbCharSetIsNotChinese, 20);
                        if (!reader.IsDBNull(3))
                            response.Gender = DbUtils.GetInt(reader,3);
                        if (reader.IsDBNull(4))
                            response.BirthDay = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 4));
                        response.IDNo = DbUtils.GetString(reader, 5);
                        response.Adress = DbUtils.GetString(reader, 6, ServerPlatform.Config.DbCharSetIsNotChinese, 100);
                        if (!IsMobileQuery)
                        {
                            response.OpenId = DbUtils.GetString(reader, 7);
                            response.UnionId = DbUtils.GetString(reader, 8);
                        }
                    }
                    reader.Close();
                    cmd.Parameters.Clear();
                    if ((IsMobileQuery) && (response != null))
                    {
                        sql.Length = 0;
                        sql.Append("select UNIONID,OPENID from MEMBER_WECHAT where MEMBERID = ").Append(DbUtils.SpellSqlParameter(conn, "MEMBERID"));
                        cmd.CommandText = sql.ToString();
                        DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERID", response.MemberId);
                        reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            response.OpenId = DbUtils.GetString(reader, 0);
                            response.UnionId = DbUtils.GetString(reader, 1);
                        }
                        reader.Close();
                        cmd.Parameters.Clear();
                    }
                    if (response != null)
                    {
                        sql.Length = 0;
                        sql.Append("select a.VIPID,a.CARDNO,a.CARDTYPE,b.CARDTYPENAME,a.GRADEID,c.GRADENAME from CARDINFO a,CARDTYPEINFO b,VIPGRADEINFO c where a.CARDTYPE = b.CARDTYPEID and a.GRADEID = c.GRADEID");
                        sql.Append(" and a.MEMBERID = ").Append(DbUtils.SpellSqlParameter(conn, "MEMBERID"));
                        cmd.CommandText = sql.ToString();
                        DbUtils.AddStrInputParameterAndValue(cmd, 20, "MEMBERID", response.MemberId);
                        reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            TCardInfo cardInfo = new TCardInfo();
                            if (response.CardInfoList == null)
                                response.CardInfoList = new List<TCardInfo>();
                            response.CardInfoList.Add(cardInfo);
                            cardInfo.CardId = DbUtils.GetInt(reader, 0);
                            cardInfo.CardCode = DbUtils.GetString(reader, 1);
                            cardInfo.CardTypeId = DbUtils.GetInt(reader, 2);
                            cardInfo.CardTypeName = DbUtils.GetString(reader, 3);
                            cardInfo.GradeName = DbUtils.GetString(reader, 5);
                            cardInfo.QRCode = cardInfo.CardCode;
                        }
                        reader.Close();
                        cmd.Parameters.Clear();
                    }
                    respData.Data = response;
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
            return (respData.Code.Equals("0"));
        }

        //public static bool GetVipCardTradeItem(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    GetVipCardTradeItemReq reqArgs = JsonConvert.DeserializeObject<GetVipCardTradeItemReq>(reqData.args.ToString());
        //    if (reqArgs == null)
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }            
        //    if ((reqArgs.BeginDate == null) || (reqArgs.BeginDate.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "BeginDate必须有值";
        //        return false;
        //    }
        //    if ((reqArgs.EndDate == null) || (reqArgs.EndDate.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "EndDate必须有值";
        //        return false;
        //    }
        //    DbConnection conn = DbConnManager.GetDbConnection("CRMDB");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    try
        //    {
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
        //            string mobile = string.Empty;
        //            ReceiptInfoList receiptInfo = new ReceiptInfoList();
        //            //List<vipReceiptInfo> vipReceiptInfoList = new List<vipReceiptInfo>();
        //            List<vipOriginalBillInto> originalBillInfoList = new List<vipOriginalBillInto>();
        //            DateTime beginDate = DateTime.MinValue;
        //            DateTime endDate = DateTime.MinValue;
        //            if ((reqArgs.BeginDate != null) && (reqArgs.BeginDate.Length > 0))
        //            {
        //                beginDate = FormatUtils.ParseDateString(reqArgs.BeginDate);
        //                if (beginDate.CompareTo(DateTime.MinValue) <= 0)
        //                {
        //                    Random rd = new Random();
        //                    string randomStr = rd.Next(1000000, 9999999).ToString();
        //                    respData.responseId = PubUtils.GetResponseId(randomStr);
        //                    respData.errCode = 1;
        //                    respData.errMessage = "BeginDate格式有误";
        //                    return false;
        //                }
        //            }
        //            if ((reqArgs.EndDate != null) && (reqArgs.EndDate.Length > 0))
        //            {
        //                endDate = FormatUtils.ParseDateString(reqArgs.EndDate);
        //                if (endDate.CompareTo(DateTime.MinValue) <= 0)
        //                {
        //                    Random rd = new Random();
        //                    string randomStr = rd.Next(1000000, 9999999).ToString();
        //                    respData.responseId = PubUtils.GetResponseId(randomStr);
        //                    respData.errCode = 1;
        //                    respData.errMessage = "EndDate格式有误";
        //                    return false;
        //                }
        //            }
        //            List<string> ServerBillIdList = new List<string>();
        //            List<string> Ids = new List<string>();
        //            string Id = string.Empty;
        //            int iCount = 1;
        //            #region 查询实时消费明细
        //            #region 查主表
        //            sql.Length = 0;
        //            sql.Append("select a.XFJLID,a.JLBH,a.SKTNO,b.MDMC,a.XFSJ,a.DJLX,a.SKYDM,a.XFJLID_OLD,b.MDDM,a.JZRQ,c.MEMBER_ID ");
        //            sql.Append(" from HYK_XFJL a,MDDY b,HYK_HYXX c where a.MDID = b.MDID and a.HYID = c.HYID(+) ");
        //            //sql.Append(" and a.MEMBER_ID = ").Append(DbUtils.SpellSqlParameter(conn, "HYID"));
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "a.XFSJ", ">=", "RQ1");
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "a.XFSJ", "<", "RQ2");
        //            sql.Append(" and a.STATUS = 1 ");
        //            sql.Append(" and (a.HYID >0 or exists(select 1 from HYK_JYCL where a.XFJLID=XFJLID and STATUS=2 and BJ_YHQ=2))");
        //            sql.Append(" order by a.XFSJ ");//BJ_YHQ select a.YHQCODE from HYK_JYCLITEM_YHQDM a,HYK_JYCL b where a.JYID = b.JYID and b.STATUS = 2 and b.XFJLID =
        //            cmd.CommandText = sql.ToString();
        //            //DbUtils.AddIntInputParameterAndValue(cmd, "HYID", reqArgs.vipId);
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", beginDate);
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", endDate.AddDays(1));
        //            DbDataReader reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                vipReceiptInfo item = new vipReceiptInfo();
        //                receiptInfo.VipCardTradeInfoArray.Add(item);
        //                item.ServerBillId = DbUtils.GetInt(reader, 0);
        //                item.TradeNo = DbUtils.GetInt(reader, 1).ToString();
        //                item.PosId = DbUtils.GetString(reader, 2);
        //                item.StoreName = DbUtils.GetString(reader, 3, ServerPlatform.Config.DbCharSetIsNotChinese, 30);
        //                item.TimeShopping = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 4));
        //                item.TradeType = DbUtils.GetInt(reader, 5);
        //                item.CashierCode = DbUtils.GetString(reader, 6);
        //                int originalServerBillId = DbUtils.GetInt(reader, 7);
        //                if (originalServerBillId > 0)
        //                {
        //                    vipOriginalBillInto billInfo = new vipOriginalBillInto();
        //                    originalBillInfoList.Add(billInfo);
        //                    billInfo.serverBillId = item.ServerBillId;
        //                    billInfo.originalServerBillId = originalServerBillId;
        //                }
        //                item.StoreCode = DbUtils.GetString(reader, 8);
        //                item.AccountDate = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 9));
        //                item.MemberId = DbUtils.GetString(reader, 10);
        //                if (Id.Length == 0)
        //                {
        //                    Id = item.ServerBillId.ToString();
        //                }
        //                else
        //                {
        //                    Id = Id + "," + item.ServerBillId.ToString();
        //                }
        //                if (iCount == 5)
        //                {
        //                    Ids.Add(Id);
        //                    iCount = 1;
        //                    Id = string.Empty;
        //                }
        //                else
        //                    iCount++;
        //                ServerBillIdList.Add(item.ServerBillId.ToString());
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();
        //            //if ((iCount > 1) && (iCount < 5) && (Id.Length > 0))
        //            if (Id.Length > 0)
        //            {
        //                Ids.Add(Id);
        //            }
        //            #endregion
        //            #region 查商品销售表、付款方式、优惠券
        //            for (int i = 0; i < Ids.Count; i++)
        //            {
        //                sql.Length = 0;
        //                sql.Append("select a.XFJLID,a.INX,b.SPMC,a.XSSL,a.XSJE,a.XSJE_JF,a.SPDM ");
        //                sql.Append(" from HYK_XFJL_SP a,SHSPXX b,SHSPSB c ");
        //                sql.Append(" where a.SHSPID = b.SHSPID ");
        //                sql.Append(" and b.SHSBID = c.SHSBID ");
        //                sql.Append(" and a.XFJLID in (").Append(Ids[i]).Append(")");
        //                sql.Append(" order by a.XFJLID,a.INX ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].GoodsItems == null)
        //                                receiptInfo.VipCardTradeInfoArray[j].GoodsItems = new List<vipReceiptArticleInfo>();
        //                            vipReceiptArticleInfo article = new vipReceiptArticleInfo();
        //                            receiptInfo.VipCardTradeInfoArray[j].GoodsItems.Add(article);
        //                            article.Inx = DbUtils.GetInt(reader, 1);
        //                            article.GoodsName = DbUtils.GetString(reader, 2, ServerPlatform.Config.DbCharSetIsNotChinese, 60);
        //                            article.Quantity = DbUtils.GetDouble(reader, 3);
        //                            article.Amount = DbUtils.GetDouble(reader, 4);
        //                            article.AmountForPoints = DbUtils.GetDouble(reader, 5);
        //                            article.GoodsCode = DbUtils.GetString(reader, 6);
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();

        //                sql.Length = 0;
        //                sql.Append("select a.XFJLID,a.INX,b.ZFFSMC,a.JE,b.ZFFSDM ");
        //                sql.Append(" from HYK_XFJL_ZFFS a,SHZFFS b ");
        //                sql.Append(" where a.ZFFSID = b.SHZFFSID ");
        //                sql.Append(" and a.XFJLID in (").Append(Ids[i]).Append(")");
        //                sql.Append(" order by a.XFJLID,a.INX ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].PaymentItems == null)
        //                                receiptInfo.VipCardTradeInfoArray[j].PaymentItems = new List<vipReceiptPaymentInfo>();
        //                            vipReceiptPaymentInfo payment = new vipReceiptPaymentInfo();
        //                            receiptInfo.VipCardTradeInfoArray[j].PaymentItems.Add(payment);
        //                            payment.Inx = DbUtils.GetInt(reader, 1);
        //                            payment.PaymentName = DbUtils.GetString(reader, 2, ServerPlatform.Config.DbCharSetIsNotChinese, 30);
        //                            payment.PayMoney = DbUtils.GetDouble(reader, 3).ToString("f2");
        //                            payment.PaymentCode = DbUtils.GetString(reader, 4);
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();

        //                sql.Length = 0;
        //                sql.Append("select c.XFJLID,c.INX,b.ZFFSDM,c.BANK_KH from HYK_XFJL_YHKZF c,SHZFFS b where c.SHZFFSID = b.SHZFFSID and c.XFJLID in (").Append(Ids[i]).Append(")");
        //                sql.Append(" order by c.XFJLID,c.INX ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    string payId = DbUtils.GetString(reader, 2);
        //                    string bankCardID = DbUtils.GetString(reader, 3);

        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].PaymentItems != null)
        //                            {
        //                                foreach (vipReceiptPaymentInfo payment in receiptInfo.VipCardTradeInfoArray[j].PaymentItems)
        //                                {
        //                                    if (payment.PaymentCode.Equals(payId))
        //                                    {
        //                                        if ((payment.BankCardNo != null) && (payment.BankCardNo.Length > 0))
        //                                        {
        //                                            payment.BankCardNo = payment.BankCardNo + ";" + bankCardID;
        //                                        }
        //                                        else
        //                                            payment.BankCardNo = bankCardID;
        //                                        break;

        //                                    }
        //                                }
        //                            }
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();

        //                sql.Length = 0;
        //                sql.Append("select a.XFJLID,a.YHQID,b.YHQMC,a.YHQMZ,a.YHQCODE ");
        //                sql.Append(" from HYK_XFJL_FQDM a,YHQDEF b ");
        //                sql.Append(" where a.YHQID = b.YHQID ");
        //                sql.Append(" and a.XFJLID in (").Append(Ids[i]).Append(")");
        //                sql.Append(" order by a.XFJLID,a.YHQID ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems == null)
        //                                receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems = new List<vipReceiptOfferCouponInfo>();
        //                            vipReceiptOfferCouponInfo offerCoupon = new vipReceiptOfferCouponInfo();
        //                            receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems.Add(offerCoupon);
        //                            offerCoupon.CouponName = DbUtils.GetString(reader, 2, ServerPlatform.Config.DbCharSetIsNotChinese, 30);
        //                            offerCoupon.OfferMoney = DbUtils.GetDouble(reader, 3);
        //                            offerCoupon.CouponId = DbUtils.GetString(reader, 4);
        //                            offerCoupon.Inx = offerCoupon.Inx + 1;
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();

        //                //2020.05.19
        //                sql.Length = 0;
        //                sql.Append("select b.XFJLID,a.YHQID,c.YHQMC,a.YHQMZ,a.YHQCODE ");
        //                sql.Append(" from HYK_JYCLITEM_YHQDM a,HYK_JYCL b,YHQDEF c ");
        //                sql.Append(" where a.JYID = b.JYID and a.YHQID = c.YHQID and b.STATUS = 2 ");
        //                sql.Append(" and b.XFJLID in (").Append(Ids[i]).Append(")");
        //                sql.Append(" order by b.XFJLID,a.YHQID ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems == null)
        //                                receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems = new List<vipReceiptOfferCouponInfo>();
        //                            vipReceiptOfferCouponInfo offerCoupon = new vipReceiptOfferCouponInfo();
        //                            receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems.Add(offerCoupon);
        //                            offerCoupon.CouponName = DbUtils.GetString(reader, 2, ServerPlatform.Config.DbCharSetIsNotChinese, 30);
        //                            offerCoupon.OfferMoney = DbUtils.GetDouble(reader, 3);
        //                            offerCoupon.CouponId = DbUtils.GetString(reader, 4);
        //                            offerCoupon.TypeId = 1;
        //                            offerCoupon.Inx = offerCoupon.Inx + 1;
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();
        //            }
        //            #endregion
        //            #endregion
        //            #region 查询历史消费明细
        //            #region 查主表
        //            Ids.Clear();
        //            Id = string.Empty;
        //            iCount = 1;
        //            sql.Length = 0;
        //            sql.Append("select a.XFJLID,a.JLBH,a.SKTNO,b.MDMC,a.XFSJ,a.DJLX,a.SKYDM,a.XFJLID_OLD,b.MDDM,a.JZRQ,c.MEMBER_ID ");
        //            sql.Append(" from HYXFJL a,MDDY b,HYK_HYXX c where a.MDID = b.MDID and a.HYID = c.HYID(+) ");
        //            //sql.Append(" and a.MEMBER_ID = ").Append(DbUtils.SpellSqlParameter(conn, "HYID"));
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "a.XFSJ", ">=", "RQ1");
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "a.XFSJ", "<", "RQ2");
        //            sql.Append(" and (a.HYID >0 or exists(select 1 from HYK_JYCL where a.XFJLID=XFJLID and STATUS=2 and BJ_YHQ=2))");
        //            sql.Append(" order by a.XFSJ ");
        //            cmd.CommandText = sql.ToString();
        //            //DbUtils.AddIntInputParameterAndValue(cmd, "HYID", reqArgs.vipId);
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", beginDate);
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", endDate.AddDays(1));
        //            reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                vipReceiptInfo item = new vipReceiptInfo();
        //                item.ServerBillId = DbUtils.GetInt(reader, 0);
        //                if (!ServerBillIdList.Contains(item.ServerBillId.ToString()))
        //                {
        //                    item.TradeNo = DbUtils.GetInt(reader, 1).ToString();
        //                    item.PosId = DbUtils.GetString(reader, 2);
        //                    item.StoreName = DbUtils.GetString(reader, 3, ServerPlatform.Config.DbCharSetIsNotChinese, 30);
        //                    item.TimeShopping = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 4));
        //                    item.TradeType = DbUtils.GetInt(reader, 5);
        //                    item.CashierCode = DbUtils.GetString(reader, 6);
        //                    int originalServerBillId = DbUtils.GetInt(reader, 7);
        //                    if (originalServerBillId > 0)
        //                    {
        //                        vipOriginalBillInto billInfo = new vipOriginalBillInto();
        //                        originalBillInfoList.Add(billInfo);
        //                        billInfo.serverBillId = item.ServerBillId;
        //                        billInfo.originalServerBillId = originalServerBillId;
        //                    }
        //                    item.StoreCode = DbUtils.GetString(reader, 8);
        //                    item.AccountDate = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 9));
        //                    item.MemberId = DbUtils.GetString(reader, 10);
        //                    if (Id.Length == 0)
        //                    {
        //                        Id = item.ServerBillId.ToString();
        //                    }
        //                    else
        //                    {
        //                        Id = Id + "," + item.ServerBillId.ToString();
        //                    }
        //                    if (iCount == 5)
        //                    {
        //                        Ids.Add(Id);
        //                        iCount = 1;
        //                        Id = string.Empty;
        //                    }
        //                    else
        //                        iCount++;
        //                    receiptInfo.VipCardTradeInfoArray.Add(item);
        //                }
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();
        //            //if ((iCount > 1) && (iCount <= 5) && (Id.Length > 0))
        //            if (Id.Length > 0)
        //            {
        //                Ids.Add(Id);
        //            }
        //            #endregion
        //            #region 查商品销售表、付款方式、优惠券
        //            for (int i = 0; i < Ids.Count; i++)
        //            {
        //                sql.Length = 0;
        //                sql.Append("select a.XFJLID,a.INX,b.SPMC,a.XSSL,a.XSJE,a.XSJE_JF,a.SPDM ");
        //                sql.Append(" from HYXFJL_SP a,SHSPXX b,SHSPSB c ");
        //                sql.Append(" where a.SHSPID = b.SHSPID ");
        //                sql.Append(" and b.SHSBID = c.SHSBID ");
        //                sql.Append(" and a.XFJLID in (").Append(Ids[i]).Append(")");
        //                sql.Append(" order by a.XFJLID,a.INX ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].GoodsItems == null)
        //                                receiptInfo.VipCardTradeInfoArray[j].GoodsItems = new List<vipReceiptArticleInfo>();
        //                            vipReceiptArticleInfo article = new vipReceiptArticleInfo();
        //                            receiptInfo.VipCardTradeInfoArray[j].GoodsItems.Add(article);
        //                            article.Inx = DbUtils.GetInt(reader, 1);
        //                            article.GoodsName = DbUtils.GetString(reader, 2, ServerPlatform.Config.DbCharSetIsNotChinese, 60);
        //                            article.Quantity = DbUtils.GetDouble(reader, 3);
        //                            article.Amount = DbUtils.GetDouble(reader, 4);
        //                            article.AmountForPoints = DbUtils.GetDouble(reader, 5);
        //                            article.GoodsCode = DbUtils.GetString(reader, 6);
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();

        //                sql.Length = 0;
        //                sql.Append("select a.XFJLID,a.INX,b.ZFFSMC,a.JE,b.ZFFSDM ");
        //                sql.Append(" from HYXFJL_ZFFS a,SHZFFS b ");
        //                sql.Append(" where a.ZFFSID = b.SHZFFSID ");
        //                sql.Append(" and a.XFJLID in (").Append(Ids[i]).Append(")");
        //                sql.Append(" order by a.XFJLID,a.INX ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].PaymentItems == null)
        //                                receiptInfo.VipCardTradeInfoArray[j].PaymentItems = new List<vipReceiptPaymentInfo>();
        //                            vipReceiptPaymentInfo payment = new vipReceiptPaymentInfo();
        //                            receiptInfo.VipCardTradeInfoArray[j].PaymentItems.Add(payment);
        //                            payment.Inx = DbUtils.GetInt(reader, 1);
        //                            payment.PaymentName = DbUtils.GetString(reader, 2, ServerPlatform.Config.DbCharSetIsNotChinese, 30);
        //                            payment.PayMoney = DbUtils.GetDouble(reader, 3).ToString("f2");
        //                            payment.PaymentCode = DbUtils.GetString(reader, 4);
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();

        //                sql.Length = 0;
        //                sql.Append("select c.XFJLID,c.INX,b.ZFFSDM,c.BANK_KH from HYXFJL_YHKZF c,SHZFFS b where c.SHZFFSID = b.SHZFFSID and c.XFJLID in (").Append(Ids[i]).Append(")");
        //                sql.Append(" order by c.XFJLID,c.INX ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    string payId = DbUtils.GetString(reader, 2);
        //                    string bankCardID = DbUtils.GetString(reader, 3);

        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].PaymentItems != null)
        //                            {
        //                                foreach (vipReceiptPaymentInfo payment in receiptInfo.VipCardTradeInfoArray[j].PaymentItems)
        //                                {
        //                                    if (payment.PaymentCode.Equals(payId))
        //                                    {
        //                                        if ((payment.BankCardNo != null) && (payment.BankCardNo.Length > 0))
        //                                        {
        //                                            payment.BankCardNo = payment.BankCardNo + ";" + bankCardID;
        //                                        }
        //                                        else
        //                                            payment.BankCardNo = bankCardID;
        //                                        break;

        //                                    }
        //                                }
        //                            }
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();

        //                sql.Length = 0;
        //                sql.Append("select a.XFJLID,a.YHQID,b.YHQMC,a.YHQMZ,a.YHQCODE ");
        //                sql.Append(" from HYXFJL_FQDM a,YHQDEF b ");
        //                sql.Append(" where a.YHQID = b.YHQID ");
        //                sql.Append(" and a.XFJLID in (").Append(Ids[i]).Append(")");//HYK_XFJL_FQDM(XFJLID,YHQCODE,YHQID,YHQMZ,CXHDBH)
        //                sql.Append(" order by a.XFJLID,a.YHQID ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems == null)
        //                                receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems = new List<vipReceiptOfferCouponInfo>();
        //                            vipReceiptOfferCouponInfo offerCoupon = new vipReceiptOfferCouponInfo();
        //                            receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems.Add(offerCoupon);
        //                            offerCoupon.CouponName = DbUtils.GetString(reader, 2, ServerPlatform.Config.DbCharSetIsNotChinese, 30);
        //                            offerCoupon.OfferMoney = DbUtils.GetDouble(reader, 3);
        //                            offerCoupon.CouponId = DbUtils.GetString(reader, 4);
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();

        //                //2020.05.19
        //                sql.Length = 0;
        //                sql.Append("select b.XFJLID,a.YHQID,c.YHQMC,a.YHQMZ,a.YHQCODE ");
        //                sql.Append(" from HYK_JYCLITEM_YHQDM a,HYK_JYCL b,YHQDEF c ");
        //                sql.Append(" where a.JYID = b.JYID and a.YHQID = c.YHQID and b.STATUS = 2 ");
        //                sql.Append(" and b.XFJLID in (").Append(Ids[i]).Append(")");
        //                sql.Append(" order by b.XFJLID,a.YHQID ");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    int serverBillId = DbUtils.GetInt(reader, 0);
        //                    for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                    {
        //                        if (serverBillId == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                        {
        //                            if (receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems == null)
        //                                receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems = new List<vipReceiptOfferCouponInfo>();
        //                            vipReceiptOfferCouponInfo offerCoupon = new vipReceiptOfferCouponInfo();
        //                            receiptInfo.VipCardTradeInfoArray[j].OfferCouponItems.Add(offerCoupon);
        //                            offerCoupon.CouponName = DbUtils.GetString(reader, 2, ServerPlatform.Config.DbCharSetIsNotChinese, 30);
        //                            offerCoupon.OfferMoney = DbUtils.GetDouble(reader, 3);
        //                            offerCoupon.CouponId = DbUtils.GetString(reader, 4);
        //                            offerCoupon.TypeId = 1;
        //                            offerCoupon.Inx = offerCoupon.Inx + 1;
        //                            break;
        //                        }
        //                    }
        //                }
        //                reader.Close();
        //            }
        //            #endregion
        //            #endregion

        //            #region 查询原单交易
        //            if (originalBillInfoList.Count > 0)
        //            {
        //                sql.Length = 0;
        //                sql.Append("select XFJLID,SKTNO,JLBH from HYK_XFJL where XFJLID in (").Append(originalBillInfoList[0].originalServerBillId);
        //                for (int i = 1; i < originalBillInfoList.Count; i++)
        //                {
        //                    sql.Append(",").Append(originalBillInfoList[i].originalServerBillId);
        //                }
        //                sql.Append(")");
        //                sql.Append(" union ");
        //                sql.Append("select XFJLID,SKTNO,JLBH from HYXFJL where XFJLID in (").Append(originalBillInfoList[0].originalServerBillId);
        //                for (int i = 1; i < originalBillInfoList.Count; i++)
        //                {
        //                    sql.Append(",").Append(originalBillInfoList[i].originalServerBillId);
        //                }
        //                sql.Append(")");
        //                sql.Append(" order by XFJLID");
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    //bool isFound = false;
        //                    int originalServerBillId = DbUtils.GetInt(reader, 0);
        //                    string originalPosId = DbUtils.GetString(reader, 1);
        //                    int originalTradeNo = DbUtils.GetInt(reader, 2);
        //                    //int serverBillId = 0;
        //                    List<int> serverBillIds = new List<int>();
        //                    for (int i = 0; i < originalBillInfoList.Count; i++)
        //                    {
        //                        if (originalServerBillId == originalBillInfoList[i].originalServerBillId)
        //                        {
        //                            //serverBillId = originalBillInfoList[i].serverBillId;
        //                            serverBillIds.Add(originalBillInfoList[i].serverBillId);
        //                            //isFound = true;
        //                            //break;
        //                        }
        //                    }
        //                    //if (isFound)
        //                    if (serverBillIds.Count > 0)
        //                    {
        //                        for (int j = 0; j < receiptInfo.VipCardTradeInfoArray.Count; j++)
        //                        {
        //                            for (int i = 0; i < serverBillIds.Count; i++)
        //                            {
        //                                if (serverBillIds[i] == receiptInfo.VipCardTradeInfoArray[j].ServerBillId)
        //                                {
        //                                    if (receiptInfo.VipCardTradeInfoArray[j].OriginalTradeInfo == null)
        //                                    {
        //                                        receiptInfo.VipCardTradeInfoArray[j].OriginalTradeInfo = new vipOriginalReceiptInfo();
        //                                        receiptInfo.VipCardTradeInfoArray[j].OriginalTradeInfo.OriginalPosId = originalPosId;
        //                                        receiptInfo.VipCardTradeInfoArray[j].OriginalTradeInfo.OriginalTradeNo = originalTradeNo.ToString();
        //                                    }
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //                reader.Close();
        //            }
        //            #endregion
        //            respData.result = receiptInfo;
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool UploadVipCardGoodsDiscRule(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    VipCardDiscReq reqArgs = JsonConvert.DeserializeObject<VipCardDiscReq>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.DiscRuleInfoArray == null))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    DbConnection conn = DbConnManager.GetDbConnection("CRMDB");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    try
        //    {
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
        //            int crmVipIdCount = 0;
        //            foreach (VipCardDiscBillInfo info in reqArgs.DiscRuleInfoArray)
        //            {
        //                string msg = string.Empty;
        //                if ((msg.Length == 0) && ((info.RuleId == null) || (info.RuleId.Length == 0)))
        //                {
        //                    msg = "RuleId必须有值";
        //                }
        //                if ((msg.Length == 0) && (info.CardTypeId <= 0))
        //                {
        //                    msg = "CardTypeId必须有值";
        //                }
        //                if ((msg.Length == 0) && ((info.BeginDate == null) || (info.BeginDate.Length == 0)))
        //                {
        //                    msg = "BeginDate必须有值";
        //                }
        //                if ((msg.Length == 0) && ((info.EndDate == null) || (info.EndDate.Length == 0)))
        //                {
        //                    msg = "EndDate必须有值";
        //                }

        //                if ((msg.Length == 0) && (info.ItemDiscRuleInfoArray == null))
        //                {
        //                    msg = "ItemDiscRuleInfoArray必须有值";
        //                }
        //                if ((msg.Length == 0) && (info.ItemDiscRuleInfoArray.Count == 0))
        //                {
        //                    msg = "ItemDiscRuleInfoArray必须有值";
        //                }

        //                if (msg.Length == 0)
        //                {
        //                    sql.Length = 0;
        //                    sql.Append("select 1 from HYKDEF where HYKTYPE = ").Append(DbUtils.SpellSqlParameter(conn, "HYKTYPE"));
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddIntInputParameterAndValue(cmd, "HYKTYPE", info.CardTypeId.ToString());
        //                    DbDataReader reader = cmd.ExecuteReader();
        //                    if (!reader.Read())
        //                    {
        //                        msg = "CardTypeId不存在";
        //                    }
        //                    reader.Close();
        //                    cmd.Parameters.Clear();
        //                }
        //                if (msg.Length > 0)
        //                {
        //                    Random rd = new Random();
        //                    string randomStr = rd.Next(1000000, 9999999).ToString();
        //                    respData.responseId = PubUtils.GetResponseId(randomStr);
        //                    respData.errCode = 1;
        //                    respData.errMessage = msg;
        //                    return false;
        //                }
        //            }
        //            int crmVipId = 0;
        //            if (crmVipIdCount > 0)
        //                crmVipId = PubUtils.GetSeq("CRMDB", 0, "HYK_HYXX", crmVipIdCount) - crmVipIdCount + 1;
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            DbTransaction dbTrans = conn.BeginTransaction();
        //            try
        //            {
        //                cmd.Transaction = dbTrans;
        //                foreach (VipCardDiscBillInfo info in reqArgs.DiscRuleInfoArray)
        //                {
        //                    sql.Length = 0;
        //                    sql.Append("update CRM_HYKZKDYD set HYKTYPE = ").Append(DbUtils.SpellSqlParameter(conn, "HYKTYPE"));
        //                    sql.Append(",RQ1 = ").Append(DbUtils.SpellSqlParameter(conn, "RQ1"));
        //                    sql.Append(",RQ2 = ").Append(DbUtils.SpellSqlParameter(conn, "RQ2"));
        //                    sql.Append(",GXSJ = ").Append(DbUtils.SpellSqlParameter(conn, "GXSJ"));
        //                    sql.Append(" where RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddIntInputParameterAndValue(cmd, "HYKTYPE", info.CardTypeId.ToString());
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", FormatUtils.ParseDatetimeString(info.BeginDate));
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", FormatUtils.ParseDatetimeString(info.EndDate));
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "GXSJ", serverTime);
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", info.RuleId);
        //                    if (cmd.ExecuteNonQuery() == 0)
        //                    {
        //                        cmd.Parameters.Clear();
        //                        sql.Length = 0;
        //                        sql.Append("insert into CRM_HYKZKDYD(RULE_ID,HYKTYPE,RQ1,RQ2,GXSJ) ");
        //                        sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "HYKTYPE"));
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "RQ1"));
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "RQ2"));
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "GXSJ"));
        //                        sql.Append(")");
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", info.RuleId);
        //                        DbUtils.AddIntInputParameterAndValue(cmd, "HYKTYPE", info.CardTypeId.ToString());
        //                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", FormatUtils.ParseDatetimeString(info.BeginDate));
        //                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", FormatUtils.ParseDatetimeString(info.EndDate));
        //                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "GXSJ", serverTime);
        //                        cmd.ExecuteNonQuery();
        //                    }
        //                    cmd.Parameters.Clear();
        //                    foreach (VipCardDiscBillItem article in info.ItemDiscRuleInfoArray)
        //                    {
        //                        if ((article.ItemCode == null) || (article.ItemCode.Length == 0))
        //                        {
        //                            dbTrans.Rollback();
        //                            Random rd = new Random();
        //                            string randomStr = rd.Next(1000000, 9999999).ToString();
        //                            respData.responseId = PubUtils.GetResponseId(randomStr);
        //                            respData.errCode = 1;
        //                            respData.errMessage = "ItemCode不能为空";
        //                            return false;
        //                        }

        //                        sql.Length = 0;
        //                        sql.Append("update CRM_HYKZKDYD_ITEM set ZKL = ").Append(DbUtils.SpellSqlParameter(conn, "ZKL"));
        //                        sql.Append(" where RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                        sql.Append(" and SPDM = ").Append(DbUtils.SpellSqlParameter(conn, "SPDM"));
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddDoubleInputParameterAndValue(cmd, "ZKL", article.DiscRate.ToString("f4"));
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", info.RuleId);
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 13, "SPDM", article.ItemCode);
        //                        if (cmd.ExecuteNonQuery() == 0)
        //                        {
        //                            cmd.Parameters.Clear();
        //                            sql.Length = 0;
        //                            sql.Append("insert into CRM_HYKZKDYD_ITEM(RULE_ID,SPDM,ZKL,SHSPID) ");
        //                            sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "SPDM"));
        //                            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "ZKL"));
        //                            sql.Append(",0");
        //                            sql.Append(")");
        //                            cmd.CommandText = sql.ToString();
        //                            DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", info.RuleId);
        //                            DbUtils.AddStrInputParameterAndValue(cmd, 13, "SPDM", article.ItemCode);
        //                            DbUtils.AddDoubleInputParameterAndValue(cmd, "ZKL", article.DiscRate.ToString("f4"));
        //                            cmd.ExecuteNonQuery();
        //                        }
        //                        cmd.Parameters.Clear();
        //                    }
        //                    cmd.Parameters.Clear();
        //                    sql.Length = 0;
        //                    sql.Append("update CRM_HYKZKDYD_ITEM a set SHSPID=(select SHSPID from SHSPXX where SHDM = 'BH' and SPDM = a.SPDM)");
        //                    sql.Append(" where exists(select 1 from SHSPXX where SHDM = 'BH' and SPDM = a.SPDM)");
        //                    sql.Append(" and RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    sql.Append(" and SHSPID = 0 ");
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", info.RuleId);
        //                    cmd.ExecuteNonQuery();
        //                    cmd.Parameters.Clear();
        //                }
        //                dbTrans.Commit();
        //                CrmDataResponse response = new CrmDataResponse();
        //                response.remark = "上传成功";
        //                respData.result = response;
        //            }
        //            catch (Exception e)
        //            {
        //                dbTrans.Rollback();
        //                throw e;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool UploadVipCardTypeInfo(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    VipCardTypeInfo reqArgs = JsonConvert.DeserializeObject<VipCardTypeInfo>(reqData.args.ToString());
        //    if (reqArgs == null)
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    string msg = string.Empty;
        //    if (reqArgs.CardTypeId <= 0)
        //    {
        //        msg = "CardTypeId必须有值";
        //    }
        //    if ((msg.Length == 0) && ((reqArgs.CardTypeName == null) || (reqArgs.CardTypeName.Length == 0)))
        //    {
        //        msg = "CardTypeName必须有值";
        //    }
        //    if (msg.Length > 0)
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = msg;
        //        return false;
        //    }
        //    DbConnection conn = DbConnManager.GetDbConnection("CRMDB");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    try
        //    {
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
        //            DbTransaction dbTrans = conn.BeginTransaction();
        //            try
        //            {
        //                cmd.Transaction = dbTrans;
        //                sql.Length = 0;
        //                sql.Append("update HYKDEF set HYKNAME = ").Append(DbUtils.SpellSqlParameter(conn, "HYKNAME"));
        //                sql.Append(" where HYKTYPE = ").Append(DbUtils.SpellSqlParameter(conn, "HYKTYPE"));
        //                cmd.CommandText = sql.ToString();
        //                DbUtils.AddStrInputParameterAndValue(cmd, 20, "HYKNAME", reqArgs.CardTypeName);
        //                DbUtils.AddIntInputParameterAndValue(cmd, "HYKTYPE", reqArgs.CardTypeId.ToString());
        //                if (cmd.ExecuteNonQuery() == 0)
        //                {
        //                    cmd.Parameters.Clear();
        //                    sql.Length = 0;
        //                    sql.Append("insert into HYKDEF(HYKTYPE,HYKKZID,HYKNAME,FXFS,HMCD,BJ_PSW,BJ_XSJL,BJ_JF,BJ_YHQZH,BJ_CZZH,BJ_CZK,YHFS,YXQCD) ");
        //                    sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "HYKTYPE"));
        //                    sql.Append(",1");
        //                    sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "HYKNAME"));
        //                    sql.Append(",0,10,0,1,1,1,1,0,1,'30Y'");
        //                    sql.Append(")");
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddIntInputParameterAndValue(cmd, "HYKTYPE", reqArgs.CardTypeId.ToString());
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 20, "HYKNAME", reqArgs.CardTypeName);
        //                    cmd.ExecuteNonQuery();
        //                }
        //                cmd.Parameters.Clear();
        //                dbTrans.Commit();
        //                CrmDataResponse response = new CrmDataResponse();
        //                response.remark = "上传成功";
        //                respData.result = response;
        //            }
        //            catch (Exception e)
        //            {
        //                dbTrans.Rollback();
        //                throw e;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool GetNonMemberTradeItem(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    GetVipCardTradeItemReq2 reqArgs = JsonConvert.DeserializeObject<GetVipCardTradeItemReq2>(reqData.args.ToString());
        //    if (reqArgs == null)
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    if ((reqArgs.BeginDate == null) || (reqArgs.BeginDate.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "BeginDate必须有值";
        //        return false;
        //    }
        //    if ((reqArgs.EndDate == null) || (reqArgs.EndDate.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "EndDate必须有值";
        //        return false;
        //    }
        //    if (reqArgs.Page < 1)
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "Page必须为大于1的整数";
        //        return false;
        //    }
        //    if ((reqArgs.PageSize < 1) || (reqArgs.PageSize > 1000))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "PageSize必须为1至1000的整数";
        //        return false;
        //    }
        //    int rowNum1 = (reqArgs.Page - 1) * reqArgs.PageSize + 1;
        //    int rowNum2 = reqArgs.Page * reqArgs.PageSize;

        //    DbConnection conn = DbConnManager.GetDbConnection("ERP");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    try
        //    {
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
        //            string mobile = string.Empty;
        //            TradeInfoList receiptInfo = new TradeInfoList();
        //            List<vipOriginalBillInto> originalBillInfoList = new List<vipOriginalBillInto>();
        //            DateTime beginDate = DateTime.MinValue;
        //            DateTime endDate = DateTime.MinValue;
        //            if ((reqArgs.BeginDate != null) && (reqArgs.BeginDate.Length > 0))
        //            {
        //                beginDate = FormatUtils.ParseDateString(reqArgs.BeginDate);
        //                if (beginDate.CompareTo(DateTime.MinValue) <= 0)
        //                {
        //                    Random rd = new Random();
        //                    string randomStr = rd.Next(1000000, 9999999).ToString();
        //                    respData.responseId = PubUtils.GetResponseId(randomStr);
        //                    respData.errCode = 1;
        //                    respData.errMessage = "BeginDate格式有误";
        //                    return false;
        //                }
        //            }
        //            if ((reqArgs.EndDate != null) && (reqArgs.EndDate.Length > 0))
        //            {
        //                endDate = FormatUtils.ParseDateString(reqArgs.EndDate);
        //                if (beginDate.CompareTo(DateTime.MinValue) <= 0)
        //                {
        //                    Random rd = new Random();
        //                    string randomStr = rd.Next(1000000, 9999999).ToString();
        //                    respData.responseId = PubUtils.GetResponseId(randomStr);
        //                    respData.errCode = 1;
        //                    respData.errMessage = "EndDate格式有误";
        //                    return false;
        //                }
        //            }
        //            DbDataReader reader = null;
        //            #region 查询实时消费明细 --改成分页，不再从实时表取，日结后再取
        //            #region 查主表
        //            //sql.Length = 0;
        //            //sql.Append("select a.SKTNO,a.JLBH,a.JYSJ,a.JZRQ,a.XSJE,b.RYDM,a.SKTNO_OLD,a.JLBH_OLD ");
        //            //sql.Append(" from XSJL a,RYXX b where a.SKY=b.PERSON_ID(+)  ");
        //            //sql.Append(" and a.SKTNO not like '5%'");
        //            //if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.SpellSqlParameter(conn, sql, " and ", "a.JZRQ", ">=", "RQ1");
        //            //if (endDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.SpellSqlParameter(conn, sql, " and ", "a.JZRQ", "<", "RQ2");
        //            //sql.Append(" order by a.JYSJ ");
        //            //cmd.CommandText = sql.ToString();
        //            ////DbUtils.AddIntInputParameterAndValue(cmd, "HYID", reqArgs.vipId);
        //            //if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", beginDate);
        //            //if (endDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", endDate.AddDays(1));
        //            //reader = cmd.ExecuteReader();
        //            //while (reader.Read())
        //            //{
        //            //    TradeInfo item = new TradeInfo();
        //            //    receiptInfo.TradeInfoArray.Add(item);
        //            //    item.PosId = DbUtils.GetString(reader, 0);
        //            //    item.TradeNo = DbUtils.GetInt(reader, 1).ToString();
        //            //    item.TimeShopping = FormatUtils.DatetimeToString(DbUtils.GetDateTime(reader, 2));
        //            //    item.AccountDate = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 3));
        //            //    item.TradeStatus = 1;
        //            //    double Amount = DbUtils.GetDouble(reader, 4);
        //            //    if (Amount < 0)
        //            //    {
        //            //        if ((item.PosId != null) && (item.PosId.Length > 0) && (item.PosId.Substring(0, 1).Equals("-")))
        //            //            item.TradeType = 0;
        //            //        else
        //            //            item.TradeType = 1;
        //            //    }
        //            //    else
        //            //    {
        //            //        if ((item.PosId != null) && (item.PosId.Length > 0) && (item.PosId.Substring(0, 1).Equals("-")))
        //            //            item.TradeType = 1;
        //            //    }
        //            //    item.CashierCode = DbUtils.GetString(reader, 5);
        //            //    int originalBillId = DbUtils.GetInt(reader, 7);
        //            //    if (originalBillId > 0)
        //            //    {
        //            //        item.OriginalTradeInfo = new OriginalTradeInfo();
        //            //        item.OriginalTradeInfo.OriginalTradeNo = originalBillId.ToString();
        //            //        item.OriginalTradeInfo.OriginalPosId = DbUtils.GetString(reader, 6);
        //            //    }
        //            //}
        //            //reader.Close();
        //            //cmd.Parameters.Clear();
        //            #endregion
        //            #region 查商品销售表、付款方式
        //            //sql.Length = 0;
        //            //sql.Append("select a.SKTNO,a.JLBH,a.INX,c.SPCODE,c.NAME,a.XSSL,a.XSJE,b.JYSJ ");
        //            //sql.Append(" from XSJLC a,XSJL b,SPXX c where a.SKTNO=b.SKTNO and a.JLBH=b.JLBH and a.SP_ID = c.SP_ID ");
        //            //if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", ">=", "RQ1");
        //            //if (endDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", "<", "RQ2");
        //            //sql.Append(" order by b.JYSJ ");
        //            //cmd.CommandText = sql.ToString();
        //            ////DbUtils.AddIntInputParameterAndValue(cmd, "HYID", reqArgs.vipId);
        //            //if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", beginDate);
        //            //if (endDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", endDate.AddDays(1));
        //            //reader = cmd.ExecuteReader();
        //            //while (reader.Read())
        //            //{
        //            //    string posId = DbUtils.GetString(reader, 0);
        //            //    int billId = DbUtils.GetInt(reader, 1);
        //            //    foreach (TradeInfo info in receiptInfo.TradeInfoArray)
        //            //    {
        //            //        if (info.PosId.Equals(posId) && info.TradeNo.Equals(billId.ToString()))
        //            //        {
        //            //            TradeArticleInfo article = new TradeArticleInfo();
        //            //            info.GoodsItems.Add(article);
        //            //            article.Inx = DbUtils.GetInt(reader, 2);
        //            //            article.GoodsCode = DbUtils.GetString(reader, 3);
        //            //            article.GoodsName = DbUtils.GetString(reader, 4);
        //            //            article.Quantity = DbUtils.GetDouble(reader, 5);
        //            //            article.Amount = DbUtils.GetDouble(reader, 6);
        //            //            break;
        //            //        }
        //            //    }
        //            //}
        //            //reader.Close();
        //            //cmd.Parameters.Clear();
        //            ////select SKTNO,JLBH,SKFS,SKJE from XSJLM_JB;
        //            //int inx = 0;
        //            //sql.Length = 0;
        //            //sql.Append("select a.SKTNO,a.JLBH,a.SKFS,a.SKJE,c.NAME,b.JYSJ ");
        //            //sql.Append(" from XSJLM_JB a,XSJL b,SKFS_JB c where a.SKTNO=b.SKTNO and a.JLBH=b.JLBH and a.SKFS=c.CODE");
        //            //if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", ">=", "RQ1");
        //            //if (endDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", "<", "RQ2");
        //            //sql.Append(" order by b.JYSJ ");
        //            //cmd.CommandText = sql.ToString();
        //            ////DbUtils.AddIntInputParameterAndValue(cmd, "HYID", reqArgs.vipId);
        //            //if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", beginDate);
        //            //if (endDate.CompareTo(DateTime.MinValue) > 0)
        //            //    DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", endDate.AddDays(1));
        //            //reader = cmd.ExecuteReader();
        //            //while (reader.Read())
        //            //{
        //            //    string posId = DbUtils.GetString(reader, 0);
        //            //    int billId = DbUtils.GetInt(reader, 1);
        //            //    foreach (TradeInfo info in receiptInfo.TradeInfoArray)
        //            //    {
        //            //        if (info.PosId.Equals(posId) && info.TradeNo.Equals(billId.ToString()))
        //            //        {
        //            //            TradePaymentInfo payment = new TradePaymentInfo();
        //            //            info.PaymentItems.Add(payment);
        //            //            payment.Inx = inx;
        //            //            payment.PaymentCode = DbUtils.GetInt(reader, 2).ToString();
        //            //            payment.PayMoney = DbUtils.GetDouble(reader, 3).ToString("f2");
        //            //            payment.PaymentName = DbUtils.GetString(reader, 4);
        //            //            break;
        //            //        }
        //            //    }
        //            //    inx ++;
        //            //}
        //            //reader.Close();
        //            //cmd.Parameters.Clear();
        //            #endregion
        //            #endregion
        //            #region 查询历史消费明细
        //            #region 查主表
        //            DateTime lastTradeTime = DateTime.MinValue;
        //            sql.Length = 0;
        //            sql.Append("select SKTNO,JLBH,JYSJ,JZRQ,XSJE,RYDM,SKTNO_OLD,JLBH_OLD from (");
        //            sql.Append("select SKTNO,JLBH,JYSJ,JZRQ,XSJE,RYDM,SKTNO_OLD,JLBH_OLD,rownum as XH from (");
        //            sql.Append("select a.SKTNO,a.JLBH,a.JYSJ,a.JZRQ,a.XSJE,b.RYDM,a.SKTNO_OLD,a.JLBH_OLD ");
        //            sql.Append(" from SKTXSJL a,RYXX b where a.SKY=b.PERSON_ID(+)  ");
        //            sql.Append(" and a.SKTNO not like '5%'");
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "a.JZRQ", ">=", "RQ1");
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "a.JZRQ", "<", "RQ2");
        //            sql.Append(" order by a.JYSJ ) t ) f ");
        //            sql.Append(" where XH >= ").Append(rowNum1);
        //            sql.Append(" and XH <= ").Append(rowNum2);
        //            cmd.CommandText = sql.ToString();
        //            //DbUtils.AddIntInputParameterAndValue(cmd, "HYID", reqArgs.vipId);
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", beginDate);
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", endDate.AddDays(1));
        //            reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                TradeInfo item = new TradeInfo();
        //                receiptInfo.TradeInfoArray.Add(item);
        //                item.PosId = DbUtils.GetString(reader, 0);
        //                item.TradeNo = DbUtils.GetInt(reader, 1).ToString();
        //                DateTime TimeShopping = DbUtils.GetDateTime(reader, 2);
        //                item.TimeShopping = FormatUtils.DatetimeToString(TimeShopping);
        //                item.AccountDate = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 3));
        //                double Amount = DbUtils.GetDouble(reader, 4);
        //                if (Amount < 0)
        //                {
        //                    if ((item.PosId != null) && (item.PosId.Length > 0) && (item.PosId.Substring(0, 1).Equals("-")))
        //                        item.TradeType = 0;
        //                    else
        //                        item.TradeType = 1;
        //                }
        //                else
        //                {
        //                    if ((item.PosId != null) && (item.PosId.Length > 0) && (item.PosId.Substring(0, 1).Equals("-")))
        //                        item.TradeType = 1;
        //                }
        //                item.CashierCode = DbUtils.GetString(reader, 5);
        //                int originalBillId = DbUtils.GetInt(reader, 7);
        //                if (originalBillId > 0)
        //                {
        //                    item.OriginalTradeInfo = new OriginalTradeInfo();
        //                    item.OriginalTradeInfo.OriginalTradeNo = originalBillId.ToString();
        //                    item.OriginalTradeInfo.OriginalPosId = DbUtils.GetString(reader, 6);
        //                }
        //                if (TimeShopping.CompareTo(lastTradeTime) > 0)
        //                    lastTradeTime = TimeShopping;
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();
        //            #endregion
        //            #region 查商品销售表、付款方式
        //            sql.Length = 0;
        //            sql.Append("select a.SKTNO,a.JLBH,a.INX,c.SPCODE,c.NAME,a.XSSL,a.XSJE,b.JYSJ ");
        //            sql.Append(" from SKTXSJLC a,SKTXSJL b,SPXX c where a.SKTNO=b.SKTNO and a.JLBH=b.JLBH and a.SP_ID = c.SP_ID ");
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", ">=", "RQ1");
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", "<", "RQ2");
        //            if (lastTradeTime.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JYSJ", "<=", "JYSJ");
        //            sql.Append(" order by b.JYSJ ");
        //            cmd.CommandText = sql.ToString();
        //            //DbUtils.AddIntInputParameterAndValue(cmd, "HYID", reqArgs.vipId);
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", beginDate);
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", endDate.AddDays(1));
        //            if (lastTradeTime.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "JYSJ", lastTradeTime.AddSeconds(1));//多一秒，可能会多几笔交易，但是添加列表时做了判断
        //            reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                string posId = DbUtils.GetString(reader, 0);
        //                int billId = DbUtils.GetInt(reader, 1);
        //                foreach (TradeInfo info in receiptInfo.TradeInfoArray)
        //                {
        //                    if (info.PosId.Equals(posId) && info.TradeNo.Equals(billId.ToString()))
        //                    {
        //                        TradeArticleInfo article = new TradeArticleInfo();
        //                        info.GoodsItems.Add(article);
        //                        article.Inx = DbUtils.GetInt(reader, 2);
        //                        article.GoodsCode = DbUtils.GetString(reader, 3);
        //                        article.GoodsName = DbUtils.GetString(reader, 4);
        //                        article.Quantity = DbUtils.GetDouble(reader, 5);
        //                        article.Amount = DbUtils.GetDouble(reader, 6);
        //                        break;
        //                    }
        //                }
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();
        //            //select SKTNO,JLBH,SKFS,SKJE from XSJLM_JB;
        //            int inx = 0;
        //            sql.Length = 0;
        //            sql.Append("select a.SKTNO,a.JLBH,a.SKFS,a.SKJE,c.NAME,b.JYSJ,c.TYPE ");
        //            sql.Append(" from SKTXSJLM_JB a,SKTXSJL b,SKFS_JB c where a.SKTNO=b.SKTNO and a.JLBH=b.JLBH and a.SKFS=c.CODE");
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", ">=", "RQ1");
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", "<", "RQ2");
        //            if (lastTradeTime.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JYSJ", "<=", "JYSJ");
        //            sql.Append(" order by b.JYSJ ");
        //            cmd.CommandText = sql.ToString();
        //            //DbUtils.AddIntInputParameterAndValue(cmd, "HYID", reqArgs.vipId);
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", beginDate);
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", endDate.AddDays(1));
        //            if (lastTradeTime.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "JYSJ", lastTradeTime.AddSeconds(1));//多一秒，可能会多几笔交易，但是添加列表时做了判断
        //            reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                string posId = DbUtils.GetString(reader, 0);
        //                int billId = DbUtils.GetInt(reader, 1);
        //                int payType = DbUtils.GetInt(reader, 6);
        //                foreach (TradeInfo info in receiptInfo.TradeInfoArray)
        //                {
        //                    if (info.PosId.Equals(posId) && info.TradeNo.Equals(billId.ToString()))
        //                    {
        //                        TradePaymentInfo payment = new TradePaymentInfo();
        //                        info.PaymentItems.Add(payment);
        //                        payment.Inx = inx;
        //                        payment.PaymentCode = DbUtils.GetInt(reader, 2).ToString();
        //                        payment.PayMoney = DbUtils.GetDouble(reader, 3).ToString("f2");
        //                        payment.PaymentName = DbUtils.GetString(reader, 4);
        //                        if (payType == 1)
        //                            payment.BankCardNo = "1";
        //                        break;
        //                    }
        //                }
        //                inx++;
        //            }
        //            reader.Close();
        //            cmd.Parameters.Clear();

        //            sql.Length = 0;
        //            sql.Append("select a.SKTNO,a.JLBH,a.INX,a.KH from XSJL_XYKJL_ONLINE a,SKTXSJL b ");
        //            sql.Append(" where a.SKTNO=b.SKTNO and a.JLBH=b.JLBH ");
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", ">=", "RQ1");
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JZRQ", "<", "RQ2");
        //            if (lastTradeTime.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.SpellSqlParameter(conn, sql, " and ", "b.JYSJ", "<=", "JYSJ");
        //            sql.Append(" order by a.SKTNO,a.JLBH,a.INX ");
        //            cmd.CommandText = sql.ToString();
        //            if (beginDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ1", beginDate);
        //            if (endDate.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "RQ2", endDate.AddDays(1));
        //            if (lastTradeTime.CompareTo(DateTime.MinValue) > 0)
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "JYSJ", lastTradeTime.AddSeconds(1));//多一秒，可能会多几笔交易，但是添加列表时做了判断
        //            reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //            {
        //                string posId = DbUtils.GetString(reader, 0);
        //                int billId = DbUtils.GetInt(reader, 1);
        //                string bankCardID = DbUtils.GetString(reader, 3);
        //                foreach (TradeInfo info in receiptInfo.TradeInfoArray)
        //                {
        //                    if (info.PosId.Equals(posId) && info.TradeNo.Equals(billId.ToString()))
        //                    {
        //                        if (info.PaymentItems != null)
        //                        {
        //                            foreach (TradePaymentInfo payment in info.PaymentItems)
        //                            {
        //                                if ((payment.BankCardNo != null) && (payment.BankCardNo.Length > 0))
        //                                {
        //                                    if (payment.BankCardNo.Equals("1"))
        //                                    {
        //                                        payment.BankCardNo = bankCardID;
        //                                    }
        //                                    else
        //                                    {
        //                                        payment.BankCardNo = payment.BankCardNo + ";" + bankCardID;
        //                                    }
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                    }
        //                    break;
        //                }
        //            }
        //            reader.Close();
        //            #endregion
        //            #endregion
        //            respData.result = receiptInfo;
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool UploadWeChatCouponInfo(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    WeChatCouponInfoReq reqArgs = JsonConvert.DeserializeObject<WeChatCouponInfoReq>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.WeChatCouponInfoArray == null) || (reqArgs.WeChatCouponInfoArray.Count == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    DbConnection conn = DbConnManager.GetDbConnection("CRMDB");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    try
        //    {
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
        //            foreach (WeChatCouponInfo info in reqArgs.WeChatCouponInfoArray)
        //            {
        //                string msg = string.Empty;
        //                if ((msg.Length == 0) && ((info.CardID == null) || (info.CardID.Length == 0)))
        //                {
        //                    msg = "CardID必须有值";
        //                }
        //                if ((msg.Length == 0) && (info.CouponId <= 0))
        //                {
        //                    msg = "CouponId必须有值";
        //                }
        //                if ((msg.Length == 0) && (info.Amount <= 0))
        //                {
        //                    msg = "Amount必须有值";
        //                }
        //                if ((msg.Length == 0) && ((info.BeginDate == null) || (info.BeginDate.Length == 0)))
        //                {
        //                    msg = "BeginDate必须有值";
        //                }
        //                if (msg.Length == 0)
        //                {
        //                    sql.Length = 0;
        //                    sql.Append("select 1 from YHQDEF where YHQID = ").Append(DbUtils.SpellSqlParameter(conn, "YHQID"));
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddIntInputParameterAndValue(cmd, "YHQID", info.CouponId.ToString());
        //                    DbDataReader reader = cmd.ExecuteReader();
        //                    if (!reader.Read())
        //                    {
        //                        msg = "CouponId不存在";
        //                    }
        //                    reader.Close();
        //                    cmd.Parameters.Clear();
        //                }
        //                if (msg.Length > 0)
        //                {
        //                    Random rd = new Random();
        //                    string randomStr = rd.Next(1000000, 9999999).ToString();
        //                    respData.responseId = PubUtils.GetResponseId(randomStr);
        //                    respData.errCode = 1;
        //                    respData.errMessage = msg;
        //                    return false;
        //                }
        //            }
        //            int seq = PubUtils.GetSeq("CRMDB", 0, "WX_YHQDEF_CARD", reqArgs.WeChatCouponInfoArray.Count) - reqArgs.WeChatCouponInfoArray.Count + 1;
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            DbTransaction dbTrans = conn.BeginTransaction();
        //            try
        //            {
        //                cmd.Transaction = dbTrans;
        //                int inx = 0;
        //                foreach (WeChatCouponInfo info in reqArgs.WeChatCouponInfoArray)
        //                {
        //                    sql.Length = 0;
        //                    sql.Append("insert into WX_YHQDEF_CARD_JL(JLBH,CARD_ID,YHQID,MZJE,QDSJ,STATUS,DJSJ) ");
        //                    sql.Append(" values(").Append(seq + inx++);
        //                    sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "CARD_ID"));
        //                    sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "YHQID"));
        //                    sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "MZJE"));
        //                    sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "QDSJ"));
        //                    sql.Append(",0");
        //                    sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "DJSJ"));
        //                    sql.Append(")");
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 60, "CARD_ID", info.CardID);
        //                    DbUtils.AddIntInputParameterAndValue(cmd, "YHQID", info.CouponId.ToString());
        //                    DbUtils.AddDoubleInputParameterAndValue(cmd, "MZJE", info.Amount.ToString("f2"));
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "QDSJ", FormatUtils.ParseDatetimeString(info.BeginDate));
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "DJSJ", serverTime);
        //                    cmd.ExecuteNonQuery();
        //                    cmd.Parameters.Clear();
        //                }
        //                dbTrans.Commit();
        //                CrmDataResponse response = new CrmDataResponse();
        //                response.remark = "上传成功";
        //                respData.result = response;
        //            }
        //            catch (Exception e)
        //            {
        //                dbTrans.Rollback();
        //                throw e;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool BFCRMOfferCoupon(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    OfferCouponReq reqArgs = JsonConvert.DeserializeObject<OfferCouponReq>(reqData.args.ToString());
        //    if (reqArgs == null)
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
            
        //    DbConnection conn = DbConnManager.GetDbConnection("CRMDB");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    DbDataReader reader = null;
        //    try
        //    {
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
        //            CouponInfoList response = new CouponInfoList();
        //            string msg = string.Empty;
        //            if ((msg.Length == 0) && ((reqArgs.UniqueId == null) || (reqArgs.UniqueId.Length == 0)))
        //            {
        //                msg = "UniqueId必须有值";
        //            }
        //            if ((msg.Length == 0) && (reqArgs.CouponType <= 0))
        //            {
        //                msg = "CouponType必须有值";
        //            }
        //            if ((msg.Length == 0) && (reqArgs.Amount <= 0))
        //            {
        //                msg = "Amount必须有值";
        //            }
        //            if ((msg.Length == 0) && (reqArgs.Quantity <= 0))
        //            {
        //                msg = "Quantity必须有值";
        //            }
        //            if ((msg.Length == 0) && ((reqArgs.BeginDate == null) || (reqArgs.BeginDate.Length == 0)))
        //            {
        //                msg = "OrderNo必须有值";
        //            }
        //            if ((msg.Length == 0) && ((reqArgs.EndDate == null) || (reqArgs.EndDate.Length == 0)))
        //            {
        //                msg = "OrderNo必须有值";
        //            }
        //            int CodeLength = 0;
        //            string CodePrefix = string.Empty;
        //            string CodeSuffix = string.Empty;
        //            if (msg.Length == 0)
        //            {
        //                sql.Length = 0;
        //                sql.Append("select CODELEN,CODEPRE,CODESUF from YHQDEF where YHQID = ").Append(DbUtils.SpellSqlParameter(conn, "YHQID"));
        //                cmd.CommandText = sql.ToString();
        //                DbUtils.AddIntInputParameterAndValue(cmd, "YHQID", reqArgs.CouponType.ToString());
        //                reader = cmd.ExecuteReader();
        //                if (!reader.Read())
        //                {
        //                    msg = "CouponType不存在";
        //                }
        //                else
        //                {
        //                    CodeLength = DbUtils.GetInt(reader, 0);
        //                    CodePrefix = DbUtils.GetString(reader, 1);
        //                    CodeSuffix = DbUtils.GetString(reader, 2);
        //                }
        //                reader.Close();
        //                cmd.Parameters.Clear();
        //            }
        //            if (msg.Length > 0)
        //            {
        //                Random rd = new Random();
        //                string randomStr = rd.Next(1000000, 9999999).ToString();
        //                respData.responseId = PubUtils.GetResponseId(randomStr);
        //                respData.errCode = 1;
        //                respData.errMessage = msg;
        //                return false;
        //            }
        //            bool isFound = false;
        //            sql.Length = 0;
        //            sql.Append("select JYBH from WX_HYK_SQJL where ORDERNO=:ORDERNO");
        //            cmd.CommandText = sql.ToString();
        //            DbUtils.AddStrInputParameterAndValue(cmd, 100, "ORDERNO", reqArgs.UniqueId);
        //            reader = cmd.ExecuteReader();
        //            if (reader.Read())
        //            {
        //                int billId = DbUtils.GetInt(reader, 0);
        //                sql.Length = 0;
        //                sql.Append("select a.YHQCODE,a.YHQMZ,a.BEGINDATE,a.ENDDATE from YHQCODE a,WX_HYK_SQJLITEM b where a.YHQCODE = b.YHQCODE and b.JYBH = ").Append(billId);
        //                cmd.CommandText = sql.ToString();
        //                reader = cmd.ExecuteReader();
        //                while (reader.Read())
        //                {
        //                    CouponInfo info = new CouponInfo();
        //                    response.CouponInfoArray.Add(info);
        //                    info.CouponType = reqArgs.CouponType;
        //                    info.CouponCode = DbUtils.GetString(reader, 0);
        //                    info.Amount = DbUtils.GetDouble(reader, 1);
        //                    info.BeginDate = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 2));
        //                    info.EndDate = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 3));
        //                }
        //                reader.Close();
        //                isFound = true;
        //                respData.result = response;
        //            }
        //            reader.Close();

        //            if (!isFound)
        //            {
        //                DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //                int transId = PubUtils.GetSeq("CRMDB", 1, "WX_HYK_SQJL");
        //                int seqEnd2 = PubUtils.GetSeq("CRMDB", 0, "YHQCODE", reqArgs.Quantity) - reqArgs.Quantity + 1;
        //                int seqInx2 = 0;
        //                Random rd = new Random();
        //                for (int i = 0; i < reqArgs.Quantity; i++)
        //                {
        //                    CouponInfo info = new CouponInfo();
        //                    response.CouponInfoArray.Add(info);
        //                    info.CouponType = reqArgs.CouponType;
        //                    info.Amount = reqArgs.Amount;
        //                    info.BeginDate = reqArgs.BeginDate;
        //                    info.EndDate = reqArgs.EndDate;
        //                    int len = CodeLength - CodePrefix.Length - CodeSuffix.Length - 2;
        //                    if (len < 6)
        //                        len = 6;
        //                    info.CouponCode = string.Format("{0}{1:D" + len.ToString() + "}{2:D2}{3}", CodePrefix, seqEnd2 - seqInx2++, rd.Next(99), CodeSuffix);
        //                }
        //                DbTransaction dbTrans = conn.BeginTransaction();
        //                try
        //                {
        //                    cmd.Transaction = dbTrans;
        //                    cmd.Parameters.Clear();
        //                    sql.Length = 0;
        //                    sql.Append("update WX_HYK_SQJL set FQSL = FQSL where ORDERNO=:ORDERNO");
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 100, "ORDERNO", reqArgs.UniqueId);
        //                    if (cmd.ExecuteNonQuery() == 0)
        //                    {
        //                        cmd.Parameters.Clear();
        //                        sql.Length = 0;
        //                        sql.Append("insert into WX_HYK_SQJL(JYBH,OPENID,ORDERNO,YHQID,FQSL,MZJE,DJSJ)");
        //                        sql.Append(" values(").Append(transId);
        //                        sql.Append(",:OPENID");
        //                        sql.Append(",:ORDERNO");
        //                        sql.Append(",").Append(reqArgs.CouponType);
        //                        sql.Append(",").Append(reqArgs.Quantity);
        //                        sql.Append(",").Append(reqArgs.Amount.ToString("f2"));
        //                        sql.Append(",:DJSJ");
        //                        sql.Append(")");
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 100, "OPENID", "CRM");
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 100, "ORDERNO", reqArgs.UniqueId);
        //                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "DJSJ", serverTime);
        //                        cmd.ExecuteNonQuery();
        //                        cmd.Parameters.Clear();

        //                        #region 写券信息表
        //                        for (int i = 0; i < response.CouponInfoArray.Count; i++)
        //                        {

        //                            sql.Length = 0;
        //                            sql.Append("insert into WX_HYK_SQJLITEM(JYBH,YHQCODE)");
        //                            sql.Append(" values(").Append(transId);
        //                            sql.Append(",'").Append(response.CouponInfoArray[i].CouponCode).Append("'");
        //                            sql.Append(")");
        //                            cmd.CommandText = sql.ToString();
        //                            cmd.ExecuteNonQuery();
        //                            cmd.Parameters.Clear();

        //                            sql.Length = 0;
        //                            sql.Append("insert into YHQCODE(YHQCODE,YHQID,YHQMZ,BEGINDATE,ENDDATE,CXHDBH,MDID_FQ,FQSJ,STATUS,XFJLID_FQ) ");
        //                            sql.Append("  values('").Append(response.CouponInfoArray[i].CouponCode);
        //                            sql.Append("',").Append(response.CouponInfoArray[i].CouponType);
        //                            sql.Append(",").Append(reqArgs.Amount.ToString("f2"));
        //                            sql.Append(",:BEGINDATE,:ENDDATE,").Append(0);
        //                            sql.Append(",").Append(1);
        //                            sql.Append(",:FQSJ,1");
        //                            sql.Append(",0");
        //                            sql.Append(")");
        //                            cmd.CommandText = sql.ToString();
        //                            DbUtils.AddDatetimeInputParameterAndValue(cmd, "BEGINDATE", FormatUtils.ParseDateString(reqArgs.BeginDate));
        //                            DbUtils.AddDatetimeInputParameterAndValue(cmd, "ENDDATE", FormatUtils.ParseDateString(reqArgs.EndDate));
        //                            DbUtils.AddDatetimeInputParameterAndValue(cmd, "FQSJ", serverTime);
        //                            cmd.ExecuteNonQuery();
        //                            cmd.Parameters.Clear();
        //                        }
        //                        #endregion
        //                    }
        //                    else
        //                    {
        //                        sql.Length = 0;
        //                        sql.Append("select JYBH from WX_HYK_SQJL where ORDERNO=:ORDERNO");
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 100, "ORDERNO", reqArgs.UniqueId);
        //                        reader = cmd.ExecuteReader();
        //                        if (reader.Read())
        //                        {
        //                            int billId = DbUtils.GetInt(reader, 0);
        //                            sql.Length = 0;
        //                            sql.Append("select a.YHQCODE,a.YHQMZ,a.BEGINDATE,a.ENDDATE from YHQCODE a,WX_HYK_SQJLITEM b where a.YHQCODE = b.YHQCODE and b.JYBH = ").Append(billId);
        //                            cmd.CommandText = sql.ToString();
        //                            reader = cmd.ExecuteReader();
        //                            while (reader.Read())
        //                            {
        //                                CouponInfo info = new CouponInfo();
        //                                response.CouponInfoArray.Add(info);
        //                                info.CouponType = reqArgs.CouponType;
        //                                info.CouponCode = DbUtils.GetString(reader, 0);
        //                                info.Amount = DbUtils.GetDouble(reader, 1);
        //                                info.BeginDate = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 2));
        //                                info.EndDate = FormatUtils.DateToString(DbUtils.GetDateTime(reader, 3));
        //                            }
        //                            reader.Close();
        //                        }
        //                        reader.Close();
        //                    }
        //                    if (msg.Length == 0)
        //                    {
        //                        dbTrans.Commit();
        //                        respData.result = response;
        //                    }
        //                    else
        //                        dbTrans.Rollback();
        //                }
        //                catch (Exception e)
        //                {
        //                    dbTrans.Rollback();
        //                    throw e;
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool ReviewVipDiscRule(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    ReviewVipDiscRuleReq reqArgs = JsonConvert.DeserializeObject<ReviewVipDiscRuleReq>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.id == null) || (reqArgs.id.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    DbConnection conn = DbConnManager.GetDbConnection("CRMDB");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    try
        //    {
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
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            DbTransaction dbTrans = conn.BeginTransaction();
        //            try
        //            {
        //                cmd.Transaction = dbTrans;
        //                sql.Length = 0;
        //                sql.Append("insert into HYKZKDYD_SHJL(JLBH,CLLX,INTIME)");
        //                sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "JLBH"));
        //                sql.Append(",").Append(reqArgs.approveResult);
        //                sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "INTIME"));
        //                sql.Append(")");
        //                cmd.CommandText = sql.ToString();
        //                DbUtils.AddIntInputParameterAndValue(cmd, "JLBH", reqArgs.id);
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "INTIME", serverTime);
        //                cmd.ExecuteNonQuery();
        //                cmd.Parameters.Clear();

        //                sql.Length = 0;
        //                sql.Append("update HYKZKDYD set BJ_PTSH = ").Append(reqArgs.approveResult);
        //                sql.Append(",PTSHSJ = ").Append(DbUtils.SpellSqlParameter(conn, "PTSHSJ"));
        //                sql.Append(" where JLBH = ").Append(DbUtils.SpellSqlParameter(conn, "JLBH"));
        //                cmd.CommandText = sql.ToString();
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "PTSHSJ", serverTime);
        //                DbUtils.AddIntInputParameterAndValue(cmd, "JLBH", reqArgs.id);
        //                cmd.ExecuteNonQuery();
        //                cmd.Parameters.Clear();

        //                dbTrans.Commit();
        //                CrmDataResponse response = new CrmDataResponse();
        //                response.remark = "上传成功";
        //                respData.result = response;
        //            }
        //            catch (Exception e)
        //            {
        //                dbTrans.Rollback();
        //                throw e;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool UploadScoreRule(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    ScoreRuleInfo reqArgs = JsonConvert.DeserializeObject<ScoreRuleInfo>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.ruleID == null) || (reqArgs.ruleID.Length == 0) || (reqArgs.ruleName == null) || (reqArgs.ruleName.Length == 0)
        //        || (reqArgs.beginDate == null) || (reqArgs.beginDate.Length == 0) || (reqArgs.endDate == null) || (reqArgs.endDate.Length == 0) 
        //        || (reqArgs.ruleOpType < 0) || (reqArgs.ruleOpType > 1))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    if ((reqArgs.ruleOpType == 0) && ((reqArgs.productArray == null) || (reqArgs.productArray.Count == 0)))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    //if ((reqArgs.ruleOpType == 0) && ((reqArgs.scoreRule == null) || (reqArgs.scoreRule.Length == 0)))
        //    //{
        //    //    Random rd = new Random();
        //    //    string randomStr = rd.Next(1000000, 9999999).ToString();
        //    //    respData.responseId = PubUtils.GetResponseId(randomStr);
        //    //    respData.errCode = 1;
        //    //    respData.errMessage = "请求数据有误";
        //    //    return false;
        //    //}
        //    //double ruleScore = 0;
        //    //double ruleMoney = 0;
        //    if (reqArgs.ruleOpType == 0)
        //    {
        //        //int index = reqArgs.scoreRule.IndexOf(":");
        //        //if (index <= 0)
        //        //{
        //        //    Random rd = new Random();
        //        //    string randomStr = rd.Next(1000000, 9999999).ToString();
        //        //    respData.responseId = PubUtils.GetResponseId(randomStr);
        //        //    respData.errCode = 1;
        //        //    respData.errMessage = "请求数据有误";
        //        //    return false;
        //        //}
        //        //string errMsg = string.Empty;
        //        //try
        //        //{
        //        //    ruleScore = int.Parse(reqArgs.scoreRule.Substring(0, index));
        //        //}
        //        //catch (Exception e)
        //        //{
        //        //    errMsg = e.Message;
        //        //}
        //        //if (errMsg.Length == 0)
        //        //{
        //        //    try
        //        //    {
        //        //        ruleMoney = int.Parse(reqArgs.scoreRule.Substring(index + 1, reqArgs.scoreRule.Length - index - 1));
        //        //    }
        //        //    catch (Exception e)
        //        //    {
        //        //        errMsg = e.Message;
        //        //    }
        //        //}
        //        //if (errMsg.Length > 0)
        //        //{
        //        //    Random rd = new Random();
        //        //    string randomStr = rd.Next(1000000, 9999999).ToString();
        //        //    respData.responseId = PubUtils.GetResponseId(randomStr);
        //        //    respData.errCode = 1;
        //        //    respData.errMessage = "请求数据有误" + errMsg;
        //        //    return false;
        //        //}
        //        //if ((reqArgs.scoreProductInfor == null) || (reqArgs.scoreProductInfor.Count == 0))
        //        //{
        //        //    Random rd = new Random();
        //        //    string randomStr = rd.Next(1000000, 9999999).ToString();
        //        //    respData.responseId = PubUtils.GetResponseId(randomStr);
        //        //    respData.errCode = 1;
        //        //    respData.errMessage = "请求数据有误" + errMsg;
        //        //    return false;
        //        //}
        //    }
        //    DbConnection conn = DbConnManager.GetDbConnection("CRMDB");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    try
        //    {
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
        //            CrmDataResponse response = new CrmDataResponse();
        //            response.remark = "上传成功";
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            int seq = PubUtils.GetSeq("CRMDB", 0, "JFDXGZ");
        //            DbTransaction dbTrans = conn.BeginTransaction();
        //            try
        //            {
        //                cmd.Transaction = dbTrans;

        //                if (reqArgs.ruleOpType == 0)
        //                {
        //                    sql.Length = 0;
        //                    sql.Append("update JFDXGZ set STATUS = 1,STOP_TIME = null");
        //                    sql.Append(",RULE_NAME = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_NAME"));
        //                    sql.Append(",BEGIN_DATE = ").Append(DbUtils.SpellSqlParameter(conn, "BEGIN_DATE"));
        //                    sql.Append(",END_DATE = ").Append(DbUtils.SpellSqlParameter(conn, "END_DATE"));
        //                    sql.Append(",UPDATE_TIME = ").Append(DbUtils.SpellSqlParameter(conn, "END_DATE"));
        //                    sql.Append(",UPDATE_SEQ = ").Append(seq);
        //                    sql.Append(" where RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 60, "RULE_NAME", reqArgs.ruleName, ServerPlatform.Config.DbCharSetIsNotChinese);
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "BEGIN_DATE", FormatUtils.ParseDatetimeString(reqArgs.beginDate));
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "END_DATE", FormatUtils.ParseDatetimeString(reqArgs.endDate));
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "UPDATE_TIME", serverTime);
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                    if (cmd.ExecuteNonQuery() == 0)
        //                    {
        //                        cmd.Parameters.Clear();
        //                        sql.Length = 0;
        //                        sql.Append("insert into JFDXGZ(RULE_ID,RULE_NAME,STATUS,BEGIN_DATE,END_DATE,INTIME,UPDATE_SEQ)");//,SCORE,MONEY,SCORE_LOW_LIMIT,SCORE_UP_LIMIT,SCORE_RULE
        //                        sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "RULE_NAME"));
        //                        sql.Append(",1");
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "BEGIN_DATE"));
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "END_DATE"));
        //                        //sql.Append(",").Append(reqArgs.scoreLowLimit);
        //                        //sql.Append(",").Append(reqArgs.scoreUpLimit);
        //                        //sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "SCORE_RULE"));
        //                        //sql.Append(",").Append(ruleScore.ToString("f2"));
        //                        //sql.Append(",").Append(ruleMoney.ToString("f2"));
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "INTIME"));
        //                        sql.Append(",").Append(seq);
        //                        sql.Append(")");
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 60, "RULE_NAME", reqArgs.ruleName, ServerPlatform.Config.DbCharSetIsNotChinese);
        //                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "BEGIN_DATE", FormatUtils.ParseDatetimeString(reqArgs.beginDate));
        //                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "END_DATE", FormatUtils.ParseDatetimeString(reqArgs.endDate));
        //                        //DbUtils.AddStrInputParameterAndValue(cmd, 20, "SCORE_RULE", reqArgs.scoreRule);
        //                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "INTIME", serverTime);
        //                        cmd.ExecuteNonQuery();
        //                        cmd.Parameters.Clear();
        //                    }
        //                    else
        //                    {
        //                        cmd.Parameters.Clear();
        //                        response.remark = "重复上传";
        //                    }
        //                    cmd.Parameters.Clear();

        //                    #region 处理明细 --要求能修改明细，所以放在此先删后插
        //                    int inx1 = 0;
        //                    int inx2 = 0;
        //                    int inx3 = 0;
        //                    bool isFound1 = false;
        //                    bool isFound2 = false;
        //                    sql.Length = 0;
        //                    sql.Append("delete from JFDXGZ_KLX ");
        //                    sql.Append(" where RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                    cmd.ExecuteNonQuery();
        //                    cmd.Parameters.Clear();
        //                    sql.Length = 0;
        //                    sql.Append("delete from JFDXGZ_SP_CJ ");
        //                    sql.Append(" where RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                    cmd.ExecuteNonQuery();
        //                    cmd.Parameters.Clear();
        //                    sql.Length = 0;
        //                    sql.Append("delete from JFDXGZ_SP_BCJ ");
        //                    sql.Append(" where RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                    cmd.ExecuteNonQuery();
        //                    cmd.Parameters.Clear();
        //                    if ((reqArgs.memberLevelArray != null) && (reqArgs.memberLevelArray.Count > 0))
        //                    {
        //                        #region 卡类型
        //                        foreach (int vipTypeId in reqArgs.memberLevelArray)
        //                        {
        //                            sql.Length = 0;
        //                            sql.Append("insert into JFDXGZ_KLX(RULE_ID,XH,HYKTYPE)");
        //                            sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                            sql.Append(",").Append(inx1);
        //                            sql.Append(",").Append(vipTypeId);
        //                            sql.Append(")");
        //                            cmd.CommandText = sql.ToString();
        //                            DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                            cmd.ExecuteNonQuery();
        //                            cmd.Parameters.Clear();
        //                            inx1++;
        //                        }
        //                        #endregion
        //                    }
        //                    if ((reqArgs.ruleOpType == 0) && ((reqArgs.productArray != null) && (reqArgs.productArray.Count > 0)))
        //                    {
        //                        #region 商品相关条件
        //                        foreach (ProductInfo info in reqArgs.productArray)
        //                        {
        //                            if (info.type == 4) //品牌不参加
        //                            {
        //                                sql.Length = 0;
        //                                sql.Append("insert into JFDXGZ_SP_BCJ(RULE_ID,XH,SJLX,SJNR,SJDM)");
        //                                sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                                sql.Append(",").Append(inx3);
        //                                sql.Append(",3");
        //                                sql.Append(",0");
        //                                sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "SJDM"));
        //                                sql.Append(")");
        //                                cmd.CommandText = sql.ToString();
        //                                DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                                DbUtils.AddStrInputParameterAndValue(cmd, 20, "SJDM", info.id);
        //                                cmd.ExecuteNonQuery();
        //                                cmd.Parameters.Clear();
        //                                inx3++;
        //                                isFound2 = true;
        //                            }
        //                            else if ((info.type >= 1) && (info.type <=3))
        //                            {
        //                                sql.Length = 0;
        //                                sql.Append("insert into JFDXGZ_SP_CJ(RULE_ID,XH,SJLX,SJNR,SJDM)");
        //                                sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                                sql.Append(",").Append(inx2);
        //                                sql.Append(",").Append(info.type);
        //                                sql.Append(",0");
        //                                sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "SJDM"));
        //                                sql.Append(")");
        //                                cmd.CommandText = sql.ToString();
        //                                DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                                DbUtils.AddStrInputParameterAndValue(cmd, 20, "SJDM", info.id);
        //                                cmd.ExecuteNonQuery();
        //                                cmd.Parameters.Clear();
        //                                inx2++;
        //                                isFound1 = true;
        //                            }
        //                        }
        //                        #endregion
        //                    }
        //                    #region 不参加的条件
        //                    //foreach (ScoreRuleProductInfo info in reqArgs.scoreProductInfor)
        //                    //{
        //                    //if ((info.memberLevelArray == null) || (info.memberLevelArray.Count == 0))
        //                    //{
        //                    //    dbTrans.Rollback();
        //                    //    Random rd = new Random();
        //                    //    string randomStr = rd.Next(1000000, 9999999).ToString();
        //                    //    respData.responseId = PubUtils.GetResponseId(randomStr);
        //                    //    respData.errCode = 1;
        //                    //    respData.errMessage = "请求数据有误";
        //                    //    return false;
        //                    //}
        //                    //foreach (MemberLevelInfo member in info.memberLevelArray)
        //                    //{
        //                    //    sql.Length = 0;
        //                    //    sql.Append("insert into JFDXGZ_KLX(RULE_ID,XH,HYKTYPE)");
        //                    //    sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    //    sql.Append(",").Append(inx1);
        //                    //    sql.Append(",").Append(member.memberLevelID);
        //                    //    sql.Append(")");
        //                    //    cmd.CommandText = sql.ToString();
        //                    //    DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                    //    cmd.ExecuteNonQuery();
        //                    //    cmd.Parameters.Clear();
        //                    //    inx1++;
        //                    //}
        //                    //    if (info.productParticipationArray != null)
        //                    //    {
        //                    //        foreach (ProductInfo product in info.productParticipationArray)
        //                    //        {
        //                    //            sql.Length = 0;
        //                    //            sql.Append("insert into JFDXGZ_SP_CJ(RULE_ID,XH,SJLX,SJNR,SJDM)");
        //                    //            sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    //            sql.Append(",").Append(inx2);
        //                    //            sql.Append(",").Append(product.type);
        //                    //            sql.Append(",0");
        //                    //            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "SJDM"));
        //                    //            sql.Append(")");
        //                    //            cmd.CommandText = sql.ToString();
        //                    //            DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                    //            DbUtils.AddStrInputParameterAndValue(cmd, 20, "SJDM", product.id);
        //                    //            cmd.ExecuteNonQuery();
        //                    //            cmd.Parameters.Clear();
        //                    //            inx2++;
        //                    //            isFound1 = true;
        //                    //        }
        //                    //    }
        //                    //    if (info.productNotParticipationArray != null)
        //                    //    {
        //                    //        foreach (ProductInfo product in info.productNotParticipationArray)
        //                    //        {
        //                    //            sql.Length = 0;
        //                    //            sql.Append("insert into JFDXGZ_SP_BCJ(RULE_ID,XH,SJLX,SJNR,SJDM)");
        //                    //            sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    //            sql.Append(",").Append(inx3);
        //                    //            sql.Append(",").Append(product.type);
        //                    //            sql.Append(",0");
        //                    //            sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "SJDM"));
        //                    //            sql.Append(")");
        //                    //            cmd.CommandText = sql.ToString();
        //                    //            DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                    //            DbUtils.AddStrInputParameterAndValue(cmd, 20, "SJDM", product.id);
        //                    //            cmd.ExecuteNonQuery();
        //                    //            cmd.Parameters.Clear();
        //                    //            inx3++;
        //                    //            isFound2 = true;
        //                    //        }
        //                    //    }
        //                    //}
        //                    #endregion
        //                    if (isFound1)
        //                    {
        //                        #region 更新参加条件ID
        //                        //--部门
        //                        sql.Length = 0;
        //                        sql.Append("update JFDXGZ_SP_CJ a set SJNR = (select SHBMID from SHBM where SHDM='BH' and a.SJDM = BMDM)");
        //                        sql.Append(" where SJLX = 1 ");
        //                        sql.Append(" and RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                        sql.Append(" and exists(select 1 from SHBM where SHDM='BH' and a.SJDM = BMDM)");
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                        cmd.ExecuteNonQuery();
        //                        cmd.Parameters.Clear();

        //                        //--品类
        //                        sql.Length = 0;
        //                        sql.Append("update JFDXGZ_SP_CJ a set SJNR = (select SHSPFLID from SHSPFL where SHDM='BH' and a.SJDM = SPFLDM)");
        //                        sql.Append(" where SJLX = 2 ");
        //                        sql.Append(" and RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                        sql.Append(" and exists(select 1 from SHSPFL where SHDM='BH' and a.SJDM = SPFLDM)");
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                        cmd.ExecuteNonQuery();
        //                        cmd.Parameters.Clear();

        //                        //--品牌
        //                        sql.Length = 0;
        //                        sql.Append("update JFDXGZ_SP_CJ a set SJNR = (select SHSBID from SHSPSB where SHDM='BH' and a.SJDM = SBDM)");
        //                        sql.Append(" where SJLX = 3 ");
        //                        sql.Append(" and RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                        sql.Append(" and exists(select 1 from SHSPSB where SHDM='BH' and a.SJDM = SBDM)");
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                        cmd.ExecuteNonQuery();
        //                        cmd.Parameters.Clear();
        //                        #endregion
        //                    }

        //                    if (isFound2)
        //                    {
        //                        #region 更新不参加条件ID
        //                        ////--部门
        //                        //sql.Length = 0;
        //                        //sql.Append("update JFDXGZ_SP_BCJ a set SJNR = (select SHBMID from SHBM where SHDM='BH' and a.SJDM = BMDM)");
        //                        //sql.Append(" where SJLX = 1 ");
        //                        //sql.Append(" and RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                        //sql.Append(" and exists(select 1 from SHBM where SHDM='BH' and a.SJDM = BMDM)");
        //                        //cmd.CommandText = sql.ToString();
        //                        //DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                        //cmd.ExecuteNonQuery();
        //                        //cmd.Parameters.Clear();

        //                        ////--品类
        //                        //sql.Length = 0;
        //                        //sql.Append("update JFDXGZ_SP_BCJ a set SJNR = (select SHSPFLID from SHSPFL where SHDM='BH' and a.SJDM = SPFLDM)");
        //                        //sql.Append(" where SJLX = 2 ");
        //                        //sql.Append(" and RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                        //sql.Append(" and exists(select 1 from SHSPFL where SHDM='BH' and a.SJDM = SPFLDM)");
        //                        //cmd.CommandText = sql.ToString();
        //                        //DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                        //cmd.ExecuteNonQuery();
        //                        //cmd.Parameters.Clear();

        //                        //--品牌
        //                        sql.Length = 0;
        //                        sql.Append("update JFDXGZ_SP_BCJ a set SJNR = (select SHSBID from SHSPSB where SHDM='BH' and a.SJDM = SBDM)");
        //                        sql.Append(" where SJLX = 3 ");
        //                        sql.Append(" and RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                        sql.Append(" and exists(select 1 from SHSPSB where SHDM='BH' and a.SJDM = SBDM)");
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                        cmd.ExecuteNonQuery();
        //                        cmd.Parameters.Clear();
        //                        #endregion
        //                    }
        //                    #endregion
        //                }
        //                else
        //                {
        //                    sql.Length = 0;
        //                    sql.Append("update JFDXGZ set STATUS = 2, STOP_TIME = ").Append(DbUtils.SpellSqlParameter(conn, "STOP_TIME"));
        //                    sql.Append(" where RULE_ID = ").Append(DbUtils.SpellSqlParameter(conn, "RULE_ID"));
        //                    sql.Append(" and STATUS = 1 ");
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "STOP_TIME", serverTime);
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 30, "RULE_ID", reqArgs.ruleID);
        //                    if (cmd.ExecuteNonQuery() == 0)
        //                    {
        //                        response.remark = "可能重复上传";
        //                    }
        //                    cmd.Parameters.Clear();
        //                }
        //                dbTrans.Commit();
        //                respData.result = response;
        //            }
        //            catch (Exception e)
        //            {
        //                dbTrans.Rollback();
        //                throw e;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool ModifyPointDiscountSetting(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    PointDiscountSettingInfo reqArgs = JsonConvert.DeserializeObject<PointDiscountSettingInfo>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.scoreRule == null) || (reqArgs.Status < 0) || (reqArgs.Status > 1))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    //if ((reqArgs.ruleOpType == 0) && ((reqArgs.scoreRule == null) || (reqArgs.scoreRule.Length == 0)))
        //    //{
        //    //    Random rd = new Random();
        //    //    string randomStr = rd.Next(1000000, 9999999).ToString();
        //    //    respData.responseId = PubUtils.GetResponseId(randomStr);
        //    //    respData.errCode = 1;
        //    //    respData.errMessage = "请求数据有误";
        //    //    return false;
        //    //}
        //    double ruleScore = 0;
        //    double ruleMoney = 0;
        //    if (reqArgs.Status == 1)
        //    {
        //        int index = reqArgs.scoreRule.IndexOf(":");
        //        if (index <= 0)
        //        {
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.errCode = 1;
        //            respData.errMessage = "请求数据有误";
        //            return false;
        //        }
        //        string errMsg = string.Empty;
        //        try
        //        {
        //            ruleScore = double.Parse(reqArgs.scoreRule.Substring(0, index));
        //        }
        //        catch (Exception e)
        //        {
        //            errMsg = e.Message;
        //        }
        //        if (errMsg.Length == 0)
        //        {
        //            try
        //            {
        //                ruleMoney = double.Parse(reqArgs.scoreRule.Substring(index + 1, reqArgs.scoreRule.Length - index - 1));
        //            }
        //            catch (Exception e)
        //            {
        //                errMsg = e.Message;
        //            }
        //        }
        //        if (errMsg.Length > 0)
        //        {
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.errCode = 1;
        //            respData.errMessage = "请求数据有误" + errMsg;
        //            return false;
        //        }
        //    }
        //    DbConnection conn = DbConnManager.GetDbConnection("CRMDB");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    try
        //    {
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
        //            CrmDataResponse response = new CrmDataResponse();
        //            response.remark = "上传成功";
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            DbTransaction dbTrans = conn.BeginTransaction();
        //            try
        //            {
        //                cmd.Transaction = dbTrans;

        //                if (reqArgs.Status == 1)
        //                {
        //                    sql.Length = 0;
        //                    sql.Append("update JFDXGZ_MAIN set STATUS = 1,INTIME = ").Append(DbUtils.SpellSqlParameter(conn, "INTIME"));
        //                    sql.Append(",SCORE = ").Append(ruleScore.ToString("f2"));
        //                    sql.Append(",MONEY = ").Append(ruleMoney.ToString("f2"));
        //                    sql.Append(",SCORE_LOW_LIMIT = ").Append(reqArgs.scoreLowLimit);
        //                    sql.Append(",SCORE_UP_LIMIT = ").Append(reqArgs.scoreUpLimit);
        //                    sql.Append(",SCORE_RULE = ").Append(DbUtils.SpellSqlParameter(conn, "SCORE_RULE"));
        //                    sql.Append(",STOP_TIME = null ");
        //                    sql.Append(" where ID = 1 ");
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "INTIME", serverTime);
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 20, "SCORE_RULE", reqArgs.scoreRule);
        //                    if (cmd.ExecuteNonQuery() == 0)
        //                    {
        //                        cmd.Parameters.Clear();
        //                        sql.Length = 0;
        //                        sql.Append("insert into JFDXGZ_MAIN(ID,SCORE,MONEY,SCORE_LOW_LIMIT,SCORE_UP_LIMIT,SCORE_RULE,STATUS,INTIME)");//
        //                        sql.Append(" values(1");
        //                        sql.Append(",").Append(ruleScore.ToString("f2"));
        //                        sql.Append(",").Append(ruleMoney.ToString("f2"));
        //                        sql.Append(",").Append(reqArgs.scoreLowLimit);
        //                        sql.Append(",").Append(reqArgs.scoreUpLimit);
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "SCORE_RULE"));
        //                        sql.Append(",1");                                
        //                        sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "INTIME"));
        //                        sql.Append(")");
        //                        cmd.CommandText = sql.ToString();
        //                        DbUtils.AddStrInputParameterAndValue(cmd, 20, "SCORE_RULE", reqArgs.scoreRule);
        //                        DbUtils.AddDatetimeInputParameterAndValue(cmd, "INTIME", serverTime);
        //                        cmd.ExecuteNonQuery();
        //                        cmd.Parameters.Clear();
        //                    }
        //                    else
        //                    {
        //                        cmd.Parameters.Clear();
        //                        response.remark = "重复上传";
        //                    }
        //                }
        //                else
        //                {
        //                    sql.Length = 0;
        //                    sql.Append("update JFDXGZ_MAIN set STATUS = 0, STOP_TIME = ").Append(DbUtils.SpellSqlParameter(conn, "STOP_TIME"));
        //                    sql.Append(" where ID = 1 ");
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "STOP_TIME", serverTime);
        //                    if (cmd.ExecuteNonQuery() == 0)
        //                    {
        //                        response.remark = "可能重复上传";
        //                    }
        //                    cmd.Parameters.Clear();
        //                }
        //                dbTrans.Commit();
        //                respData.result = response;
        //            }
        //            catch (Exception e)
        //            {
        //                dbTrans.Rollback();
        //                throw e;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool GetMemberPointInfo(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    TMemberReq reqArgs = JsonConvert.DeserializeObject<TMemberReq>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.memberID == null) || (reqArgs.memberID.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    try
        //    {
        //        string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //        reqData req = new reqData();
        //        req.data = reqArgs;
        //        string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //        req.sign = PubUtils.GetSign(timestamp);
        //        string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //        string appCode = "/api/cy_erp/get_member_attr";
        //        string respJsonStr = string.Empty;
        //        string msg = string.Empty;                
        //        bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //        if (ok)
        //        {
        //            MemberScoreInfo info = JsonConvert.DeserializeObject<MemberScoreInfo>(respJsonStr);
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.result = info;
        //        }
        //        else
        //        {
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.errCode = 1;
        //            respData.errMessage = msg;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);     
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool MemberDeduPoint(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    TMemberDeduPointReq reqArgs = JsonConvert.DeserializeObject<TMemberDeduPointReq>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.memberID == null) || (reqArgs.memberID.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "会员ID必须有值";
        //        return false;
        //    }
        //    if ((reqArgs.type != 1) && (reqArgs.type != 2))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "操作类型有误";
        //        return false;
        //    }
        //    if ((reqArgs.posSerialNum == null) || (reqArgs.posSerialNum.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "长益POS端的流水号必须有值";
        //        return false;
        //    }
        //    if ((reqArgs.machineID == null) || (reqArgs.machineID.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "款机ID必须有值";
        //        return false;
        //    }
        //    if ((reqArgs.type == 2) && (reqArgs.orderID == null) || (reqArgs.orderID.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "退货时原订单小票ID必须有值";
        //        return false;
        //    }
        //    if (reqArgs.score == 0)
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "用于积分抵现的积分必须有值";
        //        return false;
        //    }
        //    try
        //    {
        //        string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //        reqData req = new reqData();
        //        req.data = reqArgs;
        //        string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //        req.sign = PubUtils.GetSign(timestamp);
        //        string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //        string appCode = "/api/cy_erp/send_score_consumption";
        //        string respJsonStr = string.Empty;
        //        string msg = string.Empty;
        //        bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //        if (ok)
        //        {
        //            TMemberDeduPointResp info = JsonConvert.DeserializeObject<TMemberDeduPointResp>(respJsonStr);
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.result = info;
        //        }
        //        else
        //        {
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.errCode = 1;
        //            respData.errMessage = msg;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool MemberDeduPointCancel(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    TMemberDeduPointCancelReq reqArgs = JsonConvert.DeserializeObject<TMemberDeduPointCancelReq>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.crmSerialNum == null) || (reqArgs.crmSerialNum.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "crm端的流水号";
        //        return false;
        //    }
        //    if ((reqArgs.type != 1) && (reqArgs.type != 2))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "操作类型有误";
        //        return false;
        //    }
        //    try
        //    {
        //        string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //        reqData req = new reqData();
        //        req.data = reqArgs;
        //        string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //        req.sign = PubUtils.GetSign(timestamp);
        //        string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //        string appCode = "/api/cy_erp/rollback_score_consumption_op";
        //        string respJsonStr = string.Empty;
        //        string msg = string.Empty;
        //        bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //        if (ok)
        //        {
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //        }
        //        else
        //        {
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.errCode = 1;
        //            respData.errMessage = msg;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool GetCrmDeduSerial(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    TGetCrmDeduSerialReq reqArgs = JsonConvert.DeserializeObject<TGetCrmDeduSerialReq>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.posSerialNum == null) || (reqArgs.posSerialNum.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "POS端的流水号必须有值";
        //        return false;
        //    }
        //    try
        //    {
        //        string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //        reqData req = new reqData();
        //        req.data = reqArgs;
        //        string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //        req.sign = PubUtils.GetSign(timestamp);
        //        string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //        string appCode = "/api/cy_erp/query_score_serial";
        //        string respJsonStr = string.Empty;
        //        string msg = string.Empty;
        //        bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //        if (ok)
        //        {
        //            TGetCrmDeduSerialResp info = JsonConvert.DeserializeObject<TGetCrmDeduSerialResp>(respJsonStr);
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.result = info;
        //        }
        //        else
        //        {
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.errCode = 1;
        //            respData.errMessage = msg;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool GetCrmDeduStatus(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    TGetCrmDeduStatusReq reqArgs = JsonConvert.DeserializeObject<TGetCrmDeduStatusReq>(reqData.args.ToString());
        //    if ((reqArgs == null) || (reqArgs.crmSerialNum == null) || (reqArgs.crmSerialNum.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "crm端的流水号必须有值";
        //        return false;
        //    }
        //    try
        //    {
        //        string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //        reqData req = new reqData();
        //        req.data = reqArgs;
        //        string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //        req.sign = PubUtils.GetSign(timestamp);
        //        string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //        string appCode = "/api/cy_erp/query_score_status";
        //        string respJsonStr = string.Empty;
        //        string msg = string.Empty;
        //        bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //        if (ok)
        //        {
        //            TGetCrmDeduStatusResp info = JsonConvert.DeserializeObject<TGetCrmDeduStatusResp>(respJsonStr);
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.result = info;
        //        }
        //        else
        //        {
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.errCode = 1;
        //            respData.errMessage = msg;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool GetBillStatus(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    TGetBillStatusReq reqArgs = JsonConvert.DeserializeObject<TGetBillStatusReq>(reqData.args.ToString());
        //    if (reqArgs == null)
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    if ((reqArgs.machineID == null) || (reqArgs.machineID.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "款台号必须有值";
        //        return false;
        //    }
        //    if ((reqArgs.orderID == null) || (reqArgs.orderID.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "小票号必须有值";
        //        return false;
        //    }
        //    try
        //    {
        //        string ManagedThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
        //        reqData req = new reqData();
        //        req.data = reqArgs;
        //        string timestamp = PubUtils.ConvertDateTimeStr(DateTime.Now);
        //        req.sign = PubUtils.GetSign(timestamp);
        //        string reqJsonStr = PubUtils.Json_ToShortDateString(req).Replace("\r\n", "");
        //        string appCode = "/api/cy_erp/query_bill_status";
        //        string respJsonStr = string.Empty;
        //        string msg = string.Empty;
        //        bool ok = SendHttpRequest.uploadDataToRemoteServer(out msg, out respJsonStr, reqJsonStr, appCode, ManagedThreadId);
        //        if (ok)
        //        {
        //            TGetBillStatusResp info = JsonConvert.DeserializeObject<TGetBillStatusResp>(respJsonStr);
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.result = info;
        //        }
        //        else
        //        {
        //            Random rd = new Random();
        //            string randomStr = rd.Next(1000000, 9999999).ToString();
        //            respData.responseId = PubUtils.GetResponseId(randomStr);
        //            respData.errCode = 1;
        //            respData.errMessage = msg;
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        throw new Exception(e.Message);
        //    }
        //    return (respData.errCode == 0);
        //}

        //public static bool UploadBillStatus(out AppRespone respData, AppReqData reqData)
        //{
        //    respData = new AppRespone();
        //    if ((reqData == null) || (reqData.args == null) || (reqData.args.ToString().Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    TUploadReceiptFlagReq reqArgs = JsonConvert.DeserializeObject<TUploadReceiptFlagReq>(reqData.args.ToString());
        //    if (reqArgs == null)
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "请求数据有误";
        //        return false;
        //    }
        //    if ((reqArgs.machineID == null) || (reqArgs.machineID.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "款机ID必须有值";
        //        return false;
        //    }
        //    if ((reqArgs.orderID == null) || (reqArgs.orderID.Length == 0))
        //    {
        //        Random rd = new Random();
        //        string randomStr = rd.Next(1000000, 9999999).ToString();
        //        respData.responseId = PubUtils.GetResponseId(randomStr);
        //        respData.errCode = 1;
        //        respData.errMessage = "订单ID必须有值";
        //        return false;
        //    }
        //    DbConnection conn = DbConnManager.GetDbConnection("ERP");
        //    DbCommand cmd = conn.CreateCommand();
        //    StringBuilder sql = new StringBuilder();
        //    try
        //    {
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
        //            DateTime serverTime = DbUtils.GetDbServerTime(cmd);
        //            DbTransaction dbTrans = conn.BeginTransaction();
        //            try
        //            {
        //                cmd.Transaction = dbTrans;
        //                sql.Length = 0;
        //                sql.Append("update UPLOAD_XSJL_KFPJL set BJ_KFP = ").Append(reqArgs.orderStatus);
        //                sql.Append(",DJSJ = ").Append(DbUtils.SpellSqlParameter(conn, "DJSJ"));
        //                sql.Append(" where SKTNO = ").Append(DbUtils.SpellSqlParameter(conn, "SKTNO"));
        //                sql.Append("  and JLBH = ").Append(DbUtils.SpellSqlParameter(conn, "JLBH"));
        //                cmd.CommandText = sql.ToString();
        //                DbUtils.AddDatetimeInputParameterAndValue(cmd, "DJSJ", serverTime);
        //                DbUtils.AddStrInputParameterAndValue(cmd, 10, "SKTNO", reqArgs.machineID);
        //                DbUtils.AddIntInputParameterAndValue(cmd, "JLBH", reqArgs.orderID);
        //                if (cmd.ExecuteNonQuery() == 0)
        //                {
        //                    cmd.Parameters.Clear();
        //                    sql.Length = 0;
        //                    sql.Append("insert into UPLOAD_XSJL_KFPJL(SKTNO,JLBH,BJ_KFP,DJSJ) ");
        //                    sql.Append(" values(").Append(DbUtils.SpellSqlParameter(conn, "SKTNO"));
        //                    sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "JLBH"));
        //                    sql.Append(",").Append(reqArgs.orderStatus);
        //                    sql.Append(",").Append(DbUtils.SpellSqlParameter(conn, "DJSJ"));
        //                    sql.Append(")");
        //                    cmd.CommandText = sql.ToString();
        //                    DbUtils.AddStrInputParameterAndValue(cmd, 10, "SKTNO", reqArgs.machineID);
        //                    DbUtils.AddIntInputParameterAndValue(cmd, "JLBH", reqArgs.orderID);
        //                    DbUtils.AddDatetimeInputParameterAndValue(cmd, "DJSJ", serverTime);
        //                    cmd.ExecuteNonQuery();
        //                }
        //                cmd.Parameters.Clear();
        //                dbTrans.Commit();
        //                CrmDataResponse response = new CrmDataResponse();
        //                response.remark = "上传成功";
        //                respData.result = response;
        //            }
        //            catch (Exception e)
        //            {
        //                dbTrans.Rollback();
        //                throw e;
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            if (e is MyDbException)
        //                throw e;
        //            else
        //                throw new MyDbException(e.Message, cmd.CommandText);
        //        }
        //    }
        //    finally
        //    {
        //        conn.Close();
        //    }
        //    return (respData.errCode == 0);
        //}
    }
}
