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
    public class FastDelta : BotPanel
    {
        public FastDelta(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];
            _tab_Slow = TabsSimple[1];

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;

            _tab_Slow.CandleFinishedEvent += _tab_Slow_CandleFinishedEvent;

            this.ParametrsChangeByUser += FastDelta_ParametrsChangeByUser;
            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
            TrailStop = CreateParameter("Trail Stop", 0.1m, 0m, 5, 0.1m);
            MinProfitTraling = CreateParameter("Минимальный профит для трэйлинга", 0.2m, 0.2m, 2, 0.1m);
            leverage = CreateParameter("Маржинальное плечо", 0.1m, 0.1m, 10, 0.1m);
            MaxStop = CreateParameter("MaxStop", 1, 1, 10, 0.1m);
            isContract = CreateParameter("Торгуем контрактами", false);
            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });

            TimeFastLenth = CreateParameter("MA Time fast", 10, 5, 200, 5);
            TimeSlowLenth = CreateParameter("MA Time slow", 10, 5, 200, 5);

            VolFastLenth = CreateParameter("MA Vol fast", 10, 5, 200, 5);
            VolSlowLenth = CreateParameter("MA Vol slow", 10, 5, 200, 5);

            TralingMaLenth = CreateParameter("MA Traling", 10, 5, 200, 5);

            maLenth = CreateParameter("Период скользящей по объему", 24, 24, 48, 1);
            DeltaSizeK = CreateParameter("Коэфт для размера дельты", 6, 1, 40, 1);

            TimeMaFast = new MovingAverage(name + "TimeMaFast", false) { ColorBase= System.Drawing.Color.Yellow, TypePointsToSearch = PriceTypePoints.Time};
            TimeMaFast.Lenght = TimeFastLenth.ValueInt;
            TimeMaFast = (MovingAverage)_tab.CreateCandleIndicator(TimeMaFast, "New1");
            TimeMaFast.Save();

            TimeMaSlow = new MovingAverage(name + "TimeMaSlow", false) { ColorBase = System.Drawing.Color.Green,  TypePointsToSearch = PriceTypePoints.Time };
            TimeMaSlow = (MovingAverage)_tab.CreateCandleIndicator(TimeMaSlow, "New1");
            TimeMaSlow.Lenght = TimeSlowLenth.ValueInt;
            TimeMaSlow.Save();

            VolMaFast = new MovingAverage(name + "VolMaFast", false) { ColorBase = System.Drawing.Color.Yellow, Lenght = 10, TypePointsToSearch = PriceTypePoints.Volume, TypeCalculationAverage = MovingAverageTypeCalculation.Exponential };
            VolMaFast = (MovingAverage)_tab.CreateCandleIndicator(VolMaFast, "New2");
            VolMaFast.Lenght = VolFastLenth.ValueInt;
            VolMaFast.Save();

            VolMaSlow = new MovingAverage(name + "VolMaSlow", false) { ColorBase = System.Drawing.Color.Green, TypePointsToSearch = PriceTypePoints.Volume, TypeCalculationAverage = MovingAverageTypeCalculation.Exponential };
            VolMaSlow = (MovingAverage)_tab.CreateCandleIndicator(VolMaSlow, "New2");
            VolMaSlow.Lenght = VolSlowLenth.ValueInt;
            VolMaSlow.Save();

            TralingStopMa = new MovingAverage(name + "TralingStopMa", false) { ColorBase = System.Drawing.Color.Blue, TypePointsToSearch = PriceTypePoints.Close};
            TralingStopMa = (MovingAverage)_tab.CreateCandleIndicator(TralingStopMa, "Prime");
            TralingStopMa.Lenght = TralingMaLenth.ValueInt;
            TralingStopMa.Save();

            maVolume = new MovingAverage(name + "_maVolume", false);
            maVolume = (MovingAverage)_tab_Slow.CreateCandleIndicator(maVolume, "New1");
            maVolume.Lenght = maLenth.ValueInt;
            maVolume.TypeCalculationAverage = MovingAverageTypeCalculation.Exponential;
            maVolume.TypePointsToSearch = PriceTypePoints.Volume;
            maVolume.Save();

            delta = new Delta(name + "delta", false);
            delta = (Delta)_tab.CreateCandleIndicator(delta, "New3");
            delta.Save();

            rsi = new Rsi(name + "Rsi", false);
            rsi = (Rsi)_tab.CreateCandleIndicator(rsi, "New4");
            rsi.Lenght = 21;
            rsi.Save();


            /*
            _envelop = new Envelops(name + "Envelop", false);
            _envelop = (Envelops)_tab.CreateCandleIndicator(_envelop, "Prime");
            _envelop.MovingAverage.Lenght = 21;
            _envelop.Deviation = 0.3m;
            _envelop.Save();
            */
            CanTrade = false;
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
            TimeMaFast.Lenght = TimeFastLenth.ValueInt;
            TimeMaFast.Save();
            TimeMaSlow.Lenght = TimeSlowLenth.ValueInt;
            TimeMaSlow.Save();

            TralingStopMa.Lenght = TralingMaLenth.ValueInt;
            TralingStopMa.Save();

            VolMaFast.Lenght = VolFastLenth.ValueInt;
            VolMaFast.Save();
            VolMaSlow.Lenght = VolSlowLenth.ValueInt;
            VolMaSlow.Save();



        }

        private void _tab_PositionClosingSuccesEvent(Position obj)
        {
            CanTrade = false;
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
        /// Минимальный профит для трэйлинга
        /// </summary>
        private StrategyParameterDecimal MinProfitTraling;

        /// <summary>
        /// slippage
        /// проскальзывание
        /// </summary>
        public StrategyParameterInt Slippage;

        /// <summary>
        /// volume for entry
        /// объём для входа
        /// </summary>
        public StrategyParameterDecimal Volume;

        /// <summary>
        /// Trail stop length in percent
        /// длинна трейлинг стопа в процентах
        /// </summary>
        public StrategyParameterDecimal TrailStop;

        /// <summary>
        /// regime
        /// режим работы
        /// </summary>
        public StrategyParameterString Regime;

        private bool CanTrade;
        private int CandleCount;
        /// <summary>
        /// Период экспоненциальной скользящей средней по объему
        /// </summary>
        public StrategyParameterInt maLenth;
        /// <summary>
        /// коэффицент для расчета размера дельты
        /// </summary>
        private StrategyParameterInt DeltaSizeK;

        /// <summary>
        /// Длина быстрой скользящей по времени
        /// </summary>
        private StrategyParameterInt TimeFastLenth;
        /// <summary>
        /// Длина медленной скользящей по времени
        /// </summary>
        private StrategyParameterInt TimeSlowLenth;
        /// <summary>
        /// Длина быстрой скользящей по объему
        /// </summary>
        private StrategyParameterInt VolFastLenth;
        /// <summary>
        /// Длина медленной скользящей по объему
        /// </summary>
        private StrategyParameterInt VolSlowLenth;

        /// <summary>
        /// Длина скользящей Трэлингстопа
        /// </summary>
        private StrategyParameterInt TralingMaLenth;


        // indicators / индикаторы

        /// <summary>
        /// Средняя по объему (для расчета графика дельты)
        /// </summary>
        private MovingAverage maVolume;

        private MovingAverage TralingStopMa;

        private MovingAverage TimeMaFast;
        private MovingAverage TimeMaSlow;

        private MovingAverage VolMaFast;
        private MovingAverage VolMaSlow;

        private Rsi rsi;
        /// <summary>
        /// Индикатор дельты
        /// </summary>
        private Delta delta;

     //   private Envelops _envelop;

        // trade logic

        private void _tab_PositionOpeningSuccesEvent(Position position)
        {
            CanselOldOrders();
            CanTrade = false;
            decimal activationPrice = GetTrailingStopPrice(position.Direction, position.EntryPrice,true);
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
            if (_tab.CandlesAll.Count + 5 < TralingStopMa.Lenght)
            {
                return false;
            }
            
            if(_tab.CandlesAll.Count + 5 < TimeMaFast.Lenght || _tab.CandlesAll.Count + 5 < TimeMaSlow.Lenght)
            {
                return false;
            }
            if (TimeMaFast.Values[TimeMaFast.Values.Count - 1] > TimeMaSlow.Values[TimeMaSlow.Values.Count - 1])
            {
                return false;
            }
            
            if (_tab.CandlesAll.Count + 5 < VolMaFast.Lenght || _tab.CandlesAll.Count + 5 < VolMaSlow.Lenght)
            {
                return false;
            }
            
            if (VolMaFast.Values[VolMaFast.Values.Count - 1] < VolMaSlow.Values[VolMaSlow.Values.Count - 1])
            {
                return false;
            }
            
            if(_tab.CandlesAll[_tab.CandlesAll.Count - 1].IsUp && _tab.CandlesAll[_tab.CandlesAll.Count - 2].IsDown)
            {
                return false;
            }
            if (_tab.CandlesAll[_tab.CandlesAll.Count - 1].IsDown && _tab.CandlesAll[_tab.CandlesAll.Count - 2].IsUp)
            {
                return false;
            }

            /*
            if (_tab.CandlesAll[_tab.CandlesAll.Count - 1].IsUp && rsi.Values[rsi.Values.Count-1] > 60)
            {
                return false;
            }
            if (_tab.CandlesAll[_tab.CandlesAll.Count - 1].IsDown && rsi.Values[rsi.Values.Count - 1] < 40)
            {
                return false;
            }
            */
            /*
            if(_tab.CandlesAll[_tab.CandlesAll.Count-1].Low > _envelop.ValuesDown[_envelop.ValuesDown.Count - 1]
                &&
                _tab.CandlesAll[_tab.CandlesAll.Count - 1].High < _envelop.ValuesUp[_envelop.ValuesUp.Count - 1])
            {
                CanTrade = true;
            }if(_tab.CandlesAll[_tab.CandlesAll.Count-1]
            return CanTrade;
            */
            /*
            if (_tab.CandlesAll[_tab.CandlesAll.Count - 1].Volume<_tab.CandlesAll[_tab.CandlesAll.Count - 2].Volume + _tab.CandlesAll[_tab.CandlesAll.Count - 3].Volume)
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
            { // trail stop logic
                foreach (Position pos in positions)
                {
                    if (pos.State == PositionStateType.Open)
                    {
                        decimal stop = GetTrailingStopPrice(pos.Direction, pos.EntryPrice, false);

                        _tab.CloseAtServerTrailingStop(pos, stop, stop);
                        //_tab.CloseAtServerTrailingStop(pos, candles[candles.Count-1].Open, candles[candles.Count - 1].Open);
                    }
                }

                /*   
                if(positions[0].State != PositionStateType.Open)
                {
                    return;
                }

                decimal stop = GetTrailingStopPrice(positions[0].Direction, positions[0].EntryPrice, false);
                if (stop == 0)
                {
                    return;
                }
                if (positions[0].EntryPrice == 0)
                {
                   return;
                }
                if (positions[0].Direction == Side.Buy &&
                    (stop < positions[0].EntryPrice ))
                {
                    return;
                }
                if (positions[0].Direction == Side.Sell && 
                    (stop > positions[0].EntryPrice ))
                {
                    return;
                }
                if (positions[0].Direction == Side.Buy || stop > candles[candles.Count - 1].Close)
                {
                    stop = candles[candles.Count - 1].Close - candles[candles.Count - 1].Close * (TrailStop.ValueDecimal / 100);
                }
                if (positions[0].Direction == Side.Sell || stop < candles[candles.Count - 1].Close)
                {
                    stop = candles[candles.Count - 1].Close + candles[candles.Count - 1].Close * (TrailStop.ValueDecimal / 100);
                }
                bool canClose = false;
                decimal _profit = (stop - positions[0].EntryPrice) * 100 / positions[0].EntryPrice;
                if (positions[0].Direction == Side.Sell)
                {
                    _profit = -1 * _profit;
                }
                if (_profit >= MinProfitTraling.ValueDecimal)
                {
                    canClose = true;
                }
                if (canClose)
                {
                    _tab.CloseAtServerTrailingStop(positions[0], stop, stop);
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
            if (signal.Count == 0 || !signal[0].isPattern)
            {
                return;
            }

            decimal _Vol;
            decimal LastStop = 0;
            decimal price = candles[candles.Count - 1].Close;
               
            LastStop = GetTrailingStopPrice(side, price,true);
            if((side==Side.Buy && LastStop > price)||
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
        private decimal GetTrailingStopPrice(Side side ,decimal EntryPrice, bool isNewDeal)
        {
            List<decimal> result = new List<decimal>();
            decimal activationPrice=0;
                if (side == Side.Buy)
                {
                    activationPrice = TralingStopMa.Values[TralingStopMa.Values.Count - 1] - TralingStopMa.Values[TralingStopMa.Values.Count - 1] * (TrailStop.ValueDecimal / 100);
                }
                if (side == Side.Sell)
                {
                    activationPrice = TralingStopMa.Values[TralingStopMa.Values.Count - 1] + TralingStopMa.Values[TralingStopMa.Values.Count - 1] * (TrailStop.ValueDecimal / 100);
                }

            if (isNewDeal)
            {
                return activationPrice;
            }
            result.Add(activationPrice);
         //       result.Add((EntryPrice + _tab.Trades[_tab.Trades.Count - 1].Price) / 2);
                result.Sort((a, b) => decimal.Compare(a, b));
                if (side == Side.Buy)
                {
                    return result[result.Count - 1];
                }
                else
                {
                    return result[0];
                }
        }

        public override string GetNameStrategyType()
        {
            return "FastDelta";
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
