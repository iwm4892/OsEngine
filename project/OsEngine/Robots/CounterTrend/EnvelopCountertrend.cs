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
using OsEngine.Indicators;

namespace OsEngine.Robots.Trend
{
    /// <summary>
    /// Trend strategy based on indicator Envelop
    /// Трендовая стратегия на основе индикатора конверт(Envelop)
    /// </summary>
    public class EnvelopCountertrend : BotPanel
    {
        public EnvelopCountertrend(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            this.ParametrsChangeByUser += EnvelopCountertrend_ParametrsChangeByUser;


            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
            Volume= CreateParameter("Volume", 0.1m, 0.1m, 50, 0.1m);
            EnvelopDeviation = CreateParameter("Envelop Deviation", 0.3m, 0.3m, 4, 0.3m);
            EnvelopMovingLength = CreateParameter("Envelop Moving Length", 10, 10, 200, 5);
            
            Fractaillenth = CreateParameter("Длина фрактала", 51, 5, 200, 1);

            leverage = CreateParameter("Маржинальное плечо", 1m, 1m, 10, 0.1m);
            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });
            isContract = CreateParameter("Торгуем контрактами", false);
            MaxStop = CreateParameter("MaxStop", 1, 1, 10, 0.1m);
            SmaLength = CreateParameter("SmaLength", 10, 5, 150, 2);

