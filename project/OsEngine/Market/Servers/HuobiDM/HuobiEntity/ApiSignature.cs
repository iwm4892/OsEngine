using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Security.Cryptography;

namespace OsEngine.Market.Servers.HuobiDM.HuobiEntity
{
    class ApiSignature
    {
        public static string op = "op";
        public static string opValue = "auth";
        public static string accessKeyId = "AccessKeyId";
        public static string signatureMethod = "SignatureMethod";
        public static string signatureMethodValue = "HmacSHA256";
        public static string signatureVersion = "SignatureVersion";
        public static string signatureVersionValue = "2";
        public static string timestamp = "Timestamp";
        public static string signature = "Signature";
        public static string DT_FORMAT = "yyyy-MM-ddTHH:mm:ss";

        public void createSignature(string accessKey, string secretKey, string method, string host, string uri, Dictionary<string, string> param)
        {
            var sb = new StringBuilder();
            sb.Append(method.ToString().ToUpper()).Append("\n")
                .Append(host).Append("\n")
                .Append(uri).Append("\n");
            param.Remove(signature);
            param.Add(accessKeyId, accessKey);
            param.Add(signatureMethod, signatureMethodValue);
            param.Add(signatureVersion, signatureVersionValue);
            param.Add(timestamp, GetTimestamp());

            var sortDic = new SortedDictionary<string, string>(param);
            foreach (var item in param)
            {
                sb.Append(item.Key).Append('=').Append(UrlEncode(item.Value)).Append('&');
            }
            var sign = sb.ToString().TrimEnd('&');
            sign = CalculateSignature256(sign, secretKey);
            param.Add(signature, sign);
        }
        /// <summary>
        /// Hmacsha256 шифрование
        /// </summary>
        /// <param name="text"></param>
        /// <param name="secretKey"></param>
        /// <returns></returns>
        private static string CalculateSignature256(string text, string secretKey)
        {
            using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(text));

                return Convert.ToBase64String(hashmessage);
            }
        }

        /// <summary>
        /// UrlEncode
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string UrlEncode(string str)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in str)
            {
                if (WebUtility.UrlEncode(c.ToString()).Length > 1)
                {
                    builder.Append(WebUtility.UrlEncode(c.ToString()).ToUpper());
                }
                else
                {
                    builder.Append(c);
                }
            }
            return builder.ToString();
        }
        public static string GetTimestamp()
        {
            return DateTime.UtcNow.ToString(DT_FORMAT);
        }
    }
}
