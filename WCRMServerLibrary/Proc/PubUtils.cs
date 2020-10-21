using System;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Security.Cryptography;
using System.IO;
using System.Xml;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;


namespace WCRMServer.Proc
{
    public class PubUtils
    {
        private static Regex REG_URL_ENCODING = new Regex(@"%[a-f0-9]{2}");
        private static double MyEpsilon = 0.00005;
        //private static double MyNegativeEpsilon = -0.00005;

        private static decimal MyEpsilon2 = 0.00005m;
        //private static decimal MyNegativeEpsilon2 = -0.00005m;
        private static string EncodeUrl(string str)
        {
            if (str == null)
            {
                return null;
            }
            String stringToEncode = HttpUtility.UrlEncode(str, Encoding.UTF8).Replace("+", "%20").Replace("*", "%2A").Replace("(", "%28").Replace(")", "%29");
            return REG_URL_ENCODING.Replace(stringToEncode, m => m.Value.ToUpperInvariant());
        }

        public static string getError(int errorCode)
        {
            string errorName = string.Empty;
            switch (errorCode)
            {
                case 10000:
                    errorName = "成功";
                    break;
                case 20000:
                    errorName = "参数错误";
                    break;
                case 70000:
                    errorName = "用户操作错误";
                    break;
                case 80000:
                    errorName = "CRM暂时性的错误";
                    break;
                default:
                    errorName = "其他错误";
                    break;
            }
            return errorName;   

        }

        public static long ConvertDateTimeInt(System.DateTime time)
        {

            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));