            Fractail = IndicatorsFactory.CreateIndicatorByName("Fractail_lenth", name + "Fractail", false);
            Fractail = (Aindicator)_tab.CreateCandleIndicator(Fractail, "Prime");
            Fractail.ParametersDigit[0].Value = Fractaillenth.ValueInt;
            Fractail.Save();

            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Moving", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");
            _sma.ParametersDigit[0].Value = SmaLength.ValueInt;
            _sma.Save();

            _envelop = new Envelops(name + "Envelop", false);
            _envelop = (Envelops)_tab.CreateCandleIndicator(_envelop, "Prime");
            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;
            _envelop.Save();
        }

        private void EnvelopCountertrend_ParametrsChangeByUser()
        {
            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;
            _envelop.Save();
            _envelop.Reload();

            _sma.ParametersDigit[0].Value = SmaLength.ValueInt;
            _sma.Save();

        }

        // public settings / настройки публичные

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
        /// regime
        /// режим работы
        /// </summary>
        public StrategyParameterString Regime;

        /// <summary>
        /// Длина фрактала
        /// </summary>
        public StrategyParameterInt Fractaillenth;
        /// <summary>
        /// Заглушка от повторного срабатывания
        /// </summary>
        private DateTime _LastCandleTime;
        /// <summary>
        /// Плечо
        /// </summary>
        private StrategyParameterDecimal leverage;
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

        private decimal _lastUp;
        private decimal _lastDown;
        private decimal _lastMa;

        public StrategyParameterInt SmaLength;
        // indicators / индикаторы

        private Envelops _envelop;

        private Aindicator Fractail;
        private Aindicator _sma;
        // trade logic

        private void _tab_PositionOpeningSuccesEvent(Position position)
        {
            _tab.SellAtStopCancel();
            _tab.BuyAtStopCancel();
            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                decimal stop = GetStop(openPositions[i].Direction);
                _tab.CloseAtStop(openPositions[i], stop, stop);
                _tab.CloseAtProfit(openPositions[i],_lastMa,_lastMa);
                if(openPositions[i].Direction == Side.Buy)
                {
                    _tab.BuyAtLimitToPosition(openPositions[i], openPositions[i].EntryPrice - openPositions[i].EntryPrice * 0.03m, openPositions[i].OpenVolume);
                }
                else
                {
                    _tab.SellAtLimitToPosition(openPositions[i], openPositions[i].EntryPrice + openPositions[i].EntryPrice * 0.03m, openPositions[i].OpenVolume);
                }

                /*
                //decimal st =  GetStop(openPositions[i].Direction);//
                if (openPositions[i].Direction == Side.Buy)
                {
                    decimal stop = GetStop(openPositions[i].Direction);
                    decimal dl = openPositions[i].EntryPrice * 0.03m;//(_lastUp - _lastDown) / 2;//openPositions[i].EntryPrice - stop;
                    _tab.CloseAtStop(openPositions[i], openPositions[i].EntryPrice - dl, openPositions[i].EntryPrice - dl);
                    _tab.CloseAtProfit(openPositions[i], openPositions[i].EntryPrice + dl, openPositions[i].EntryPrice + dl);
                }
                else
                {
                    decimal stop = GetLastFractail(Fractail.DataSeries.ByName("SeriesUp"));
                    decimal dl = openPositions[i].EntryPrice * 0.03m;//(_lastUp - _lastDown) / 2; //stop - openPositions[i].EntryPrice;
                    _tab.CloseAtStop(openPositions[i], openPositions[i].EntryPrice + dl, openPositions[i].EntryPrice + dl); 
                    _tab.CloseAtProfit(openPositions[i], openPositions[i].EntryPrice - dl, openPositions[i].EntryPrice - dl);
                }
                */

            }

        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            if (Regime.ValueString == "Off")
            {
                return;
            }
            _envelop.Process(candles);
            if(candles.Count +5 < _envelop.MovingAverage.Lenght)
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
            if (GetLastFractail(Fractail.DataSeries.ByName("SeriesUp")) == 0 || GetLastFractail(Fractail.DataSeries.ByName("SeriesDown")) == 0)
            {
                return;
            }
            _lastUp = _envelop.ValuesUp[_envelop.ValuesUp.Count - 1];
            _lastDown = _envelop.ValuesDown[_envelop.ValuesDown.Count - 1];
            _lastMa = _sma.DataSeries[0].Values[_sma.DataSeries[0].Values.Count - 1];
            if (_lastUp==0|| _lastDown == 0)
            {
                return;
            }
            _tab.SellAtStopCancel();
            _tab.BuyAtStopCancel();

            List<Position> positions = _tab.PositionsOpenAll;

            if(positions.Count != 0)
            {
                LogicClosePosition();
            }
            if (Regime.ValueString == "OnlyClosePosition")
            {
                return;
            }

            if (positions == null || positions.Count == 0)
            {
                LogicOpenPosition(candles);
            }
        }
        private void LogicClosePosition()
        {

            if (_tab.PositionsLast != null && _tab.PositionsLast.State != PositionStateType.Done)
            {
                _tab.CloseAtProfit(_tab.PositionsLast, _lastMa, _lastMa);
            }

            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
               // _tab.CloseAtProfit(openPositions[i], _lastMa, _lastMa);
                /*
                if (openPositions[i].Direction == Side.Buy)
                {
                    _tab.CloseAtTrailingStop(openPositions[i], lastclose - lastclose , lastclose - lastclose );
                    //_tab.CloseAtProfit(openPositions[i], _lastUp, _lastUp);
                }
                else
                {
                    _tab.CloseAtTrailingStop(openPositions[i], lastclose + lastclose , lastclose + lastclose );
                    //_tab.CloseAtProfit(openPositions[i], _lastDown, _lastDown);
                }
                */


            }
            
        }
        private void LogicOpenPosition(List<Candle> candles)
        {
           
            if(_lastMa<_lastDown || _lastMa > _lastUp)
            {
                return;
            }
            List<Position> openPositions = _tab.PositionsOpenAll;
            if (openPositions == null || openPositions.Count == 0)
            {
                // long
                if (Regime.ValueString != "OnlyShort")
                {
                     _tab.BuyAtStop(GetVolume(Side.Buy), _lastDown, _lastDown, StopActivateType.LowerOrEqyal);
                }

                // Short
                if (Regime.ValueString != "OnlyLong")
                {
                    _tab.SellAtStop(GetVolume(Side.Sell), _lastUp, _lastUp, StopActivateType.HigherOrEqual);
                }
            }


        }
        private decimal GetLastFractail(List<decimal> values)
        {
            return values.FindLast(x => x != 0);
        }
        private decimal GetVolume(Side side)
        {
            decimal Laststop = 0;
            decimal priceEnter = 0;
            if (side == Side.Buy)
            {
                Laststop = GetStop(side);//GetLastFractail(Fractail.DataSeries.ByName("SeriesDown"));
                priceEnter = _lastDown;
            }
            else
            {
                Laststop = GetStop(side);////GetLastFractail(Fractail.DataSeries.ByName("SeriesUp"));
                priceEnter = _lastUp;
            }
            if (GetPrice(priceEnter)==0)
            {
                Console.WriteLine("Ошибка");
            }
            decimal VollAll = leverage.ValueDecimal * (GetBalance()) / GetPrice(priceEnter);

            decimal StopSize = Math.Abs((Laststop - priceEnter) / priceEnter);

            Math.Abs((Laststop - priceEnter) / priceEnter);
            if (StopSize <= 0)
            {
                return 0;
            }
            decimal _Vol = (MaxStop.ValueDecimal / 100) * VollAll / (StopSize);
            if (_Vol > VollAll)
            {
                _Vol = VollAll;
            }

            _Vol = GetVol(_Vol);

            return _Vol;
        }
        private decimal GetStop(Side side)
        {
            if(side == Side.Buy)
            {
                return _lastDown - _lastDown * 0.2m; 
            }
            else
            {
                return _lastUp + _lastUp * 0.2m;
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
        private decimal GetVol(decimal v)
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
        private decimal GetBalance()
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

        public override string GetNameStrategyType()
        {
            return "EnvelopCountertrend";
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
