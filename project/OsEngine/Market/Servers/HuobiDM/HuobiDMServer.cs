using System;
using System.Collections.Generic;
using System.Globalization;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Logging;
using System.Threading;
using OsEngine.Market.Servers.Entity;
using OsEngine.Market.Servers.HuobiDM.HuobiEntity;

namespace OsEngine.Market.Servers.HuobiDM
{
    class HuobiDMServer :AServer
    {
        public HuobiDMServer()
        {
            HuobiDMServerRealization realization = new HuobiDMServerRealization();
            ServerRealization = realization;

            CreateParameterString(OsLocalization.Market.ServerParamPublicKey, "");
            CreateParameterPassword(OsLocalization.Market.ServerParamSecretKey, "");
        }
        /// <summary>
        /// instrument history query
        /// запрос истории по инструменту
        /// </summary>
        public List<Candle> GetCandleHistory(string nameSec, TimeSpan tf)
        {
            return ((HuobiDMServerRealization)ServerRealization).GetCandleHistory(nameSec, tf);
        }

    }
    public class HuobiDMServerRealization : IServerRealization
    {
        public HuobiDMServerRealization()
        {
            ServerStatus = ServerConnectStatus.Disconnect;
        }

        /// <summary>
        /// server type
        /// тип сервера
        /// </summary>
        public ServerType ServerType
        {
            get { return ServerType.HuobiDM; }
        }

        /// <summary>
        /// server status
        /// статус серверов
        /// </summary>
        public ServerConnectStatus ServerStatus { get; set; }
        
        /// <summary>
        /// server parameters
        /// параметры сервера
        /// </summary>
        public List<IServerParameter> ServerParameters { get; set; }

        /// <summary>
        /// server time
        /// время сервера
        /// </summary>
        public DateTime ServerTime { get; set; }

        /// <summary>
        /// binance client
        /// </summary>
        private HuobiDMClient _client;

        public event Action<Order> MyOrderEvent;
        public event Action<MyTrade> MyTradeEvent;
        public event Action<List<Portfolio>> PortfolioEvent;
        public event Action<List<Security>> SecurityEvent;
        public event Action<MarketDepth> MarketDepthEvent;
        public event Action<Trade> NewTradesEvent;
        public event Action ConnectEvent;
        public event Action DisconnectEvent;

        public void CanselOrder(Order order)
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            if (_client == null)
            {
                _client = new HuobiDMClient(((ServerParameterString)ServerParameters[0]).Value, ((ServerParameterPassword)ServerParameters[1]).Value);
                _client.DaysToLoad = ((ServerParameterInt)ServerParameters[3]).Value;
                _client.Connected += _client_Connected;
                _client.Disconnected += _client_Disconnected;
                _client.UpdatePairs += _client_UpdatePairs;
                _client.NewPortfolio += _client_NewPortfolio;
                _client.NewTradesEvent += _client_NewTradesEvent;
                _client.UpdateMarketDepth += _client_UpdateMarketDepth;
                /*_client.UpdatePortfolio += _client_UpdatePortfolio;
                _client.MyTradeEvent += _client_MyTradeEvent;
                _client.MyOrderEvent += _client_MyOrderEvent;
                */
                _client.LogMessageEvent += SendLogMessage;
            }

            _client.Connect();
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client.Connected -= _client_Connected;
                _client.Disconnected -= _client_Disconnected;
                _client.UpdatePairs -= _client_UpdatePairs;
                _client.LogMessageEvent -= SendLogMessage;
                _client.NewPortfolio -= _client_NewPortfolio;
                _client.NewTradesEvent -= _client_NewTradesEvent;
            }

