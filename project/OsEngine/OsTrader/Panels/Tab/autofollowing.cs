/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Shapes;
using OsEngine.Alerts;
using OsEngine.Charts.CandleChart;
using OsEngine.Charts.CandleChart.Elements;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Language;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Connectors;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.Optimizer;
using OsEngine.Market.Servers.Tester;
using OsEngine.OsTrader.Panels.Tab.Internal;

namespace OsEngine.OsTrader.Panels.Tab
{
    /// <summary>
    /// trading tab / 
    /// вкладка для торговли 
    /// </summary>
    public class AutoFollowing : IIBotTab
    {
        /// <summary>
        /// constructor / 
        /// конструктор
        /// </summary>
        public AutoFollowing(string name, StartProgram startProgram)
        {
            TabName = name;
            StartProgram = startProgram;

            try
            {
                _connector = new ConnectorCandles(TabName, startProgram);
                _connector.OrderChangeEvent += _connector_OrderChangeEvent;
                _connector.MyTradeEvent += _connector_MyTradeEvent;
                _connector.BestBidAskChangeEvent += _connector_BestBidAskChangeEvent;
                _connector.GlassChangeEvent += _connector_GlassChangeEvent;
                _connector.TimeChangeEvent += StrategOneSecurity_TimeServerChangeEvent;
                _connector.NewCandlesChangeEvent += LogicToEndCandle;
                _connector.LastCandlesChangeEvent += LogicToUpdateLastCandle;
                _connector.TickChangeEvent += _connector_TickChangeEvent;
                _connector.LogMessageEvent += SetNewLogMessage;
                _connector.ConnectorStartedReconnectEvent += _connector_ConnectorStartedReconnectEvent;

                _connector.NewCandlesChangeEvent += _connector_NewCandlesChangeEvent;

                _marketDepthPainter = new MarketDepthPainter(TabName);
                _marketDepthPainter.LogMessageEvent += SetNewLogMessage;

                _journal = new Journal.Journal(TabName, startProgram);

                _journal.PositionStateChangeEvent += _journal_PositionStateChangeEvent;
                _journal.PositionNetVolumeChangeEvent += _journal_PositionNetVolumeChangeEvent;
                _journal.UserSelectActionEvent += _journal_UserSelectActionEvent;
                _journal.LogMessageEvent += SetNewLogMessage;


                _chartMaster = new ChartCandleMaster(TabName, StartProgram);
                _chartMaster.LogMessageEvent += SetNewLogMessage;
                _chartMaster.SetNewSecurity(_connector.NamePaper, _connector.TimeFrameBuilder, _connector.PortfolioName, _connector.ServerType);
                _chartMaster.SetPosition(_journal.AllPosition);

                _alerts = new AlertMaster(TabName, _connector, _chartMaster);
                _alerts.LogMessageEvent += SetNewLogMessage;
                _dealCreator = new PositionCreator();


                _lastTickIndex = 0;

            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        private void _connector_NewCandlesChangeEvent(List<Candle> candles)
        {
            if (candles != null && candles.Count > 1)
            {
                candles[candles.Count - 2].Trades = new List<Trade>();
                candles[candles.Count - 2].ClasterData.data = new List<ClasterData.PriseData>();
            }
        }

        /// <summary>
        /// the connector has started the reconnection procedure / 
        /// коннектор запустил процедуру переподключения
        /// </summary>
        /// <param name="securityName">security name / имя бумаги</param>
        /// <param name="timeFrame">timeframe DateTime/ таймфрейм бумаги</param>
        /// <param name="timeFrameSpan">timeframe TimeSpan / таймфрейм в виде времени</param>
        /// <param name="portfolioName">porrtfolio name / номер портфеля</param>
        /// <param name="serverType">server type / тип сервера у коннектора</param>
        void _connector_ConnectorStartedReconnectEvent(string securityName, TimeFrame timeFrame, TimeSpan timeFrameSpan, string portfolioName, ServerType serverType)
        {
            if (string.IsNullOrEmpty(securityName)// ||
                                                  //string.IsNullOrEmpty(portfolioName)
                )
            {
                return;
            }

            _chartMaster.SetNewSecurity(securityName, _connector.TimeFrameBuilder, portfolioName, serverType);
        }

        // control / управление

        /// <summary>
        /// start drawing this robot / 
        /// начать прорисовку этого робота
        /// </summary> 
        public void StartPaint(WindowsFormsHost hostChart, WindowsFormsHost hostGlass, WindowsFormsHost hostOpenDeals,
                     WindowsFormsHost hostCloseDeals, Rectangle rectangleChart, WindowsFormsHost hostAlerts, TextBox textBoxLimitPrice, Grid gridChartControlPanel)
        {
            try
            {
                _chartMaster.StartPaint(hostChart, rectangleChart);
                _marketDepthPainter.StartPaint(hostGlass, textBoxLimitPrice);
                _journal.StartPaint(hostOpenDeals, hostCloseDeals);
                _alerts.StartPaint(hostAlerts);
                _chartMaster.StartPaintChartControlPanel(gridChartControlPanel);
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// stop drawing this robot / 
        /// остановить прорисовку этого робота
        /// </summary>
        public void StopPaint()
        {
            try
            {
                _chartMaster.StopPaint();
                _marketDepthPainter.StopPaint();
                _journal.StopPaint();
                _alerts.StopPaint();
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// unique robot name / 
        /// уникальное имя робота
        /// </summary>
        public string TabName { get; set; }

        /// <summary>
        /// tab num /
        /// номер вкладки
        /// </summary>
        public int TabNum { get; set; }

        /// <summary>
        /// clear data in the robot / 
        /// очистить данные в роботе
        /// </summary>
        public void Clear()
        {
            try
            {
                _journal.Clear();
                _chartMaster.Clear();
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// remove the robot and all child structures / 
        /// удалить робота и все дочерние структуры
        /// </summary>
        public void Delete()
        {
            try
            {
                _journal.Delete();
                _connector.Delete();
                _manualControl.Delete();
                _chartMaster.Delete();
                _alerts.DeleteAll();
                _marketDepthPainter.Delete();

                if (File.Exists(@"Engine\" + TabName + @"SettingsBot.txt"))
                {
                    File.Delete(@"Engine\" + TabName + @"SettingsBot.txt");
                }

                if (DeleteBotEvent != null)
                {
                    DeleteBotEvent(TabNum);
                }
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// whether the connector is connected to download data / 
        /// подключен ли коннектор на скачивание данных
        /// </summary>
        public bool IsConnected
        {
            get { return _connector.IsConnected; }
        }

        /// <summary>
        /// the program that created the object / 
        /// программа создавшая объект
        /// </summary>
        public StartProgram StartProgram;

        // logging / работа с логом

        /// <summary>
        /// put a new message in the log / 
        /// положить в лог новое сообщение
        /// </summary>
        public void SetNewLogMessage(string message, LogMessageType messageType)
        {
            if (LogMessageEvent != null)
            {
                LogMessageEvent(message, messageType);
            }
            else if (messageType == LogMessageType.Error)
            {
                System.Windows.MessageBox.Show(message);
            }
        }

        /// <summary>
        /// outgoing message for log / 
        /// исходящее сообщение для лога
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;

        // indicator management / менеджмент индикаторов

        /// <summary>
        /// create indicator / 
        /// создать индикатор
        /// </summary>
        /// <param name="indicator">indicator / индикатор</param>
        /// <param name="nameArea">the name of the area on which it will be placed. Default: "Prime" / название области на которую он будет помещён. По умолчанию: "Prime"</param>
        /// <returns></returns>
        public IIndicatorCandle CreateCandleIndicator(IIndicatorCandle indicator, string nameArea)
        {
            return _chartMaster.CreateIndicator(indicator, nameArea);
        }

        /// <summary>
        /// remove indicator / 
        /// удалить индикатор 
        /// </summary>
        public void DeleteCandleIndicator(IIndicatorCandle indicator)
        {
            _chartMaster.DeleteIndicator(indicator);
        }

        /// <summary>
        /// all available indicators in the system / 
        /// все доступные индикаторы в системе
        /// </summary>
        public List<IIndicatorCandle> Indicators
        {
            get { return _chartMaster.Indicators; }
        }

        // drawing elements / рисование элементов

        /// <summary>
        /// add custom element to the chart / 
        /// добавить на график пользовательский элемент
        /// </summary>
        public void SetChartElement(IChartElement element)
        {
            _chartMaster.SetChartElement(element);
        }

        /// <summary>
        /// remove user element from chart / 
        /// удалить с графика пользовательский элемент
        /// </summary>
        public void DeleteChartElement(IChartElement element)
        {
            _chartMaster.DeleteChartElement(element);
        }

        /// <summary>
        /// remove all custom elements from the graphic / 
        /// удалить все пользовательские элементы с графика
        /// </summary>
        public void DeleteAllChartElement()
        {
            _chartMaster.DeleteAllChartElement();
        }

        // closed components / закрытые составные части

        /// <summary>
        /// class responsible for connecting the tab to the exchange
        /// класс отвечающий за подключение вкладки к бирже
        /// </summary>
        public ConnectorCandles Connector
        {
            get { return _connector; }
        }
        private ConnectorCandles _connector;

        public ConnectorCandles _MasterConector;

        /// <summary>
        /// an object that holds settings for assembling candles / 
        /// объект хранящий в себе настройки для сборки свечей
        /// </summary>
        public TimeFrameBuilder TimeFrameBuilder
        {
            get { return _connector.TimeFrameBuilder; }
        }

        /// <summary>
        /// chart drawing master / 
        /// мастер прорисовки чарта
        /// </summary>
        private ChartCandleMaster _chartMaster;

        /// <summary>
        /// class drawing a marketDepth / 
        /// класс прорисовывающий движения стакана котировок
        /// </summary>
        private MarketDepthPainter _marketDepthPainter;

        /// <summary>
        /// transaction creation wizard / 
        /// мастер создания сделок
        /// </summary>
        private PositionCreator _dealCreator;

        /// <summary>
        /// Journal positions / 
        /// журнал
        /// </summary>
        private Journal.Journal _journal;

        /// <summary>
        /// settings maintenance settings / 
        /// настройки ручного сопровождения
        /// </summary>
        private BotManualControl _manualControl;

        /// <summary>
        /// alerts wizard /
        /// мастер Алертов
        /// </summary>
        private AlertMaster _alerts;

        // properties / свойства 

        /// <summary>
        ///  the status of the server to which the tab is connected /
        /// статус сервера к которому подключена вкладка
        /// </summary>
        public ServerConnectStatus ServerStatus
        {
            get
            {
                if (ServerMaster.GetServers() == null)
                {
                    return ServerConnectStatus.Disconnect;
                }
                IServer myServer = _connector.MyServer;

                if (myServer == null)
                {
                    return ServerConnectStatus.Disconnect;
                }

                return myServer.ServerStatus;
            }
        }

        /// <summary>
        /// security to trading / 
        /// инструмент для торговли
        /// </summary>
        public Security Securiti
        {
            get
            {
                if (_security == null ||
                    _security.Name != _connector.NamePaper)
                {
                    _security = _connector.Security;
                }
                return _security;
            }
            set { _security = value; }
        }
        private Security _security;

        /// <summary>
        /// timeframe data received / 
        /// таймФрейм получаемых данных
        /// </summary>
        public TimeSpan TimeFrame
        {
            get { return _connector.TimeFrameTimeSpan; }
        }

        /// <summary>
        /// trading account / 
        /// счёт для торговли
        /// </summary>
        public Portfolio Portfolio
        {
            get
            {
                if (_portfolio == null)
                {
                    _portfolio = _connector.Portfolio;
                }
                return _portfolio;
            }
            set { _portfolio = value; }
        }
        private Portfolio _portfolio;

        /// <summary>
        /// All positions are owned by bot. Open, closed and with errors / 
        /// все позиции принадлежащие боту. Открытые, закрытые и с ошибками
        /// </summary>
        public List<Position> PositionsAll
        {
            get { return _journal.AllPosition; }
        }

        /// <summary>
        /// all open, partially open and opening positions owned by bot
        /// все открытые, частично открытые и открывающиеся позиции принадлежащие боту
        /// </summary>
        public List<Position> PositionsOpenAll
        {
            get { return _journal.OpenPositions; }
        }

        /// <summary>
        /// stop-limit orders
        /// все ожидающие цены ордера бота
        /// </summary>
        public List<PositionOpenerToStop> PositionOpenerToStopsAll
        {
            get { return _stopsOpener; }
        }

        /// <summary>
        /// all closed, error positions owned by bot / 
        /// все закрытые, с ошибками позиции принадлежащие боту
        /// </summary>
        public List<Position> PositionsCloseAll
        {
            get { return _journal.CloseAllPositions; }
        }

        /// <summary>
        /// last open position / 
        /// последняя открытая позиция
        /// </summary>
        public Position PositionsLast
        {
            get { return _journal.LastPosition; }
        }

        /// <summary>
        /// all open positions are short / 
        /// все открытые позиции шорт
        /// </summary>
        public List<Position> PositionOpenShort
        {
            get { return _journal.OpenAllShortPositions; }
        }

        /// <summary>
        /// all open positions long / 
        /// все открытые позиции лонг
        /// </summary>
        public List<Position> PositionOpenLong
        {
            get { return _journal.OpenAllLongPositions; }
        }

        /// <summary>
        /// exchange position for security
        /// позиция на бирже по инструменту
        /// </summary>
        public PositionOnBoard PositionsOnBoard
        {
            get
            {
                try
                {
                    if (Portfolio == null || Securiti == null)
                    {
                        return null;
                    }

                    List<PositionOnBoard> positionsOnBoard = Portfolio.GetPositionOnBoard();

                    if (positionsOnBoard != null && positionsOnBoard.Count != 0 &&
                        positionsOnBoard.Find(pose => pose.PortfolioName == Portfolio.Number && pose.SecurityNameCode == Securiti.Name) != null)
                    {
                        return positionsOnBoard.Find(pose => pose.SecurityNameCode == Securiti.Name);
                    }
                }
                catch (Exception error)
                {
                    SetNewLogMessage(error.ToString(), LogMessageType.Error);
                }

                return null;
            }
        }

        /// <summary>
        /// net position recruited by the robot / 
        /// нетто позиция набранная роботом
        /// </summary>
        public decimal VolumeNetto
        {
            get
            {
                try
                {
                    List<Position> openPos = PositionsOpenAll;

                    decimal volume = 0;

                    for (int i = 0; openPos != null && i < openPos.Count; i++)
                    {
                        if (openPos[i].Direction == Side.Buy)
                        {
                            volume += openPos[i].OpenVolume;
                        }
                        else // if (openPos[i].Direction == Side.Sell)
                        {
                            volume -= openPos[i].OpenVolume;
                        }
                    }
                    return volume;
                }
                catch (Exception error)
                {
                    SetNewLogMessage(error.ToString(), LogMessageType.Error);
                    return 0;
                }
            }
        }

        /// <summary>
        /// were there closed positions on the current bar / 
        /// были ли закрытые позиции на текущем баре
        /// </summary>
        public bool CheckTradeClosedThisBar()
        {
            List<Position> allClosedPositions = PositionsCloseAll;

            if (allClosedPositions == null)
            {
                return false;
            }

            int totalClosedPositions = allClosedPositions.Count;

            if (totalClosedPositions >= 20)
            {
                allClosedPositions = allClosedPositions.GetRange(totalClosedPositions - 20, 20);
            }

            Candle lastCandle = CandlesAll[CandlesAll.Count - 1];

            foreach (Position position in allClosedPositions)
            {
                if (position.TimeClose >= lastCandle.TimeStart)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// all candles of the instrument. Both molded and completed / 
        /// все свечи инструмента. И формируемые и завершённые
        /// </summary>
        public List<Candle> CandlesAll
        {
            get
            {
                return _connector.Candles(false);
            }
        }

        /// <summary>
        /// all candles of the instrument. Only completed / 
        /// все свечи инструмента. Только завершённые
        /// </summary>
        public List<Candle> CandlesFinishedOnly
        {
            get { return _connector.Candles(true); }
        }

        /// <summary>
        /// all instrument trades / 
        /// все тики по инструменту
        /// </summary>
        public List<Trade> Trades
        {
            get { return _connector.Trades; }
        }

        /// <summary>
        /// server time / 
        /// текущее время сервера
        /// </summary>
        public DateTime TimeServerCurrent
        {
            get { return _connector.MarketTime; }
        }

        /// <summary>
        /// marketDepth / 
        /// стакан по инструменту
        /// </summary>
        public MarketDepth MarketDepth { get; set; }

        /// <summary>
        /// best selling price / 
        /// лучшая цена продажи инструмента
        /// </summary>
        public decimal PriceBestAsk
        {
            get { return _connector.BestAsk; }
        }

        /// <summary>
        /// best buy price / 
        /// лучшая цена покупки инструмента этой вкладки
        /// </summary>
        public decimal PriceBestBid
        {
            get { return _connector.BestBid; }
        }

        /// <summary>
        /// marketDepth center price /
        /// цена центра стакана
        /// </summary>
        public decimal PriceCenterMarketDepth
        {
            get
            {
                return (_connector.BestAsk + _connector.BestBid) / 2;
            }
        }

        // call control windows / вызыв окон управления

        /// <summary>
        /// show connector settings window / 
        /// показать окно настроек коннектора
        /// </summary>
        public void ShowConnectorDialog()
        {
            _connector.ShowDialog();
        }

        /// <summary>
        /// show custom settings window / 
        /// показать индивидуальное окно настроек
        /// </summary>
        public void ShowManualControlDialog()
        {
            _manualControl.ShowDialog();
        }





        /// <summary>
        /// move the graph to the current time / 
        /// переместить график к текущему времени
        /// </summary>
        public void GoChartToThisTime(DateTime time)
        {
            _chartMaster.GoChartToTime(time);
        }

        // standard public functions for position management
        // стандартные публичные функции для управления позицией





        /// <summary>
        /// withdraw all robot open orders from the system / 
        /// отозвать все открытые роботом ордера из системы
        /// </summary>
        public void CloseAllOrderInSystem()
        {
            try
            {
                List<Position> positions = _journal.OpenPositions;

                if (positions == null)
                {
                    return;
                }

                for (int i = 0; i < positions.Count; i++)
                {
                    CloseAllOrderToPosition(positions[i]);
                }
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// withdraw all orders from the system associated with this transaction / 
        /// отозвать все ордера из системы, связанные с этой сделкой
        /// </summary>
        public void CloseAllOrderToPosition(Position position)
        {
            try
            {
                position.StopOrderIsActiv = false;
                position.ProfitOrderIsActiv = false;


                if (position.OpenOrders != null &&
                   position.OpenOrders.Count > 0)
                {
                    for (int i = 0; i < position.OpenOrders.Count; i++)
                    {
                        Order order = position.OpenOrders[i];
                        if (order.State != OrderStateType.Done
                            && order.State != OrderStateType.Fail && order.State != OrderStateType.Cancel)
                        {
                            _connector.OrderCancel(position.OpenOrders[i]);
                        }
                    }
                }


                if (position.CloseOrders != null)
                {
                    for (int i = 0; i < position.CloseOrders.Count; i++)
                    {
                        Order closeOrder = position.CloseOrders[i];
                        if (closeOrder.State != OrderStateType.Done
                        && closeOrder.State != OrderStateType.Fail && closeOrder.State != OrderStateType.Cancel)
                        {
                            _connector.OrderCancel(closeOrder);
                        }
                    }
                }
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// withdraw order / 
        /// отозвать ордер
        /// </summary>
        public void CloseOrder(Order order)
        {
            _connector.OrderCancel(order);
        }

        // внутренние функции управления позицией
        // internal position management functions

        private void CloseDeal(Position position, OrderPriceType priceType, decimal price, TimeSpan lifeTime,
            bool isStopOrProfit)
        {
            try
            {
                if (position == null)
                {
                    return;
                }

                position.ProfitOrderIsActiv = false;
                position.StopOrderIsActiv = false;

                for (int i = 0; position.CloseOrders != null && i < position.CloseOrders.Count; i++)
                {
                    if (position.CloseOrders[i].State == OrderStateType.Activ
                        && position.CloseOrders[i].TypeOrder != OrderPriceType.LimitStop
                        && position.CloseOrders[i].TypeOrder != OrderPriceType.MarketStop
                        )
                    {
                        _connector.OrderCancel(position.CloseOrders[i]);
                    }
                }

                for (int i = 0; position.OpenOrders != null && i < position.OpenOrders.Count; i++)
                {
                    if (position.OpenOrders[i].State == OrderStateType.Activ
                        && position.OpenOrders[i].TypeOrder != OrderPriceType.LimitStop
                        && position.OpenOrders[i].TypeOrder != OrderPriceType.MarketStop
                        )
                    {
                        _connector.OrderCancel(position.OpenOrders[i]);
                    }
                }

                if (Securiti == null)
                {
                    return;
                }

                Side sideCloseOrder = Side.Buy;
                if (position.Direction == Side.Buy)
                {
                    sideCloseOrder = Side.Sell;
                }
                price = RoundPrice(price, Securiti, sideCloseOrder);

                if (position.State == PositionStateType.Done &&
                    position.OpenVolume == 0)
                {
                    return;
                }

                position.State = PositionStateType.Closing;

                Order closeOrder = _dealCreator.CreateCloseOrderForDeal(position, price, priceType, lifeTime, StartProgram);

                if (closeOrder == null)
                {
                    if (position.OpenVolume == 0)
                    {
                        position.State = PositionStateType.OpeningFail;
                    }

                    return;
                }

                if (isStopOrProfit)
                {
                    closeOrder.IsStopOrProfit = true;
                }
                position.AddNewCloseOrder(closeOrder);
                _connector.OrderExecute(closeOrder);
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
        }

        /// <summary>
        /// adjust order price to the needs of the exchange / 
        /// подогнать цену контракта под нужды биржи
        /// </summary>
        /// <param name="price">current price / текущая цена по которой интерфейс высокого уровня захотел закрыть позицию</param>
        /// <param name="security">security / бумага</param>
        /// <param name="side">side / сторона входа</param>
        private decimal RoundPrice(decimal price, Security security, Side side)
        {
            try
            {
                if (Securiti.PriceStep == 0)
                {
                    return price;
                }

                if (security.Decimals > 0)
                {
                    price = Math.Round(price, Securiti.Decimals);

                    decimal minStep = 0.1m;

                    for (int i = 1; i < security.Decimals; i++)
                    {
                        minStep = minStep * 0.1m;
                    }

                    while (price % Securiti.PriceStep != 0)
                    {
                        price = price - minStep;
                    }
                }
                else
                {
                    price = Math.Round(price, 0);
                    while (price % Securiti.PriceStep != 0)
                    {
                        price = price - 1;
                    }
                }

                if (side == Side.Buy &&
                    Securiti.PriceLimitHigh != 0 && price > Securiti.PriceLimitHigh)
                {
                    price = Securiti.PriceLimitHigh;
                }

                if (side == Side.Sell &&
                    Securiti.PriceLimitLow != 0 && price < Securiti.PriceLimitLow)
                {
                    price = Securiti.PriceLimitLow;
                }

                return price;
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }

            return 0;
        }

        // handling alerts and stop maintenance
        // обработка алертов и сопровождения стопов

        private object _lockerManualReload = new object();

        /// <summary>
        /// Дата последней проверки стопов
        /// </summary>
        private DateTime _lastCheckStopTime = DateTime.Now;
        /// <summary>
        /// get journal / 
        /// взять журнал
        /// </summary>
        public Journal.Journal GetJournal()
        {
            return _journal;
        }

        // дозакрытие сделки если на закрытии мы взяли больший объём чем нужно
        // closing a deal if at closing we took more volume than necessary

        /// <summary>
        /// time to close the deal / 
        /// время когда надо совершить дозакрытие сделки
        /// </summary>
        private DateTime _lastClosingSurplusTime;


        /// <summary>
        /// stop opening waiting for its price / 
        /// стоп - открытия ожидающие своей цены
        /// </summary>
        private List<PositionOpenerToStop> _stopsOpener;


        // incoming data processing
        // обработка входящих данных

        /// <summary>
        /// new MarketDepth / 
        /// пришёл новый стакан
        /// </summary>
        void _connector_GlassChangeEvent(MarketDepth marketDepth)
        {
            MarketDepth = marketDepth;

            _marketDepthPainter.ProcessMarketDepth(marketDepth);

            if (MarketDepthUpdateEvent != null)
            {
                MarketDepthUpdateEvent(marketDepth);
            }
        }
    


    /// <summary>
    /// position status has changed
    /// изменился статус сделки
    /// </summary>
    private void _journal_PositionStateChangeEvent(Position position)
    {
        try
        {
            _chartMaster.SetPosition(PositionsAll);
        }
        catch (Exception error)
        {
            SetNewLogMessage(error.ToString(), LogMessageType.Error);
        }
    }

    /// <summary>
    /// open position volume changed
    /// изменился открытый объём по сделке
    /// </summary>
    void _journal_PositionNetVolumeChangeEvent(Position position)
    {
        if (PositionNetVolumeChangeEvent != null)
        {
            PositionNetVolumeChangeEvent(position);
        }
    }

    /// <summary>
    /// candle is finished / 
    /// завершилась свеча
    /// </summary>
    /// <param name="candles">свечи</param>
    private void LogicToEndCandle(List<Candle> candles)
    {
        try
        {
            if (candles == null)
            {
                return;
            }

            if (_stopsOpener != null &&
                _stopsOpener.Count != 0)
            {
                //_stopsOpener.Clear();
            }

            _chartMaster.SetCandles(candles);

            if (CandleFinishedEvent != null)
            {
                CandleFinishedEvent(candles);
            }

        }
        catch (Exception error)
        {
            SetNewLogMessage(error.ToString(), LogMessageType.Error);
        }
    }

    /// <summary>
    /// candle is update / 
    /// обновилась последняя свеча
    /// </summary>
    private void LogicToUpdateLastCandle(List<Candle> candles)
    {
        try
        {
            _chartMaster.SetCandles(candles);
            if (CandleUpdateEvent != null)
            {
                CandleUpdateEvent(candles);
            }

        }
        catch (Exception error)
        {
            SetNewLogMessage(error.ToString(), LogMessageType.Error);
        }
    }

    /// <summary>
    /// user ordered a position change / 
    /// пользователь заказал изменение позиции
    /// </summary>
    private void _journal_UserSelectActionEvent(Position position, SignalType signalType)
    {
            try
            {
            }
            catch (Exception error)
            {
                SetNewLogMessage(error.ToString(), LogMessageType.Error);
            }
    }

    /// <summary>
    /// has the session started today? / 
    /// стартовала ли сегодня сессия
    /// </summary>
    private bool _firstTickToDaySend;

    /// <summary>
    /// last tick index / 
    /// последний индекс тика
    /// </summary>
    private int _lastTickIndex;

    /// <summary>
    /// new tiki came / 
    /// пришли новые тики
    /// </summary>
    private void _connector_TickChangeEvent(List<Trade> trades)
    {
        if (trades == null ||
            trades.Count == 0)
        {
            return;
        }

        if (_chartMaster == null)
        {
            return;
        }

        _chartMaster.SetTick(trades);

        Trade trade = trades[trades.Count - 1];

        if (_firstTickToDaySend == false && FirstTickToDayEvent != null)
        {
            if (trade.Time.Hour == 10
                && (trade.Time.Minute == 1 || trade.Time.Minute == 0))
            {
                _firstTickToDaySend = true;
                FirstTickToDayEvent(trade);
            }
        }

        if (_lastTickIndex == 0)
        {
            _lastTickIndex = trades.Count - 1;
            return;
        }

        int curCount = trades.Count;

        if (curCount == _lastTickIndex)
        {
            return;
        }

        List<Position> openPositions = _journal.OpenPositions;


        for (int i2 = _lastTickIndex; i2 < curCount && trades[i2] != null; i2++)
        {
            if (trades[i2] == null)
            {
                trades.RemoveAt(i2);
                return;
            }

            if (NewTickEvent != null)
            {
                try
                {
                    NewTickEvent(trades[i2]);
                }
                catch (Exception error)
                {
                    SetNewLogMessage(error.ToString(), LogMessageType.Error);
                }

            }
        }

        _lastTickIndex = curCount;

    }

    /// <summary>
    /// incoming my deal / 
    /// входящая моя сделка
    /// </summary>
    private void _connector_MyTradeEvent(MyTrade trade)
    {
        _journal.SetNewMyTrade(trade);
    }

    /// <summary>
    /// server time has changed / 
    /// изменилось время сервера
    /// </summary>
    void StrategOneSecurity_TimeServerChangeEvent(DateTime time)
    {
        if (_manualControl != null)
        {
            _manualControl.ServerTime = time;
        }

        if (ServerTimeChangeEvent != null)
        {
            ServerTimeChangeEvent(time);
        }
    }

    /// <summary>
    /// incoming orders / 
    /// входящие ордера
    /// </summary>
    private void _connector_OrderChangeEvent(Order order)
    {
        Order orderInJournal = _journal.IsMyOrder(order);

        if (orderInJournal == null)
        {
            return;
        }
        _journal.SetNewOrder(order);

        if (OrderUpdateEvent != null)
        {
            OrderUpdateEvent(orderInJournal);
        }
    }

    /// <summary>
    /// incoming new bid with ask / 
    /// входящие новые бид с аском
    /// </summary>
    private void _connector_BestBidAskChangeEvent(decimal bestBid, decimal bestAsk)
    {
        _journal.SetNewBidAsk(bestBid, bestAsk);

        _marketDepthPainter.ProcessBidAsk(bestBid, bestAsk);

        if (BestBidAskChangeEvent != null)
        {
            BestBidAskChangeEvent(bestBid, bestAsk);
        }
    }
    /// <summary>
    /// Выставить на сервере стоп для позиции
    /// </summary>
    /// <param name="position">Позиция</param>
    /// <param name="priceLimit">Цена стопа</param>
    /// <param name="priceRedLine">Цена при достижении которой выставляется стоп ордер</param>

    /// <summary>
    /// The morning session started. Send the first trades
    /// утренняя сессия стартовала. Пошли первые тики
    /// </summary>
    public event Action<Trade> FirstTickToDayEvent;

    /// <summary>
    /// new trades
    /// пришли новые тики
    /// </summary>
    public event Action<Trade> NewTickEvent;

    /// <summary>
    /// new server time
    /// изменилось время сервера
    /// </summary>
    public event Action<DateTime> ServerTimeChangeEvent;

    /// <summary>
    /// last candle finished / 
    /// завершилась новая свечка
    /// </summary>
    public event Action<List<Candle>> CandleFinishedEvent;

    /// <summary>
    /// last candle update /
    /// обновилась последняя свечка
    /// </summary>
    public event Action<List<Candle>> CandleUpdateEvent;

    /// <summary>
    /// new marketDepth
    /// пришёл новый стакан
    /// </summary>
    public event Action<MarketDepth> MarketDepthUpdateEvent;

    /// <summary>
    /// bid ask change
    /// изменился лучший бид/аск (лучшая цена покупки, лучшая цена продажи)
    /// </summary>
    public event Action<decimal, decimal> BestBidAskChangeEvent;

    /// <summary>
    /// position successfully closed / 
    /// позиция успешно закрыта
    /// </summary>
    public event Action<Position> PositionClosingSuccesEvent;

    /// <summary>
    /// position successfully opened /
    /// позиция успешно открыта
    /// </summary>
    public event Action<Position> PositionOpeningSuccesEvent;

    /// <summary>
    /// open position volume has changed / 
    /// у позиции изменился открытый объём
    /// </summary>
    public event Action<Position> PositionNetVolumeChangeEvent;

    /// <summary>
    /// opening position failed / 
    /// открытие позиции не случилось
    /// </summary>
    public event Action<Position> PositionOpeningFailEvent;

    /// <summary>
    /// position closing failed / 
    /// закрытие позиции не прошло
    /// </summary>
    public event Action<Position> PositionClosingFailEvent;

    /// <summary>
    /// the robot is removed from the system / 
    /// робот удаляется из системы
    /// </summary>
    public event Action<int> DeleteBotEvent;

    /// <summary>
    /// updated order
    /// обновился ордер
    /// </summary>
    public event Action<Order> OrderUpdateEvent;

    }  
}