            return (long)(time - startTime).TotalMilliseconds;

        }

        public static string ConvertDateTimeStr(System.DateTime time)
        {

            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));

            return (time - startTime).TotalMilliseconds.ToString("f0");

        }

        public static DateTime convertIntDateTime(int time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
            DateTime dt = startTime.AddSeconds(time);
            return dt;
        }

        public static string Json_ToShortDateString(object json)
        {
            Newtonsoft.Json.Converters.IsoDateTimeConverter timeConverter = new Newtonsoft.Json.Converters.IsoDateTimeConverter();
            timeConverter.DateTimeFormat = "yyyy'-'MM'-'dd";
            return JsonConvert.SerializeObject(json, Newtonsoft.Json.Formatting.Indented, timeConverter);
        }

        public static int Truncate(double a)
        {
            if (a >= 0)
                return Convert.ToInt32(Math.Truncate(a + MyEpsilon));
            else
                return Convert.ToInt32(Math.Truncate(a - MyEpsilon));
        }

        public static int Truncate(decimal a)
        {
            if (a >= 0)
                return Convert.ToInt32(Math.Truncate(a + MyEpsilon2));
            else
                return Convert.ToInt32(Math.Truncate(a - MyEpsilon2));
        }

        public static string GetResponseId(string randomStr)
        {
            string responseId = string.Empty;
            responseId = string.Format("P{0}{1}", DateTime.Now.ToString("yyyyMMddHHmmssfff"), randomStr);
            return responseId;
        }

        public static string GetSign(string Data, string appId, string timeStamp, string appSecret)
        {
            //--组装计算签名的串
            StringBuilder sb = new StringBuilder();
            string StringTemp = EncodeUrl(Data);
            sb.Append(StringTemp).Append(appId).Append(timeStamp).Append(appSecret);
            string bizParas = sb.ToString();
            ServerPlatform.WriteLog(DateTime.Now.ToString("yyyy-MM-dd"), "\r\n MD5 data:" + bizParas.ToString());
            //bizParas = EncodeUrl(bizParas);
            //ServerPlatform.WriteLog(DateTime.Now.ToString("yyyy-MM-dd"), "\r\n MD5 data2:" + bizParas.ToString());
            //bizParas = bizParas + appSecret;
            //ServerPlatform.WriteLog(DateTime.Now.ToString("yyyy-MM-dd"), "\r\n MD5 data3:" + bizParas.ToString());
            string signStr = PasswordEncryptUtils.MD5Encrypt(bizParas);
            return signStr.ToUpper();
        }

        public static int GetSeq(string connName, int dbId, String tableName, int step = 1)
        {
            DbConnection conn = DbConnManager.GetDbConnection(connName);
            DbCommand cmd = conn.CreateCommand();
            StringBuilder sql = new StringBuilder();
            int seq = step;
            try
            {
                conn.Open();
                DbTransaction dbTrans = conn.BeginTransaction();
                try
                {
                    cmd.Transaction = dbTrans;
                    sql.Append("update TBL_SEQUENCE set SEQ = SEQ + ").Append(step).Append(" where TABLE_NAME = '").Append(tableName).Append("'");
                    cmd.CommandText = sql.ToString();
                    if (cmd.ExecuteNonQuery() == 0)
                    {
                        sql.Length = 0;
                        sql.Append("insert into TBL_SEQUENCE (TABLE_NAME,SEQ) values ('").Append(tableName).Append("', ").Append(step).Append(")");
                        cmd.CommandText = sql.ToString();
                        cmd.ExecuteNonQuery();
                    }
                    else
                    {
                        sql.Length = 0;
                        sql.Append("select SEQ from TBL_SEQUENCE where TABLE_NAME = '").Append(tableName).Append("'");
                        cmd.CommandText = sql.ToString();
                        DbDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            seq = DbUtils.GetInt(reader, 0);
                        }
                        reader.Close();
                    }
                    dbTrans.Commit();
                }
                catch (Exception e)
                {
                    dbTrans.Rollback();
                    throw e;
                }
            }
            finally
            {
                conn.Close();
            }
            return dbId * 100000000 + seq;
        }

        public static DateTime GetVipCardValidDate(out string PrefixCode, out string SuffixCoe, out int CodeLength, DbCommand cmd, int vipTypeId)
        {
            PrefixCode = string.Empty;
            SuffixCoe = string.Empty;
            CodeLength = 0;
            int ValidDateNum = 0;
            StringBuilder sql = new StringBuilder();
            sql.Length = 0;
            sql.Append("select CARDCODELENGTH,PREFIXCODE,SUFFIXCODE,VALIDITYDATE from CARDTYPEINFO where CARDTYPEID = ").Append(vipTypeId);
            cmd.CommandText = sql.ToString();
            DbDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                CodeLength = DbUtils.GetInt(reader, 0);
                PrefixCode = DbUtils.GetString(reader, 1);
                SuffixCoe = DbUtils.GetString(reader, 2);
                ValidDateNum = DbUtils.GetInt(reader, 3);
            }
            reader.Close();
            DateTime CurrDate = DbUtils.GetDbServerTime(cmd);
            if (ValidDateNum == 0)
                ValidDateNum = 1;
            if (CodeLength == 0)
                CodeLength = 10;
            CurrDate.AddYears(ValidDateNum);
            return CurrDate;
        }
    }

    public class LogFile
    {
        private string myDateStr = null;
        private string myPathName = null;
        private string myFileNamePrefix = null;
        private StreamWriter myWriter = null;
        private Object sync = new Object();

        public LogFile(string pathName, string fileNamePrefix)
        {
            myPathName = pathName;
            myFileNamePrefix = fileNamePrefix;
            if ((myPathName != null) && (myPathName.Length > 0) && (!Directory.Exists(myPathName)))
                Directory.CreateDirectory(myPathName);
        }

        public void Write(String dateStr, String logText)
        {
            if ((myPathName == null) || (myPathName.Length == 0) || (dateStr == null) || (dateStr.Length == 0))
                return;
            lock (sync)
            {
                if ((myDateStr == null) || (!myDateStr.Equals(dateStr)))
                {
                    if (myWriter != null)
                    {
                        myWriter.Close();
                        myWriter = null;
                    }
                    myDateStr = dateStr;
                    StringBuilder sb = new StringBuilder();
                    if (myFileNamePrefix != null)
                        sb.Append(myFileNamePrefix);
                    sb.Append(myDateStr).Append(".log");
                    try
                    {
                        myWriter = new StreamWriter(Path.Combine(myPathName, sb.ToString()), true, Encoding.GetEncoding(936));
                        //myWriter = new StreamWriter(Path.Combine(myPathName, sb.ToString()), true);
                    }
                    catch (IOException e)
                    {
                        //文件已被其它进程打开了
                        for (int i = 1; i < 100; i++)
                        {
                            try
                            {
                                sb.Length = 0;
                                if (myFileNamePrefix != null)
                                    sb.Append(myFileNamePrefix);
                                sb.Append(myDateStr).Append("_").Append(i.ToString().PadLeft(2, '0')).Append(".log");
                                myWriter = new StreamWriter(Path.Combine(myPathName, sb.ToString()), true, Encoding.GetEncoding(936));
                                //myWriter = new StreamWriter(Path.Combine(myPathName, sb.ToString()), true);
                                break;
                            }
                            catch (IOException ex)
                            {
                            }
                        }
                    }
                }

                if (myWriter != null)
                {
                    try
                    {
                        myWriter.Write(logText);
                        myWriter.Flush();
                    }
                    catch (IOException e)
                    {

                    }
                }
            }
        }

        public void Close()
        {
            lock (sync)
            {
                try
                {
                    if (myWriter != null)
                    {
                        myWriter.Close();
                    }
                }
                catch (IOException e)
                {

                }
            }
        }
    }

    public class ErrorFileWriter
    {
        private string myPathName = null;
        private StreamWriter myWriter = null;
        private Object sync = new Object();

        public ErrorFileWriter(string pathName)
        {
            myPathName = pathName;
            if ((myPathName != null) && (myPathName.Length > 0) && (!Directory.Exists(myPathName)))
                Directory.CreateDirectory(myPathName);
        }
        public bool SetFileName(string fileName)
        {
            if ((myPathName != null) && (myPathName.Length > 0))
            {
                try
                {
                    myWriter = new StreamWriter(Path.Combine(myPathName, fileName), true, Encoding.GetEncoding(936));
                    //myWriter = new StreamWriter(Path.Combine(myPathName, fileName), true);
                }
                catch (IOException e)
                {
                    myWriter = null;
                    return false;
                }
            }
            return true;
        }
        public void Write(String logText)
        {
            if (myWriter != null)
            {
                lock (sync)
                {
                    try
                    {
                        myWriter.Write(logText);
                        myWriter.Flush();
                    }
                    catch (IOException e)
                    {

                    }
                }
            }
        }
        public void Close()
        {
            if (myWriter != null)
            {
                lock (sync)
                {
                    try
                    {
                        myWriter.Close();
                    }
                    catch (IOException e)
                    {

                    }
                }
            }
        }
    }

    public class FormatUtils
    {
        private static char[] hexCharArray = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

        public static DateTime ParseDateString(string str)
        {
            DateTime date = DateTime.MinValue;
            if (str.Length > 0)
                DateTime.TryParseExact(str, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out date);
            return date;
        }
        public static string DateToString(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }
        public static DateTime ParseDatetimeString(string str)
        {
            DateTime datetime = DateTime.MinValue;
            if (str.Length > 0)
                DateTime.TryParseExact(str, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out datetime);
            return datetime;
        }
        public static string DatetimeToString(DateTime date)
        {
            return date.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string BytesToHexStr(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(hexCharArray[(bytes[i] >> 4) & 0x0F]);
                sb.Append(hexCharArray[bytes[i] & 0x0F]);
            }
            return sb.ToString();
        }

        public static string StringToBase64(string str)
        {
            System.Text.Encoding encode = System.Text.Encoding.ASCII;
            byte[] bytedata = encode.GetBytes(str);
            string strBase64 = Convert.ToBase64String(bytedata, 0, bytedata.Length);
            return strBase64;
        }
    }

    public class EncryptUtils
    {
        public static byte[] DesEncrypt(byte[] key, byte[] src)
        {
            byte[] keyIV = key;
            DESCryptoServiceProvider provider = new DESCryptoServiceProvider();
            provider.Mode = CipherMode.ECB;
            provider.Padding = PaddingMode.None;
            MemoryStream mStream = new MemoryStream();
            CryptoStream cStream = new CryptoStream(mStream, provider.CreateEncryptor(key, keyIV), CryptoStreamMode.Write);
            cStream.Write(src, 0, src.Length);
            cStream.FlushFinalBlock();
            return mStream.ToArray();
        }

        public static byte[] DesDecrypt(byte[] key, byte[] src)
        {
            byte[] keyIV = key;
            DESCryptoServiceProvider provider = new DESCryptoServiceProvider();
            provider.Mode = CipherMode.ECB;
            provider.Padding = PaddingMode.None;
            MemoryStream mStream = new MemoryStream();
            CryptoStream cStream = new CryptoStream(mStream, provider.CreateDecryptor(key, keyIV), CryptoStreamMode.Write);
            cStream.Write(src, 0, src.Length);
            cStream.FlushFinalBlock();
            return mStream.ToArray();

        }

        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="clearTxt"></param>
        /// <returns></returns>
        public static string AesEncrypt(string secretKey, string clearTxt)
        {

            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);

            using (RijndaelManaged cipher = new RijndaelManaged())
            {
                cipher.Mode = CipherMode.ECB;
                cipher.Padding = PaddingMode.PKCS7;
                cipher.KeySize = 128;
                cipher.BlockSize = 128;
                cipher.Key = keyBytes;
                cipher.IV = keyBytes;

                byte[] valueBytes = Encoding.UTF8.GetBytes(clearTxt);

                byte[] encrypted;
                using (ICryptoTransform encryptor = cipher.CreateEncryptor())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream writer = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            writer.Write(valueBytes, 0, valueBytes.Length);
                            writer.FlushFinalBlock();
                            encrypted = ms.ToArray();

                            StringBuilder sb = new StringBuilder();
                            for (int i = 0; i < encrypted.Length; i++)
                                sb.Append(Convert.ToString(encrypted[i], 16).PadLeft(2, '0'));
                            return sb.ToString().ToUpperInvariant();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="encrypted"></param>
        /// <returns></returns>
        public static string AesDecypt(string secretKey, string encrypted)
        {

            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);

            using (RijndaelManaged cipher = new RijndaelManaged())
            {
                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.PKCS7;
                cipher.KeySize = 128;
                cipher.BlockSize = 128;
                cipher.Key = keyBytes;
                cipher.IV = keyBytes;

                List<byte> lstBytes = new List<byte>();
                for (int i = 0; i < encrypted.Length; i += 2)
                    lstBytes.Add(Convert.ToByte(encrypted.Substring(i, 2), 16));

                using (ICryptoTransform decryptor = cipher.CreateDecryptor())
                {
                    using (MemoryStream msDecrypt = new MemoryStream(lstBytes.ToArray()))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }
    }

    public class PasswordEncryptUtils
    {
        private const int C1 = 21469;
        private const int C2 = 12347;
        private const int KeyWord = 26493;

        private static string Encrypt(string src)
        {
            int key = KeyWord;
            char[] dest = new char[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                dest[i] = (char)((byte)src[i] ^ (key >> 8));
                key = ((dest[i] + key) * C1 + C2) % 65536;
            }
            return new string(dest);
        }

        private static string Decrypt(string src)
        {
            int key = KeyWord;
            char[] dest = new char[src.Length];
            for (int i = 0; i < src.Length; i++)
            {
                dest[i] = (char)((byte)src[i] ^ (key >> 8));
                key = ((src[i] + key) * C1 + C2) % 65536;
            }
            return new string(dest);
        }

        public static string PasswordEncrypt(string src)
        {
            int len = src.Length;
            char[] src2 = new char[len];
            for (int i = 0; i < len; i++)
            {
                src2[i] = src[len - i - 1];
            }
            string dest2 = Encrypt(new string(src2));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                sb.Append(((byte)dest2[i]).ToString("d3"));
            }
            return sb.ToString();
        }

        public static string PasswordDecrypt(string src)
        {
            int len = src.Length;
            if ((len > 0) && (len % 3 == 0))
            {
                len = len / 3;
                try
                {
                    char[] src2 = new char[len];
                    for (int i = 0; i < len; i++)
                    {
                        src2[i] = (char)int.Parse(src.Substring(i * 3, 3));
                    }
                    string dest2 = Decrypt(new string(src2));
                    char[] dest = new char[len];
                    for (int i = 0; i < len; i++)
                    {
                        dest[i] = dest2[len - i - 1];
                    }

                    return new string(dest);
                }
                catch
                {
                    return string.Empty;
                }
            }
            return string.Empty;
        }

        private static byte[] HexStrToBytes(string s)
        {
            byte[] bytes = new byte[s.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(s.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        public static string DESEncrypt(string data, string sKey)
        {
            byte[] key = Encoding.ASCII.GetBytes(sKey);
            int len1 = data.Length;
            if (len1 == 0)
                return string.Empty;
            byte[] bytes1 = Encoding.ASCII.GetBytes(data);
            byte[] bytes2 = null;
            int len2 = bytes1.Length;
            int mod = (len2 % 8);
            if (mod == 0)
                bytes2 = new byte[len2];
            else
                bytes2 = new byte[len2 + 8 - mod];
            for (int i = 0; i < len2; i++)
            {
                bytes2[i] = bytes1[i];
            }
            if (mod > 0)
            {
                for (int i = 0; i < (8 - mod); i++)
                {
                    bytes2[len2 + i] = (byte)0;
                }
            }
            byte[] bytes3 = EncryptUtils.DesEncrypt(key, bytes2);
            return FormatUtils.BytesToHexStr(bytes3);
        }

        public static string DESDecrypt(string data, string sKey)
        {
            byte[] key = Encoding.ASCII.GetBytes(sKey);
            int len1 = data.Length;
            if (len1 == 0)
                return string.Empty;
            byte[] bytes1 = HexStrToBytes(data);
            int len2 = bytes1.Length;
            int mod = (len2 % 8);
            if (mod != 0)
            {
                return string.Empty;
            }
            int j = 0;
            byte[] bytes2 = EncryptUtils.DesDecrypt(key, bytes1);
            for (int i = bytes2.Length - 1; i >= 0; i--)
            {
                if (bytes2[i] == 0)
                    j++;
            }
            byte[] bytes3 = new byte[bytes2.Length - j];
            for (int i = 0; i < bytes2.Length - j; i++)
            {
                bytes3[i] = bytes2[i];
            }
            return System.Text.Encoding.Default.GetString(bytes3);
        }

        #region MD5 加密（散列码 Hash 加密）
        /// <summary>
        /// MD5 加密（散列码 Hash 加密）
        /// </summary>
        /// <param name="code">明文</param>
        /// <returns>密文</returns>
        public static string MD5Encrypt(string code)
        {
            /* 获取原文内容的byte数组 */
            byte[] sourceCode = Encoding.UTF8.GetBytes(code);
            byte[] targetCode;    //声明用于获取目标内容的byte数组

            /* 创建一个MD5加密服务提供者 */
            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            targetCode = md5.ComputeHash(sourceCode);    //执行加密
            md5.Clear();
            /* 对字符数组进行转码 */
            StringBuilder sb = new StringBuilder();
            foreach (byte b in targetCode)
            {
                sb.AppendFormat("{0:X2}", b);
            }

            return sb.ToString();
        }
        #endregion

        //public static byte[] AESEncrypt(string key, string iv, string text)
        //{
        //    byte[] data = Encoding.UTF8.GetBytes(text);
        //    SymmetricAlgorithm aes = Rijndael.Create();
        //   // byte[] keyArray = Encoding.UTF8.GetBytes(key);
        //    //byte[] IVArray = Encoding.UTF8.GetBytes(iv);

        //    Byte[] bKey = new Byte[32];
        //    Array.Copy(Encoding.UTF8.GetBytes(key.PadRight(bKey.Length)), bKey, bKey.Length);
        //    Byte[] bVector = new Byte[16];
        //    Array.Copy(Encoding.UTF8.GetBytes(iv.PadRight(bVector.Length)), bVector, bVector.Length);

        //    aes.Key = bKey;
        //    aes.IV = bVector;
        //    aes.Mode = CipherMode.CBC;
        //    aes.Padding = PaddingMode.Zeros;

        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        //        {
        //            cs.Write(data, 0, data.Length);
        //            cs.FlushFinalBlock();
        //            byte[] cipherBytes = ms.ToArray(); // 得到加密后的字节数组  
        //            cs.Close();
        //            ms.Close();
        //            aes.Clear();

        //            return cipherBytes;
        //        }
        //    }
        //}

        //// AES 解密  
        //public static string AESDecrypt(string key, string iv, byte[] data)
        //{
        //    SymmetricAlgorithm aes = Rijndael.Create();
        //    byte[] keyArray = Encoding.UTF8.GetBytes(key);
        //    byte[] IVArray = Encoding.UTF8.GetBytes(iv);
        //    aes.Key = keyArray;
        //    aes.IV = keyArray;
        //    aes.Mode = CipherMode.CBC;
        //    aes.Padding = PaddingMode.Zeros;
        //    byte[] decryptBytes = new byte[data.Length];

        //    using (MemoryStream ms = new MemoryStream(data))
        //    {
        //        using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
        //        {
        //            cs.Read(decryptBytes, 0, decryptBytes.Length);
        //            cs.Close();
        //            ms.Close();
        //        }
        //    }
        //    aes.Clear();

        //    return System.Text.Encoding.Unicode.GetString(decryptBytes).Replace("\0", " ");
        //} 
    }

    public static class AESHelper
   {
      /// <summary>
      /// AES加密
      /// </summary>
      /// <param name="Data">被加密的明文</param>
      /// <param name="Key">密钥</param>
      /// <param name="Vector">向量</param>
      /// <returns>密文</returns>
      public static Byte[] AESEncrypt(Byte[] Data, String Key, String Vector)
      {
       Byte[] bKey = new Byte[32];
       Array.Copy(Encoding.UTF8.GetBytes(Key.PadRight(bKey.Length)), bKey, bKey.Length);
       Byte[] bVector = new Byte[16];
       Array.Copy(Encoding.UTF8.GetBytes(Vector.PadRight(bVector.Length)), bVector, bVector.Length);
       Byte[] Cryptograph = null; // 加密后的密文
       Rijndael Aes = Rijndael.Create();
       Aes.Mode = CipherMode.CBC;
       Aes.KeySize = 256;
       try
       {
        // 开辟一块内存流
        using (MemoryStream Memory = new MemoryStream())
        {
         // 把内存流对象包装成加密流对象
         using (CryptoStream Encryptor = new CryptoStream(Memory,
          Aes.CreateEncryptor(bKey, bVector),
          CryptoStreamMode.Write))
         {
          // 明文数据写入加密流
          Encryptor.Write(Data, 0, Data.Length);
          Encryptor.FlushFinalBlock();


          Cryptograph = Memory.ToArray();
         }
        }
       }
       catch
       {
        Cryptograph = null;
       }


       return Cryptograph;
      }


      /// <summary>
      /// AES解密
      /// </summary>
      /// <param name="Data">被解密的密文</param>
      /// <param name="Key">密钥</param>
      /// <param name="Vector">向量</param>
      /// <returns>明文</returns>
      public static Byte[] AESDecrypt(Byte[] Data, String Key, String Vector)
      {
       Byte[] bKey = new Byte[32];
       Array.Copy(Encoding.UTF8.GetBytes(Key.PadRight(bKey.Length)), bKey, bKey.Length);
       Byte[] bVector = new Byte[16];
       Array.Copy(Encoding.UTF8.GetBytes(Vector.PadRight(bVector.Length)), bVector, bVector.Length);


       Byte[] original = null; // 解密后的明文


       Rijndael Aes = Rijndael.Create();
       try
       {
        // 开辟一块内存流，存储密文
        using (MemoryStream Memory = new MemoryStream(Data))
        {
         // 把内存流对象包装成加密流对象
         using (CryptoStream Decryptor = new CryptoStream(Memory,
         Aes.CreateDecryptor(bKey, bVector),
         CryptoStreamMode.Read))
         {
          // 明文存储区
          using (MemoryStream originalMemory = new MemoryStream())
          {
           Byte[] Buffer = new Byte[1024];
           Int32 readBytes = 0;
           while ((readBytes = Decryptor.Read(Buffer, 0, Buffer.Length)) > 0)
           {
            originalMemory.Write(Buffer, 0, readBytes);
           }


           original = originalMemory.ToArray();
          }
         }
        }
       }
       catch
       {
        original = null;
       }


       return original;
      }
    }

    public static class AESCrypto
    {
        /// <summary>
        /// IV向量为固定值
        /// </summary>
        //private static byte[] _iV = {
        // 85, 60, 12, 116,
        // 99, 189, 173, 19,
        // 138, 183, 232, 248,
        // 82, 232, 200, 242
        //};


        public static byte[] Decrypt(byte[] encryptedBytes, string skey, string Vector)
        {
            MemoryStream mStream = new MemoryStream(encryptedBytes);
            //mStream.Write( encryptedBytes, 0, encryptedBytes.Length );
            //mStream.Seek( 0, SeekOrigin.Begin );

            Byte[] key = new Byte[32];
            Array.Copy(Encoding.UTF8.GetBytes(skey.PadRight(key.Length)), key, key.Length);
            Byte[] _iV = new Byte[16];
            Array.Copy(Encoding.UTF8.GetBytes(Vector.PadRight(_iV.Length)), _iV, _iV.Length);

            RijndaelManaged aes = new RijndaelManaged();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 256;
            aes.Key = key;
            aes.IV = _iV;
            CryptoStream cryptoStream = new CryptoStream(mStream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            try
            {


                byte[] tmp = new byte[encryptedBytes.Length + 32];
                int len = cryptoStream.Read(tmp, 0, encryptedBytes.Length + 32);
                byte[] ret = new byte[len];
                Array.Copy(tmp, 0, ret, 0, len);
                return ret;
            }
            finally
            {
                cryptoStream.Close();
                mStream.Close();
                aes.Clear();
            }
        }


        public static byte[] Encrypt(byte[] plainBytes, string skey, string Vector)
        {
            MemoryStream mStream = new MemoryStream();
            RijndaelManaged aes = new RijndaelManaged();

            Byte[] key = new Byte[32];
            Array.Copy(Encoding.UTF8.GetBytes(skey.PadRight(key.Length)), key, key.Length);
            Byte[] _iV = new Byte[16];
            Array.Copy(Encoding.UTF8.GetBytes(Vector.PadRight(_iV.Length)) ,_iV, _iV.Length);

            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 256;
            aes.Key = key;
            aes.IV = _iV;
            CryptoStream cryptoStream = new CryptoStream(mStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            try
            {
                cryptoStream.Write(plainBytes, 0, plainBytes.Length);
                cryptoStream.FlushFinalBlock();
                return mStream.ToArray();
            }
            finally
            {
                cryptoStream.Close();
                mStream.Close();
                aes.Clear();
            }
        }
    }

    //程式碼如下，將字串 "text to be encrypted" 加密後解密看結果是否相同 :   
    public class Encrypt
    {

        public static byte[] Awake(string data, string key, string iv)
        {
            //-----------------  
            //設定 cipher 格式 AES-256-CBC 
            RijndaelManaged rijalg = new RijndaelManaged();
            rijalg.BlockSize = 128;
            rijalg.KeySize = 256;
            rijalg.FeedbackSize = 128;
            rijalg.Padding = PaddingMode.PKCS7;
            rijalg.Mode = CipherMode.CBC;

            rijalg.Key = (new SHA256Managed()).ComputeHash(Encoding.ASCII.GetBytes(key));
            rijalg.IV = System.Text.Encoding.ASCII.GetBytes(iv);

            //-----------------  
            //加密  
            ICryptoTransform encryptor = rijalg.CreateEncryptor(rijalg.Key, rijalg.IV);

            byte[] encrypted;
            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                    {
                        swEncrypt.Write(data);
                    }
                    encrypted = msEncrypt.ToArray();
                }
            }

            //-----------------  
            //加密後的 base64 字串 :  
            //eiLbdhFSFrDqvUJmjbUgwD8REjBRoRWWwHHImmMLNZA=  
            return encrypted;
            //return Convert.ToBase64String(encrypted).ToString();

            //-----------------  
            ////解密  
            //ICryptoTransform decryptor = rijalg.CreateDecryptor(rijalg.Key, rijalg.IV);

            //string plaintext;
            //// Create the streams used for decryption.   
            //using (MemoryStream msDecrypt = new MemoryStream(encrypted))
            //{
            //    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            //    {
            //        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            //        {

            //            // Read the decrypted bytes from the decrypting stream   
            //            // and place them in a string.  
            //            plaintext = srDecrypt.ReadToEnd();
            //        }
            //    }
            //}

            ////-----------------  
            ////最後印出字串 "text to be encrypted"  
            //Debug.Log(plaintext);
        }
    }  
}
