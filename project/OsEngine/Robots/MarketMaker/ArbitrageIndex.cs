﻿/*
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
    public class ArbitrageIndex : BotPanel
    {
        public ArbitrageIndex(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            TabCreate(BotTabType.Index);
            _tabIndex = TabsIndex[0];

            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[0];
            TabCreate(BotTabType.Simple);
            _tab2 = TabsSimple[1];
            _tabIndex.SpreadChangeEvent += _tabIndex_SpreadChangeEvent;

            _tab1.PositionOpeningSuccesEvent += _PositionOpeningSuccesEvent;
            _tab2.PositionOpeningSuccesEvent += _PositionOpeningSuccesEvent;

            Analiser = new MarketDepthSpreadAnaliser();
            Analiser.addTab(_tab1);
            Analiser.addTab(_tab2);
            Analiser.SpreadChangeEvent += Analiser_SpreadChangeEvent;

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
            
            Volume1 = CreateParameter("Volume1", 3m, 1, 50, 1);
            Volume2 = CreateParameter("Volume2", 3m, 1, 50, 1);

            minSpread = CreateParameter("minSpread", 0.4m, 0.1m, 3, 0.05m);
            minProfit = CreateParameter("minProfit", 0.3m, 0.1m, 3, 0.05m);

            Slippage = CreateParameter("Slipage", 0, 0, 20, 1);
            ParametrsChangeByUser += ArbitrageIndex_ParametrsChangeByUser;
        }

        private void Analiser_SpreadChangeEvent(decimal obj)
        {
            Console.WriteLine("Spread: " + obj);
        }

        private void _PositionOpeningSuccesEvent(Position obj)
        {
            obj.ComissionValue = 0.075m;
            //throw new System.NotImplementedException();
        }

        private void _tabIndex_SpreadChangeEvent(List<Candle> candles)
        {
            if (Regime.ValueString == "Off")
            {
                return;
            }
            if (candles.Count < 0 || _tab1.IsConnected == false
                || _tab2.IsConnected == false)
            {
                return;
            }
            List<Position> positions1 = _tab1.PositionsOpenAll;
            List<Position> positions2 = _tab2.PositionsOpenAll;

            decimal pr1 = _tab1.CandlesAll[_tab1.CandlesAll.Count - 1].Close;
            decimal pr2 = _tab2.CandlesAll[_tab2.CandlesAll.Count - 1].Close;
            decimal profit = 0.000000000m;
            if (positions1.Count == 0 && positions2.Count == 0)
            {
                if (candles[candles.Count - 1].Close > minSpread.ValueDecimal || candles[candles.Count - 1].Close < -minSpread.ValueDecimal)
                {
                    if (pr1 > pr2)
                    {
                        _tab1.SellAtMarket(Volume1.ValueDecimal);
                        _tab2.BuyAtMarket(Volume2.ValueDecimal);
                    }
                    else
                    {
                        _tab1.BuyAtMarket(Volume1.ValueDecimal);
                        _tab2.SellAtMarket(Volume2.ValueDecimal);
                    }
                }
            }
            else
            {
                if (positions1[0].Direction == Side.Buy)
                {
                    if (positions1[0].State != PositionStateType.Open ||
                        positions2[0].State != PositionStateType.Open)
                    {
                        return;
                    }
                    pr1 = _tab1.PriceBestBid;
                    pr2 = _tab2.PriceBestAsk;
                    profit = (pr1 - positions1[0].EntryPrice) / positions1[0].EntryPrice + (positions2[0].EntryPrice - pr2) / positions2[0].EntryPrice;
                    if (profit > minProfit.ValueDecimal/100)
                    {
                        _tab1.CloseAllAtMarket();
                        _tab2.CloseAllAtMarket();
                    }

                }
                if (positions1[0].Direction == Side.Sell)
                {
                    if (positions1[0].State != PositionStateType.Open ||
                        positions2[0].State != PositionStateType.Open)
                    {
                        return;
                    }
                    pr1 = _tab1.PriceBestAsk;
                    pr2 = _tab2.PriceBestBid;
                    profit = (positions1[0].EntryPrice - pr1) / positions1[0].EntryPrice + (pr2 - positions2[0].EntryPrice) / positions2[0].EntryPrice;
                    if ( profit > minProfit.ValueDecimal/100)
                    {
                        _tab1.CloseAllAtMarket();
                        _tab2.CloseAllAtMarket();
                    }
                }
                Console.WriteLine("profit: " + profit);
            }

        }

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
            return "ArbitrageIndex";
        }


        public override void ShowIndividualSettingsDialog()
        {

        }

        /// <summary>
        /// index tab
        /// вкладка для формирования индекса
        /// </summary>
        private BotTabIndex _tabIndex;

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

        public StrategyParameterDecimal minSpread;

        public StrategyParameterDecimal minProfit;

        private MarketDepthSpreadAnaliser Analiser;
        // logic логика


    }
}