﻿using Com.Lmax.Api.Account;
using Com.Lmax.Api.OrderBook;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market.Servers.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OsEngine.Market.Servers.Lmax
{
    /// <summary>
    /// сервер LMax
    /// </summary>
    public class LmaxServer : AServer
    {
        public LmaxServer()
        {
            LmaxServerRealization realization = new LmaxServerRealization();
            ServerRealization = realization;

            CreateParameterString("FIX – Trading IP/DNS", "91.215.166.178");
            CreateParameterString("FIX – Market Data IP/DNS", "91.215.166.179");
            CreateParameterString("UI - URL", "https://web-order.demo.eisco.co.uk");
            CreateParameterInt("Port", 443);
            CreateParameterString("Username", "");
            CreateParameterPassword("Password", "");
        }

        private void RealizationOnCandlesReadyEvent(List<Candle> candles)
        {
            CandlesReadyEvent?.Invoke(candles);
        }

        /// <summary>
        /// запрос истории по инструменту
        /// </summary>
        public List<Candle> GetCandleHistory(string nameSec, TimeSpan tf)
        {
            return ((LmaxServerRealization)ServerRealization).GetCandleHistory(nameSec, tf);

        }

        public event Action<List<Candle>> CandlesReadyEvent;
    }

    public class LmaxServerRealization : IServerRealization
    {
        public LmaxServerRealization()
        {
            ServerStatus = ServerConnectStatus.Disconnect;
            _startWorkingTime = new DateTime(0001, 01, 01, 22, 05, 00);
            _endWorkingTime = new DateTime(0001, 01, 01, 22, 00, 00);
        }

        /// <summary>
        /// тип сервера
        /// </summary>
        public ServerType ServerType { get { return ServerType.Lmax; } }

        public List<IServerParameter> ServerParameters { get; set; }

        /// <summary>
        /// время сервера
        /// </summary>
        public DateTime ServerTime { get; set; }

        private readonly DateTime _startWorkingTime;
        private readonly DateTime _endWorkingTime;

        // запросы

        private DateTime _lastStartServerTime;

        private LmaxFixClient _client;

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();

                _client.Connected -= ClientOnConnected;
                _client.UpdatedSecurities -= ClientOnUpdatedSecurities;
                _client.Disconnected -= ClientOnDisconnected;
                _client.UpdatePortfolios -= ClientOnUpdatePortfolios;
                _client.UpdateMarketDepth -= ClientOnUpdateMarketDepth;
                _client.MyTradeEvent -= ClientOnMyTradeEvent;
                _client.MyOrderEvent -= ClientOnMyOrderEvent;
                _client.LogMessageEvent -= SendLogMessage;
            }

            _client = null;
            ServerStatus = ServerConnectStatus.Disconnect;
        }

        public void Connect()
        {
            var currentTime = DateTime.UtcNow;

            if (currentTime.Hour == _endWorkingTime.Hour && currentTime.Minute < _startWorkingTime.Minute && currentTime.Minute > _endWorkingTime.Minute)
            {
                return;
            }
            if (_client == null)
            {
                _client = new LmaxFixClient(((ServerParameterString)ServerParameters[0]).Value,
                                            ((ServerParameterString)ServerParameters[1]).Value,
                                            ((ServerParameterString)ServerParameters[2]).Value,
                                            ((ServerParameterInt)ServerParameters[3]).Value,
                                            ((ServerParameterString)ServerParameters[4]).Value,
                                            ((ServerParameterPassword)ServerParameters[5]).Value,
                                             _startWorkingTime, _endWorkingTime);

                _client.Connected += ClientOnConnected;
                _client.UpdatedSecurities += ClientOnUpdatedSecurities;
                _client.Disconnected += ClientOnDisconnected;
                _client.UpdatePortfolios += ClientOnUpdatePortfolios;
                _client.UpdateMarketDepth += ClientOnUpdateMarketDepth;
                _client.MyTradeEvent += ClientOnMyTradeEvent;
                _client.MyOrderEvent += ClientOnMyOrderEvent;
                _client.LogMessageEvent += SendLogMessage;
            }

            _lastStartServerTime = DateTime.Now;

            if (_client.IsCreated)
            {
                _client.Connect();
            }
            else
            {
                SendLogMessage("Не удалось начать подключение, отсутствует один или несколько обязательных параметров", LogMessageType.Error);
            }
        }

        private void ClientOnDisconnected()
        {
            if (DisconnectEvent != null)
            {
                DisconnectEvent();
            }
            ServerStatus = ServerConnectStatus.Disconnect;
        }

        public void GetSecurities()
        {
            _client.GetSecyrities();
        }

        public void GetPortfolios()
        {
            _client.GetPortfolios();
        }

        public void SendOrder(Order order)
        {
            string securityId = _securities.Find(sec => sec.Name == order.SecurityNameCode).NameId;
            _client.SendNewOrderSingle(securityId, order);
        }

        public void CanselOrder(Order order)
        {
            string securityId = _securities.Find(sec => sec.Name == order.SecurityNameCode).NameId;
            _client.CancelOrder(securityId, order);
        }

        /// <summary>
        /// подписаться на получение данных по инструменту
        /// </summary>
        public void Subscrible(Security security)
        {
            _client.SubscribeToPaper(security.NameId);
        }

        /// <summary>
        /// запросить статус ордеров
        /// </summary>
        /// <param name="orders">список ордеров</param>
        public void GetOrdersState(List<Order> orders)
        {
            foreach (var order in orders)
            {
                string secId = _securities.Find(s => s.Name == order.SecurityNameCode).NameId;

                if (secId != null)
                {
                    _client.GetOrderState(secId, order);
                }
            }
        }

        public ServerConnectStatus ServerStatus { get; set; }

        private void ClientOnConnected()
        {
            ConnectEvent?.Invoke();
            ServerStatus = ServerConnectStatus.Connect;
        }

        /// <summary>
        /// обновились портфели
        /// </summary>
        private void ClientOnUpdatePortfolios(AccountStateEvent accountState)
        {
            var portfolioInfo = new List<Portfolio>();

            foreach (var accountStateWallet in accountState.Wallets)
            {
                portfolioInfo.Add(new Portfolio()
                {
                    Number = accountStateWallet.Key,
                    ValueCurrent = accountStateWallet.Value,
                });
            }

            PortfolioEvent?.Invoke(portfolioInfo);
        }

        private List<Security> _securities;

        /// <summary>
        /// пришел список инструментов биржи
        /// </summary>
        private void ClientOnUpdatedSecurities(List<Instrument> securities)
        {
            if (_securities == null)
            {
                _securities = new List<Security>();
            }

            for (int sec = 0; sec < securities.Count; sec++)
            {
                var lmaxSec = securities[sec];

                if (_securities.Find(s => s.Name == lmaxSec.Name.Replace("/", "-")) != null)
                {
                    _securities.Add(
                        new Security
                        {
                            Name = lmaxSec.Name.Replace("/", "-") + "_2",
                            NameId = lmaxSec.Id.ToString(),
                            NameClass = lmaxSec.Contract.Currency,
                            Lot = lmaxSec.OrderBook.QuantityIncrement,
                            PriceStep = lmaxSec.OrderBook.PriceIncrement,
                            PriceStepCost = lmaxSec.OrderBook.PriceIncrement,
                        });

                    if (lmaxSec.OrderBook.PriceIncrement < 1)
                    {
                        _securities.Last().Decimals =
                            lmaxSec.OrderBook.PriceIncrement.ToString(CultureInfo.InvariantCulture).Split('.')[1].Length;
                    }
                    continue;
                }
                _securities.Add(
                    new Security
                    {
                        Name = lmaxSec.Name.Replace("/", "-"),
                        NameId = lmaxSec.Id.ToString(),
                        NameClass = lmaxSec.Contract.Currency,
                        Lot = lmaxSec.OrderBook.QuantityIncrement,
                        PriceStep = lmaxSec.OrderBook.PriceIncrement,
                        PriceStepCost = lmaxSec.OrderBook.PriceIncrement
                    });

                if (lmaxSec.OrderBook.PriceIncrement < 1)
                {
                    _securities.Last().Decimals =
                        lmaxSec.OrderBook.PriceIncrement.ToString(CultureInfo.InvariantCulture).Split('.')[1].Length;
                }
            }

            SecurityEvent?.Invoke(_securities);
        }

        /// <summary>
        /// все стаканы
        /// </summary>
        private List<MarketDepth> _depths;

        private readonly object _depthLocker = new object();

        /// <summary>
        /// обновились стаканы и тики
        /// </summary>
        private void ClientOnUpdateMarketDepth(OrderBookEvent data)
        {
            try
            {
                lock (_depthLocker)
                {
                    if (_depths == null)
                    {
                        _depths = new List<MarketDepth>();
                    }

                    MarketDepth needDepth = null;

                    var needSecName = _securities.Find(s => s.NameId == data.InstrumentId.ToString()).Name;

                    if (needSecName != null)
                    {
                        needDepth = _depths.Find(depth => depth.SecurityNameCode == needSecName);
                    }

                    if (needDepth == null)
                    {
                        needDepth = new MarketDepth();
                        needDepth.SecurityNameCode = _securities.Find(sec => sec.NameId == data.InstrumentId.ToString()).Name;
                        _depths.Add(needDepth);
                    }

                    needDepth.Asks = new List<MarketDepthLevel>();
                    needDepth.Bids = new List<MarketDepthLevel>();

                    for (int i = 0; data.AskPrices != null && i < data.AskPrices.Count; i++)
                    {
                        needDepth.Asks.Add(new MarketDepthLevel()
                        {
                            Ask = data.AskPrices[i].Quantity,
                            Price = data.AskPrices[i].Price,
                        });
                    }

                    for (int i = 0; data.BidPrices != null && i < data.BidPrices.Count; i++)
                    {
                        needDepth.Bids.Add(new MarketDepthLevel()
                        {
                            Bid = data.BidPrices[i].Quantity,
                            Price = data.BidPrices[i].Price,
                        });
                    }

                    needDepth.Time = new DateTime(1970, 1, 1).AddMilliseconds(data.Timestamp);

                    if (needDepth.Time == DateTime.MinValue)
                    {
                        return;
                    }

                    if (MarketDepthEvent != null)
                    {
                        MarketDepthEvent(needDepth.GetCopy());
                    }

                    if (data.LastTradedPrice != Decimal.MinValue)
                    {
                        Trade trade = new Trade();
                        trade.SecurityNameCode = needDepth.SecurityNameCode;
                        trade.Price = data.LastTradedPrice;
                        trade.Time = needDepth.Time;

                        if (NewTradesEvent != null)
                        {
                            NewTradesEvent(trade);
                        }
                    }
                }
            }
            catch (Exception error)
            {
                SendLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// запрос истории по инструменту
        /// </summary>
        public List<Candle> GetCandleHistory(string nameSec, TimeSpan tf)
        {
            return null;
        }

        /// <summary>
        /// обновился мой ордер
        /// </summary>
        private void ClientOnMyOrderEvent(Order order)
        {
            if (order.SecurityNameCode != null)
            {
                order.SecurityNameCode = _securities.Find(s => s.NameId == order.SecurityNameCode).Name;
            }

            if (MyOrderEvent != null)
            {
                MyOrderEvent(order);
            }
        }

        /// <summary>
        /// пришла моя сделка
        /// </summary>
        private void ClientOnMyTradeEvent(MyTrade myTrade)
        {
            myTrade.SecurityNameCode = _securities.Find(s => s.NameId == myTrade.SecurityNameCode).Name;

            if (MyTradeEvent != null)
            {
                MyTradeEvent(myTrade);
            }
        }


        public event Action<Order> MyOrderEvent;
        public event Action<MyTrade> MyTradeEvent;
        public event Action<List<Portfolio>> PortfolioEvent;
        public event Action<List<Security>> SecurityEvent;
        public event Action<MarketDepth> MarketDepthEvent;
        public event Action<Trade> NewTradesEvent;
        public event Action ConnectEvent;
        public event Action DisconnectEvent;


        // сообщения для лога

        /// <summary>
        /// добавить в лог новое сообщение
        /// </summary>
        private void SendLogMessage(string message, LogMessageType type)
        {
            LogMessageEvent?.Invoke(message, type);
        }

        /// <summary>
        /// исходящее сообщение для лога
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;

    }
}
