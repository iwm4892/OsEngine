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
            
            _CountTrade = CreateParameter("Открыто доборов", 0,0, 5, 1);

            leverage = CreateParameter("Маржинальное плечо", 1m, 1m, 10, 0.1m);
            isContract = CreateParameter("Торгуем контрактами", false);

            ParametrsChangeByUser += ArbitrageIndex_ParametrsChangeByUser;

        }

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
                && - obj > _CountTrade.ValueInt * minSpreadAdd.ValueDecimal
                && _CountTrade.ValueInt <= MaxTrade.ValueInt)
            {
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
                //ClosePositions();
                //Добор позиции
                if(_CountTrade.ValueInt !=0 
                    && obj > _CountTrade.ValueInt* minSpreadAdd.ValueDecimal
                    && _CountTrade.ValueInt <=MaxTrade.ValueInt)
                {
                    UpdatePositions();
                }
            }

        }
        private void OpenPositions()
        {
            if (Regime.ValueString == "OnlyClosePosition")
            {
                return;
            }

            foreach (var t in Analiser._tabs)
            {
                decimal vol = leverage.ValueDecimal * (GetBalance(t._tab)) / GetPrice(t._tab.CandlesAll[t._tab.CandlesAll.Count-1].Close,t._tab) / TabsSimple.Count;
                if (t.side == Side.Buy)
                {
                    t._tab.BuyAtMarket(GetVol(vol,t._tab));
                }
                else
                {
                    t._tab.SellAtMarket(GetVol(vol,t._tab));
                }
            }
            _CountTrade.ValueInt += 1;
        }
        private void UpdatePositions()
        {
            foreach (var t in Analiser._tabs)
            {

                decimal vol = leverage.ValueDecimal * (GetBalance(t._tab)) / GetPrice(t._tab.CandlesAll[t._tab.CandlesAll.Count - 1].Close, t._tab) / TabsSimple.Count;
                if (t.side == Side.Buy)
                {
                    t._tab.BuyAtMarketToPosition(t._tab.PositionsLast,GetVol(vol, t._tab));
                }
                else
                {
                    t._tab.SellAtMarketToPosition(t._tab.PositionsLast,GetVol(vol, t._tab));
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
                    case "LINKUSDT": return Math.Round(v, 2);
                    case "XMRUSDT": return Math.Round(v, 3);
                    case "ATOMUSDT": return Math.Round(v, 2);
                    case "TRXUSDT": return Math.Round(v, 0);
                    case "ADAUSDT": return Math.Round(v, 0);
                    case "BNBUSDT": return Math.Round(v, 2);
                    case "BTCUSDT": return Math.Round(v, 3);
                    case "ETCUSDT": return Math.Round(v, 2);
                    case "BCHUSDT": return Math.Round(v, 3);
                    case "ZECUSDT": return Math.Round(v, 3);
                    case "LTCUSDT": return Math.Round(v, 3);
                    case "XTZUSDT": return Math.Round(v, 1);
                    case "XRPUSDT": return Math.Round(v, 1);
                    case "XLMUSDT": return Math.Round(v, 0);
                    case "ONTUSDT": return Math.Round(v, 1);
                    case "IOTAUSDT": return Math.Round(v, 1);
                    case "BATUSDT": return Math.Round(v, 1);
                    case "VETUSDT": return Math.Round(v, 0);
                    case "NEOUSDT": return Math.Round(v, 2);

                        

                }
                return Math.Round(v, 3);
            }
        }
        private decimal GetBalance(BotTabSimple _tab)
        {
            return 300;
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
