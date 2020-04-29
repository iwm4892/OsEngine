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
    public class TrendLine : BotPanel
    {
        public TrendLine(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

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

            DeleteEvent += Strategy_DeleteEvent;

            _trendLine = IndicatorsFactory.CreateIndicatorByName("TrendLine", name + "TrendLine", false);
            _trendLine = (Aindicator)_tab.CreateCandleIndicator(_trendLine, "Prime");
            _trendLine.ParametersDigit[0].Value = Fractaillenth.ValueInt;
            _trendLine.Save();

            _Ssma = IndicatorsFactory.CreateIndicatorByName("Ssma", name + "_Ssma", false);
            _Ssma = (Aindicator)_tab.CreateCandleIndicator(_Ssma, "Prime");
            _Ssma.ParametersDigit[0].Value = Fractaillenth.ValueInt;
            _Ssma.Save();

            _FastMa = IndicatorsFactory.CreateIndicatorByName("Ssma", name + "_FastMa", false);
            _FastMa = (Aindicator)_tab.CreateCandleIndicator(_FastMa, "Prime");
            _FastMa.ParametersDigit[0].Value = 15;
            _FastMa.Save();

            _SlowMa = IndicatorsFactory.CreateIndicatorByName("Ssma", name + "_SlowMa", false);
            _SlowMa = (Aindicator)_tab.CreateCandleIndicator(_SlowMa, "Prime");
            _SlowMa.ParametersDigit[0].Value = 30;
            _SlowMa.Save();

            Fractail = _trendLine.IncludeIndicators.FindLast(x => x.Name == name +"TrendLineFractail");

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
            decimal bal = GetBalance();
        }

        private void PriceChanel_work_ParametrsChangeByUser()
        {
            Fractail.ParametersDigit[0].Value = Fractaillenth.ValueInt;
            Fractail.Save();
            Fractail.Reload();

            _trendLine.ParametersDigit[0].Value = Fractaillenth.ValueInt;
            _trendLine.Save();
            _trendLine.Reload();
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
            return "TrendLine";
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


        //settings / настройки публичные

        /// <summary>
        /// slippage
        /// проскальзывание
        /// </summary>
        public StrategyParameterDecimal Slipage;


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
        private decimal _lastTrendUp;
        private decimal _lastTrendDown;


        private Aindicator Fractail;
        private Aindicator _trendLine;
        private Aindicator _Ssma;
        private Aindicator _FastMa;
        private Aindicator _SlowMa;
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
        private decimal _lastfastma;
        private decimal _lastslowma;
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
            _trendLine.Process(candles);
            _lastPcUp = GetLastFractail(Fractail.DataSeries.ByName("SeriesUp"));
            _lastPcDown = GetLastFractail(Fractail.DataSeries.ByName("SeriesDown"));
            _lastTrendUp = _trendLine.DataSeries.ByName("SeriesUp")[_trendLine.DataSeries.ByName("SeriesUp").Count - 1];
            _lastTrendDown = _trendLine.DataSeries.ByName("SeriesDown")[_trendLine.DataSeries.ByName("SeriesDown").Count - 1];
            _lastfastma = _FastMa.DataSeries.ByName("Ma")[_FastMa.DataSeries.ByName("Ma").Count - 1];
            _lastslowma = _SlowMa.DataSeries.ByName("Ma")[_FastMa.DataSeries.ByName("Ma").Count - 1];

            if (_lastPcUp == 0 || _lastPcDown == 0 )
            {
                return;
            }

            List<Position> openPositions = _tab.PositionsOpenAll;
            if (openPositions != null && openPositions.Count != 0)
            {
                LogicClosePosition();
            }

            if (_lastTrendUp == 0 || _lastTrendDown == 0)
            {
                return;
            }
            /*
            if(candles[candles.Count-1].High > _lastTrendUp
                || candles[candles.Count - 1].Low < _lastTrendDown)
            {
                return;
            }
            */

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
                if (Regime.ValueString != "OnlyShort" && _lastTrendDown !=0 && _lastfastma >_lastslowma)
                {
                        decimal priceEnter = _lastTrendDown;
                        _tab.BuyAtStop(GetVolume(Side.Buy), priceEnter + Slipage.ValueDecimal, priceEnter, StopActivateType.LowerOrEqyal,1);
                }

                // Short
                if (Regime.ValueString != "OnlyLong" && _lastTrendUp !=0 && _lastfastma < _lastslowma)
                {
                        decimal priceEnter = _lastTrendUp;
                        _tab.SellAtStop(GetVolume(Side.Sell), priceEnter - Slipage.ValueDecimal, priceEnter, StopActivateType.HigherOrEqual,1);
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
                List<decimal> ma = _Ssma.DataSeries.ByName("Ma");
                decimal lastma = ma[ma.Count - 1];
                if (openPositions[i].Direction == Side.Buy)
                {
                    decimal newfr = GetLastFractail(Fractail.DataSeries.ByName("SeriesDown"));
                    _tab.CloseAtTrailingStop(openPositions[i], newfr, newfr - Slipage.ValueDecimal);
                    _tab.CloseAtProfit(openPositions[i], lastma + Slipage.ValueDecimal, lastma);
                }
                else
                {

                    decimal newfr = GetLastFractail(Fractail.DataSeries.ByName("SeriesUp"));
                    _tab.CloseAtTrailingStop(openPositions[i], newfr, newfr + Slipage.ValueDecimal);
                    _tab.CloseAtProfit(openPositions[i], lastma - Slipage.ValueDecimal, lastma);

                }
            }

        }

        private void Strateg_PositionOpen(Position position)
        {
            _tab.SellAtStopCancel();
            _tab.BuyAtStopCancel();
            decimal profit = Math.Abs((_lastPcUp - _lastPcDown)) * 1;

            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {

                if (openPositions[i].Direction == Side.Buy)
                {
                    _tab.SellAtStopCancel();
                    _tab.CloseAtStop(openPositions[i], _lastPcDown, _lastPcDown - Slipage.ValueDecimal);
                    _tab.CloseAtProfit(openPositions[i],openPositions[i].EntryPrice+profit + Slipage.ValueDecimal, openPositions[i].EntryPrice + profit);
                }
                else
                {
                    _tab.BuyAtStopCancel();
                    _tab.CloseAtStop(openPositions[i], _lastPcUp, _lastPcUp + Slipage.ValueDecimal);
                    _tab.CloseAtProfit(openPositions[i], openPositions[i].EntryPrice - profit - Slipage.ValueDecimal, openPositions[i].EntryPrice - profit);
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
                priceEnter = _lastTrendDown;
            }
            else
            {
                Laststop = GetLastFractail(Fractail.DataSeries.ByName("SeriesUp"));
                priceEnter = _lastTrendUp;
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