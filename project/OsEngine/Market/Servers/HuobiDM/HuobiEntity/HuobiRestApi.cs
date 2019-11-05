using OsEngine.Market.Servers.HuobiDM.HuobiEntity;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using OsEngine.Logging;
using System.Net;
using Newtonsoft.Json;

namespace OsEngine.Market.Servers.HuobiDM.HuobiEntity
{
    class HuobiRestApi
    {
        #region HuoBiApi
        /// <summary>
        /// Арес Api
        /// </summary>
        private readonly string HUOBI_HOST = string.Empty;

        /// <summary>
        /// URL Api
        /// </summary>
        private readonly string HUOBI_HOST_URL = string.Empty;
        /// <summary>
        /// Метод шифрования
        /// </summary>
        private const string HUOBI_SIGNATURE_METHOD = "HmacSHA256";
        /// <summary>
        /// Версия Api
        /// </summary>
        private const int HUOBI_SIGNATURE_VERSION = 2;
        /// <summary>
        /// ACCESS_KEY
        /// </summary>
        private readonly string ACCESS_KEY = string.Empty;
        /// <summary>
        /// SECRET_KEY()
        /// </summary>
        private readonly string SECRET_KEY = string.Empty;
        #endregion

        /// <summary>
        /// Получить информацию о контрактах
        /// </summary>
        private const string GET_CONTRACT_INFO = "/api/v1/contract_contract_info";
        /// <summary>
        /// Получить информацию о цене индекса контракта
        /// </summary>
        private const string GET_CONTRACT_INDEX = "/api/v1/contract_index";
        /// <summary>
        /// Получить лимиты на цену контракта
        /// </summary>
        private const string GET_CONTRACT_PRICE_LIMIT = "/api/v1/contract_price_limit";
        /// <summary>
        /// Получить информацию об открытых интересах по контракту
        /// </summary>
        private const string GET_CONTRACT_OPEN_INTEREST = "/api/v1/contract_open_interest";
        /// <summary>
        /// Получить глубину рынка
        /// </summary>
        private const string GET_CONTRACT_DEPTH = "/market/depth";
        /// <summary>
        /// Получить данные K-Line
        /// </summary>
        private const string GET_CONTRACT_KLINE = "/market/history/kline";
        /// <summary>
        /// Отменить ордер
        /// </summary>
        private const string POST_CANCEL_ORDER = "/api/v1/contract_cancel";
        /// <summary>
        /// Получить информацию об ордере
        /// </summary>
        private const string POST_ORDER_INFO = "/api/v1/contract_order_info";
        /// <summary>
        /// Получить торговые детали ордера
        /// </summary>
        private const string POST_ORDER_DETAIL = "/api/v1/contract_order_detail";
        /// <summary>
        /// Выставить ордер
        /// </summary>
        private const string POST_PLACE_ORDER = "/api/v1/contract_order";
        /// <summary>
        /// Информация о позиции пользователя
        /// </summary>
        private const string POST_POSITION_ORDER = "/api/v1/contract_position_info";
        /// <summary>
        /// Портфолио
        /// </summary>
        private const string POST_CONTRACT_ACCONT_INFO = "/api/v1/contract_account_info";
        /// <summary>
        /// Проверка работы сервера
        /// </summary>
        private const string GET_HEARTBEAT = "/heartbeat/";

        private RestClient client;
        public HuobiRestApi(string accessKey, string secretKey, string huobi_host = "api.hbdm.com")
        {
            ACCESS_KEY = accessKey;
            SECRET_KEY = secretKey;
            HUOBI_HOST_URL = "https://" + huobi_host;
            Uri uri = new Uri(HUOBI_HOST_URL);
            HUOBI_HOST = uri.Host;

            if (string.IsNullOrEmpty(ACCESS_KEY))
                SendLogMessage("ACCESS_KEY Cannt Be Null Or Empty", LogMessageType.Error);
            if (string.IsNullOrEmpty(SECRET_KEY))
                SendLogMessage("SECRET_KEY  Cannt Be Null Or Empty", LogMessageType.Error);
            if (string.IsNullOrEmpty(HUOBI_HOST))
                SendLogMessage("HUOBI_HOST  Cannt Be Null Or Empty", LogMessageType.Error);

            client = new RestClient(HUOBI_HOST_URL);
            client.AddDefaultHeader("Content-Type", "application/json");
            client.AddDefaultHeader("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.71 Safari/537.36");
        }

        #region log messages / сообщения для лога

        /// <summary>
        /// add a new log message
        /// добавить в лог новое сообщение
        /// </summary>
        private void SendLogMessage(string message, LogMessageType type)
        {
            if (LogMessageEvent != null)
            {
                LogMessageEvent(message, type);
            }
        }

        /// <summary>
        /// send exeptions
        /// отправляет исключения
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;

        #endregion

