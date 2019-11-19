/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
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

            _tab1.PositionOpeningSuccesEvent += _PositionOpeningSuccesEvent;
            _tab2.PositionOpeningSuccesEvent += _PositionOpeningSuccesEvent;

            _tab1.PositionOpeningFailEvent += _PositionOpeningFailEvent;
            _tab1.PositionClosingFailEvent += _PositionClosingFailEvent;
            _tab2.PositionClosingFailEvent += _PositionClosingFailEvent;

            _tab1.PositionClosingSuccesEvent += _PositionClosingSuccesEvent;
            _tab2.PositionClosingSuccesEvent += _PositionClosingSuccesEvent;

            Analiser = new MarketDepthSpreadAnaliser();
            Analiser.addTab(_tab1);
            Analiser.addTab(_tab2);
            Analiser.SpreadChangeEvent += Analiser_SpreadChangeEvent;

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
            
            Volume1 = CreateParameter("Volume1", 3m, 1, 1000, 1);
            Volume2 = CreateParameter("Volume2", 3m, 1, 1000, 1);

            minSpread = CreateParameter("minSpread", 0.4m, 0.1m, 3, 0.05m);
            minProfit = CreateParameter("minProfit", 0.3m, 0.1m, 3, 0.05m);

            Slippage = CreateParameter("Slipage", 0, 0, 20, 1);
            
            BitmexFix = CreateParameter("BitmexFix", false);

            ParametrsChangeByUser += ArbitrageIndex_ParametrsChangeByUser;
            if (_tabs == null) _tabs = new Tabs();

        }

        private void _PositionOpeningFailEvent(Position obj)
        {
           // throw new NotImplementedException();
        }

        private void _PositionClosingSuccesEvent(Position obj)
        {
            if (BitmexFix.ValueBool)
            {
                foreach (var tab in TabsSimple)
                {
                    if(tab.PositionsLast.State == PositionStateType.Open)
                    {
                        tab.CloseAllAtMarket();
                    }
                }

            }

        }

        private void _PositionClosingFailEvent(Position obj)
        {
            if (BitmexFix.ValueBool)
            {
                obj.State = PositionStateType.Open;
            }
        }
        private void _PositionOpeningSuccesEvent(Position obj)
        {
            obj.Comission = 0.075m;
            if (BitmexFix.ValueBool)
            {
                foreach (var tab in TabsSimple)
                {
                    if (tab.PositionsLast == null || tab.PositionsLast.State == PositionStateType.Done || tab.PositionsLast.State == PositionStateType.OpeningFail)
                    {
                        if (obj.Direction == Side.Buy)
                        {
                            _tabs.SellTable.SellAtMarket(_tabs.SellVol);
                        }
                        else
                        {
                            _tabs.BuyTable.BuyAtMarket(_tabs.BuyVol);
                        }
                        Status = PositionStateType.Open;
                    }
                }
            }
        }

        private void Analiser_SpreadChangeEvent(decimal obj)
        {

            Console.WriteLine("Spread: " + obj);
            if(obj > minSpread.ValueDecimal || obj < -minSpread.ValueDecimal)
            {
                _tab1.SetNewLogMessage("Spread: "+obj,Logging.LogMessageType.Signal);
            }
            if (Regime.ValueString == "Off")
            {
                return;
            }
            if (_tab1.IsConnected == false || _tab2.IsConnected == false)
            {
                return;
            }
            if (_tab1.CandlesAll == null || _tab2.CandlesAll == null ||_tab1.CandlesAll.Count == 0 || _tab2.CandlesAll.Count == 0) return;


            decimal profit = 0m;
            var countOpen = 0;
            var countClose = 0;
            foreach(var tab in TabsSimple)
            {
                if(tab.PositionsLast == null || 
                    tab.PositionsLast.State == PositionStateType.Done || 
                    tab.PositionsLast.State == PositionStateType.Deleted || 
                    tab.PositionsLast.State == PositionStateType.OpeningFail)
                {
                    countOpen++;
                }
                if (tab.PositionsLast != null && tab.PositionsLast.State == PositionStateType.Open )
                {
                    countClose++;
                }
            }
            if (countOpen == 2) canOpen = true;
            if (countClose == 2) canClose = true;

            if (canOpen && Status != PositionStateType.Opening)
            {
                if (obj > minSpread.ValueDecimal || obj < - minSpread.ValueDecimal)
                {
                    OpenPositions();
                }
                Status = PositionStateType.Opening;
            }
            if (canClose && Status == PositionStateType.Open)
            {
                ClosePositions();
                Status = PositionStateType.Closing;
            }
        }
        private void OpenPositions()
        {
            if (_tab1.CandlesAll[_tab1.CandlesAll.Count - 1].Close > _tab2.CandlesAll[_tab2.CandlesAll.Count - 1].Close)
            {
                _tabs.BuyTable = _tab2;
                _tabs.BuyVol = Volume2.ValueDecimal;
                _tabs.SellTable = _tab1;
                _tabs.SellVol = Volume1.ValueDecimal;
            }
            else
            {
                _tabs.BuyTable = _tab1;
                _tabs.BuyVol = Volume1.ValueDecimal;
                _tabs.SellTable = _tab2;
                _tabs.SellVol = Volume2.ValueDecimal;

            }
            if (BitmexFix.ValueBool)
            {
                if (_tabs.SellTable.Connector.ServerType == ServerType.BitMex)
                {
                    _tabs.SellTable.SellAtMarket(_tabs.SellVol);
                }
                else if (_tabs.BuyTable.Connector.ServerType == ServerType.BitMex)
                {
                    _tabs.BuyTable.BuyAtMarket(_tabs.BuyVol);
                }
            }
            else
            {
                _tabs.SellTable.SellAtMarket(_tabs.SellVol);
                _tabs.BuyTable.BuyAtMarket(_tabs.BuyVol);
            }

        }
        private void ClosePositions()
        {
            if (_tab1.PositionsLast.Direction == Side.Buy)
            {
                _tabs.BuyTable = _tab1;
                _tabs.BuyVol = Volume1.ValueDecimal;
                _tabs.SellTable = _tab2;
                _tabs.SellVol = Volume2.ValueDecimal;
            }
            else
            {
                _tabs.BuyTable = _tab2;
                _tabs.BuyVol = Volume2.ValueDecimal;
                _tabs.SellTable = _tab1;
                _tabs.SellVol = Volume1.ValueDecimal;
            }
            decimal profit = 0;

            profit = (_tabs.BuyTable.PriceBestBid - _tabs.BuyTable.PositionsLast.EntryPrice) / _tabs.BuyTable.PositionsLast.EntryPrice
                + (_tabs.SellTable.PositionsLast.EntryPrice - _tabs.SellTable.PriceBestAsk) / _tabs.SellTable.PositionsLast.EntryPrice;

            if (profit > minProfit.ValueDecimal / 100)
            {
                if (BitmexFix.ValueBool)
                {
                    foreach (var tab in TabsSimple)
                    {
                        if (tab.Connector.ServerType == ServerType.BitMex)
                        {
                            tab.CloseAllAtMarket();
                        }
                    }
                }
                else
                {
                    _tab1.CloseAllAtMarket();
                    _tab2.CloseAllAtMarket();
                }
            }
            /*
            List<Position> positions1 = _tab1.PositionsOpenAll;
            List<Position> positions2 = _tab2.PositionsOpenAll;
            decimal pr1;
            decimal pr2;
            if (positions1[0].Direction == Side.Buy)
            {
                pr1 = _tab1.PriceBestBid;
                pr2 = _tab2.PriceBestAsk;
                profit = (pr1 - positions1[0].EntryPrice) / positions1[0].EntryPrice + (positions2[0].EntryPrice - pr2) / positions2[0].EntryPrice;
                if (profit > minProfit.ValueDecimal / 100)
                {
                    if (BitmexFix.ValueBool)
                    {
                        foreach (var tab in TabsSimple)
                        {
                            if (tab.Connector.ServerType == ServerType.BitMex)
                            {
                                tab.CloseAllAtMarket();
                            }
                        }
                    }
                    else
                    {
                        _tab1.CloseAllAtMarket();
                        _tab2.CloseAllAtMarket();
                    }
                }

            }
            if (positions1[0].Direction == Side.Sell)
            {
                pr1 = _tab1.PriceBestAsk;
                pr2 = _tab2.PriceBestBid;
                profit = (positions1[0].EntryPrice - pr1) / positions1[0].EntryPrice + (pr2 - positions2[0].EntryPrice) / positions2[0].EntryPrice;
                if (profit > minProfit.ValueDecimal / 100)
                {
                    if (BitmexFix.ValueBool)
                    {
                        foreach (var tab in TabsSimple)
                        {
                            if (tab.Connector.ServerType == ServerType.BitMex)
                            {
                                tab.CloseAllAtMarket();
                            }
                        }
                    }
                    else
                    {
                        _tab1.CloseAllAtMarket();
                        _tab2.CloseAllAtMarket();
                    }
                }
            }
            */
            Console.WriteLine("profit: " + Math.Round(profit * 100, 2));

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
        class Tabs
        {
            public BotTabSimple BuyTable;
            public decimal BuyVol;
            public BotTabSimple SellTable;
            public decimal SellVol;
        }
        private Tabs _tabs;
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
        /// volume
        /// объём исполняемый в одной сделке
        /// </summary>
        public StrategyParameterDecimal Volume1;
        public StrategyParameterDecimal Volume2;
        /// <summary>
        /// Один из серверов Bitmex
        /// </summary>
        private StrategyParameterBool BitmexFix;

        public StrategyParameterDecimal minSpread;

        public StrategyParameterDecimal minProfit;

        private MarketDepthSpreadAnaliser Analiser;
        /// <summary>
        /// Можно открыватьпозицию
        /// </summary>
        private bool canOpen;
        
        /// <summary>
        /// Можно закрывать позицию
        /// </summary>
        private bool canClose;
        // logic логика


    }
}
