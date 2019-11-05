using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using OsEngine.Entity;
using OsEngine.Logging;
using RestSharp;
//using WebSocket4Net;
using OsEngine.Market.Servers.Entity;
using OsEngine.Market.Servers.HuobiDM.HuobiEntity;
using System.Security.Authentication;
using WebSocketSharp;

namespace OsEngine.Market.Servers.HuobiDM
{
    class HuobiDMClient
    {
        public HuobiDMClient(string pubKey, string secKey)
        {
            ApiKey = pubKey;
            SecretKey = secKey;
            _rateGate = new RateGate(1, TimeSpan.FromMilliseconds(500));
        }
        public string ApiKey;
        public string SecretKey;

        private string _baseUrl = "https://api.hbdm.com";
        
        RateGate _rateGate;

        private HuobiRestApi HbRestApi;
        /// <summary>
        /// shows whether connection works
        /// работает ли соединение
        /// </summary>
        public bool IsConnected;

        /// <summary>
        /// connecto to the exchange
        /// установить соединение с биржей 
        /// </summary>
        public void Connect()
        {

            if (string.IsNullOrEmpty(ApiKey) ||
                string.IsNullOrEmpty(SecretKey))
            {
                return;
            }
            HbRestApi = new HuobiRestApi(ApiKey, SecretKey);
            HbRestApi.LogMessageEvent += SendLogMessage;
            // check server availability for HTTP communication with it / проверяем доступность сервера для HTTP общения с ним
            Uri uri = new Uri(_baseUrl);
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(uri);
                HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (Exception exception)
            {
                SendLogMessage("Сервер не доступен. Отсутствует интернет. ", LogMessageType.Error);
                return;
            }

            IsConnected = HbRestApi.Heartbeat();
            if (!IsConnected) return;

            if (Connected != null)
            {
                Connected();
            }

            Thread converter = new Thread(Converter);
            converter.CurrentCulture = new CultureInfo("ru-RU");
            converter.IsBackground = true;
            converter.Start();

        }
        /// <summary>
        /// bring the program to the start. Clear all objects involved in connecting to the server
        /// привести программу к моменту запуска. Очистить все объекты участвующие в подключении к серверу
        /// </summary>
        public void Dispose()
        {
            foreach (var ws in _wsStreams)
            {
                ws.Value.OnOpen -= Connect;
                ws.Value.OnClose -= Disconnect;
                ws.Value.OnError -= WsError;
                ws.Value.OnMessage -= GetRes;

                ws.Value.Close();
            
            }

            IsConnected = false;

            if (Disconnected != null)
            {
                Disconnected();
            }

            _isDisposed = true;
        }
        
        /// <summary>
        /// take balance
        /// взять баланс
        /// </summary>
        public List<HBContractBalanse> GetBalance()
        {
            lock (_lock)
            {
                try
                {
                    var res = HbRestApi.GetBalanses();

                    if (res == null)
                    {
                        return null;
                    }

                    if (NewPortfolio != null)
                    {
                        NewPortfolio(res);
                    }
                    return res;
                    
                }
                catch (Exception ex)
                {
                    SendLogMessage(ex.ToString(), LogMessageType.Error);
                    return null;
                }
            }
        }
        /// <summary>
        /// take securities
        /// взять бумаги
        /// </summary>
        public List<HBContractInfo> GetSecurities()
        {
            lock (_lock)
            {
                try
                {
                    var res = HbRestApi.GetContractInfo();



                    if (UpdatePairs != null)
                    {
                        UpdatePairs(res);
                    }

                    return res;
                }
                catch (Exception ex)
                {
                    if (LogMessageEvent != null)
                    {
                        LogMessageEvent(ex.ToString(), LogMessageType.Error);
                    }

                    return null;
                }
            }
        }
        /// <summary>
        /// subscribe this security to get depths and trades
        /// подписать данную бумагу на получение стаканов и трейдов
        /// </summary>
        public void SubscribleTradesAndDepths(Security security)
        {
            if (!_wsStreams.ContainsKey(security.Name))
            {
                string urlStr = "wss://www.hbdm.com/ws";

                _wsClient = new WebSocket(urlStr); // create web-socket / создаем вебсокет
  
                _wsClient.OnOpen += Connect;

                _wsClient.OnClose += Disconnect;

                _wsClient.OnError += WsError;

                _wsClient.OnMessage+= GetRes;

                _wsClient.ConnectAsync();
                

                _wsStreams.Add(security.Name, _wsClient);
            }

        }
        /// <summary>
        /// WebSocket client
        /// клиент вебсокет
        /// </summary>
        private WebSocket _wsClient;

        private object _lock = new object();
        /// <summary>
        /// there was a request to clear the object
        /// произошёл запрос на очистку объекта
        /// </summary>
        private bool _isDisposed;

