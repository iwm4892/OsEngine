/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
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

            _atr = new Atr(name + "ATR", false) { Lenght = 14, ColorBase = Color.DodgerBlue, };
            _atr.Save();

            this.ParametrsChangeByUser += EnvelopTrendBitmex_ParametrsChangeByUser;
            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyClosePosition", "OnlyShort", "OnlyLong" });
            Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
            EnvelopDeviation = CreateParameter("Envelop Deviation", 0.3m, 0.3m, 4, 0.1m);
            EnvelopMovingLength = CreateParameter("Envelop Moving Length", 10, 10, 200, 5);
            TrailStop = CreateParameter("Trail Stop", 0.1m, 0m, 5, 0.1m);
            MinProfitTraling = CreateParameter("Минимальный профит для трэйлинга", 0.2m, 0.2m, 2, 0.1m);
            leverage = CreateParameter("Маржинальное плечо", 0.1m, 0.1m, 10, 0.1m);
            MaxStop = CreateParameter("MaxStop", 1, 1, 10, 0.1m);
            isContract = CreateParameter("Торгуем контрактами", false);
            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });
            VolumeDecimals = CreateParameter("Volume Decimals", 0, 0, 20, 1);

            FastMA = new MovingAverage(name + "FastMA", false) { ColorBase = System.Drawing.Color.Yellow, Lenght = 15, TypePointsToSearch = PriceTypePoints.Close, TypeCalculationAverage = MovingAverageTypeCalculation.Simple };
            FastMA = (MovingAverage)_tab.CreateCandleIndicator(FastMA, "Prime");
            FastMA.Lenght = 15;
            FastMA.Save();

            SlowMA = new MovingAverage(name + "SlowMA", false) { ColorBase = System.Drawing.Color.Blue, Lenght = 30, TypePointsToSearch = PriceTypePoints.Close, TypeCalculationAverage = MovingAverageTypeCalculation.Simple };
            SlowMA = (MovingAverage)_tab.CreateCandleIndicator(SlowMA, "Prime");
            SlowMA.Lenght = 30;
            SlowMA.Save();

            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;

        }

        private void EnvelopTrendBitmex_ParametrsChangeByUser()
        {
            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;
            _envelop.Save();
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
        /// Минимальный профит для трэйлинга
        /// </summary>
        private StrategyParameterDecimal MinProfitTraling;

        /// <summary>
        /// slippage
        /// проскальзывание
        /// </summary>
        public StrategyParameterInt Slippage;

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
        private decimal _lastUp;
        private decimal _lastDown;
        private decimal _lastClose;
        private decimal _lastAtr;




        private MovingAverage FastMA;
        private MovingAverage SlowMA;

        /// <summary>
        /// ATR
        /// </summary>
        private Atr _atr;


        /// <summary>
        /// Заглушка от повторного срабатывания
        /// </summary>
        private DateTime _LastCandleTime;
        /// <summary>
        /// Количество знаков после запятой в объеме
        /// </summary>
        public StrategyParameterInt VolumeDecimals;
        // trade logic

        private void _tab_PositionOpeningSuccesEvent(Position position)
        {
            if (_tab.Connector.MyServer.ServerType == ServerType.Optimizer)
            {
                position.ComissionType = ComissionType.Percent;
                position.ComissionValue = 0.1m;
            }
            
            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();
            CanselOldOrders();
            decimal activationPrice = GetTrailingStopPrice(position.Direction, position.EntryPrice,true);
            _tab.CloseAtServerTrailingStop(position, activationPrice, activationPrice);


        }
        private bool ValidateParams()
        {

            if (Regime.ValueString == "Off")
            {
                return false;
            }
            if (_tab.CandlesAll == null || _tab.CandlesAll.Count == 0)
            {
                return false;
            }
            if (_tab.CandlesAll.Count + 5 < _envelop.MovingAverage.Lenght)
            {
                return false;
            }
            if (_lastUp == 0 || _lastDown == 0 || _lastClose == 0)
            {
                return false;
            }

            if (_lastClose < _lastDown || _lastClose > _lastUp)
            {
                return false;
            }
            return true;
        }
        private void CanselOldOrders()
        {
            List<Position> openPositions = _tab.PositionsOpenAll;
            Position[] poses = openPositions.ToArray();
            for (int i = 0; poses != null && i < poses.Length; i++)
            {
                if (poses[i].State == PositionStateType.ClosingFail)
                {
                    poses[i].State = PositionStateType.Open;
                }
                if (poses[i].State == PositionStateType.Opening || poses[i].State == PositionStateType.OpeningFail)
                {
                    _tab.CloseAllOrderToPosition(poses[i]);
                    _tab.GetJournal().DeletePosition(poses[i]);
                }
            }
        }

        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            _tab.BuyAtStopCancel();
            _tab.SellAtStopCancel();
            CanselOldOrders();

            _envelop.Process(candles);
            _atr.Process(candles);
            _lastUp = _envelop.ValuesUp[_envelop.ValuesUp.Count - 1];
            _lastDown = _envelop.ValuesDown[_envelop.ValuesDown.Count - 1];
            _lastClose = candles.Last().Close;
            _lastAtr = _atr.Values[_atr.Values.Count - 1];

            if (_LastCandleTime != candles[candles.Count - 1].TimeStart)
            {
                _LastCandleTime = candles[candles.Count - 1].TimeStart;
            }
            else
            {
                return;
            }
            if (!ValidateParams())
            {
                return;
            }
            
            LogicClosePOsition();
            
            if (Regime.ValueString == "OnlyClosePosition")
            {
                return;
            }

            if (CanTrade())
            {
                LogicOpenPosition(candles);
            }
        }
        private bool CanTrade()
        {
            List<Position> positions = _tab.PositionsOpenAll;
            return (positions.Find(x => x.State == PositionStateType.Open) == null);
        }
        private void LogicClosePOsition()
        {
            foreach (Position pos in _tab.PositionsOpenAll)
            {
                if (pos.State == PositionStateType.Open)
                {
                    decimal stop = GetTrailingStopPrice(pos.Direction, pos.EntryPrice, false);
                    _tab.CloseAtServerTrailingStop(pos, stop, stop);
                }
            }

        }
        private void LogicOpenPosition(List<Candle> candles)
        {
            List<Position> openPositions = _tab.PositionsOpenAll;
            if (openPositions == null || openPositions.Count == 0)
            {
                // long
                if (Regime.ValueString != "OnlyShort")
                {
                    if (FastMA.Values[FastMA.Values.Count - 1] > SlowMA.Values[SlowMA.Values.Count - 1]

                        )
                    {
                        decimal priceEnter = _lastUp;
                        _tab.BuyAtStop(GetVolume(Side.Buy), priceEnter + Slippage.ValueInt, priceEnter, StopActivateType.HigherOrEqual);
                    }
                }

                // Short
                if (Regime.ValueString != "OnlyLong")
                {
                    if (FastMA.Values[FastMA.Values.Count - 1] < SlowMA.Values[SlowMA.Values.Count - 1]

                        )
                    {
                        decimal priceEnter = _lastDown;
                        _tab.SellAtStop(GetVolume(Side.Sell), priceEnter - Slippage.ValueInt, priceEnter, StopActivateType.LowerOrEqyal);
                    }
                }
                return;
            }
        }
        private decimal GetVolume(Side side)
        {
            decimal Laststop = 0;
            decimal priceEnter = 0;
            if (side == Side.Buy)
            {
                priceEnter = _lastUp;
            }
            else
            {
                priceEnter = _lastDown;
            }
            Laststop = GetTrailingStopPrice(side, priceEnter, true);

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
        private decimal GetBalance()
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

        private decimal GetVol(decimal v)
        {
            if (isContract.ValueBool)
            {
                return (int)v;
            }
            else
            {
                CultureInfo culture = new CultureInfo("ru-RU");
                string [] _v = v.ToString(culture).Split(',');
                return (_v[0] + "," + _v[1].Substring(0, VolumeDecimals.ValueInt)).ToDecimal();
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
                activationPrice = _lastUp - _lastUp * (TrailStop.ValueDecimal / 100);

            }
            if (side == Side.Sell)
            {
                activationPrice = _lastDown + _lastDown * (TrailStop.ValueDecimal / 100);
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
