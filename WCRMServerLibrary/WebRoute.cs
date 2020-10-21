using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using Newtonsoft.Json;
using System.Configuration;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using System.Threading;

namespace WCRMServer.Proc
{
    public class WebInterface
    {
        public static void InterFace(out string respJsonStr, string Content, string appId, string method)
        {
            respJsonStr = string.Empty;
            AppRespone respData = new AppRespone();
            AppReqData reqData = JsonConvert.DeserializeObject<AppReqData>(Content);
            if ((reqData != null) && (method != null) && (method.Length > 0))
            {
                string appSecret = string.Empty;
                string msg = string.Empty;
                bool ok = false;
                //if ((msg.Length == 0) && ((reqData.reqId == null) || (reqData.reqId.Length == 0)))
                //{
                //    msg = "reqId必须有值";
                //}
                string reqJsonStr = string.Empty;
                if ((msg.Length == 0) && ((reqData.TimeStamp == null) || (reqData.TimeStamp.Length == 0)))
                {
                    msg = "TimeStamp必须有值";
                }
                if ((msg.Length == 0) && ((reqData.Sign == null) || (reqData.Sign.Length == 0)))
                {
                    msg = "Sign必须有值";
                }
                if ((msg.Length == 0) && ((reqData.Data == null) || (reqData.Data.ToString().Length == 0)))
                {
                    reqJsonStr = "";
                    //msg = "Data必须有值";
                }
                else
                {
                    reqJsonStr = reqData.Data.ToString();
                }
                if ((msg.Length == 0) && ((reqData.Format == null) || (reqData.Format.Length == 0)))
                {
                    msg = "Format必须有值";
                }
                if ((msg.Length == 0) && ((reqData.Encrypt == null) || (reqData.Encrypt.Length == 0)))
                {
                    msg = "Encrypt必须有值";
                }
                long times = PubUtils.ConvertDateTimeInt(DateTime.Now);
                if ((msg.Length == 0) && (times - long.Parse(reqData.TimeStamp) > 3600000)) //ts为1个小时内有效
                {
                    msg = "请求包已失效";
                }
                if (msg.Length == 0)
                {
                    ok = ServerPlatform.getAppSecret(out msg, out appSecret, appId);
                    if (ok)
                    {
                        string sign = PubUtils.GetSign(reqJsonStr, appId, reqData.TimeStamp, appSecret);
                        if ((reqData.Sign == null) || (!reqData.Sign.Equals(sign)))
                        {
                            msg = "数字签名失败";
                            ok = false;
                        }
                    }
                }
                if (ok)
                {
                    #region 会员注册或绑定
                    if (method == "Wechat.V1.Register")
                    {
                        CRMProc.Register(out respData, reqJsonStr);
                    }
                    #endregion
                    #region 会员资料更新
                    if (method == "Wechat.V1.UpdateMemberInfo")
                    {
                        CRMProc.UploadVipCardInfo(out respData, reqJsonStr);
                    }
                    #endregion
                    #region 会员查询
                    if (method == "Wechat.V1.GetMemberInfo")
                    {
                        CRMProc.GetMemberInfo(out respData, reqJsonStr);
                    }
                    #endregion

                    #region 
                    //#region 上传折扣信息
                    //if (reqData.method == "UploadVipCardGoodsDiscRule")
                    //{
                    //    BFCRMProc.UploadVipCardGoodsDiscRule(out respData, reqData);
                    //}
                    //#endregion

                    //#region 会员电子小票
                    //if (reqData.method == "GetVipCardTradeItem")
                    //{
                    //    BFCRMProc.GetVipCardTradeItem(out respData, reqData);
                    //}
                    //#endregion

                    //#region 上传卡类型信息
                    //if (reqData.method == "UploadVipCardTypeInfo")
                    //{
                    //    BFCRMProc.UploadVipCardTypeInfo(out respData, reqData);
                    //}
                    //#endregion

                    //#region 获取非会员订单信息
                    //if (reqData.method == "GetAllMemberTradeItem")
                    //{
                    //    BFCRMProc.GetNonMemberTradeItem(out respData, reqData);
                    //}
                    //#endregion

                    //#region 上传微信券信息
                    //if (reqData.method == "UploadWeChatCouponInfo")
                    //{
                    //    BFCRMProc.UploadWeChatCouponInfo(out respData, reqData);
                    //}
                    //#endregion

                    //#region 上传微信券信息
                    //if (reqData.method == "BFCRMOfferCoupon")
                    //{
                    //    BFCRMProc.BFCRMOfferCoupon(out respData, reqData);
                    //}
                    //#endregion

                    //#region 会员折扣规则审核
                    //if (reqData.method == "ReviewVipDiscRule")
                    //{
                    //    BFCRMProc.ReviewVipDiscRule(out respData, reqData);
                    //}
                    //#endregion

                    //#region 同步积分抵现规则
                    //if (reqData.method == "UploadScoreRule")
                    //{
                    //    BFCRMProc.UploadScoreRule(out respData, reqData);
                    //}
                    //#endregion
                    //#region 修改积分通用规则接口
                    //if (reqData.method == "ModifyPointDiscountSetting")
                    //{
                    //    BFCRMProc.ModifyPointDiscountSetting(out respData, reqData);
                    //}
                    //#endregion
                    //#region 获取会员总积分/等级/成长
                    //if (reqData.method == "GetMemberPointInfo")
                    //{
                    //    BFCRMProc.GetMemberPointInfo(out respData, reqData);
                    //}
                    //#endregion
                    //#region 积分抵现
                    //if (reqData.method == "MemberDeduPoint")
                    //{
                    //    BFCRMProc.MemberDeduPoint(out respData, reqData);
                    //}
                    //#endregion
                    //#region 积分抵现冲正
                    //if (reqData.method == "MemberDeduPointCancel")
                    //{
                    //    BFCRMProc.MemberDeduPointCancel(out respData, reqData);
                    //}
                    //#endregion
                    //#region 查询CRM端积分流水号
                    //if (reqData.method == "GetCrmDeduSerial")
                    //{
                    //    BFCRMProc.GetCrmDeduSerial(out respData, reqData);
                    //}
                    //#endregion
                    //#region 根据CRM端积分流水号查询积分变更状态
                    //if (reqData.method == "GetCrmDeduStatus")
                    //{
                    //    BFCRMProc.GetCrmDeduStatus(out respData, reqData);
                    //}
                    //#endregion
                    //#region 查询该订单是否开过发票
                    //if (reqData.method == "GetBillStatus")
                    //{
                    //    BFCRMProc.GetBillStatus(out respData, reqData);
                    //}
                    //#endregion
                    //#region 上传开发票标记
                    //if (reqData.method == "UploadBillStatus")
                    //{
                    //    BFCRMProc.UploadBillStatus(out respData, reqData);
                    //}
                    //#endregion
                    #endregion
                }
                else
                {
                    respData.Code = "1000";
                    respData.Message = msg;
                }
            }
            else
            {
                respData.Code = "1000";
                respData.Message = "请求数据有误";
            }
            respJsonStr = ServerPlatform.Json_ToShortDateString(respData).Replace("\r\n", "");
        }
    }
}
