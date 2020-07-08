/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.MarketMaker
{
    public class ArbitrageFutures : BotPanel
    {
        public ArbitrageFutures(string name, StartProgram startProgram)
            : base(name, startProgram)
        {

            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[0];
            TabCreate(BotTabType.Simple);
            _tab2 = TabsSimple[1];

            _tab1.PositionClosingSuccesEvent += PositionClosingSuccesEvent;
            _tab2.PositionClosingSuccesEvent += PositionClosingSuccesEvent;

            Analiser = new MarketDepthSpreadAnaliser();
            Analiser.addTab(_tab1);
            Analiser.addTab(_tab2);
            Analiser.SpreadChangeEvent += Analiser_SpreadChangeEvent;
            Analiser.ProfitChangeEvent += Analiser_ProfitChangeEvent;

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyClosePosition" });

            minSpread = CreateParameter("minSpread", 0.5m, 0.1m, 3, 0.05m);
            minSpreadAdd = CreateParameter("Добирать при расхождении", 0.5m, 0.1m, 3, 0.05m);

            minProfit = CreateParameter("minProfit", 0.5m, 0.1m, 3, 0.05m);

            Slippage = CreateParameter("Slipage", 0, 0, 20, 1);
            MaxTrade = CreateParameter("MaxTrade", 4, 1, 5, 1);

            _CountTrade = CreateParameter("Открыто доборов", 0, 0, 5, 1);

            leverage = CreateParameter("Маржинальное плечо", 1m, 1m, 10, 0.1m);
            isContract = CreateParameter("Торгуем контрактами", false);
            autoCanselTrades = CreateParameter("Принудительно закрывать по времени", true);

            DepoCurrency1 = CreateParameter("Валюта 1", "Currency2", new[] { "Currency1", "Currency2" });
            DepoCurrency2 = CreateParameter("Валюта 2", "Currency2", new[] { "Currency1", "Currency2" });

            divider1 = CreateParameter("Размер 1 контр.", 1, 1, 100, 1);
            divider2 = CreateParameter("Размер 2 контр.", 1, 1, 100, 1);

            ParametrsChangeByUser += ArbitrageIndex_ParametrsChangeByUser;
            
            Thread worker = new Thread(LogicError);
            worker.IsBackground = true;
            worker.Start();

        }
        private void LogicError()
        {

            while (true)
            {
                Thread.Sleep(5 * 1000);
                if (Regime.ValueString == "Off")
                {
                    continue;
                }
                foreach (var tab in Analiser.Tabs)
                {
                    if (!tab.IsConnected
                    || tab.MarketDepth == null
                    || tab.MarketDepth.Asks == null || tab.MarketDepth.Asks.Count == 0
                    || tab.MarketDepth.Bids == null || tab.MarketDepth.Bids.Count == 0
                    || tab.PriceBestBid == 0 || tab.PriceBestAsk == 0
                    || tab.CandlesAll == null || tab.CandlesAll.Count == 0) continue;

                    if (tab.Connector.MyServer.ServerType == ServerType.Tester ||
                        tab.Connector.MyServer.ServerType == ServerType.Optimizer) return;
                }
                decimal counttades = 0;
                _lastTradeTime = DateTime.MinValue;
                foreach(var tab in Analiser.Tabs)
                {
                    counttades += tab.PositionsOpenAll.Count;
                    if(tab.PositionsLast!= null)
                    {
                        if (_lastTradeTime < tab.PositionsLast.TimeOpen) _lastTradeTime = tab.PositionsLast.TimeOpen;
                        if (_lastTradeTime < tab.PositionsLast.TimeClose) _lastTradeTime = tab.PositionsLast.TimeClose;
                    }
                }
                if(counttades == 1)
                {
                    if(_lastTradeTime.ToLocalTime().AddSeconds(10) > DateTime.Now)
                    {
                        _tab1.CloseAllAtMarket();
                        _tab2.CloseAllAtMarket();
                    }
                }
            }
        }
        private DateTime _lastTradeTime;

        private void Analiser_ProfitChangeEvent(decimal obj)
        {
            if (_lastTime.AddSeconds(2) < DateTime.Now)
            {
                _lastTime = DateTime.Now;
            }
            else
            {
                return;
            }
            ClosePositions(obj);
            //Добор позиции
            if (_CountTrade.ValueInt != 0
                && -obj > _CountTrade.ValueInt * minSpreadAdd.ValueDecimal
                && _CountTrade.ValueInt <= MaxTrade.ValueInt)
            {
                _tab1.SetNewLogMessage("Profit: " + _tab1.Securiti.Name + ": " + obj, Logging.LogMessageType.Signal);
                UpdatePositions();
            }

        }

        private void PositionClosingSuccesEvent(Position obj)
        {
            _CountTrade.ValueInt = 0;
        }
        private DateTime _lastTime;
        private void Analiser_SpreadChangeEvent(decimal obj)
        {
            if (_lastTime.AddSeconds(2) < DateTime.Now)
            {
                _lastTime = DateTime.Now;
            }
            else
            {
                return;
            }

            Console.WriteLine("Spread: " + obj);
            if (obj > minSpread.ValueDecimal || obj < -minSpread.ValueDecimal)
            {
                _tab1.SetNewLogMessage("Spread: " + _tab1.Securiti.Name + ": " + obj, Logging.LogMessageType.Signal);
            }
            if (Regime.ValueString == "Off")
            {
                return;
            }
            if (_tab1.CandlesAll == null || _tab2.CandlesAll == null || _tab1.CandlesAll.Count == 0 || _tab2.CandlesAll.Count == 0) return;

            decimal CanTrade = 0;
            foreach (var el in Analiser.Tabs)
            {
                List<Position> openPositions = el.PositionsOpenAll;
                if (openPositions == null || openPositions.Count == 0)
                {
                    CanTrade += 1;
                }
            }
            if (CanTrade == 2)
            {
                if (obj > minSpread.ValueDecimal || obj < -minSpread.ValueDecimal)
                {
                    OpenPositions();

                }
            }
            if (CanTrade == 0)
            {
                //ClosePositions();
                //Добор позиции
                if (_CountTrade.ValueInt != 0
                    && obj > _CountTrade.ValueInt * minSpreadAdd.ValueDecimal
                    && _CountTrade.ValueInt <= MaxTrade.ValueInt)
                {
                    UpdatePositions();
                }
            }

        }
        private struct TabVol
        {
            public BotTabSimple Tab;
            public decimal Volume;
        }
        private List<TabVol> tabVols = new List<TabVol>();
        private void OpenPositions()
        {
            if (Regime.ValueString == "OnlyClosePosition")
            {
                return;
            }
            if (!Analiser.CanTrade)
            {
                return;
            }
            
            decimal minbalance = GetMinimalUSDBalance();
            tabVols.Clear();
            foreach(var t in Analiser._tabs)
            {
                tabVols.Add(new TabVol { Tab = t._tab, Volume = GetTradeVolume(t._tab, minbalance) });
            }
            foreach(var el in tabVols)
            {
                if (el.Volume == 0) return;
            }
            foreach (var t in Analiser._tabs)
            {
                foreach(var el in tabVols)
                {
                    if(t._tab == el.Tab)
                    {
                        if (t.side == Side.Buy)
                        {
                            t._tab.BuyAtMarket(el.Volume);
                        }
                        else
                        {
                            t._tab.SellAtMarket(el.Volume);
                        }
                    }
                }
           }
            _CountTrade.ValueInt += 1;
        }
        private void UpdatePositions()
        {
            if (!Analiser.CanTrade)
            {
                return;
            }
            decimal minbalance = GetMinimalUSDBalance();
            tabVols.Clear();
            foreach (var t in Analiser._tabs)
            {
                tabVols.Add(new TabVol { Tab = t._tab, Volume = GetTradeVolume(t._tab, minbalance) });
            }
            foreach (var el in tabVols)
            {
                if (el.Volume == 0) return;
            }
            foreach (var t in Analiser._tabs)
            {
                foreach (var el in tabVols)
                {
                    if (t._tab == el.Tab)
                    {
                        if (t.side == Side.Buy)
                        {
                            t._tab.BuyAtMarketToPosition(t._tab.PositionsLast, el.Volume);
                        }
                        else
                        {
                            t._tab.SellAtMarketToPosition(t._tab.PositionsLast, el.Volume);
                        }
                    }
                }
            }
            _CountTrade.ValueInt += 1;
        }
        private void ClosePositions()
        {
            decimal profit = Analiser.GetProfitPersent();
            ClosePositions(profit);
        }
        private void ClosePositions(decimal profit)
        {
            if (profit > minProfit.ValueDecimal)
            {
                foreach (var t in Analiser.Tabs)
                {
                    t.CloseAllAtMarket();
                    t.SetNewLogMessage("profit: " + profit, Logging.LogMessageType.Signal);
                }
            }
            Console.WriteLine("profit: " + Math.Round(profit, 2));

        }
        private PositionStateType Status;


        /// <summary>
        /// user change params
        /// пользователь изменил параметр
        /// </summary>
        void ArbitrageIndex_ParametrsChangeByUser()
        {
        }

        /// <summary>
        /// name bot
        /// взять уникальное имя
        /// </summary>
        public override string GetNameStrategyType()
        {
            return "ArbitrageFutures";
        }


        public override void ShowIndividualSettingsDialog()
        {

        }
        /// <summary>
        /// trade tab
        /// вкладка для торговли
        /// </summary>
        private BotTabSimple _tab1;

        /// <summary>
        /// trade tab
        /// вкладка для торговли
        /// </summary>
        private BotTabSimple _tab2;

        /// <summary>
        /// slippage / проскальзывание
        /// </summary>
        public StrategyParameterInt Slippage;

        /// <summary>
        /// regime
        /// режим работы робота
        /// </summary>
        public StrategyParameterString Regime;

        /// <summary>
        /// Используемый процент Депозита
        /// </summary>
        public StrategyParameterInt Volume;

        public StrategyParameterDecimal minSpread;

        public StrategyParameterDecimal minProfit;

        private MarketDepthSpreadAnaliser Analiser;
        /// <summary>
        /// торгуем контрактами
        /// </summary>
        private StrategyParameterBool isContract;
        /// <summary>
        /// Принудительно закрывать сделки по времени
        /// </summary>
        private StrategyParameterBool autoCanselTrades;

        /// <summary>
        /// Плечо
        /// </summary>
        private StrategyParameterDecimal leverage;

        /// <summary>
        /// Максимальное количество доборов позиции
        /// </summary>
        public StrategyParameterInt MaxTrade;

        /// <summary>
        /// Текущее количество сделок
        /// </summary>
        private StrategyParameterInt _CountTrade;

        /// <summary>
        /// Прикаком спреде добирать позицию
        /// </summary>
        public StrategyParameterDecimal minSpreadAdd;

        /// <summary>
        /// Вылюта депозита (первая или вторая валюта валютной пары) 1я панель
        /// </summary>
        private StrategyParameterString DepoCurrency1;
        /// <summary>
        /// Вылюта депозита (первая или вторая валюта валютной пары) 2я панель
        /// </summary>
        private StrategyParameterString DepoCurrency2;

        /// <summary>
        /// Размер одного контракта панели 1
        /// </summary>
        public StrategyParameterInt divider1;
        /// <summary>
        /// Размер одного контракта панель 2
        /// </summary>
        public StrategyParameterInt divider2;

        private decimal GetRoundVolume(decimal v, BotTabSimple _tab)
        {
            return CryptoUtil.GetRoundVolume(_tab, v);
        }
        private decimal GetBalance(BotTabSimple _tab)
        {
            return CryptoUtil.GetBalance(_tab);
        }
        private decimal GetUSDBalance(BotTabSimple _tab)
        {
            if ((DepoCurrency1.ValueString == "Currency2" && _tab == _tab1)
                    || (DepoCurrency2.ValueString == "Currency2" && _tab == _tab2))
            {
                return GetBalance(_tab);
            }
            else
            {
                return GetBalance(_tab) * _tab.CandlesAll.Last().Close;
            }

        }
        private decimal GetMinimalUSDBalance()
        {
            return Math.Min(GetUSDBalance(_tab1), GetUSDBalance(_tab2));
        }
        private decimal GetTradeVolume(BotTabSimple tab,decimal minbalance)
        {
            decimal vol = leverage.ValueDecimal * minbalance / tab.CandlesAll.Last().Close / TabsSimple.Count;
            if (tab == _tab1)
            {
                vol = vol / divider1.ValueInt;
            }
            else
            {
                vol = vol / divider2.ValueInt;
            }
            vol = GetRoundVolume(vol, tab);
            return vol;
        }

    }
}
