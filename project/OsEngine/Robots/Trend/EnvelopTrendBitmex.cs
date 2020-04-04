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
    public class EnvelopTrendBitmex : BotPanel
    {
        public EnvelopTrendBitmex(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;
            _envelop = new Envelops(name + "Envelop", false);
            _envelop = (Envelops)_tab.CreateCandleIndicator(_envelop, "Prime");
            _envelop.Save();

            this.ParametrsChangeByUser += EnvelopTrendBitmex_ParametrsChangeByUser;
            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
            Volume= CreateParameter("Volume", 0.1m, 0.1m, 50, 0.1m);
            EnvelopDeviation = CreateParameter("Envelop Deviation", 0.3m, 0.3m, 4, 0.1m);
            EnvelopMovingLength = CreateParameter("Envelop Moving Length", 10, 10, 200, 5);
            TrailStop = CreateParameter("Trail Stop", 0.1m, 0m, 5, 0.1m);
            MinProfitTraling = CreateParameter("Минимальный профит для трэйлинга", 0.2m, 0.2m, 2, 0.1m);
            leverage = CreateParameter("Маржинальное плечо", 0.1m, 0.1m, 10, 0.1m);
            MaxStop = CreateParameter("MaxStop", 1, 1, 10, 0.1m);
            isContract = CreateParameter("Торгуем контрактами", false);
            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });

            VolMaFast = new MovingAverage(name + "VolMaFast", false) { ColorBase = System.Drawing.Color.Yellow, Lenght = 10, TypePointsToSearch = PriceTypePoints.Volume };
            VolMaFast = (MovingAverage)_tab.CreateCandleIndicator(VolMaFast, "New1");
            VolMaFast.Lenght = 40;// (int)(EnvelopMovingLength.ValueInt);
            VolMaFast.Save();

            VolMaSlow = new MovingAverage(name + "VolMaSlow", false) { ColorBase = System.Drawing.Color.Green, TypePointsToSearch = PriceTypePoints.Volume };
            VolMaSlow = (MovingAverage)_tab.CreateCandleIndicator(VolMaSlow, "New1");
            VolMaSlow.Lenght = (int)(VolMaFast.Lenght * 2);
            VolMaSlow.Save();

            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;

            CanTrade = false;
        }

        private void EnvelopTrendBitmex_ParametrsChangeByUser()
        {
            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;
            _envelop.Save();

    //        VolMaFast.Lenght = (int)(EnvelopMovingLength.ValueInt);
    //        VolMaFast.Save();
            VolMaSlow.Lenght = (int)(VolMaFast.Lenght * 2);
            VolMaSlow.Save();

        }

        private void _tab_PositionClosingSuccesEvent(Position obj)
        {
            CanTrade = false;

            List<Position> positions = _tab.PositionsOpenAll;
            bool CanCansel = false;
            /*
            foreach (Position pos in positions)
            {
                if (pos.State == PositionStateType.Open|| pos.State == PositionStateType.ClosingSurplus)
                {
                    _tab.GetJournal().DeletePosition(pos);
                }
            }
            */

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
        /// Envelop deviation from center moving average 
        /// Envelop отклонение от скользящей средней
        /// </summary>
        public StrategyParameterDecimal EnvelopDeviation;

        /// <summary>
        /// moving average length in Envelop 
        /// длинна скользящей средней в конверте
        /// </summary>
        public StrategyParameterInt EnvelopMovingLength;

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


        // indicators / индикаторы

        private Envelops _envelop;

        private int CandleCount;

        private bool CanTrade;

        private MovingAverage VolMaFast;
        private MovingAverage VolMaSlow;
        // trade logic

        private void _tab_PositionOpeningSuccesEvent(Position position)
        {
            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();
            CanTrade = false;
            CanselOldOrders();
            decimal activationPrice = GetTrailingStopPrice(position.Direction, position.EntryPrice,true);
            _tab.CloseAtServerTrailingStop(position, activationPrice, activationPrice);

            /*
            if (position.Direction == Side.Buy)
            {

                decimal activationPrice = _envelop.ValuesUp[_envelop.ValuesUp.Count - 1] -
                    _envelop.ValuesUp[_envelop.ValuesUp.Count - 1] * (TrailStop.ValueDecimal / 100);


                decimal orderPrice = activationPrice - _tab.Securiti.PriceStep * Slippage.ValueInt;

                _tab.CloseAtServerTrailingStop(position,
                    activationPrice, orderPrice);
            }
            if (position.Direction == Side.Sell)
            {

                decimal activationPrice = _envelop.ValuesDown[_envelop.ValuesDown.Count - 1] +
                    _envelop.ValuesDown[_envelop.ValuesDown.Count - 1] * (TrailStop.ValueDecimal / 100);

                decimal orderPrice = activationPrice + _tab.Securiti.PriceStep * Slippage.ValueInt;

                _tab.CloseAtServerTrailingStop(position,
                    activationPrice, orderPrice);
            }
            */

        }
        private bool ValidateParams()
        {

            if (Regime.ValueString == "Off")
            {
                return false;
            }
            if (_tab.CandlesAll.Count + 5 < _envelop.MovingAverage.Lenght)
            {
                return false;
            }
            /*
            if (_tab.CandlesAll.Count + 5 < VolMaFast.Lenght || _tab.CandlesAll.Count + 5 < VolMaSlow.Lenght)
            {
                return false;
            }
            if (VolMaFast.Values[VolMaFast.Values.Count - 1] < VolMaSlow.Values[VolMaSlow.Values.Count - 1])
            {
                return false;
            }
            */
            
            
            if(_tab.CandlesAll[_tab.CandlesAll.Count-1].Close > _envelop.ValuesDown[_envelop.ValuesDown.Count - 1]
                &&
                _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close < _envelop.ValuesUp[_envelop.ValuesUp.Count - 1])
            {
                CanTrade = true;
            }
            return CanTrade;
            
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
            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();

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
                if (_tab.PositionOpenerToStopsAll.Count == 0 || _tab.PositionOpenerToStopsAll == null)
                {
                    OpenPosition(candles);
                    // open logic
                }
            }
            else
            { // trail stop logic
                foreach (Position pos in positions)
                {
                    if (pos.State == PositionStateType.Open)
                    {
                        decimal stop = GetTrailingStopPrice(pos.Direction, pos.EntryPrice, false);

                        _tab.CloseAtServerTrailingStop(pos, stop, stop);
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

            //    System.Threading.Thread.Sleep(500);

        }
        private void OpenPosition(List<Candle> candles)
        {

            if (!ValidateParams())
            {
                return;
            }

            decimal _Vol;
            decimal LastStop = 0;
            decimal price = _envelop.ValuesUp[_envelop.ValuesUp.Count - 1] + Slippage.ValueInt * _tab.Securiti.PriceStep;
               
            LastStop = GetTrailingStopPrice(Side.Buy, price,true);
            if (LastStop == 0 || price==0)
            {
                return;
            }
            decimal VollAll = leverage.ValueDecimal * (_tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked) / GetPrice(price);

            decimal StopSize = Math.Abs((LastStop - price) / price);
            if (StopSize <= 0)
            {
                return;
            }
            _Vol = (MaxStop.ValueDecimal / 100) * VollAll / (StopSize);


            // нужно разбираться почему так происходит
            
            if (_Vol > VollAll)
            {
                _Vol = VollAll;
            }

            _Vol = GetVol(_Vol);

            if (_Vol > 0 && candles[candles.Count-1].High < _envelop.ValuesUp[_envelop.ValuesUp.Count - 2])
            {
                _tab.BuyAtStopMarket(_Vol,
                    _envelop.ValuesUp[_envelop.ValuesUp.Count - 1] +
                    Slippage.ValueInt * _tab.Securiti.PriceStep,
                    _envelop.ValuesUp[_envelop.ValuesUp.Count - 1],
                    StopActivateType.HigherOrEqual, 1);
            }
            // описание для продажи
            price = _envelop.ValuesDown[_envelop.ValuesDown.Count - 1] -Slippage.ValueInt * _tab.Securiti.PriceStep;
            LastStop = GetTrailingStopPrice(Side.Sell, price,true);
            //   LastStop = GetStopByPattern(side, price, Signal);
            if (LastStop == 0 || price == 0)
            {
                return;
            }
            VollAll = leverage.ValueDecimal * (_tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked) / GetPrice(price);

            StopSize = Math.Abs((LastStop - price) / price);
            if (StopSize <= 0)
            {
                return;
            }
            _Vol = (MaxStop.ValueDecimal / 100) * VollAll / (StopSize);


            // нужно разбираться почему так происходит

            if (_Vol > VollAll)
            {
                _Vol = VollAll;
            }

            _Vol = GetVol(_Vol);
           
            if (_Vol > 0 && candles[candles.Count - 1].Low > _envelop.ValuesDown[_envelop.ValuesDown.Count - 2])
            {
                _tab.SellAtStopMarket(_Vol,
                     _envelop.ValuesDown[_envelop.ValuesDown.Count - 1] -
                     Slippage.ValueInt * _tab.Securiti.PriceStep,
                    _envelop.ValuesDown[_envelop.ValuesDown.Count - 1],
                    StopActivateType.LowerOrEqyal, 1);
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
            /*
            if (isNewDeal)
            {
                if (side == Side.Buy)
                {
                    activationPrice = _envelop.ValuesDown[_envelop.ValuesDown.Count - 1] +
                       _envelop.ValuesUp[_envelop.ValuesDown.Count - 1] * (TrailStop.ValueDecimal / 100);

                }
                if (side == Side.Sell)
                {
                    activationPrice = _envelop.ValuesUp[_envelop.ValuesUp.Count - 1] -
                       _envelop.ValuesDown[_envelop.ValuesUp.Count - 1] * (TrailStop.ValueDecimal / 100);
                }
            }
            else
            {
                
                if (side == Side.Buy)
                {
                    activationPrice = _envelop.ValuesUp[_envelop.ValuesUp.Count - 1] - _envelop.ValuesUp[_envelop.ValuesUp.Count - 1] * (TrailStop.ValueDecimal / 100);

                }
                if (side == Side.Sell)
                {
                    activationPrice = _envelop.ValuesDown[_envelop.ValuesDown.Count - 1] + _envelop.ValuesDown[_envelop.ValuesDown.Count - 1] * (TrailStop.ValueDecimal / 100);
                }
            }
            */
            if (side == Side.Buy)
            {
                activationPrice = _envelop.ValuesUp[_envelop.ValuesUp.Count - 1] - _envelop.ValuesUp[_envelop.ValuesUp.Count - 1] * (TrailStop.ValueDecimal / 100);

            }
            if (side == Side.Sell)
            {
                activationPrice = _envelop.ValuesDown[_envelop.ValuesDown.Count - 1] + _envelop.ValuesDown[_envelop.ValuesDown.Count - 1] * (TrailStop.ValueDecimal / 100);
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
            return "EnvelopTrendBitmex";
        }

        public override void ShowIndividualSettingsDialog()
        {
           
        }

        /// <summary>
        /// tab to trade
        /// вкладка для торговли
        /// </summary>
        private BotTabSimple _tab;
    }
}
