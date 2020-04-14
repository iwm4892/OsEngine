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


            Analiser = new MarketDepthSpreadAnaliser();
            Analiser.addTab(_tab1);
            Analiser.addTab(_tab2);
            Analiser.SpreadChangeEvent += Analiser_SpreadChangeEvent;

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyClosePosition" });
            
            minSpread = CreateParameter("minSpread", 0.4m, 0.1m, 3, 0.05m);
            minProfit = CreateParameter("minProfit", 0.3m, 0.1m, 3, 0.05m);

            Slippage = CreateParameter("Slipage", 0, 0, 20, 1);

            leverage = CreateParameter("Маржинальное плечо", 1m, 1m, 10, 0.1m);

            ParametrsChangeByUser += ArbitrageIndex_ParametrsChangeByUser;

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
            if (_tab1.CandlesAll == null || _tab2.CandlesAll == null ||_tab1.CandlesAll.Count == 0 || _tab2.CandlesAll.Count == 0) return;

            decimal CanTrade=0;
            foreach(var el in Analiser.Tabs)
            {
                List<Position> openPositions = el.PositionsOpenAll;
                if(openPositions == null || openPositions.Count == 0)
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
                ClosePositions();
            }

        }
        private void OpenPositions()
        {
            foreach(var t in Analiser._tabs)
            {
                decimal vol = leverage.ValueDecimal * (GetBalance(t._tab)) / GetPrice(t._tab.CandlesAll[t._tab.CandlesAll.Count-1].Close,t._tab);//GetBalance(t._tab)*Volume.ValueInt/100;
                if (t.side == Side.Buy)
                {
                    t._tab.BuyAtMarket(GetVol(vol,t._tab));
                }
                else
                {
                    t._tab.SellAtMarket(GetVol(vol,t._tab));
                }
            }        
        }
        private void ClosePositions()
        {
            decimal openVol = 0;
            decimal profit = 0;
            foreach (var t in Analiser._tabs)
            {
                List<Position> Op = t._tab.PositionsAll;
                foreach(Position pos in Op)
                {
                    profit += pos.ProfitOperationPersent;
                }
            }
            if(profit > minProfit.ValueDecimal / 100)
            {
                foreach (var t in Analiser.Tabs)
                {
                    t.CloseAllAtMarket();
                }
            }

            //Console.WriteLine("profit: " + Math.Round(profit * 100, 2));

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
        /// Плечо
        /// </summary>
        private StrategyParameterDecimal leverage;

        private decimal GetPrice(decimal price,BotTabSimple _tab)
        {
            if (_tab.Connector.MyServer.ServerType == ServerType.BitMex)
            {
                if (_tab.Securiti.Name == "ETHUSD")
                {
                    return price * 0.000001m;
                }
            }

            return price;
        }
        private decimal GetVol(decimal v,BotTabSimple _tab)
        {
            if (isContract.ValueBool)
            {
                return (int)v;
            }
            else
            {
                switch (_tab.Securiti.Name)
                {
                    case "ETHUSDT": return Math.Round(v, 3);
                    case "EOSUSDT": return Math.Round(v, 1);
                }
                return Math.Round(v, 3);
            }
        }
        private decimal GetBalance(BotTabSimple _tab)
        {
            if (_tab.Connector.MyServer.ServerType == ServerType.Tester ||
                _tab.Connector.MyServer.ServerType == ServerType.Optimizer)
            {
                return _tab.Portfolio.ValueCurrent;
            }
            if (_tab.Connector.MyServer.ServerType == ServerType.BinanceFutures)
            {
                List<PositionOnBoard> bal = _tab.Portfolio.GetPositionOnBoard();
                if (bal != null && bal.Count > 0)
                {
                    PositionOnBoard b = bal.FindLast(x => x.SecurityNameCode == "USDT");
                    if (b != null)
                    {
                        return b.ValueCurrent;
                    }
                }
            }
            if (_tab.Connector.MyServer.ServerType == ServerType.BitMex)
            {
                return _tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked;
            }
            return 0;
        }


    }
}