            _client = null;
            ServerStatus = ServerConnectStatus.Disconnect;
        }
        void _client_Connected()
        {
            if (ConnectEvent != null)
            {
                ConnectEvent();
            }
            ServerStatus = ServerConnectStatus.Connect;
        }
        void _client_Disconnected()
        {
            if (DisconnectEvent != null)
            {
                DisconnectEvent();
            }
            ServerStatus = ServerConnectStatus.Disconnect;
        }
        void _client_UpdatePairs(List<HBContractInfo> pairs)
        {
            if (_securities == null)
            {
                _securities = new List<Security>();
            }

            foreach (var sec in pairs)
            {
                Security security = new Security();
                if (sec.contract_type == "this_week") security.Name = sec.symbol + "_CW";
                if (sec.contract_type == "next_week") security.Name = sec.symbol + "_NW";
                if (sec.contract_type == "quarter") security.Name = sec.symbol + "_CQ";
                //security.Name = sec.contract_code;
                security.NameFull = security.Name;//sec.contract_code;
                security.NameClass = sec.symbol;
                security.NameId = security.Name;//sec.symbol + sec.contract_code;
                security.SecurityType = SecurityType.CurrencyPair;
                // sec.filters[1] - минимальный объем равный цена * объем
                security.Lot = sec.contract_size;
                security.PriceStep = sec.price_tick;
                security.PriceStepCost = security.PriceStep;

                security.PriceLimitLow = 1;
                security.PriceLimitHigh = int.MaxValue;

                if (security.PriceStep < 1)
                {
                    string prStep = security.PriceStep.ToString(CultureInfo.InvariantCulture);
                    security.Decimals = Convert.ToString(prStep).Split('.')[1].Split('1')[0].Length + 1;
                }
                else
                {
                    security.Decimals = 0;
                }

                security.State = SecurityStateType.Activ;
                _securities.Add(security);
            }

            if (SecurityEvent != null)
            {
                SecurityEvent(_securities);
            }
        }
        private List<Portfolio> _portfolios;
        void _client_NewPortfolio(List<HBContractBalanse> portfs)
        {
            try
            {
                if (portfs == null)
                {
                    return;
                }

                if (_portfolios == null)
                {
                    _portfolios = new List<Portfolio>();
                }

                foreach (var onePortf in portfs)
                {
                    Portfolio newPortf = new Portfolio();
                    newPortf.Number = onePortf.symbol;
                    newPortf.ValueCurrent = onePortf.margin_balance;
                    newPortf.ValueBlocked = onePortf.margin_frozen;

                    _portfolios.Add(newPortf);
                }

                if (PortfolioEvent != null)
                {
                    PortfolioEvent(_portfolios);
                }
            }
            catch (Exception error)
            {
                SendLogMessage(error.ToString(), LogMessageType.Error);
            }
        }
        /// <summary>
        /// all depths
        /// все стаканы
        /// </summary>
        private List<MarketDepth> _depths;

        private readonly object _depthLocker = new object();

        void _client_UpdateMarketDepth(HBWsResponse<HBDepth> myDepth)
        {
            try
            {
                lock (_depthLocker)
                {
                    if (_depths == null)
                    {
                        _depths = new List<MarketDepth>();
                    }
                    if (myDepth.tick == null) return;

                    if (myDepth.tick.asks == null || myDepth.tick.asks.Count == 0 ||
                        myDepth.tick.bids == null || myDepth.tick.bids.Count == 0)
                    {
                        return;
                    }
                    var SecurityNameCode = myDepth.ch.Split('.')[1].ToUpper();
                    var needDepth = _depths.Find(depth =>
                        depth.SecurityNameCode == SecurityNameCode);

                    if (needDepth == null)
                    {
                        needDepth = new MarketDepth();
                        needDepth.SecurityNameCode = SecurityNameCode;
                        _depths.Add(needDepth);
                    }

                    List<MarketDepthLevel> ascs = new List<MarketDepthLevel>();
                    List<MarketDepthLevel> bids = new List<MarketDepthLevel>();

                    for (int i = 0; i < myDepth.tick.asks.Count; i++)
                    {
                        ascs.Add(new MarketDepthLevel()
                        {
                            Ask     = myDepth.tick.asks[i][1]
                            ,
                            Price   = myDepth.tick.asks[i][0]

                        }); ;
                    }

                    for (int i = 0; i < myDepth.tick.bids.Count; i++)
                    {
                        bids.Add(new MarketDepthLevel()
                        {
                            Bid =
                                myDepth.tick.bids[i][1]
                            ,
                            Price = myDepth.tick.bids[i][0]

                        });
                    }

                    needDepth.Asks = ascs;
                    needDepth.Bids = bids;
                    needDepth.Time = ServerTime;

                    if (needDepth.Time == DateTime.MinValue)
                    {
                        return;
                    }

                    if (MarketDepthEvent != null)
                    {
                        MarketDepthEvent(needDepth.GetCopy());
                    }
                }
            }
            catch (Exception error)
            {
                SendLogMessage(error.ToString(), LogMessageType.Error);
            }
        }
        /// <summary>
        /// multi-threaded access locker to ticks
        /// блокиратор многопоточного доступа к тикам
        /// </summary>
        private readonly object _newTradesLoker = new object();

        void _client_NewTradesEvent(HBWsResponse<HBTick> trades)
        {
            lock (_newTradesLoker)
            {
                if (trades.tick == null)
                {
                    return;
                }
                var SecArr = trades.ch.Split('.');
                var SecurityNameCode = SecArr[1];
                for (int i=0;i<trades.tick.data.Count;i++)
                {
                    Trade trade = new Trade();
                    trade.SecurityNameCode = SecurityNameCode;
                    trade.Price = trades.tick.data[i].price;
                    trade.Id = trades.tick.data[i].id;
                    trade.Volume = trades.tick.data[i].amount;
                    trade.Side = trades.tick.data[i].direction == "buy" ? Side.Buy : Side.Sell;
                    trade.Time = new DateTime(1970, 1, 1).AddMilliseconds(Convert.ToDouble(trades.tick.data[i].ts)).ToLocalTime();


                    if (NewTradesEvent != null)
                    {
                        NewTradesEvent(trade);
                    }
                }

            }
        }


        private List<Security> _securities;
        public List<Candle> GetCandleDataToSecurity(Security security, TimeFrameBuilder timeFrameBuilder, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            return null;
            //throw new NotImplementedException();
        }

        public void GetOrdersState(List<Order> orders)
        {
            throw new NotImplementedException();
        }

        public void GetPortfolios()
        {
            _client.GetBalance();
            //throw new NotImplementedException();
        }

        public void GetSecurities()
        {
            _client.GetSecurities();
        }

        public List<Trade> GetTickDataToSecurity(Security security, DateTime startTime, DateTime endTime, DateTime actualTime)
        {
            List<Trade> lastTrades = new List<Trade>();
            PreSaveDataSet ds = new PreSaveDataSet("HuobiDM", security.Name);
            ds.NewLogMessageEvent += SendLogMessage;
            lastTrades = ds.LoadTrades();
            return lastTrades;
        }

        public void SendOrder(Order order)
        {
            _client.ExecuteOrder(order);
        }

        public void Subscrible(Security security)
        {
            _client.SubscribleTradesAndDepths(security);
        }

        internal List<Candle> GetCandleHistory(string nameSec, TimeSpan tf)
        {
            return _client.GetCandles(nameSec, tf);
            //throw new NotImplementedException();
        }
        // log messages
        // сообщения для лога

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
        /// outgoing log message
        /// исходящее сообщение для лога
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;
    }
}
