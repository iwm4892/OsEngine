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
    public class FractialMA_work : BotPanel
    {
        public FractialMA_work(string name, StartProgram startProgram)
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
            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyClosePosition", "OnlyShort", "OnlyLong" });

            Slipage = CreateParameter("Slipage", 0m, 0m, 20, 0.1m);
            VolumeFix1 = CreateParameter("VolumeFix1", 0m, 0.01m, 20, 0.01m);
            VolumeFix2 = CreateParameter("VolumeFix2", 0m, 0.01m, 20, 0.01m);

            LengthAtr = CreateParameter("LengthAtr", 14, 0, 200, 1);
            LengthUp = CreateParameter("LengthUp", 3, 0, 200, 1);
            LengthDown = CreateParameter("LengthDown", 3, 0, 200, 1);
            //Slipage = 10;
            //VolumeFix1 = 1;
            //VolumeFix2 = 1;
            //LengthAtr = 14;
            KofAtr = 0.5m;
            //LengthUp = 3;
            //LengthDown = 3;

            DeleteEvent += Strategy_DeleteEvent;
            
            Thread closerThread = new Thread(CloseFailPosition);
            closerThread.IsBackground = true;
            closerThread.Start();

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
                    && _tab.PositionsLast.CloseOrders[_tab.PositionsLast.CloseOrders.Count-1].State == OrderStateType.Fail)
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
        /// volume first
        /// фиксированный объем для входа в первую позицию
        /// </summary>
        public StrategyParameterDecimal VolumeFix1;

        /// <summary>
        /// volume next
        /// фиксированный объем для входа во вторую позицию
        /// </summary>
        public StrategyParameterDecimal VolumeFix2;

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
                    decimal priceEnter = _lastPcUp + (_lastAtr * KofAtr);
                    _tab.BuyAtStop(VolumeFix1.ValueDecimal, priceEnter + Slipage.ValueDecimal, priceEnter, StopActivateType.HigherOrEqual);
                }

                // Short
                if (Regime.ValueString != "OnlyLong")
                {
                    decimal priceEnter = _lastPcDown - (_lastAtr * KofAtr);
                    _tab.SellAtStop(VolumeFix1.ValueDecimal, priceEnter - Slipage.ValueDecimal, priceEnter, StopActivateType.LowerOrEqyal);
                }
                return;
            }

            openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                if (openPositions[i].State == PositionStateType.Open)
                {
                    if (openPositions[i].Direction == Side.Buy)
                    {
                        if (openPositions[i].OpenVolume < (VolumeFix1.ValueDecimal + VolumeFix2.ValueDecimal) &&
                            candles[candles.Count - 1].Close < _lastPcUp - (_lastAtr * KofAtr))
                        {
                            decimal priceEnter = _lastPcUp - (_lastAtr * KofAtr);
                            _tab.BuyAtLimitToPosition(openPositions[i], priceEnter, VolumeFix2.ValueDecimal);

                        }
                    }
                    else
                    {
                        if (openPositions[i].OpenVolume < (VolumeFix1.ValueDecimal + VolumeFix2.ValueDecimal) &&
                            candles[candles.Count - 1].Close > _lastPcUp - (_lastAtr * KofAtr))
                        {
                            decimal priceEnter = _lastPcDown + (_lastAtr * KofAtr);
                            _tab.SellAtLimitToPosition(openPositions[i], priceEnter, VolumeFix2.ValueDecimal);

                        }
                    }
                }
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

                if (openPositions[i].Direction == Side.Buy)
                {
                    decimal priceClose = _lastPcDown;
                    _tab.CloseAtTrailingStop(openPositions[i], priceClose, priceClose - Slipage.ValueDecimal);
                }
                else
                {
                    decimal priceClose = _lastPcUp;
                    _tab.CloseAtTrailingStop(openPositions[i], priceClose, priceClose + Slipage.ValueDecimal);
                }
            }

        }

        private void Strateg_PositionOpen(Position position)
        {
            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {

                if (openPositions[i].Direction == Side.Buy)
                {
                    _tab.SellAtStopCanсel();
                //    _tab.CloseAtStop(openPositions[i], _tab.CandlesAll[_tab.CandlesAll.Count-1].Low, _tab.CandlesAll[_tab.CandlesAll.Count - 1].Low - Slipage.ValueDecimal);
                //    _tab.CloseAtProfit(openPositions[i], openPositions[i].EntryPrice+_lastAtr*0.5m, openPositions[i].EntryPrice + _lastAtr * 0.5m - Slipage.ValueDecimal);
                }
                else
                {
                    _tab.BuyAtStopCanсel();
                //   _tab.CloseAtStop(openPositions[i], _tab.CandlesAll[_tab.CandlesAll.Count - 1].High, _tab.CandlesAll[_tab.CandlesAll.Count - 1].High + Slipage.ValueDecimal);
                //    _tab.CloseAtProfit(openPositions[i], openPositions[i].EntryPrice - _lastAtr * 0.5m, openPositions[i].EntryPrice + _lastAtr * 0.5m + Slipage.ValueDecimal);
                }

            }
        }
    }

}