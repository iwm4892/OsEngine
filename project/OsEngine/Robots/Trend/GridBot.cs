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
    /// Трендовая стратегия на основе сетки ордеров
    /// </summary>
    public class GridBot : BotPanel
    {
        public GridBot(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;


            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
            Volume= CreateParameter("Volume", 0.1m, 0.1m, 50, 0.1m);

            MaxStop = CreateParameter("Просадка за стоп", 1, 1, 10, 0.1m);

            PriceLevleLine = new PriceLevleLine(name + "_PriceLevleLine", false);
            PriceLevleLine = (PriceLevleLine)_tab.CreateCandleIndicator(PriceLevleLine, "Prime");
            PriceLevleLine.PaintOn = false;
            PriceLevleLine.Save();

            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });
            isContract = CreateParameter("Торгуем контрактами", false);
        }

        // public settings / настройки публичные

        /// <summary>
        /// slippage
        /// проскальзывание
        /// </summary>
        public StrategyParameterInt Slippage;
        
        /// <summary>
        /// Количество ордеров на открытие
        /// </summary>
        public StrategyParameterInt CountProitOrders;
        /// <summary>
        /// Количество открывающих ордеров
        /// </summary>
        public StrategyParameterInt CountOpenOrders;
        /// <summary>
        /// volume for entry
        /// объём для входа
        /// </summary>
        public StrategyParameterDecimal Volume;

        /// <summary>
        /// regime
        /// режим работы
        /// </summary>
        public StrategyParameterString Regime;
        
        /// <summary>
        /// Размер стопа
        /// </summary>
        public StrategyParameterInt StopSize;
        /// <summary>
        /// Цена закрытия прошлого дня
        /// </summary>
        public decimal LastDayPrice;

        /// <summary>
        /// Направление торговли
        /// </summary>
        private Side TradeSide;

        /// <summary>
        /// Максимальный размер стопа (% от депозита)
        /// </summary>
        private StrategyParameterDecimal MaxStop;
        /// <summary>
        /// Вылюта депозита (первая или вторая валюта валютной пары)
        /// </summary>
        private StrategyParameterString DepoCurrency;
        /// <summary>
        /// торгуем контрактами
        /// </summary>
        private StrategyParameterBool isContract;

        // indicators / индикаторы

        /// <summary>
        /// Индикатор уровней
        /// </summary>
        private PriceLevleLine PriceLevleLine;

        // trade logic

        private void _tab_PositionOpeningSuccesEvent(Position position)
        {

        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (Regime.ValueString != "On")
            {
                return;
            }
            if(candles[candles.Count-1].TimeStart.Day != candles[candles.Count - 2].TimeStart.Day)
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

            if(_tab.PositionsLast == null || _tab.PositionsLast.State == PositionStateType.Done)
            {
                logicOpenPosition();
                return;
            }
            if(_tab.PositionsLast.State != PositionStateType.Done && _tab.PositionsLast.Direction == TradeSide && !PositionIsOpen(_tab.PositionsLast))
            {
                UpdatePosition(_tab.PositionsLast);
                return;
            }

            if (_tab.PositionsLast.State != PositionStateType.Done && _tab.PositionsLast.Direction != TradeSide && !PositionIsOpen(_tab.PositionsLast))
            {
                _tab.CloseAllOrderToPosition(_tab.PositionsLast);
                logicOpenPosition();
                return;
            }
            if (PositionIsOpen(_tab.PositionsLast))
            {
                ClosePosition(_tab.PositionsLast);
            }

        }
        private bool PositionIsOpen(Position position)
        {
            bool result = false;
            foreach(Order ord in position.OpenOrders)
            {
                if(ord.State == OrderStateType.Done || ord.State == OrderStateType.Patrial)
                {
                    result = true;
                    return result;
                }
            }
            return result;
        }
        private void logicOpenPosition()
        {
            decimal StartPrice = GetStartPrice();
            decimal MaxVol = GetMaxVol(StartPrice);
        }
        private void ClosePosition(Position position)
        {

        }
        private decimal GetStartPrice()
        {
            decimal result=0;

            return result;
        }
        private decimal  GetMaxVol(decimal price)
        {
            decimal _Vol;
            decimal _stop;
            if (TradeSide == Side.Buy)
            {
                _stop = price - price * StopSize.ValueInt / 100;
            }
            else
            {
                _stop = price + price * StopSize.ValueInt / 100;
            }

            decimal VollAll =  (_tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked) / GetPrice(price);
            
            decimal StopSize_ = Math.Abs((_stop - price) / price);

            _Vol = (MaxStop.ValueDecimal / 100) * VollAll / (StopSize_);

            if (_Vol > VollAll)
            {
                _Vol = VollAll;
            }
            
            _Vol = GetVol(_Vol);
            return _Vol;

        }
        private void UpdatePosition(Position position)
        {

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
            return "GridBot";
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