        #region Служебные методы
        /// <summary>
        /// Отправить Запрос
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="resourcePath"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private T SendRequest<T>(string resourcePath, string parameters = "") where T : new()
        {
            try
            {
                parameters = UriEncodeParameterValue(GetCommonParameters() + parameters);//Параметры запроса
                var sign = GetSignatureStr(Method.GET, HUOBI_HOST, resourcePath, parameters);//Подпись
                parameters += $"&Signature={sign}";

                var url = $"{resourcePath}?{parameters}";

                var request = new RestRequest(url, Method.GET);
                //            var result = client.Execute<HBResponse<T>>(request);
                //            return result.Data;
                var response = client.Execute(request).Content;
                if (response.Contains("error"))
                {
                    var error = JsonConvert.DeserializeAnonymousType(response, new HBError());
                    throw new Exception(error.err_msg);
                }
                HBResponse<T> result = JsonConvert.DeserializeAnonymousType(response, new HBResponse<T>());
                return result.Data;
            }
            catch (Exception exception)
            {
                SendLogMessage(exception.ToString(), LogMessageType.Error);
                return new T();
            }
        }
        private T SendRequest<T, P>(string resourcePath, P postParameters) where T : new()
        {
            try
            {

                var parameters = UriEncodeParameterValue(GetCommonParameters());//Параметры запроса
                var sign = GetSignatureStr(Method.POST, HUOBI_HOST, resourcePath, parameters);//Подпись
                parameters += $"&Signature={sign}";

                var url = $"{resourcePath}?{parameters}";

                var request = new RestRequest(url, Method.POST);
                request.AddJsonBody(postParameters);
                foreach (var item in request.Parameters)
                {
                    item.Value = item.Value.ToString();
                }
                var response = client.Execute(request).Content;
                if (response.Contains("error"))
                {
                    var error = JsonConvert.DeserializeAnonymousType(response, new HBError());
                    throw new Exception(error.err_msg);
                }
                HBResponse<T> result = JsonConvert.DeserializeAnonymousType(response, new HBResponse<T>());
                return result.Data;
            }
            catch (Exception exception)
            {
                SendLogMessage(exception.ToString(), LogMessageType.Error);
                return new T();
            }
        }
        /// <summary>
        /// Получить постоянную часть параметров
        /// </summary>
        /// <returns></returns>
        private string GetCommonParameters()
        {
            return $"AccessKeyId={ACCESS_KEY}&SignatureMethod={HUOBI_SIGNATURE_METHOD}&SignatureVersion={HUOBI_SIGNATURE_VERSION}&Timestamp={DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")}";
        }
        /// <summary>
        /// Uri参数值进行转义
        /// </summary>
        /// <param name="parameters">Параметры</param>
        /// <returns></returns>
        private string UriEncodeParameterValue(string parameters)
        {
            var sb = new StringBuilder();
            var paraArray = parameters.Split('&');
            var sortDic = new SortedDictionary<string, string>();
            foreach (var item in paraArray)
            {
                var para = item.Split('=');
                sortDic.Add(para.First(), UrlEncode(para.Last()));
            }
            foreach (var item in sortDic)
            {
                sb.Append(item.Key).Append("=").Append(item.Value).Append("&");
            }
            return sb.ToString().TrimEnd('&');
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
        /// Шеширование параметров
        /// </summary>
        /// <param name="method">Метод</param>
        /// <param name="host">Адрес</param>
        /// <param name="resourcePath">Адрес метода</param>
        /// <param name="parameters">Параметры</param>
        /// <returns></returns>
        private string GetSignatureStr(Method method, string host, string resourcePath, string parameters)
        {
            var sign = string.Empty;
            StringBuilder sb = new StringBuilder();
            sb.Append(method.ToString().ToUpper()).Append("\n")
                .Append(host).Append("\n")
                .Append(resourcePath).Append("\n");
            //Порядок параметров
            var paraArray = parameters.Split('&');
            List<string> parametersList = new List<string>();
            foreach (var item in paraArray)
            {
                parametersList.Add(item);
            }
            parametersList.Sort(delegate (string s1, string s2) { return string.CompareOrdinal(s1, s2); });
            foreach (var item in parametersList)
            {
                sb.Append(item).Append("&");
            }
            sign = sb.ToString().TrimEnd('&');

            sign = CalculateSignature256(sign, SECRET_KEY);
            return UrlEncode(sign);
            return sign;
        }
        #endregion
        public List<HBContractInfo> GetContractInfo()
        {
            var result = SendRequest<List<HBContractInfo>>(GET_CONTRACT_INFO);
            return result;
        }

        public bool Heartbeat()
        {
            try
            {
                RestClient rest = new RestClient("https://www.hbdm.com");
                rest.AddDefaultHeader("Content-Type", "application/json");
                var url = $"{GET_HEARTBEAT}";
                var request = new RestRequest(url, Method.GET);
                //    var result = rest.Execute<HBResponse<Dictionary<string, string>>>(request).Data;

                var response = rest.Execute(request).Content;
                if (response.Contains("error"))
                {
                    var error = JsonConvert.DeserializeAnonymousType(response, new HBError());
                    throw new Exception(error.err_msg);
                }
                HBResponse<Dictionary<string, string>> result = JsonConvert.DeserializeAnonymousType(response, new HBResponse<Dictionary<string, string>>());

                if (result.Status == "ok")
                {
                    if (result.Data["heartbeat"] == "1")
                    {
                        return true;
                    }
                    else
                    {
                        throw new Exception("Сервер не работает");
                    }
                }
                else
                {
                    throw new Exception("Сервер не работает");
                }
            }
            catch (Exception exception)
            {
                SendLogMessage(exception.ToString(), LogMessageType.Error);
                return false;
            }

        }

        public List<HBContractBalanse> GetBalanses()
        {
            try
            {
                var param = new Dictionary<string, string>();
                var result = SendRequest<List<HBContractBalanse>, Dictionary<string, string>>(POST_CONTRACT_ACCONT_INFO, param);
                return result;
            }
            catch (Exception exception)
            {
                SendLogMessage(exception.ToString(), LogMessageType.Error);
                return new List<HBContractBalanse>();
            }

        }

        public List<HBCandle> GetCandleDataToSecurity(string security)
        {
            try
            {

            }
            catch (Exception exception)
            {
                SendLogMessage(exception.ToString(), LogMessageType.Error);
                return new List<HBCandle>();
            }

            return new List<HBCandle>();
        }
    }

    }
