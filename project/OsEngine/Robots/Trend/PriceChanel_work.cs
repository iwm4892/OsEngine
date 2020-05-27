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
using System.Drawing;
using System.Threading;
using OsEngine.Logging;
using OsEngine.Indicators;
using System.Linq;

namespace OsEngine.Robots.Trend
{
    /// <summary>
    ///Breakthrough of the channel built by PriceChannel + -ATR * coefficient,
    /// additional input when the price leaves below the channel line by ATR * coefficient.
    /// Trailing stop on the bottom line of the PriceChannel channel
    /// 
    /// Прорыв канала постоенного по PriceChannel+-ATR*коэффициент ,
    /// дополнительный вход при уходе цены ниже линии канала на ATR*коэффициент.
    /// Трейлинг стоп по нижней линии канала PriceChannel
    /// </summary>
    public class PriceChanel_work : BotPanel
    {
        public PriceChanel_work(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            _pc = new PriceChannel(name + "PriceChannel", false) { LenghtUpLine = 3, LenghtDownLine = 3, ColorUp = Color.DodgerBlue, ColorDown = Color.DarkRed };
            _atr = new Atr(name + "ATR", false) { Lenght = 14, ColorBase = Color.DodgerBlue, };

            _pc.Save();
            _atr.Save();

            _tab.CandleFinishedEvent += Strateg_CandleFinishedEvent;
            _tab.PositionOpeningSuccesEvent += Strateg_PositionOpen;
            _tab.PositionOpeningFailEvent += Strateg_PositionOpeningFailEvent;
            _tab.PositionClosingSuccesEvent += Strateg_PositionClosingSuccesEvent;
            this.ParametrsChangeByUser += PriceChanel_work_ParametrsChangeByUser;
            _tab.BestBidAskChangeEvent += _tab_BestBidAskChangeEvent;

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyClosePosition", "OnlyShort", "OnlyLong" });
            leverage = CreateParameter("Маржинальное плечо", 1m, 1m, 10, 0.1m);
            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });
            isContract = CreateParameter("Торгуем контрактами", false);
            MaxStop = CreateParameter("MaxStop", 1, 1, 10, 0.1m);
            Fractaillenth = CreateParameter("Длина фрактала", 51, 5, 200, 1);

            Slipage = CreateParameter("Slipage", 0m, 0m, 20, 0.1m);

            LengthAtr = CreateParameter("LengthAtr", 14, 14, 200, 1);
            LengthUp = CreateParameter("LengthUp", 14, 14, 200, 1);
            LengthDown = CreateParameter("LengthDown", 14, 14, 200, 1);

            LengthPC = CreateParameter("Длина скользящей для PriceChannel", 14, 14, 200, 1);
            
            LengthAtr.ValueInt = LengthPC.ValueInt;
            LengthUp.ValueInt = LengthPC.ValueInt;
            LengthDown.ValueInt = LengthPC.ValueInt;

            //Slipage = 10;
            //VolumeFix1 = 1;
            //VolumeFix2 = 1;
            //LengthAtr = 14;
            KofAtr = 0.5m;
            //LengthUp = 3;
            //LengthDown = 3;

            DeleteEvent += Strategy_DeleteEvent;

            FastMA = new MovingAverage(name + "FastMA", false) { ColorBase = System.Drawing.Color.Yellow, Lenght = 15, TypePointsToSearch = PriceTypePoints.Close, TypeCalculationAverage = MovingAverageTypeCalculation.Simple };
            FastMA = (MovingAverage)_tab.CreateCandleIndicator(FastMA, "Prime");
            FastMA.Lenght = 15;
            FastMA.Save();

            SlowMA = new MovingAverage(name + "SlowMA", false) { ColorBase = System.Drawing.Color.Blue, Lenght = 30, TypePointsToSearch = PriceTypePoints.Close, TypeCalculationAverage = MovingAverageTypeCalculation.Simple };
            SlowMA = (MovingAverage)_tab.CreateCandleIndicator(SlowMA, "Prime");
            SlowMA.Lenght = 30;
            SlowMA.Save();

            Fractail = IndicatorsFactory.CreateIndicatorByName("Fractail_lenth", name + "Fractail", false);
            Fractail = (Aindicator)_tab.CreateCandleIndicator(Fractail, "Prime");
            Fractail.ParametersDigit[0].Value = Fractaillenth.ValueInt;
            Fractail.Save();

