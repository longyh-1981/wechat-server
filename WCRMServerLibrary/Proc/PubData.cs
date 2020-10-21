using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WCRMServer.Proc
{
    public class reqData
    {
        public Object data = null;
        public string sign = string.Empty;
    }

    public class respData
    {
        public int code = 10000;
        public string message = string.Empty;
        public Object data = null;
    }

    public class TDeptItemInfo
    {
        public string orgName = string.Empty;
        public string orgID = string.Empty;
        public int currentLevelID = 0;
        public string parentOrgID = string.Empty;
    }

    public class TDeptInfo
    {
        public int opType = 0;
        public List<TDeptItemInfo> orgList = new List<TDeptItemInfo>();
    }

    public class TCategoryItemInfo
    {
        public string classifyName = string.Empty;
        public string classifyID = string.Empty;
        public int currentLevelID = 0;
        public string parentID = string.Empty;
    }

    public class TCategoryInfo
    {
        public int opType = 0;
        public List<TCategoryItemInfo> productClassifyList = new List<TCategoryItemInfo>();
    }

    public class TBrandItemInfo
    {
        public string brandName = string.Empty;
        public string brandEngName = string.Empty;
        public string brandID = string.Empty;
    }

    public class TBrandInfo
    {
        public int opType = 0;
        public List<TBrandItemInfo> productBrandList = new List<TBrandItemInfo>();
    }

    public class TArticleItemInfo
    {
        public string orgID = string.Empty;
        public string productClassifyID = string.Empty;
        public string productBrandID = string.Empty;
        public string productID = string.Empty;
        public string productName = string.Empty;
        public int productStatus = 0;
        public string productCharge = string.Empty;
        public string taxCode = string.Empty;
        public string taxRate = string.Empty;
    }

    public class TArticleInfo
    {
        public int opType = 0;
        public List<TArticleItemInfo> productList = new List<TArticleItemInfo>();
    }

    public class TPaymentItemInfo
    {
        public string payID = string.Empty;
        public string payName = string.Empty;
        public int currentLevel = 0;
        public string parentPayId = string.Empty;
    }

    public class TPaymentInfo
    {
        public int opType = 0;
        public List<TPaymentItemInfo> payInforList = new List<TPaymentItemInfo>();
    }

    public class TMemberReq
    {
        public string memberID = string.Empty;
    }

    public class TMemberInfo
    {
        public int code = 0;
        public string message = string.Empty;
        public int score = 0;
        public string grade = string.Empty;
        public string growth = string.Empty;
        public int memberStatus = 0;
    }

    public class AppReqData
    {
        public string TimeStamp = string.Empty;
        public string Sign = string.Empty;
        public string Format = string.Empty;
        public string Encrypt = string.Empty;
        public Object Data = null;
    }

    public class AppRespone
    {
        public string Code = "0";
        public string Message = string.Empty;
        public Object Data = null;
    }

    public class CrmDataResponse
    {
        public string remark = string.Empty;
    }

    public class VipCardInfo
    {
        public int crmVipId = 0;
        public string VipId = string.Empty;
        public string CardCode = string.Empty;
        public string CardTrack = string.Empty;
        public string CardTypeId = string.Empty;
        public string VipName = string.Empty;
        public string Mobile = string.Empty;
        public string IDNo = string.Empty;
        public int Gender = -1;
        public string BirthDay = string.Empty;
        public string Address = string.Empty;
        public string StoreCode = string.Empty;
        public string Manager = string.Empty;
        public int storeId = 1;
        public bool IsExists = false;
    }

    public class VipCardUpload
    {
        public List<VipCardInfo> VipCardInfoArray = new List<VipCardInfo>();
    }

    public class GetVipCardTradeItemReq
    {
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
        public GetVipCardTradeItemReq()
        {
            this.BeginDate = string.Empty;
            this.EndDate = string.Empty;
        }
    }

    public class GetVipCardTradeItemReq2
    {
        public string BeginDate { get; set; }
        public string EndDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public GetVipCardTradeItemReq2()
        {
            this.BeginDate = string.Empty;
            this.EndDate = string.Empty;
            this.Page = 1;
            this.PageSize = 100;
        }
    }

    public class vipReceiptArticleInfo
    {
        public int Inx = 0;
        public string GoodsCode = string.Empty;
        public string GoodsName = string.Empty;
        public double Quantity = 0;
        public double Amount = 0;
        public double AmountForPoints = 0;
    }

    public class vipReceiptPaymentInfo
    {
        public int Inx = 0;
        public string PaymentCode = string.Empty;
        public string PaymentName = string.Empty;
        public string PayMoney = string.Empty;
        public string BankCardNo = string.Empty;
    }

    public class vipReceiptOfferCouponInfo
    {
        public int Inx = 0;
        public string CouponId = string.Empty;
        public string CouponName = string.Empty;
        public int TypeId = 0;
        public double OfferMoney = 0;
    }

    public class vipOriginalReceiptInfo
    {
        public string OriginalPosId = string.Empty;
        public string OriginalTradeNo = string.Empty;
    }

    public class vipReceiptInfo
    {
        public int ServerBillId = 0;
        public string StoreCode = string.Empty;
        public string StoreName = string.Empty;
        public string PosId = string.Empty;
        public string TradeNo = string.Empty;
        public string TimeShopping = string.Empty;
        public string AccountDate = string.Empty;
        public string MemberId = string.Empty;
        public int TradeType = 0;
        public int TradeStatus = 0;
        public string CashierCode = string.Empty;
        public double PayCashCardMoney = 0;
        public List<vipReceiptArticleInfo> GoodsItems = null;
        public List<vipReceiptPaymentInfo> PaymentItems = null;
        public List<vipReceiptOfferCouponInfo> OfferCouponItems = null;
        public vipOriginalReceiptInfo OriginalTradeInfo = null;
    }

    public class vipOriginalBillInto
    {
        public int serverBillId = 0;
        public int originalServerBillId = 0;
        public string originalPosId = string.Empty;
        public string originalTradeNo = string.Empty;
    }

    public class ReceiptInfoList
    {
        public List<vipReceiptInfo> VipCardTradeInfoArray = new List<vipReceiptInfo>();
    }

    public class VipCardDiscBillItem
    {
        public string ItemCode = string.Empty;
        public double DiscRate = 0;
        public int ArticleId = 0;
    }

    public class VipCardDiscBillInfo
    {
        public string RuleId = string.Empty;
        public int CardTypeId = 0;
        public string BeginDate = string.Empty;
        public string EndDate = string.Empty;
        public List<VipCardDiscBillItem> ItemDiscRuleInfoArray = new List<VipCardDiscBillItem>();
    }

    public class VipCardDiscReq
    {
        public List<VipCardDiscBillInfo> DiscRuleInfoArray = new List<VipCardDiscBillInfo>();
    }

    public class VipCardTypeInfo
    {
        public int CardTypeId = 0;
        public string CardTypeName = string.Empty;
    }

    public class WeChatCouponInfo
    {
        public string CardID = string.Empty;
        public int CouponId = 0;
        public double Amount = 0;
        public string BeginDate = string.Empty;
    }

    public class WeChatCouponInfoReq
    {
        public List<WeChatCouponInfo> WeChatCouponInfoArray = new List<WeChatCouponInfo>();
    }

    public class TradeArticleInfo
    {
        public int Inx = 0;
        public string GoodsCode = string.Empty;
        public string GoodsName = string.Empty;
        public double Quantity = 0;
        public double Amount = 0;
    }

    public class TradePaymentInfo
    {
        public int Inx = 0;
        public string PaymentCode = string.Empty;
        public string PaymentName = string.Empty;
        public string PayMoney = string.Empty;
        public string BankCardNo = string.Empty;
    }

    public class OriginalTradeInfo
    {
        public string OriginalPosId = string.Empty;
        public string OriginalTradeNo = string.Empty;
    }

    public class TradeInfo
    {
        public string PosId = string.Empty;
        public string TradeNo = string.Empty;
        public string TimeShopping = string.Empty;
        public string AccountDate = string.Empty;
        public int TradeType = 0;
        public int TradeStatus = 0;
        public string CashierCode = string.Empty;
        public List<TradeArticleInfo> GoodsItems = new List<TradeArticleInfo>();
        public List<TradePaymentInfo> PaymentItems = new List<TradePaymentInfo>();
        public OriginalTradeInfo OriginalTradeInfo = null;
    }

    public class TradeInfoList
    {
        public List<TradeInfo> TradeInfoArray = new List<TradeInfo>();
    }

    public class OfferCouponReq
    {
        public string UniqueId = string.Empty;
        public int CouponType = 0;
        public double Amount = 0;
        public int Quantity = 0;
        public string BeginDate = string.Empty;
        public string EndDate = string.Empty;
    }

    public class CouponInfo
    {
        public int CouponType = 0;
        public string CouponCode = string.Empty;
        public double Amount = 0;
        public string BeginDate = string.Empty;
        public string EndDate = string.Empty;
        public string remark = string.Empty;
    }

    public class CouponInfoList
    {
        public List<CouponInfo> CouponInfoArray = new List<CouponInfo>();
    }

    public class TUploadTradeProductInfo
    {
        public int Index = 0;
        public string productID = string.Empty;
        public double productNum = 0;
        public double charge = 0;
        public double scoreCharge = 0;
    }

    public class TUploadTradePayInfo
    {
        public string payID = string.Empty;
        public string payName = string.Empty;
        public string BankCardID = string.Empty;
        public double payCharge = 0;
    }

    public class TUploadTradeCouponInfo
    {
        public string couponID = string.Empty;
        public int couponKind = 0;
    }

    public class TUploadTradeInfo
    {
        public int type = 1;
        public string storeCode = string.Empty;
        public string storeName = string.Empty;
        public string memberID = string.Empty;
        public string machineID = string.Empty;
        public string orderID = string.Empty;
        public string oldMachineID = string.Empty;
        public string oldOrderID = string.Empty;
        public string orderOccurTime = string.Empty;
        public string orderBelongTime = string.Empty;
        public double cardCharge = 0;
        public List<TUploadTradeProductInfo> productList = new List<TUploadTradeProductInfo>();
        public List<TUploadTradePayInfo> payList = new List<TUploadTradePayInfo>();
        public List<TUploadTradeCouponInfo> couponList = new List<TUploadTradeCouponInfo>();
    }

    public class TUploadCouponItemInfo
    {
        public string couponName = string.Empty;
        public string couponRule = string.Empty;
        public int couponType = -1;
        public string couponValidTime = string.Empty;
        public string payID = string.Empty;
        public string couponDesInfor1 = string.Empty;
        public string couponDesInfor2 = string.Empty;
        public string couponDesInfor3 = string.Empty;
    }

    public class TUploadCouponInfo
    {
        public int pageNum = 0;
        public int curPageNum = 0;
        public int curNum = 0;
        public int couponKind = 0;
        public List<TUploadCouponItemInfo> couponList = new List<TUploadCouponItemInfo>();
    }

    public class TUploadTradeCouponItem
    {
        public int couponKind = -1;
        public int couponType = 0;
        public string couponValidTime = string.Empty;
        public int Index = 0;
        public string productID = string.Empty;
        public double couponCharge = 0;
    }

    public class TUploadTradeInfoOfCoupon
    {
        public string machineID = string.Empty;
        public string orderID = string.Empty;
        public List<TUploadTradeCouponItem> couponUsedList = new List<TUploadTradeCouponItem>();
    }

    public class TUploadTradeInfo2
    {
        public int id = 0;
        public string machineID = string.Empty;
        public string orderID = string.Empty;
        public int tm = 0;
    }

    public class TCodeInfo
    {
        public string code = string.Empty;
    }

    public class TVipDiscItemInfo
    {
        public int type = 0;
        public List<TCodeInfo> inforList = new List<TCodeInfo>();
    }

    public class TVipDiscRuleInfo
    {
        public int ruleNo = 0;
        public int joinFlag = 0;
        public double discRate = 0;
        public List<TVipDiscItemInfo> discountInforList = new List<TVipDiscItemInfo>();
    }

    public class TVipDiscBillInfo
    {
        public int id = 0;
        public int type = 0;
        public int gradeLevel = 0;
        public string deptCode = string.Empty;
        public string validStartTime = string.Empty;
        public string validEndTime = string.Empty;
        public bool bIsPriority = false;
        public List<TVipDiscRuleInfo> disRuleInforList = new List<TVipDiscRuleInfo>();
    }

    public class TVipDiscBillId
    {
        public int id = 0;
        public int billId = 0;
        public int type = 0;
        public DateTime updateTime = DateTime.MinValue;
    }

    public class ReviewVipDiscRuleReq
    {
        public string id = string.Empty;
        public int approveResult = 0;
    }

    public class MemberLevelInfo
    {
        public int memberLevelID = 0;
    }

    public class ProductInfo
    {
        public int type = 0;//值为1：部门，值为2：品类，值为3：品牌（包含），值为4：品牌（不包含）
        public string id = string.Empty;//部门ID或品类ID或品牌ID对于部门ID或品类ID为父级别的，所有子类别都必须选上；
    }

    //public class ScoreRuleProductInfo
    //{
    //    public List<MemberLevelInfo> memberLevelArray = new List<MemberLevelInfo>();
    //    public List<ProductInfo> productParticipationArray = new List<ProductInfo>();
    //    public List<ProductInfo> productNotParticipationArray = new List<ProductInfo>();
    //}

    public class ScoreRuleInfo
    {
        public string ruleName = string.Empty;
        public string ruleID = string.Empty;
        public int ruleOpType = 0;
        public string beginDate = string.Empty;
        public string endDate = string.Empty;
        //public int scoreLowLimit = 0;
        //public int scoreUpLimit = 0;
        //public string scoreRule = string.Empty;
        public List<int> memberLevelArray = new List<int>();
        public List<ProductInfo> productArray = new List<ProductInfo>();
    }

    public class PointDiscountSettingInfo
    {
        public int scoreLowLimit = 0;
        public int scoreUpLimit = 0;
        public string scoreRule = string.Empty;
        public int Status = 0;
    }

    public class MemberScoreInfo
    {
        public int score = 0;
        public string grade = string.Empty;
        public string growth = string.Empty;
        public int memberStatus = 0;
    }

    public class TMemberDeduPointReq
    {
        public string memberID = string.Empty;
        public int type = 0;
        public string posSerialNum = string.Empty;
        public string machineID = string.Empty;
        public string orderID = string.Empty;
        public string oldOrderID = string.Empty;
        public int score = 0;
    }

    public class TMemberDeduPointResp
    {
        public int totalScore = 0;
        public string serialNum = string.Empty;
    }

    public class TMemberDeduPointCancelReq
    {
        public int type = 0;
        public string crmSerialNum = string.Empty;
    }

    public class TGetCrmDeduSerialReq
    {
        public string posSerialNum = string.Empty;
    }

    public class TGetCrmDeduSerialResp
    {
        public string serialNum = string.Empty;
    }

    public class TGetCrmDeduStatusReq
    {
        public string crmSerialNum = string.Empty;
    }

    public class TGetCrmDeduStatusResp
    {
        public int status = 0;
    }

    public class TGetBillStatusReq
    {
        public string memberID = string.Empty;
        public string machineID = string.Empty;
        public string orderID = string.Empty;
    }

    public class TGetBillStatusResp
    {
        public bool isBilled = false;
    }

    public class TNONMemberJLBH
    {
        public string posId = string.Empty;
        public int billId = 0;
        public List<int> Ids = new List<int>();
    }

    public class TUploadNonMemberTradeProductInfo
    {
        public int Index = 0;
        public string productID = string.Empty;
        public double productNum = 0;
        public double charge = 0;
        //public double scoreCharge = 0;
    }

    public class TUploadNonMemberTradePayInfo
    {
        public string payID = string.Empty;
        public string payName = string.Empty;
        public string BankCardID = string.Empty;
        public double payCharge = 0;
    }

    public class TUploadNonMemberTradeInfo
    {
        public int type = 1;
        public string storeCode = string.Empty;
        public string storeName = string.Empty;
        //public string memberID = string.Empty;
        public string machineID = string.Empty;
        public string orderID = string.Empty;
        public string oldMachineID = string.Empty;
        public string oldOrderID = string.Empty;
        public string orderOccurTime = string.Empty;
        public string orderBelongTime = string.Empty;
        public double cardCharge = 0;
        public List<TUploadNonMemberTradeProductInfo> productList = new List<TUploadNonMemberTradeProductInfo>();
        public List<TUploadNonMemberTradePayInfo> payList = new List<TUploadNonMemberTradePayInfo>();
        //public List<TUploadTradeCouponInfo> couponList = new List<TUploadTradeCouponInfo>();
    }

    public class TUploadReceiptFlagReq
    {
        public string machineID = string.Empty;
        public string orderID = string.Empty;
        public int orderStatus = 0;
    }

    public class TRegisterReq
    {
        public string Mobile = string.Empty;
        public string MemberName = string.Empty;
        public string BirthDay = string.Empty;
        public int Gender = -1;
        public string CardCode = string.Empty;
        public string UnionId = string.Empty;
        public string OpenId = string.Empty;
    }

    public class TRegisterResponse
    {
        public string MemberId = string.Empty;
        public string CardId = string.Empty;
        public string CardCode = string.Empty;
        public int CardTypeId = 0;
        public string CardTypeName = string.Empty;
        public string GradeName = string.Empty;
    }

    public class TUpdateVipCardInfoReq
    {
        public string MemberId = string.Empty;
        public string MemberName = string.Empty;
        public string BirthDay = string.Empty;
        public string IDNo = string.Empty;
        public string Adress = string.Empty;
        public string MemberLabel = string.Empty;
        public int Gender = -1;
    }

    public class TGetMemberInfoReq
    {
        public string Mobile = string.Empty;
        public string OpenId = string.Empty;
    }

    public class TCardInfo
    {
        public int CardId = 0;
        public string CardCode = string.Empty;
        public int CardTypeId = 0;
        public string CardTypeName = string.Empty;
        public string GradeName = string.Empty;
        public string QRCode = string.Empty;
    }

    public class TGetMemberInfoResponse
    {
        public string MemberId = string.Empty;
        public string Mobile = string.Empty;
        public string MemberName = string.Empty;
        public int Gender = 0;
        public string BirthDay = string.Empty;
        public string UnionId = string.Empty;
        public string OpenId = string.Empty;
        public string IDNo = string.Empty;
        public string Adress = string.Empty;
        public string TotalPoints = string.Empty;
        public string TotalCoupon = string.Empty;
        public string MemberLabel = string.Empty;
        public List<TCardInfo> CardInfoList = null;
    }
}
