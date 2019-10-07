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
    public class MovingChanelFlat : BotPanel
    {
        public MovingChanelFlat(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;

            _movingChanel = new MovingChanel(name + "MovingChanel", false);
            _movingChanel = (MovingChanel)_tab.CreateCandleIndicator(_movingChanel, "Prime");
            _movingChanel.Save();

            this.ParametrsChangeByUser += _ParametrsChangeByUser;
            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
            Volume= CreateParameter("Volume", 0.1m, 0.1m, 50, 0.1m);
            MovingChanelDeviation = CreateParameter("MovingChanel Deviation", 3m, 1m, 300, 10m);
            MovingChanelMovingLength = CreateParameter("MovingChanel Moving Length", 9, 3, 300, 1);

            TrailStop = CreateParameter("Trail Stop", 0.1m, 0m, 5, 0.1m);
            TralingMaLenth = CreateParameter("Размр скользящей трэлинга", 1, 1, 100, 1);
            MinProfitTraling = CreateParameter("Минимальный профит для трэйлинга", 0.2m, 0.2m, 2, 0.1m);

            leverage = CreateParameter("Маржинальное плечо", 0.1m, 0.1m, 10, 0.1m);
            MaxStop = CreateParameter("MaxStop", 0.1m, 0.1m, 10, 0.1m);
            isContract = CreateParameter("Торгуем контрактами", false);
            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });


            _movingChanel.Deviation = MovingChanelDeviation.ValueDecimal;
            _movingChanel.MaLenth = MovingChanelMovingLength.ValueInt;

            TabCreate(BotTabType.Simple);
            _tabFast = TabsSimple[1];
            _tabFast.CandleFinishedEvent += _tabFast_CandleFinishedEvent;

            TralingMA = new MovingAverage(name + "TralingMA", false);
            TralingMA = (MovingAverage)_tabFast.CreateCandleIndicator(TralingMA, "Prime");
            TralingMA.Lenght = TralingMaLenth.ValueInt;
            TralingMA.Save();
            TrendCandleCount = 24;
            CanTrade = false;
        }

        private void _tabFast_CandleFinishedEvent(List<Candle> obj)
        {
            List<Position> positions = _tab.PositionsOpenAll;
             // trail stop logic
                foreach (Position pos in positions)
                {
                    if (pos.State == PositionStateType.Open)
                    {
                        decimal stop = GetTrailingStopPrice(pos.Direction, pos.EntryPrice, false);
                        if (stop != 0)
                        {
                        
                        if (Math.Abs(stop - pos.EntryPrice)/ pos.EntryPrice > MinProfitTraling.ValueDecimal/100)
                        {
                            _tab.CloseAtServerTrailingStop(pos, stop, stop);
                        }
                        }
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

        private void _ParametrsChangeByUser()
        {
            _movingChanel.Deviation = MovingChanelDeviation.ValueDecimal;
            _movingChanel.MaLenth= MovingChanelMovingLength.ValueInt;
            _movingChanel.Save();

            TralingMA.Lenght = TralingMaLenth.ValueInt;
            TralingMA.Save();
        }

        private void _tab_PositionClosingSuccesEvent(Position obj)
        {
            CanTrade = true;
            /*
            List<Position> positions = _tab.PositionsOpenAll;
            bool CanCansel = false;
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
        public StrategyParameterDecimal MovingChanelDeviation;

        /// <summary>
        /// moving average length in Envelop 
        /// длинна скользящей средней в конверте
        /// </summary>
        public StrategyParameterInt MovingChanelMovingLength;

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

        private MovingChanel _movingChanel;

        private int TrendCandleCount;

        private decimal Trend;

        private bool CanTrade;

        private MovingAverage TralingMA;
        
        /// <summary>
        /// Размер скользящей для трэйлинга
        /// </summary>
        public StrategyParameterInt TralingMaLenth;
        // trade logic
        private decimal size;
        private void _tab_PositionOpeningSuccesEvent(Position position)
        {
            CanselOldOrders();
            CanTrade = false;
            decimal activationPrice = GetTrailingStopPrice(position.Direction, position.EntryPrice,true);
            if (position.Direction == Side.Buy)
            {
                
                _tab.CloseAtProfit(position, position.EntryPrice + size*3, position.EntryPrice + size * 3);
            }
            else
            {
                _tab.CloseAtProfit(position, position.EntryPrice - size * 3, position.EntryPrice - size * 3);
            }
            _tab.CloseAtServerTrailingStop(position, activationPrice, activationPrice);
        }
        private bool ValidateParams()
        {

            if (Regime.ValueString == "Off")
            {
                return false;
            }
            if (_tab.CandlesAll.Count + 9 < _movingChanel.MaLenth)
            {
                return false;
            }
            /*
            if (_tab.CandlesAll[_tab.CandlesAll.Count - 1].High > _movingChanel.ValuesUp[_movingChanel.ValuesUp.Count - 1] ||
                _tab.CandlesAll[_tab.CandlesAll.Count - 1].Low < _movingChanel.ValuesDown[_movingChanel.ValuesUp.Count - 1])
            {
                CanTrade = false;
                return CanTrade;
            }
            if (_tab.CandlesAll[_tab.CandlesAll.Count - 1].High < _movingChanel.ValuesUp[_movingChanel.ValuesUp.Count - 1] &&
                _tab.CandlesAll[_tab.CandlesAll.Count - 1].Low > _movingChanel.ValuesDown[_movingChanel.ValuesUp.Count - 1])
            {
                CanTrade = true;
            }
            */
            if(_tab.CandlesAll[_tab.CandlesAll.Count - 1].Close< _movingChanel.ValuesUp[_movingChanel.ValuesUp.Count - 1] &&
                _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close > _movingChanel.ValuesDown[_movingChanel.ValuesUp.Count - 1])
            {
                CanTrade = true;
            }
            /*
            if(_tab.CandlesAll[_tab.CandlesAll.Count-1].Low > _envelop.ValuesDown[_envelop.ValuesDown.Count - 1]
                &&
                _tab.CandlesAll[_tab.CandlesAll.Count - 1].High < _envelop.ValuesUp[_envelop.ValuesUp.Count - 1])
            {
                CanTrade = true;
            }
            return CanTrade;
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
            if (candles.Count > 0)
            {
                Trend = Trend + (candles[candles.Count - 1].Close - candles[candles.Count - 1].Open);
                if (candles.Count > TrendCandleCount)
                {
                    Trend = Trend - (candles[candles.Count - TrendCandleCount].Close - candles[candles.Count - TrendCandleCount].Open);
                }
            }
            //CanselOldOrders();
            if (!ValidateParams())
            {
                return;
            }
            
            List<Position> positions = _tab.PositionsOpenAll;
            
            foreach(Position pos in positions)
            {
                if(pos.State== PositionStateType.Open)
                {
                    CanTrade = false;
                }
            }
            if (CanTrade)
            {
                    OpenPosition(candles);
                    // open logic
            }
            else
            { // trail stop logic
                foreach (Position pos in positions)
                {
                    if (pos.State == PositionStateType.Open)
                    {
                        decimal stop = GetTrailingStopPrice(pos.Direction, pos.EntryPrice, false);

                    //    _tab.CloseAtServerTrailingStop(pos, stop, stop);
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

            decimal _Vol;
            decimal LastStop;
            decimal price = _movingChanel.ValuesDown[_movingChanel.ValuesDown.Count-1];
               
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
            if (_Vol > VollAll)
            {
                _Vol = VollAll;
            }

            _Vol = GetVol(_Vol);

            if (_Vol > 0 && Trend<0)
            {
                _tab.BuyAtStop(_Vol,price,price, StopActivateType.LowerOrEqyal, 1);
            }
            // описание для продажи
            price = _movingChanel.ValuesUp[_movingChanel.ValuesDown.Count - 1];
            LastStop = GetTrailingStopPrice(Side.Sell, price,true);
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

            if (_Vol > VollAll)
            {
                _Vol = VollAll;
            }

            _Vol = GetVol(_Vol);
           
            if (_Vol > 0 && Trend > 0)
            {
                _tab.SellAtStop(_Vol, price, price, StopActivateType.HigherOrEqual, 1);
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
            
            if (isNewDeal)
            {
                size = (_movingChanel.ValuesUp[_movingChanel.ValuesUp.Count - 1] - _movingChanel.ValuesDown[_movingChanel.ValuesDown.Count - 1]);
                if (side == Side.Buy)
                {

                    activationPrice = EntryPrice - size;
                }
                if (side == Side.Sell)
                {
                    activationPrice = EntryPrice + size;
                }
                
            }
            else
            {
                
                if (side == Side.Buy)
                {
                     
                    {
                        activationPrice = _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close - size;//_movingChanel.MovingShadowBody.Values[_movingChanel.MovingShadowBody.Values.Count-1];
                    }
                }
                if (side == Side.Sell)
                {
                    activationPrice = _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close + size;//_movingChanel.MovingShadowBody.Values[_movingChanel.MovingShadowBody.Values.Count - 1];
                }
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
            return "MovingChanelFlat";
        }

        public override void ShowIndividualSettingsDialog()
        {
           
        }

        /// <summary>
        /// tab to trade
        /// вкладка для торговли
        /// </summary>
        private BotTabSimple _tab;
        private BotTabSimple _tabFast;
    }
}
