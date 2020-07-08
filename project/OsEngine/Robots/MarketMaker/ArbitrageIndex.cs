/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Indicators;
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
            _tabIndex.SpreadChangeEvent += _tabIndex_SpreadChangeEvent;
            _tabIndex.UserFormula = "A0/A1";

            _envelop = new Envelops(name + "Envelop", false);
            _envelop = (Envelops)_tabIndex.CreateCandleIndicator(_envelop, "Prime");
            _envelop.Save();

            TabCreate(BotTabType.Simple);
            _tab1 = TabsSimple[0];
            TabCreate(BotTabType.Simple);
            _tab2 = TabsSimple[1];

            _tab1.PositionOpeningSuccesEvent += _PositionOpeningSuccesEvent;
            _tab2.PositionOpeningSuccesEvent += _PositionOpeningSuccesEvent;

            
            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyLong", "OnlyShort", "OnlyClosePosition" });
            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });

            minSpread = CreateParameter("minSpread", 0.4m, 0.1m, 3, 0.05m);
            minProfit = CreateParameter("minProfit", 0.3m, 0.1m, 3, 0.05m);

            Slippage = CreateParameter("Slipage", 0, 0, 20, 1);
            ParametrsChangeByUser += ArbitrageIndex_ParametrsChangeByUser;

            EnvelopDeviation = CreateParameter("Envelop Deviation", 0.3m, 5m, 10, 0.3m);
            EnvelopMovingLength = CreateParameter("Envelop Moving Length", 10, 5, 200, 5);

            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;

            VolumeDecimals1 = CreateParameter("Volume1 Decimals", 0, 0, 20, 1);
            VolumeDecimals2 = CreateParameter("Volume2 Decimals", 0, 0, 20, 1);

            MaDay1 = IndicatorsFactory.CreateIndicatorByName("Sma", name + "MaDay1", false);
            MaDay1 = (Aindicator)_tab1.CreateCandleIndicator(MaDay1, "Prime");
            MaDay1.ParametersDigit[0].Value = 24;
            MaDay1.Save();
            
            MaDay2 = IndicatorsFactory.CreateIndicatorByName("Sma", name + "MaDay2", false);
            MaDay2 = (Aindicator)_tab2.CreateCandleIndicator(MaDay2, "Prime");
            MaDay2.ParametersDigit[0].Value = 24;
            MaDay2.Save();


        }
        /// <summary>
        /// Envelop deviation from center moving average 
        /// Envelop отклонение от скользящей средней
        /// </summary>
        public StrategyParameterDecimal EnvelopDeviation;

        /// <summary>
        /// moving average length in Envelop 
        /// длинна скользящей средней в конверте
        /// </summary>
        public StrategyParameterInt EnvelopMovingLength;

        private Envelops _envelop;
        private decimal _lastUp;
        private decimal _lastDown;
        private decimal _lastClose;

        private Aindicator MaDay1;
        private Aindicator MaDay2;
        private decimal _lastMa1;
        private decimal _lastMa2;

        private decimal pr1;
        private decimal pr2;
        private decimal vol1;
        private decimal vol2;

        /// <summary>
        /// Вылюта депозита (первая или вторая валюта валютной пары)
        /// </summary>
        private StrategyParameterString DepoCurrency;

        private void _PositionOpeningSuccesEvent(Position obj)
        {
            //obj.ComissionValue = 0.075m;
            //obj.ComissionType = ComissionType.Percent;
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
            if (_LastCandleTime != candles[candles.Count - 1].TimeStart)
            {
                _LastCandleTime = candles[candles.Count - 1].TimeStart;
            }
            else
            {
                return;
            }

            _envelop.Process(candles);
            _lastUp = _envelop.ValuesUp.Last();
            _lastDown = _envelop.ValuesDown.Last();
            _lastClose = candles.Last().Close;

            _lastMa1 = MaDay1.DataSeries[0].Values.Last();
            _lastMa2 = MaDay2.DataSeries[0].Values.Last();

            if (_lastUp == 0 || _lastDown == 0 || _lastClose == 0 || _lastMa1 == 0 || _lastMa2 == 0)
            {
                return;
            }

            List<Position> positions1 = _tab1.PositionsOpenAll;
            List<Position> positions2 = _tab2.PositionsOpenAll;

            decimal pr1 = _tab1.CandlesAll[_tab1.CandlesAll.Count - 1].Close;
            decimal pr2 = _tab2.CandlesAll[_tab2.CandlesAll.Count - 1].Close;
            decimal vol1 = GetVol(GetBalance(_tab1) / GetPrice(_tab1,pr1),1);
            decimal vol2 = GetVol(GetBalance(_tab2) / GetPrice(_tab2, pr2),2);
            
            if (vol1 == 0 || vol2 == 0) return;

            if (positions1.Count == 0 && positions2.Count == 0)
            {
                LogicOpenPosition();
            }
            else
            {
                LogicClosePosition(candles);
            }
        }
        private void LogicClosePosition(List<Candle> candles)
        {
            decimal _center = (_lastUp + _lastDown) / 2;

            if (
                (_lastClose > _center && candles[candles.Count - 2].Close < _center)
                || (_lastClose < _center && candles[candles.Count - 2].Close > _center)
              )
            {
                _tab1.CloseAllAtMarket();
                _tab2.CloseAllAtMarket();
            }

        }
        private void LogicOpenPosition()
        {
            if(pr1 > _lastMa1 && pr2<_lastMa2 && _lastClose>_lastUp) 
            {
                _tab1.SellAtMarket(vol1);
                _tab2.BuyAtMarket(vol2);
            }
            if (pr1 < _lastMa1 && pr2 > _lastMa2 && _lastClose < _lastDown)
            {
                _tab1.BuyAtMarket(vol1);
                _tab2.SellAtMarket(vol2);
            }

        }

        private decimal GetVol(decimal v,int ind)
        {

            CultureInfo culture = new CultureInfo("ru-RU");
            string[] _v = v.ToString(culture).Split(',');
            if (_v.Count() == 1)
            {
                return v;
            }
            if (ind == 1)
            {
                if (VolumeDecimals1.ValueInt == 0)
                {
                    return (int)v;
                }
                else
                {
                    return (_v[0] + "," + _v[1].Substring(0, Math.Min(VolumeDecimals1.ValueInt, _v[1].Length))).ToDecimal();
                }
            }
            if (ind == 2)
            {
                if (VolumeDecimals2.ValueInt == 0)
                {
                    return (int)v;
                }
                else
                {
                    return (_v[0] + "," + _v[1].Substring(0, Math.Min(VolumeDecimals2.ValueInt, _v[1].Length))).ToDecimal();
                }
            }
            return 0;
        }

        private decimal GetPrice(BotTabSimple _tab, decimal price)
        {
            if (_tab.Connector.MyServer.ServerType == ServerType.BitMex)
            {
                if (_tab.Securiti.Name == "ETHUSD")
                {
                    return price * 0.000001m;
                }
            }

            if (DepoCurrency.ValueString == "Currency2")
            {
                return price;
            }
            else
            {
                return 1 / price;
            }
        }
        private decimal GetBalance(BotTabSimple _tab)
        {
            if (_tab.Connector.MyServer.ServerType == ServerType.Tester ||
                _tab.Connector.MyServer.ServerType == ServerType.Optimizer)
            {
                if (_tab.Portfolio.ValueBlocked != 0)
                {
                    Console.WriteLine("Заблокировано " + _tab.Portfolio.ValueBlocked);
                }
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
            if (_tab.Connector.MyServer.ServerType == ServerType.Binance)
            {
                List<PositionOnBoard> bal = _tab.Portfolio.GetPositionOnBoard();
                if (bal != null && bal.Count > 0)
                {
                    PositionOnBoard b = bal.FindLast(x => x.SecurityNameCode == _tab.Securiti.NameClass);
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
        /// <summary>
        /// user change params
        /// пользователь изменил параметр
        /// </summary>
        void ArbitrageIndex_ParametrsChangeByUser()
        {
            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;
            _envelop.Save();
            _envelop.Reload();
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


        public StrategyParameterDecimal minSpread;

        public StrategyParameterDecimal minProfit;

        /// <summary>
        /// Количество знаков после запятой в объеме
        /// </summary>
        public StrategyParameterInt VolumeDecimals1;
        /// <summary>
        /// Количество знаков после запятой в объеме
        /// </summary>
        public StrategyParameterInt VolumeDecimals2;

        /// <summary>
        /// Заглушка от повторного срабатывания
        /// </summary>
        private DateTime _LastCandleTime;

        // logic логика


    }
}