            TrendMA = new MovingAverage(name + "TrendMA", false) { ColorBase = System.Drawing.Color.AntiqueWhite, Lenght = 300, TypePointsToSearch = PriceTypePoints.Close, TypeCalculationAverage = MovingAverageTypeCalculation.Simple };
            TrendMA = (MovingAverage)_tab.CreateCandleIndicator(TrendMA, "Prime");
            TrendMA.Lenght = 300;
            TrendMA.Save();

            Thread closerThread = new Thread(CloseFailPosition);
            closerThread.IsBackground = true;
            closerThread.Start();

        }

        private void _tab_BestBidAskChangeEvent(decimal arg1, decimal arg2)
        {
            if (Regime.ValueString == "Off")
            {
                return;
            }
        }

        private void PriceChanel_work_ParametrsChangeByUser()
        {
            Fractail.ParametersDigit[0].Value = Fractaillenth.ValueInt;
            Fractail.Save();
            Fractail.Reload();

            LengthAtr.ValueInt = LengthPC.ValueInt;
            LengthUp.ValueInt = LengthPC.ValueInt;
            LengthDown.ValueInt = LengthPC.ValueInt;
        }

        private void CloseFailPosition()
        {

            while (true)
            {
                Thread.Sleep(1000);

                if (MainWindow.ProccesIsWorked == false)
                {
                    return;
                }

                if (_tab.PositionsLast != null
                    && _tab.PositionsLast.State == PositionStateType.Closing
                    && _tab.PositionsLast.CloseOrders != null
                    && (_tab.PositionsLast.CloseOrders[_tab.PositionsLast.CloseOrders.Count - 1].State == OrderStateType.Fail
                    || _tab.PositionsLast.CloseOrders[_tab.PositionsLast.CloseOrders.Count - 1].State == OrderStateType.Cancel))
                //&& string.IsNullOrWhiteSpace(_tab.PositionsLast.OpenOrders[0].NumberMarket))
                {
                    _tab.CloseAtMarket(_tab.PositionsLast, _tab.PositionsLast.OpenVolume);

                }
            }

        }

        private void Strateg_PositionClosingSuccesEvent(Position obj)
        {
        }

        private void Strateg_PositionOpeningFailEvent(Position obj)
        {
        }

        /// <summary>
        /// uniq name
        /// взять уникальное имя
        /// </summary>
        public override string GetNameStrategyType()
        {
            return "PriceChanel_work";
        }

        /// <summary>
        /// settings GUI
        /// показать окно настроек
        /// </summary>
        public override void ShowIndividualSettingsDialog()
        {
        }
        /// <summary>
        /// trading tab
        /// вкладка для торговли
        /// </summary>
        private BotTabSimple _tab;
        /// <summary>
        /// Заглушка от повторного открытия
        /// </summary>
        private DateTime _LastCandleTime;
        /// <summary>
        /// Atr period
        /// период ATR
        /// </summary>
        public StrategyParameterInt LengthAtr;

        /// <summary>
        /// PriceChannel up line length
        /// период PriceChannel Up
        /// </summary>
        public StrategyParameterInt LengthUp;

        /// <summary>
        /// PriceChannel down line length
        /// период PriceChannel Down
        /// </summary>
        public StrategyParameterInt LengthDown;
        /// <summary>
        /// PriceChannel up line length
        /// период PriceChannel общий
        /// </summary>
        public StrategyParameterInt LengthPC;

        /// <summary>
        /// PriceChannel
        /// </summary>
        private PriceChannel _pc;

        /// <summary>
        /// ATR
        /// </summary>
        private Atr _atr;

        //settings / настройки публичные

        /// <summary>
        /// slippage
        /// проскальзывание
        /// </summary>
        public StrategyParameterDecimal Slipage;


        /// <summary>
        /// atr coef
        /// коэффициент ATR
        /// </summary>
        public decimal KofAtr;

        /// <summary>
        /// regime
        /// режим работы
        /// </summary>
        public StrategyParameterString Regime;


        /// <summary>
        /// delete save files
        /// удаление файла с сохранением
        /// </summary>
        void Strategy_DeleteEvent()
        {
            if (File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                File.Delete(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt");
            }
        }

        private decimal _lastPcUp;
        private decimal _lastPcDown;
        private decimal _lastAtr;

        private MovingAverage FastMA;
        private MovingAverage SlowMA;
        
        private MovingAverage TrendMA;
        

        private Aindicator Fractail;
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
        /// <summary>
        /// Длина фрактала
        /// </summary>
        public StrategyParameterInt Fractaillenth;
        // logic логика

        /// <summary>
        /// candle finished event
        /// событие завершения свечи
        /// </summary>
        private void Strateg_CandleFinishedEvent(List<Candle> candles)
        {
            if (Regime.ValueString == "Off")
            {
                return;
            }
            _pc.LenghtUpLine = LengthUp.ValueInt;
            _pc.LenghtDownLine = LengthDown.ValueInt;
            _pc.Process(candles);
            _atr.Lenght = LengthAtr.ValueInt;
            _atr.Process(candles);

            if (_pc.ValuesUp == null || _pc.ValuesDown == null || _atr.Values == null)
            {
                return;
            }
            if (GetLastFractail(Fractail.DataSeries.ByName("SeriesUp")) == 0 || GetLastFractail(Fractail.DataSeries.ByName("SeriesDown")) == 0)
            {
                return;
            }
            _lastPcUp = _pc.ValuesUp[_pc.ValuesUp.Count - 1];
            _lastPcDown = _pc.ValuesDown[_pc.ValuesDown.Count - 1];
            _lastAtr = _atr.Values[_atr.Values.Count - 1];

            if (_pc.ValuesUp == null || _pc.ValuesDown == null || _pc.ValuesUp.Count < _pc.LenghtUpLine + 1 ||
                _pc.ValuesDown.Count < _pc.LenghtDownLine + 1 || _atr.Values == null || _atr.Values.Count < _atr.Lenght + 1)
            {
                return;
            }

            List<Position> openPositions = _tab.PositionsOpenAll;
            if (openPositions != null && openPositions.Count != 0)
            {
                LogicClosePosition();
            }

            if (Regime.ValueString == "OnlyClosePosition")
            {
                return;
            }
            if (openPositions == null || openPositions.Count == 0)
            {
                LogicOpenPosition(candles);
            }
        }

        /// <summary>
        /// logic open position
        /// логика открытия первой позиции и дополнительного входа
        /// </summary>
        private void LogicOpenPosition(List<Candle> candles)
        {
            _tab.SellAtStopCancel();
            _tab.BuyAtStopCancel();
            if (_LastCandleTime != candles[candles.Count - 1].TimeStart)
            {
                _LastCandleTime = candles[candles.Count - 1].TimeStart;
            }
            else
            {
                return;
            }
            List<Position> openPositions = _tab.PositionsOpenAll;
            if (openPositions == null || openPositions.Count == 0)
            {
                // long
                if (Regime.ValueString != "OnlyShort")
                {
                    if (FastMA.Values[FastMA.Values.Count - 1] > SlowMA.Values[SlowMA.Values.Count - 1]
                        
                        )
                    {
                        decimal priceEnter = _lastPcUp + (_lastAtr * KofAtr);
                        //   priceEnter = GetLastFractail(Fractail.ValuesUp);
                        //_tab.BuyAtStop(VolumeFix1.ValueDecimal, priceEnter + Slipage.ValueDecimal, priceEnter, StopActivateType.HigherOrEqual);
                        _tab.BuyAtStop(GetVolume(Side.Buy), priceEnter + Slipage.ValueDecimal, priceEnter, StopActivateType.HigherOrEqual);
                    }
                }

                // Short
                if (Regime.ValueString != "OnlyLong")
                {
                    if (FastMA.Values[FastMA.Values.Count - 1] < SlowMA.Values[SlowMA.Values.Count - 1]

                        )
                    {
                        decimal priceEnter = _lastPcDown - (_lastAtr * KofAtr);
                        //   priceEnter = GetLastFractail(Fractail.ValuesDown);
                        //_tab.SellAtStop(VolumeFix1.ValueDecimal, priceEnter - Slipage.ValueDecimal, priceEnter, StopActivateType.LowerOrEqyal);
                        _tab.SellAtStop(GetVolume(Side.Sell), priceEnter - Slipage.ValueDecimal, priceEnter, StopActivateType.LowerOrEqyal);
                    }
                }
                return;
            }

        }

        /// <summary>
        /// logic close position
        /// логика зыкрытия позиции
        /// </summary>
        private void LogicClosePosition()
        {
            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                if (openPositions[i].State != PositionStateType.Open)
                {
                    continue;
                }
                /*
                if (openPositions[i].Direction == Side.Buy)
                {
                    if(FastMA.Values[FastMA.Values.Count - 2] > SlowMA.Values[SlowMA.Values.Count - 2]
                        && FastMA.Values[FastMA.Values.Count - 1] < SlowMA.Values[SlowMA.Values.Count - 1])
                    {
                        _tab.CloseAllAtMarket();
                        return;
                    }
                }
                else
                {
                    if (FastMA.Values[FastMA.Values.Count - 2] < SlowMA.Values[SlowMA.Values.Count - 2]
                        && FastMA.Values[FastMA.Values.Count - 1] > SlowMA.Values[SlowMA.Values.Count - 1])
                    {
                        _tab.CloseAllAtMarket();
                        return;
                    }
                }
                */
                if (openPositions[i].Direction == Side.Buy)
                {
                    /*
                    if((_tab.CandlesAll.Last().Close - openPositions[i].EntryPrice)/ openPositions[i].EntryPrice > 0.005m)
                    {
                        decimal st = openPositions[i].EntryPrice + 3 * openPositions[i].EntryPrice * openPositions[i].ComissionValue / 100;
                        _tab.CloseAtTrailingStop(openPositions[i], st, st - Slipage.ValueDecimal);
                    }
                    */
                    decimal priceClose = _lastPcDown;
                    decimal newfr = GetLastFractail(Fractail.DataSeries.ByName("SeriesDown"));
                    //_tab.CloseAtTrailingStop(openPositions[i], priceClose, priceClose - Slipage.ValueDecimal);
                    _tab.CloseAtTrailingStop(openPositions[i], newfr, newfr - Slipage.ValueDecimal);

                }
                else
                {
                    /*
                    if ((openPositions[i].EntryPrice -_tab.CandlesAll.Last().Close ) / openPositions[i].EntryPrice > 0.005m)
                    {
                        decimal st = openPositions[i].EntryPrice - 3 * openPositions[i].EntryPrice * openPositions[i].ComissionValue / 100;
                        _tab.CloseAtTrailingStop(openPositions[i], st, st - Slipage.ValueDecimal);
                    }
                    */

                    decimal priceClose = _lastPcUp;
                    decimal newfr = GetLastFractail(Fractail.DataSeries.ByName("SeriesUp"));
                    //_tab.CloseAtTrailingStop(openPositions[i], priceClose, priceClose + Slipage.ValueDecimal);
                    _tab.CloseAtTrailingStop(openPositions[i], newfr, newfr + Slipage.ValueDecimal);

                }
            }

        }

        private void Strateg_PositionOpen(Position position)
        {
            _tab.SellAtStopCancel();
            _tab.BuyAtStopCancel();
            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {

                if (openPositions[i].Direction == Side.Buy)
                {
                    _tab.SellAtStopCancel();
                    decimal stop = GetLastFractail(Fractail.DataSeries.ByName("SeriesDown"));
                    _tab.CloseAtStop(openPositions[i], stop, stop - Slipage.ValueDecimal);
                     //   _tab.CloseAtStop(openPositions[i], _tab.CandlesAll[_tab.CandlesAll.Count-1].Low, _tab.CandlesAll[_tab.CandlesAll.Count - 1].Low - Slipage.ValueDecimal);
                 //   _tab.CloseAtProfit(openPositions[i], openPositions[i].EntryPrice + _lastAtr, openPositions[i].EntryPrice + _lastAtr - Slipage.ValueDecimal);
                }
                else
                {
                    _tab.BuyAtStopCancel();
                    decimal stop = GetLastFractail(Fractail.DataSeries.ByName("SeriesUp"));
                    _tab.CloseAtStop(openPositions[i], stop, stop + Slipage.ValueDecimal);
                    //    _tab.CloseAtStop(openPositions[i], _tab.CandlesAll[_tab.CandlesAll.Count - 1].High, _tab.CandlesAll[_tab.CandlesAll.Count - 1].High + Slipage.ValueDecimal);
                 //   _tab.CloseAtProfit(openPositions[i], openPositions[i].EntryPrice - _lastAtr, openPositions[i].EntryPrice - _lastAtr + Slipage.ValueDecimal);
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
                Laststop = GetLastFractail(Fractail.DataSeries.ByName("SeriesDown"));
                priceEnter = _lastPcUp + (_lastAtr * KofAtr);
            }
            else
            {
                Laststop = GetLastFractail(Fractail.DataSeries.ByName("SeriesUp"));
                priceEnter = _lastPcDown - (_lastAtr * KofAtr);
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
            if (_tab.Connector.MyServer.ServerType == ServerType.Tester||
                _tab.Connector.MyServer.ServerType == ServerType.Optimizer)
            {
                return _tab.Portfolio.ValueCurrent;
            }
            if(_tab.Connector.MyServer.ServerType == ServerType.BinanceFutures)
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