        /// <summary>
        /// takes messages from the general queue, converts them to C # classes and sends them to up
        /// берет сообщения из общей очереди, конвертирует их в классы C# и отправляет на верх
        /// </summary>
        public void Converter()
        {
            while (true)
            {
                try
                {
                    if (!_newMessage.IsEmpty)
                    {
                        string mes;

                        if (_newMessage.TryDequeue(out mes))
                        {
                            if (mes.Contains("error"))
                            {
                                SendLogMessage(mes, LogMessageType.Error);
                            }
                            
                            else if (mes.Contains("trade.detail"))
                            {
                                var quotes = JsonConvert.DeserializeAnonymousType(mes, new HBWsResponse<HBTick>());

                                if (NewTradesEvent != null)
                                {
                                    NewTradesEvent(quotes);
                                }
                                continue;
                            }
                            
                            else if (mes.Contains(".depth."))
                            {
                                var quotes = JsonConvert.DeserializeAnonymousType(mes, new HBWsResponse<HBDepth>());

                                if (UpdateMarketDepth != null)
                                {
                                    UpdateMarketDepth(quotes);
                                }
                                continue;
                            }
                            
                        }
                    }
                    else
                    {
                        if (_isDisposed)
                        {
                            return;
                        }
                        Thread.Sleep(1);
                    }
                }

                catch (Exception exception)
                {
                    SendLogMessage(exception.ToString(), LogMessageType.Error);
                }
            }
        }

        /// <summary>
        /// queue of new messages from the exchange server
        /// очередь новых сообщений, пришедших с сервера биржи
        /// </summary>
        private ConcurrentQueue<string> _newMessage = new ConcurrentQueue<string>();
        
        /// <summary>
        /// data stream collection
        /// коллекция потоков данных
        /// </summary>
        private Dictionary<string, WebSocket> _wsStreams = new Dictionary<string, WebSocket>();

        /// <summary>
        /// takes messages that came through ws and puts them in a general queue
        /// берет пришедшие через ws сообщения и кладет их в общую очередь
        /// </summary>        
        private void GetRes(object sender, MessageEventArgs e)
        {
            if (_isDisposed == true)
            {
                return;
            }
            
            var msg = GZipHelper.GZipDecompressString(e.RawData);
            var steam = (WebSocket)sender;
            if (msg.IndexOf("ping") != -1) 
            {
                var reponseData = msg.Replace("ping", "pong");
                steam.Send(reponseData);
            }
            else
            {
                //Console.WriteLine("get: "+msg);
                _newMessage.Enqueue(msg);
            }
        }
        /// <summary>
        /// ws-connection is opened
        /// соединение по ws открыто
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connect(object sender, EventArgs e)
        {
            IsConnected = true;

            var stream = (WebSocket)sender;

            foreach (var item in _wsStreams)
            {
                if (item.Value.Equals(stream))
                {
                    var msg = $"{{\"sub\":\"market.{item.Key.ToUpper()}.trade.detail\",\"id\":\"{item.Key.ToUpper()}\"}}";
                    item.Value.Send(msg);
                   // Console.WriteLine(msg);
                    msg = $"{{\"sub\":\"market.{item.Key.ToUpper()}.depth.step0\",\"id\":\"{item.Key.ToUpper()}\"}}";
                    item.Value.Send(msg);
                }
            }

        }

        /// <summary>
        /// ws-connection is closed
        /// соединение по ws закрыто
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Disconnect(object sender, CloseEventArgs e)
        {
            if (IsConnected)
            {
                IsConnected = false;

                _wsStreams.Clear();

                if (Disconnected != null)
                {
                    Disconnected();
                }
            }
        }
        /// <summary>
        /// error from ws4net
        /// ошибка из ws4net
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WsError(object sender, ErrorEventArgs e)
        {
            SendLogMessage("Ошибка из ws4net :" + e, LogMessageType.Error);
        }


        #region outgoing events / исходящие события

        /// <summary>
        /// my new orders
        /// новые мои ордера
        /// </summary>
        public event Action<Order> MyOrderEvent;

        /// <summary>
        /// my new trades
        /// новые мои сделки
        /// </summary>
        public event Action<MyTrade> MyTradeEvent;

        /// <summary>
        /// new portfolios
        /// новые портфели
        /// </summary>
        public event Action<List<HBContractBalanse>> NewPortfolio;
        /// <summary>
        /// new security in the system
        /// новые бумаги в системе
        /// </summary>
        public event Action<List<HBContractInfo>> UpdatePairs;
        /*
        /// <summary>
        /// portfolios updated
        /// обновились портфели
        /// </summary>
        public event Action<OutboundAccountInfo> UpdatePortfolio;


        /// <summary>
        /// depth updated
        /// обновился стакан
        /// </summary>
        public event Action<DepthResponse> UpdateMarketDepth;

*/
        /// <summary>
        /// depth updated
        /// обновился стакан
        /// </summary>
        public event Action<HBWsResponse<HBDepth>> UpdateMarketDepth;
        /// <summary>
        /// ticks updated
        /// обновились тики
        /// </summary>
        public event Action<HBWsResponse<HBTick>> NewTradesEvent;

        /// <summary>
        /// API connection established
        /// соединение с API установлено
        /// </summary>
        public event Action Connected;

        /// <summary>
        /// API connection lost
        /// соединение с API разорвано
        /// </summary>
        public event Action Disconnected;

        #endregion

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
    }
}
