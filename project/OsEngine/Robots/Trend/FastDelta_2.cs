/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.IO;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.Trend
{
    /// <summary>
    /// Trend strategy based on indicator Envelop
    /// Трендовая стратегия на основе индикатора конверт(Envelop)
    /// </summary>
    public class FastDelta_2 : BotPanel
    {
        public FastDelta_2(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];
            _tab_Slow = TabsSimple[1];

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;

            _tab_Slow.CandleFinishedEvent += _tab_Slow_CandleFinishedEvent1;

            _tab_Slow.CandleFinishedEvent += _tab_Slow_CandleFinishedEvent;

            this.ParametrsChangeByUser += FastDelta_ParametrsChangeByUser;
            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            leverage = CreateParameter("Маржинальное плечо", 0.1m, 0.1m, 10, 0.1m);
            MaxStop = CreateParameter("MaxStop", 1, 1, 10, 0.1m);
            isContract = CreateParameter("Торгуем контрактами", false);
            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });


            maLenth = CreateParameter("Период скользящей по объему", 24, 24, 48, 1);
            DeltaSizeK = CreateParameter("Коэфт для размера дельты", 6, 1, 40, 1);



            maVolume = new MovingAverage(name + "_maVolume", false);
            maVolume = (MovingAverage)_tab_Slow.CreateCandleIndicator(maVolume, "New1");
            maVolume.Lenght = maLenth.ValueInt;
            maVolume.TypeCalculationAverage = MovingAverageTypeCalculation.Exponential;
            maVolume.TypePointsToSearch = PriceTypePoints.Volume;
            maVolume.Save();

            delta = new Delta(name + "delta", false);
            delta = (Delta)_tab.CreateCandleIndicator(delta, "New3");
            delta.Save();

        }

        private void _tab_Slow_CandleFinishedEvent1(List<Candle> candles)
        {
            List<Position> positions = _tab.PositionsOpenAll;
            foreach (Position pos in positions)
            {
                if (pos.State == PositionStateType.Open)
                {
                    bool closePos = false;
                    if (pos.Direction == Side.Buy && candles[candles.Count - 1].ClasterData.MaxData.Price < candles[candles.Count - 2].ClasterData.MaxData.Price && candles[candles.Count - 1].IsDown)
                    {
                        closePos = true;
                    }
                    if (pos.Direction == Side.Sell && candles[candles.Count - 1].ClasterData.MaxData.Price > candles[candles.Count - 2].ClasterData.MaxData.Price && candles[candles.Count - 1].IsUp)
                    {
                        closePos = true;
                    }
                    if (closePos)
                    {
                        _tab.CloseAtMarket(pos, pos.OpenVolume);
                    }
                }
            }

        }

        private void _tab_Slow_CandleFinishedEvent(List<Candle> obj)
        {
            DeltaStepCheck();
        }
        private void DeltaStepCheck()
        {
            if (maVolume.Values.Count == 0 || maVolume.Values[maVolume.Values.Count - 1] == 0)
            {
                _tab.Connector.TimeFrameBuilder.DeltaPeriods = (int)_tab_Slow.CandlesAll[_tab_Slow.CandlesAll.Count - 1].Volume / DeltaSizeK.ValueInt;
                return;
            }
            if (_tab.Connector.TimeFrameBuilder.DeltaPeriods != (int)maVolume.Values[maVolume.Values.Count - 1] / DeltaSizeK.ValueInt)
            {
                _tab.Connector.TimeFrameBuilder.DeltaPeriods = (int)maVolume.Values[maVolume.Values.Count - 1] / DeltaSizeK.ValueInt;
            }
        }

        private void FastDelta_ParametrsChangeByUser()
        {
        }

        private void _tab_PositionClosingSuccesEvent(Position obj)
        {

        }

        // public settings / настройки публичные

        /// <summary>
        /// Вылюта депозита (первая или вторая валюта валютной пары)
        /// </summary>
        private StrategyParameterString DepoCurrency;
        /// <summary>
        /// торгуем контрактами
        /// </summary>
        private StrategyParameterBool isContract;
        /// <summary>
        /// Максимальный размер стопа (% от депозита)
        /// </summary>
        private StrategyParameterDecimal MaxStop;
        /// <summary>
        /// Плечо
        /// </summary>
        private StrategyParameterDecimal leverage;


        /// <summary>
        /// regime
        /// режим работы
        /// </summary>
        public StrategyParameterString Regime;

        /// <summary>
        /// Период экспоненциальной скользящей средней по объему
        /// </summary>
        public StrategyParameterInt maLenth;
        /// <summary>
        /// коэффицент для расчета размера дельты
        /// </summary>
        private StrategyParameterInt DeltaSizeK;

        private decimal LastStop;

        /// <summary>
        /// Цена закрытия прошлого дня
        /// </summary>
        public decimal LastDayPrice;

        /// <summary>
        /// Направление торговли
        /// </summary>
        private Side TradeSide;
        // indicators / индикаторы

        /// <summary>
        /// Средняя по объему (для расчета графика дельты)
        /// </summary>
        private MovingAverage maVolume;

        /// <summary>
        /// Индикатор дельты
        /// </summary>
        private Delta delta;

        // trade logic

        private void _tab_PositionOpeningSuccesEvent(Position position)
        {

            decimal activationPrice = LastStop;
            _tab.CloseAtServerTrailingStop(position, activationPrice, activationPrice);
        }
        private bool ValidateParams()
        {

            if (Regime.ValueString == "Off")
            {
                return false;
            }
            if (_tab_Slow.CandlesAll.Count+5 < maVolume.Lenght)
            {
                return false;
            }
            /* 
            if(_tab.CandlesAll[_tab.CandlesAll.Count - 1].IsUp && _tab.CandlesAll[_tab.CandlesAll.Count - 2].IsDown)
            {
                return false;
            }
            if (_tab.CandlesAll[_tab.CandlesAll.Count - 1].IsDown && _tab.CandlesAll[_tab.CandlesAll.Count - 2].IsUp)
            {
                return false;
            }
            */
            return true;
        }
        private void CanselOldOrders()
        {
            List<Position> positions = _tab.PositionsOpenAll;
            bool CanCansel = false;
            foreach (Position pos in positions)
            {
                if (pos.State == PositionStateType.Opening)
                {
                    CanCansel = true;
                }
            }
            if (CanCansel)
            {
                _tab.CloseAllOrderInSystem();

            }
        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            CanselOldOrders();

            if (candles[candles.Count - 1].TimeStart.Day != candles[candles.Count - 2].TimeStart.Day)
            {
                LastDayPrice = candles[candles.Count - 2].Close;
            }
            if (candles[candles.Count - 1].Close > LastDayPrice)
            {
                TradeSide = Side.Buy;
            }
            else
            {
                TradeSide = Side.Sell;
            }

            List<Position> positions = _tab.PositionsOpenAll;
            bool canOpen = true;
            foreach(Position pos in positions)
            {
                if(pos.State== PositionStateType.Open)
                {
                    canOpen = false;
                }
            }
            if (canOpen)
            {
                // open logic
                OpenPosition(candles);
            }
            else
            { 
                /*
                // stop logic
                foreach (Position pos in positions)
                {
                    if (pos.State == PositionStateType.Open)
                    {
                        bool closePos = false;
                        if(pos.Direction == Side.Buy && candles[candles.Count-1].ClasterData.MaxData.Price < candles[candles.Count - 2].ClasterData.MaxData.Price && candles[candles.Count - 1].IsDown)
                        {
                            closePos = true;
                        }
                        if (pos.Direction == Side.Sell && candles[candles.Count - 1].ClasterData.MaxData.Price > candles[candles.Count - 2].ClasterData.MaxData.Price && candles[candles.Count - 1].IsUp)
                        {
                            closePos = true;
                        }
                        if (closePos)
                        {
                            _tab.CloseAtMarket(pos, pos.OpenVolume);
                        }
                    }
                }
                */

            }

          //      System.Threading.Thread.Sleep(500);

        }
        private void OpenPosition(List<Candle> candles)
        {

            if (!ValidateParams())
            {
                return;
            }
            Side side = Side.Buy;
            if (candles[candles.Count - 1].IsDown)
            {
                side = Side.Sell;

            }
            List<IIndicatorCandle> indicators = new List<IIndicatorCandle>();
            indicators.Add(delta);
            List<string> patterns = new List<string>();
            patterns.Add("Signal_pattern"); //Сигналка

            List<Pattern> signal = Pattern.GetValidatePatterns(candles, indicators, patterns);
            if (signal.Count == 0 || !signal[0].isPattern || signal[0].Side != TradeSide)
            {
                return;
            }

            decimal _Vol;
           
            decimal price = candles[candles.Count - 1].Close;

            LastStop = _tab_Slow.CandlesAll[_tab_Slow.CandlesAll.Count-1].Open;
            if ((side==Side.Buy && LastStop > price)||
                (side == Side.Sell && LastStop < price))
            {
                return;
            }
            if (LastStop == 0 || price==0)
            {
                return;
            }
            decimal VollAll = leverage.ValueDecimal * (_tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked) / GetPrice(price);

            decimal StopSize = Math.Abs((LastStop - price) / price);

            Math.Abs((LastStop - price) / price);
            if (StopSize <= 0)
            {
                return;
            }
            _Vol = (MaxStop.ValueDecimal / 100) * VollAll / (StopSize);
            if (_Vol > VollAll)
            {
                _Vol = VollAll;
            }

            _Vol = GetVol(_Vol);

            if (_Vol > 0)
            {
                if (side == Side.Buy)
                {
                    _tab.BuyAtMarket(_Vol);
                }
                else
                {
                    _tab.SellAtMarket(_Vol);
                }
            }
        }
        private decimal GetVol(decimal v)
        {
            if (isContract.ValueBool)
            {
                return (int)v;
            }
            else
            {
                return v;
            }
        }
        private decimal GetPrice(decimal price)
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

        public override string GetNameStrategyType()
        {
            return "FastDelta_2";
        }

        public override void ShowIndividualSettingsDialog()
        {
           
        }

        /// <summary>
        /// tab to trade
        /// вкладка для торговли
        /// </summary>
        private BotTabSimple _tab;
        private BotTabSimple _tab_Slow;
    }
}
