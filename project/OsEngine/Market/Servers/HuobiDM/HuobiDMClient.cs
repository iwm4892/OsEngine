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
using WebSocketSharp;
using System.Linq;


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
        /// <summary>
        /// Прогружать дней
        /// </summary>
        public int DaysToLoad;
        /// <summary>
        /// Плечо
        /// </summary>
        public int Leverege;
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

            Thread converterUserData = new Thread(ConverterUserData);
            converterUserData.CurrentCulture = new CultureInfo("ru-RU");
            converterUserData.IsBackground = true;
            converterUserData.Start();


            CreateUserDataStream();


        }
        /// <summary>
        /// create user data thread
        /// создать поток пользовательских данных
        /// </summary>
        /// <returns></returns>
        private bool CreateUserDataStream()
        {
            try
            {
                string urlStr = "wss://api.hbdm.com/notification";//"wss://www.hbdm.com/ws";

                _wsClient = new WebSocket(urlStr); 

                _wsClient.OnOpen += UserDataConnect;

                _wsClient.OnClose += Disconnect;

                _wsClient.OnError += WsError;

                _wsClient.OnMessage += UserDataMessageHandler;

                _wsClient.ConnectAsync();

                _wsStreams.Add("userDataStream", _wsClient);

                return true;
            }
            catch (Exception exception)
            {
                SendLogMessage(exception.Message, LogMessageType.Connect);
                return false;
            }

        }
        /// <summary>
        /// queue of new messages from the exchange server
        /// очередь новых сообщений, пришедших с сервера биржи
        /// </summary>
        private ConcurrentQueue<string> _newUserDataMessage = new ConcurrentQueue<string>();
        /// <summary>
        /// user data handler
        /// обработчик пользовательских данных
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserDataMessageHandler(object sender, MessageEventArgs e)
        {
            if (_isDisposed)
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
                _newUserDataMessage.Enqueue(msg);
            }

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
                if(ws.Key== "userDataStream")
                {
                    ws.Value.OnMessage -= UserDataMessageHandler;
                }
                else
                {
                    ws.Value.OnMessage -= GetRes;
                }
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
        /// take candles
        /// взять свечи
        /// </summary>
        /// <returns></returns>
        public List<Candle> GetCandles(string nameSec, TimeSpan tf)
        {
            string needTf = "";

            switch ((int)tf.TotalMinutes)
            {
                case 1:
                    needTf = "1min";
                    break;
                case 2:
                    needTf = "2min";
                    break;
                case 3:
                    needTf = "3min";
                    break;
                case 5:
                    needTf = "5min";
                    break;
                case 10:
                    needTf = "10min";
                    break;
                case 15:
                    needTf = "15min";
                    break;
                case 20:
                    needTf = "20min";
                    break;
                case 30:
                    needTf = "30min";
                    break;
                case 45:
                    needTf = "45min";
                    break;
                case 60:
                    needTf = "1hour";
                    break;
                case 120:
                    needTf = "2hour";
                    break;
                case 240:
                    needTf = "4hour";
                    break;
                case 1440:
                    needTf = "1day";
                    break;
            }

            if ((new[] { "1min", "5min", "15min", "30min", "1hour", "4hour", "1day" }).Contains(needTf))
            {
                string param = $"&symbol={nameSec.ToUpper()}&period={needTf}&size=2000";
                var res = HbRestApi.GetCandleDataToSecurity(nameSec.ToUpper(), param);

                var candles = _deserializeCandles(res,tf);
                return candles;

            }
            else
            {
                if (needTf == "2min")
                {
                    string param = $"&symbol={nameSec.ToUpper()}&period=1min&size=2000";
                    var res = HbRestApi.GetCandleDataToSecurity(nameSec.ToUpper(), param);
                    var candles = _deserializeCandles(res,tf);
                    var newCandles = BuildCandles(candles, 2, 1);
                    return newCandles;
                }
                else if (needTf == "10min")
                {
                    string param = $"&symbol={nameSec.ToUpper()}&period=5min&size=2000";
                    var res = HbRestApi.GetCandleDataToSecurity(nameSec.ToUpper(), param);
                    var candles = _deserializeCandles(res, tf);
                    var newCandles = BuildCandles(candles, 10, 5);
                    return newCandles;
                }
                else if (needTf == "20min")
                {
                    string param = $"&symbol={nameSec.ToUpper()}&period=5min&size=2000";
                    var res = HbRestApi.GetCandleDataToSecurity(nameSec.ToUpper(), param);
                    var candles = _deserializeCandles(res, tf);
                    var newCandles = BuildCandles(candles, 20, 5);
                    return newCandles;
                }
                else if (needTf == "45min")
                {
                    string param = $"&symbol={nameSec.ToUpper()}&period=15min&size=2000";
                    var res = HbRestApi.GetCandleDataToSecurity(nameSec.ToUpper(), param);
                    var candles = _deserializeCandles(res, tf);
                    var newCandles = BuildCandles(candles, 45, 15);
                    return newCandles;
                }
                else if (needTf == "2hour")
                {
                    string param = $"&symbol={nameSec.ToUpper()}&period=1hour&size=2000";
                    var res = HbRestApi.GetCandleDataToSecurity(nameSec.ToUpper(), param);
                    var candles = _deserializeCandles(res, tf);
                    var newCandles = BuildCandles(candles, 45, 15);
                    return newCandles;
                }
            }
            return null;
        }

        /// <summary>
        /// converts candles of one timeframe to a larger
        /// преобразует свечи одного таймфрейма в больший
        /// </summary>
        /// <param name="oldCandles"></param>
        /// <param name="needTf"></param>
        /// <param name="oldTf"></param>
        /// <returns></returns>
        private List<Candle> BuildCandles(List<Candle> oldCandles, int needTf, int oldTf)
        {
            List<Candle> newCandles = new List<Candle>();

            int index = oldCandles.FindIndex(can => can.TimeStart.Minute % needTf == 0);

            int count = needTf / oldTf;

            int counter = 0;

            Candle newCandle = new Candle();

            for (int i = index; i < oldCandles.Count; i++)
            {
                counter++;

                if (counter == 1)
                {
                    newCandle = new Candle();
                    newCandle.Open = oldCandles[i].Open;
                    newCandle.TimeStart = oldCandles[i].TimeStart;
                    newCandle.Low = Decimal.MaxValue;
                }

                newCandle.High = oldCandles[i].High > newCandle.High
                    ? oldCandles[i].High
                    : newCandle.High;

                newCandle.Low = oldCandles[i].Low < newCandle.Low
                    ? oldCandles[i].Low
                    : newCandle.Low;

                newCandle.Volume += oldCandles[i].Volume;

                if (counter == count)
                {
                    newCandle.Close = oldCandles[i].Close;
                    newCandle.State = CandleState.Finished;
                    newCandles.Add(newCandle);
                    counter = 0;
                }

                if (i == oldCandles.Count - 1 && counter != count)
                {
                    newCandle.Close = oldCandles[i].Close;
                    newCandle.State = CandleState.None;
                    newCandles.Add(newCandle);
                }
            }

            return newCandles;
        }

        private readonly object _candleLocker = new object();
        /// <summary>
        /// convert JSON to candles
        /// преобразует JSON в свечи
        /// </summary>
        /// <param name="jsonCandles"></param>
        /// <returns></returns>
        private List<Candle> _deserializeCandles(List<HBCandle> candles, TimeSpan tf)
        {
            try
            {
                lock (_candleLocker)
                {
                    candles.Reverse();
                    //var now = DateTime.Now;
                    var DateStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
                    while (DateStart< DateTime.Now.AddMinutes(-tf.TotalMinutes))
                    {
                        DateStart = DateStart.AddMinutes(tf.TotalMinutes);
                    }
                    var _candles = new List<Candle>();
                    foreach(var _candle in candles)
                    {
                        var candle = new Candle();
                        candle.High = _candle.high;
                        candle.Low = _candle.low;
                        candle.Open = _candle.open;
                        candle.Close = _candle.close;
                        candle.Volume = _candle.vol;
                        candle.TimeStart = DateStart;
                        _candles.Add(candle);
                        DateStart = DateStart.AddMinutes(-tf.TotalMinutes);
                        if(DateStart < DateTime.Now.AddDays(-DaysToLoad))
                        {
                            break;
                        }
                    }
                    _candles.Reverse();
                    return _candles;
                }
            }
            catch (Exception error)
            {
                SendLogMessage(error.ToString(), LogMessageType.Error);
                return null;
            }
        }
        public string GetSecuritiName(string Securiti, string contract_type)
        {
            switch (contract_type)
            {
                case "this_week": return Securiti.ToUpper()+"_CW"; 
                case "next_week": return Securiti.ToUpper() + "_NW"; 
                case "quarter": return Securiti.ToUpper() + "_CQ"; 
                default : return "";
            }
        }
        private string GetContractType(string Securiti)
        {
            var Simbol = Securiti.ToUpper().Split('_');
            string contract_type = "";
            switch (Simbol[1])
            {
                case "CW": contract_type = "this_week"; break;
                case "NW": contract_type = "next_week"; break;
                case "CQ": contract_type = "quarter"; break;
            }
            return contract_type;
        }
        /// <summary>
        /// execute order
        /// исполнить ордер
        /// </summary>
        /// <param name="order"></param>
        public void ExecuteOrder(Order order)
        {
            lock (_lockOrder)
            {
                try
                {
                    if (IsConnected == false)
                    {
                        return;
                    }
                    var Simbol = order.SecurityNameCode.ToUpper().Split('_');
                    string contract_type = "";
                    Dictionary<string, string> param = new Dictionary<string, string>();
                    param["symbol"] = Simbol[0];
                    contract_type = GetContractType(order.SecurityNameCode);
                    param["contract_type"] = contract_type;
                    param["client_order_id"] = order.NumberUser.ToString();
                    param["price"]      = order.Price.ToString(CultureInfo.InvariantCulture).Replace(CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator, ".");
                    param["volume"]     = order.Volume.ToString();
                    param["direction"]  = order.Side== Side.Buy ? "buy":"sell";
                    param["offset"]     = order.IsStopOrProfit ? "close" : "open";
                    param["lever_rate"] = Leverege.ToString();
                    switch (order.TypeOrder)
                    {
                        case OrderPriceType.Limit:
                            param["order_price_type"] = "limit"; break;
                        case OrderPriceType.Market:
                            param["order_price_type"] = "opponent"; break;
                        /*
                        case OrderPriceType.LimitStop:
                            param["order_price_type"] = "Limit"; break;
                        case OrderPriceType.MarketStop:
                            param["order_price_type"] = "Limit"; break;
                        case OrderPriceType.BuyStop:
                            param["order_price_type"] = "Limit"; break;
                        case OrderPriceType.SellStop:
                            param["order_price_type"] = "Limit"; break;
                        */
                      
                     }
                    var res = HbRestApi.ExecuteOrder(param);

                    if (res != null && res.Count!=0)
                    {
                        SendLogMessage(res.ToString(), LogMessageType.Trade);
                        if (res.ContainsKey("order_id"))
                        {
                            order.NumberMarket = res["order_id"].ToString();
                            order.State = OrderStateType.Activ;
                        }
                    }
                    else
                    {
                        order.State = OrderStateType.Fail;
                        if (MyOrderEvent != null)
                        {
                            MyOrderEvent(order);
                        }

                    }
                }
                catch (Exception ex)
                {
                    SendLogMessage(ex.ToString(), LogMessageType.Error);
                }
            }
        }
        
        /// <summary>
        /// cancel order
        /// отменить ордер
        /// </summary>
        public void CanselOrder(Order order)
        {
            lock (_lockOrder)
            {
                try
                {
                    Dictionary<string, string> param = new Dictionary<string, string>();
                    param["symbol"] = order.SecurityNameCode.Split('_')[0];
                    param["client_order_id"] = order.NumberUser.ToString();
                    param["order_id"] = order.NumberMarket;
                    HbRestApi.CanselOrder(param);
                }
                catch (Exception ex)
                {
                    SendLogMessage(ex.ToString(), LogMessageType.Error);

                }
            }
        }
        /// <summary>
        /// chack order state
        /// проверить ордера на состояние
        /// </summary>
        public bool GetOrdersState(List<Order> oldOpenOrders)
        {
            Dictionary<string, string> param = new Dictionary<string, string>();
            param["symbol"] = "";
            param["client_order_id"] = "";
            for (int i = 0; i < oldOpenOrders.Count; i++)
            {
                var sec = oldOpenOrders[i].SecurityNameCode.ToUpper().Split('_')[0];
                if (param["symbol"].IndexOf(sec) == -1)
                {
                    param["symbol"] += "," + sec;
                }
                if (param["client_order_id"].IndexOf(oldOpenOrders[i].NumberUser.ToString()) == -1)
                {
                    param["client_order_id"] += "," + oldOpenOrders[i].NumberUser.ToString();
                }
                
            }
            var allOrders = HbRestApi.GetOrdersState(param);
            for (int i = 0; i < oldOpenOrders.Count; i++)
            {
                HBOrder myOrder = allOrders.Find(ord => ord.order_id == oldOpenOrders[i].NumberMarket);

                if (myOrder == null)
                {
                    continue;
                }
            
            }
                /*
                string endPoint = "/api/v3/allOrders";

                List<HistoryOrderReport> allOrders = new List<HistoryOrderReport>();

                for (int i = 0; i < namesSec.Count; i++)
                {
                    var param = new Dictionary<string, string>();
                    param.Add("symbol=", namesSec[i].ToUpper());
                    //param.Add("&recvWindow=" , "100");
                    //param.Add("&limit=", GetNonce());
                    param.Add("&limit=", "500");
                    //"symbol={symbol.ToUpper()}&recvWindow={recvWindow}"

                    var res = CreateQuery(Method.GET, endPoint, param, true);

                    HistoryOrderReport[] orders = JsonConvert.DeserializeObject<HistoryOrderReport[]>(res);

                    if (orders != null && orders.Length != 0)
                    {
                        allOrders.AddRange(orders);
                    }
                }

                for (int i = 0; i < oldOpenOrders.Count; i++)
                {
                    HistoryOrderReport myOrder = allOrders.Find(ord => ord.orderId == oldOpenOrders[i].NumberMarket);

                    if (myOrder == null)
                    {
                        continue;
                    }

                    if (myOrder.status == "NEW")
                    { // order is active. Do nothing / ордер активен. Ничего не делаем
                        continue;
                    }

                    else if (myOrder.status == "FILLED" ||
                        myOrder.status == "PARTIALLY_FILLED")
                    { // order executed / ордер исполнен

                        MyTrade trade = new MyTrade();
                        trade.NumberOrderParent = oldOpenOrders[i].NumberMarket;
                        trade.NumberTrade = NumberGen.GetNumberOrder(StartProgram.IsOsTrader).ToString();
                        trade.SecurityNameCode = oldOpenOrders[i].SecurityNameCode;
                        trade.Time = new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToDouble(myOrder.updateTime));
                        trade.Side = oldOpenOrders[i].Side;

                        if (MyTradeEvent != null)
                        {
                            MyTradeEvent(trade);
                        }
                    }
                    else
                    {
                        Order newOrder = new Order();
                        newOrder.NumberMarket = oldOpenOrders[i].NumberMarket;
                        newOrder.NumberUser = oldOpenOrders[i].NumberUser;
                        newOrder.SecurityNameCode = oldOpenOrders[i].SecurityNameCode;
                        newOrder.State = OrderStateType.Cancel;

                        newOrder.Volume = oldOpenOrders[i].Volume;
                        newOrder.VolumeExecute = oldOpenOrders[i].VolumeExecute;
                        newOrder.Price = oldOpenOrders[i].Price;
                        newOrder.TypeOrder = oldOpenOrders[i].TypeOrder;
                        newOrder.TimeCallBack = new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToDouble(myOrder.updateTime));
                        newOrder.TimeCancel = newOrder.TimeCallBack;
                        newOrder.ServerType = ServerType.Binance;
                        newOrder.PortfolioNumber = oldOpenOrders[i].PortfolioNumber;

                        if (MyOrderEvent != null)
                        {
                            MyOrderEvent(newOrder);
                        }
                    }
                }
                */
                return true;
        }

        private object _lockOrder = new object();

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

        public string user_id;

        /// <summary>
        /// takes messages from the general queue, converts them to C # classes and sends them to up
        /// берет сообщения из общей очереди, конвертирует их в классы C# и отправляет на верх
        /// </summary>
        public void ConverterUserData()
        {
            while (true)
            {
                try
                {
                    if (!_newUserDataMessage.IsEmpty)
                    {
                        string mes;

                        if (_newUserDataMessage.TryDequeue(out mes))
                        {
                            if (mes.Contains("\"op\":\"auth\""))
                            {
                                var auth = JsonConvert.DeserializeAnonymousType(mes, new HBAuth());
                                if(auth.data != null)
                                {
                                    user_id = auth.data.UserId.ToString();
                                }
                                continue;
                            }
                            if (mes.Contains("\"op\":\"sub\""))
                            {
                                continue;
                            }
                            if (mes.Contains("err-msg"))
                            {
                                SendLogMessage(mes, LogMessageType.Error);
                            }
                            else if (mes.Contains("orders.")) 
                            {
                                var order = JsonConvert.DeserializeAnonymousType(mes, new HBOrder());

                                string orderNumUser = order.client_order_id;

                                if (string.IsNullOrEmpty(orderNumUser) ||
                                    orderNumUser == "null")
                                {
                                    orderNumUser = order.order_id;
                                }

                                try
                                {
                                    Convert.ToInt32(orderNumUser);
                                }
                                catch (Exception)
                                {
                                    continue;
                                }

                                if (order.status == 3)
                                {
                                    Order newOrder = new Order();
                                    newOrder.SecurityNameCode = GetSecuritiName(order.symbol, order.contract_type);
                                    newOrder.TimeCallBack = new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToDouble(order.created_at)).ToLocalTime();
                                    newOrder.NumberUser = Convert.ToInt32(orderNumUser);

                                    newOrder.NumberMarket = order.order_id.ToString();
                                    newOrder.Side = order.direction == "buy" ? Side.Buy : Side.Sell;
                                    newOrder.State = OrderStateType.Activ;
                                    newOrder.Volume = order.volume;
                                    newOrder.Price = order.price;
                                    newOrder.ServerType = ServerType.HuobiDM;
                                    newOrder.PortfolioNumber = newOrder.SecurityNameCode;

                                    if (MyOrderEvent != null)
                                    {
                                        MyOrderEvent(newOrder);
                                    }
                                }
                                else if (order.status == 7)
                                {
                                    Order newOrder = new Order();
                                    newOrder.SecurityNameCode = GetSecuritiName(order.symbol, order.contract_type);
                                    newOrder.TimeCallBack = new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToDouble(order.created_at)).ToLocalTime();
                                    newOrder.TimeCancel = newOrder.TimeCallBack;
                                    newOrder.NumberUser = Convert.ToInt32(orderNumUser);
                                    newOrder.NumberMarket = order.order_id.ToString();
                                    newOrder.Side = order.direction == "buy" ? Side.Buy : Side.Sell;
                                    newOrder.State = OrderStateType.Cancel;
                                    newOrder.Volume = order.volume;
                                    newOrder.Price = order.price;
                                    newOrder.ServerType = ServerType.HuobiDM;
                                    newOrder.PortfolioNumber = newOrder.SecurityNameCode;

                                    if (MyOrderEvent != null)
                                    {
                                        MyOrderEvent(newOrder);
                                    }
                                }
                                else if (order.status == 4 || order.status == 6)
                                {
                                    foreach (HBOrderTrade _trade in order.trade)
                                    {
                                        MyTrade trade = new MyTrade();
                                        trade.Time = new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToDouble(_trade.created_at)).ToLocalTime();
                                        trade.NumberOrderParent = order.order_id.ToString();
                                        trade.NumberTrade = _trade.trade_id.ToString();
                                        trade.Volume = _trade.trade_volume;
                                        trade.Price = _trade.trade_price;
                                        trade.SecurityNameCode = GetSecuritiName(order.symbol, order.contract_type);
                                        trade.Side = order.direction == "buy" ? Side.Buy : Side.Sell;

                                        if (MyTradeEvent != null)
                                        {
                                            MyTradeEvent(trade);
                                        }

                                    }
                                }


                            }
                            else if (mes.Contains("accounts."))
                            {
                                var portfolios = JsonConvert.DeserializeAnonymousType(mes, new HBWsResponse <List<HBContractBalanse>>());

                                if (UpdatePortfolio != null)
                                {
                                    UpdatePortfolio(portfolios.data);
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
        private void UserDataConnect(object sender, EventArgs e)
        {
            IsConnected = true;

            var ws = (WebSocket)sender;

            Dictionary<string, string> param = new Dictionary<string, string>();
            ApiSignature _sign = new ApiSignature();
            try
            {
                _sign.createSignature(ApiKey, SecretKey, "GET", "api.hbdm.com", "/notification", param);
            }
            catch (Exception exception)
            {
                SendLogMessage(exception.Message, LogMessageType.Connect);
            }
            param.Add(ApiSignature.op, ApiSignature.opValue);
            param.Add("type", "api");
            param.Add("cid", "userdata");
            string msg = JsonConvert.SerializeObject(param);
            ws.Send(msg);
            msg = $"{{\"op\": \"sub\",\"topic\": \"orders.*\",\"cid\": \"userdata\"}}";
            ws.Send(msg);
            msg = $"{{\"op\": \"sub\",\"topic\": \"accounts.*\",\"cid\": \"userdata\"}}";
            ws.Send(msg);
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
        
        /// <summary>
        /// portfolios updated
        /// обновились портфели
        /// </summary>
        public event Action<List<HBContractBalanse>> UpdatePortfolio;

        /*


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
