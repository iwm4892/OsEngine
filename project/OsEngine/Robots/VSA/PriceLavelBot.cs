﻿using System;
using System.Collections.Generic;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.OsTrader.Panels.Tab;
using MessageBox = System.Windows.MessageBox;
using OsEngine.OsTrader.Panels;
using System.Threading;
using OsEngine.Indicators;

namespace OsEngine.Robots.VSA
{
    public class PriceLavelBot : BotPanel
    {
        private BotTabSimple _tab;
        private BotTabSimple _tab_pattern;
        private BotTabSimple _tab_delta;

        // начальное значение стопа(при выставлении ордера)
        //public StrategyParameterInt _startStop;
        public Decimal _startStop;
        /// <summary>
        /// Размер лота
        /// </summary>
        private StrategyParameterDecimal _Volume;
        /// <summary>
        /// Индикатор дельты
        /// </summary>
        private Delta delta_pattern;
        /// <summary>
        /// Индикатор объема
        /// </summary>
        private Volume Volume_pattern;

        /// <summary>
        /// Индикатор дельты
        /// </summary>
        private Delta delta_delta;
        /// <summary>
        /// Индикатор объема
        /// </summary>
        private Volume Volume_delta;

        /// <summary>
        /// Индикатор уровней
        /// </summary>
        private PriceLevleLine PriceLevleLine;

        /// <summary>
        /// режим on/off
        /// </summary>
        public StrategyParameterString Regime;
        /// <summary>
        /// Период экспоненциальной скользящей средней
        /// </summary>
        public StrategyParameterInt maLenth;
        /// <summary>
        /// Проверка на близость к уровню поддержки / сопротивления
        /// </summary>
        private bool TradeLevel;
        /// <summary>
        /// Использовать безубыток
        /// </summary>
        private StrategyParameterBool UseSafe;
        /// <summary>
        /// Цена на которой закончилась прошлая сессия
        /// </summary>
        private Decimal LastSessionEndPrice;
        /// <summary>
        /// Направление торговли
        /// </summary>
        private Side TradeSide;
        /// <summary>
        /// Проскальзывание
        /// </summary>
        private StrategyParameterInt _Slipage;
        private decimal Slipage;
        /// <summary>
        /// Плечо
        /// </summary>
        private StrategyParameterInt leverage;
        /// <summary>
        /// Количество ордеров в лесенке набора позиции
        /// </summary>
        private StrategyParameterInt StepCount;
        /// <summary>
        /// коэффицент для расчета размера дельты
        /// </summary>
        private StrategyParameterInt DeltaSizeK;
        /// <summary>
        /// Средняя по объему (для расчета графика дельты)
        /// </summary>
        private MovingAverage maVolume;
        /// <summary>
        /// Средняя по объему (для вычисления флета)
        /// </summary>
        private MovingAverage maVolumeSlow;
        /// <summary>
        /// Максимальный размер стопа (% от депозита)
        /// </summary>
        private StrategyParameterDecimal MaxStop;
        /// <summary>
        /// Минимальный профит для трэйлинга
        /// </summary>
        private StrategyParameterDecimal MinProfitTraling;

        private TradeSessions _TradeSessions;
        /// <summary>
        /// Текущий расчитанный стоп
        /// </summary>
        private decimal LastStop;
        /// <summary>
        /// Риск потерь за день
        /// </summary>
        private decimal RiskOnDay;
        /// <summary>
        /// Скользящая для стопа
        /// </summary>
        private MovingAverage mA;
        /// <summary>
        /// Использовать Азиатскую сессию
        /// </summary>
        private StrategyParameterBool SessionAsia;
        /// <summary>
        /// Использовать Европейскую сессию
        /// </summary>
        private StrategyParameterBool SessionEU;
        /// <summary>
        /// Использовать Американскую сессию
        /// </summary>
        private StrategyParameterBool SessionUSA;
        /// <summary>
        /// Использовать Российскую сессию
        /// </summary>
        private StrategyParameterBool SessionRUS;
        /// <summary>
        /// Торги круглосуточно
        /// </summary>
        private StrategyParameterBool SessionDay;
        /// <summary>
        /// Вылюта депозита (первая или вторая валюта валютной пары)
        /// </summary>
        private StrategyParameterString DepoCurrency;
        /// <summary>
        /// торгуем контрактами
        /// </summary>
        private StrategyParameterBool isContract;
        /// <summary>
        /// отрисовывать ли уровни
        /// </summary>
        private StrategyParameterBool PaintLevels;
        /// <summary>
        /// Использовать безубыточный стоп
        /// </summary>
        private StrategyParameterBool Breakeven;
        private bool NeedBreakeven;

        public override string GetNameStrategyType()
        {
            return "PriceLavelBot";
        }

        public override void ShowIndividualSettingsDialog()
        {
            MessageBox.Show("У данной стратегии пока нет настроек");
        }

        public PriceLavelBot(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            this.ParametrsChangeByUser += PriceLavelBot_ParametrsChangeByUser;

            TabCreate(BotTabType.Simple);
            _tab = TabsSimple[0];

            SessionAsia = CreateParameter("Торговать Азию", false);
            SessionEU = CreateParameter("Торговать Европу", false);
            SessionUSA = CreateParameter("Торговать Америку", false);
            SessionRUS = CreateParameter("Торговать Россию", false);
            SessionDay = CreateParameter("Круглосуточно", false);


            _TradeSessions = new TradeSessions(name + "_TradeSessions", false, GetListSessionTypes());
            _TradeSessions = (TradeSessions)_tab.CreateCandleIndicator(_TradeSessions, "Prime");
            _TradeSessions.Save();

            PriceLevleLine = new PriceLevleLine(name + "_PriceLevleLine", false);
            PriceLevleLine = (PriceLevleLine)_tab.CreateCandleIndicator(PriceLevleLine, "Prime");
            PriceLevleLine.PaintOn = false;
            PriceLevleLine.Save();

            maLenth = CreateParameter("maLenth", 24, 24, 48, 1);
            maVolume = new MovingAverage(name + "_maVolume", false);
            maVolume = (MovingAverage)_tab.CreateCandleIndicator(maVolume, "New1");
            maVolume.Lenght = maLenth.ValueInt;
            maVolume.TypeCalculationAverage = MovingAverageTypeCalculation.Exponential;
            maVolume.TypePointsToSearch = PriceTypePoints.Volume;
            maVolume.Save();

            maVolumeSlow = new MovingAverage(name + "maVolumeSlow", false);
            maVolumeSlow = (MovingAverage)_tab.CreateCandleIndicator(maVolumeSlow, "New1");
            maVolumeSlow.ColorBase = System.Drawing.Color.Green;
            maVolumeSlow.Lenght = 48;
            maVolumeSlow.TypeCalculationAverage = MovingAverageTypeCalculation.Simple;
            maVolumeSlow.TypePointsToSearch = PriceTypePoints.Volume; 
            maVolumeSlow.Save();

            mA = new MovingAverage(name + "mA", false) { Lenght = 9 };
            mA = (MovingAverage)_tab.CreateCandleIndicator(mA, "Prime");
            mA.Save();

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            UseSafe = CreateParameter("Использовать сейф", true);
            _Volume = CreateParameter("Volume", 1, 0.00m, 100, 1);
            MinProfitTraling = CreateParameter("Минимальный профит для трэйлинга",0.2m, 0.2m, 2, 0.1m);

            MaxStop = CreateParameter("MaxStop", 1, 1, 10, 0.1m);

            _Slipage = CreateParameter("_Slipage", 1, 1, 50, 1);
            leverage = CreateParameter("Маржинальное плечо", 1, 1, 10, 1);
            StepCount = CreateParameter("Ордеров в лесенке", 1, 1, 10, 1);

            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });

            isContract = CreateParameter("Торгуем контрактами", false);

            PaintLevels = CreateParameter("Отрисовывать уровни", true);

            Breakeven = CreateParameter("Использовать безубыток", true);
            NeedBreakeven = Breakeven.ValueBool;

            DeltaSizeK = CreateParameter("Делитель основного ТФ", 6, 1, 40, 1);

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.CandleUpdateEvent += _tab_CandleUpdateEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;
            _tab.NewTickEvent += _tab_NewTickEvent;
            _tab.FirstTickToDayEvent += _tab_FirstTickToDayEvent;
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;


            //Младший тайм фрейм
            TabCreate(BotTabType.Simple);
            _tab_pattern = TabsSimple[1];

            _tab_pattern.CandleFinishedEvent += _tab_pattern_CandleFinishedEvent;

            delta_pattern = new Delta(name + "_delta", false);
            delta_pattern = (Delta)_tab_pattern.CreateCandleIndicator(delta_pattern, "New1");
            delta_pattern.Save();

            Volume_pattern = new Volume(name + "_Volume", false);
            Volume_pattern = (Volume)_tab_pattern.CreateCandleIndicator(Volume_pattern, "New2");
            Volume_pattern.Save();


            //график Дельты
            TabCreate(BotTabType.Simple);
            _tab_delta = TabsSimple[2];

            _tab_delta.CandleFinishedEvent += _tab_delta_CandleFinishedEvent; ;

            delta_delta = new Delta(name + "delta_delta", false);
            delta_delta = (Delta)_tab_delta.CreateCandleIndicator(delta_delta, "New1");
            delta_delta.Save();

            Volume_delta = new Volume(name + "delta_Volume", false);
            Volume_delta = (Volume)_tab_delta.CreateCandleIndicator(Volume_delta, "New2");
            Volume_delta.Save();



            //    lines = new List<LineHorisontal>();

            Thread closerThread = new Thread(CloseFailPosition);
            closerThread.IsBackground = true;
            closerThread.Start();

        }

        private void _tab_NewTickEvent(Trade obj)
        {
            if (_tab.PositionsLast != null && _tab.PositionsLast.OpenVolume > 0 && _tab.PositionsLast.StopOrderRedLine == 0)
            {
                LastStop = GetStopLevel(_tab.PositionsLast.Direction, _tab.PositionsLast.EntryPrice);
                _tab.CloseAtServerTrailingStop(_tab.PositionsLast, LastStop, LastStop);
            }
            /*
            if (_tab.PositionsLast != null && _tab.PositionsLast.OpenVolume > 0) {

                if (
                    ((obj.Price - _tab.PositionsLast.EntryPrice) / _tab.PositionsLast.EntryPrice > 0.02m && _tab.PositionsLast.Direction == Side.Buy) 
                    || ((obj.Price - _tab.PositionsLast.EntryPrice) / _tab.PositionsLast.EntryPrice < -0.02m && _tab.PositionsLast.Direction == Side.Sell)
                    )
                {
                    _tab.CloseAllAtMarket();
                }
            }
            */
            if (NeedBreakeven)
            {
                if (_tab.PositionsLast != null && _tab.PositionsLast.OpenVolume > 0)
                {
                    CloseAtBreakeven(_tab.CandlesAll);
                    
                }
            }

        }

        private void _tab_PositionClosingSuccesEvent(Position obj)
        {
            if (obj.OpenVolume == 0)
            {
                _tab.CloseAllOrderInSystem();
            }
        }

        private List<TradeSessions.SessionType> GetListSessionTypes()
        {
            List<TradeSessions.SessionType> _result = new List<TradeSessions.SessionType>();
            if (SessionAsia.ValueBool)
            {
                _result.Add(TradeSessions.SessionType.Asia);
            }
            if (SessionEU.ValueBool)
            {
                _result.Add(TradeSessions.SessionType.EU);
            }
            if (SessionRUS.ValueBool)
            {
                _result.Add(TradeSessions.SessionType.RUS);
            }
            if (SessionUSA.ValueBool)
            {
                _result.Add(TradeSessions.SessionType.USA);
            }
            if (SessionDay.ValueBool)
            {
                _result.Add(TradeSessions.SessionType.Day);
            }
            return _result;
        }
        private void Fillsession()
        {
            List<TradeSessions.SessionType> _result = GetListSessionTypes();
            _TradeSessions.FillSessions(_result);
        }
        private void PriceLavelBot_ParametrsChangeByUser()
        {
            Fillsession();
        }

        private void CloseFailPosition()
        {
            /*
            while (true)
            {
                Thread.Sleep(1000);

                if (MainWindow.ProccesIsWorked == false)
                {
                    return;
                }

                if (_tab.PositionsLast != null && _tab.PositionsLast.State == PositionStateType.ClosingFail)
                {

                    if (_tab.PositionsLast.OpenVolume == 0)
                    
                        {
                            _tab.GetJournal().DeletePosition(_tab.PositionsLast);
                    }
                    else
                    {
                        _tab.CloseAtMarket(_tab.PositionsLast, _tab.PositionsLast.OpenVolume);
                    }
                    
                }
            }
            */
        }
        private void _tab_FirstTickToDayEvent(Trade obj)
        {
            RiskOnDay = 0;
        }

        /// <summary>
        /// Открытие по паттерну
        /// </summary>
        /// <param name="indicators">Индикаторы</param>
        /// <param name="candles">Свечи</param>
        /// <param name="patterns">Список паттернов</param>
        private void OpenByPattrn(List<IIndicator> indicators, List<Candle> candles, List<string> patterns)
        {
            if (!ValidateParams())
            {
                return;
            }
            if (!CanOpenPosition())
            {
                return;
            }
            if (candles.Count < 2)
            {
                return;
            }
            
            
            bool CanFindPattern = false;
            
            for (int i = 1; i <= 1; i++)
            {
                Candle candle = candles[candles.Count - i];
                List<PriceLevleLine.levlel> lvl = PriceLevleLine.LevleData.FindAll(x => x.Value <= candle.High && x.Value >= candle.Low);

                if (lvl != null && lvl.Count > 0)
                {
                    List<PriceLevleLine.levlel> filter = GetFilterlvl();
                    foreach (PriceLevleLine.levlel l in lvl)
                    {
                        if (filter != null && filter.Find(f => f.Value == l.Value) != null)
                        {
                            continue;
                        }
                        CanFindPattern = true;
                    }
                }
            }
            
            if(TradeSide == Side.Buy && candles[candles.Count - 2].Close< LastSessionEndPrice)
            {
                CanFindPattern = false;
            }
            if (TradeSide == Side.Sell && candles[candles.Count - 2].Close > LastSessionEndPrice)
            {
                CanFindPattern = false;
            }
            if (!CanFindPattern)
            {
                return;
            }
            
            // открытие позиций по патерну
            List<Pattern> signal = Pattern.GetValidatePatterns(candles, indicators, patterns);
            if (signal.Count > 0 && signal[0].isPattern)
            {
                if (signal.Count != 0 && signal[0].isPattern)
                {
                    if (signal[0].Side == TradeSide)
                    {
                        _tab.SetNewLogMessage("Открытие по патерну " + signal[0].GetType().Name, LogMessageType.Signal);
                        OpenPosition(signal[0].Side, candles[candles.Count - 1].Close,signal[0].GetType().Name);
                    }
                }
            }

        }
        private List<PriceLevleLine.levlel> GetFilterlvl()
        {
            List<PriceLevleLine.levlel> result = new List<PriceLevleLine.levlel>();
            List<PriceLevleLine.levlel> lvls;
            PriceLevleLine.levlel lvl;
            
            if (TradeSide == Side.Buy)
            {
                lvl = PriceLevleLine.LevleData.Find(l => l.Value == _TradeSessions.MaxSessionPrice);
                if (lvl != null)
                {
                    result.Add(lvl);
                }
            }
            else
            {
                lvl = PriceLevleLine.LevleData.Find(l => l.Value == _TradeSessions.MinSessionPrice);
                if (lvl != null)
                {
                    result.Add(lvl);
                }
            }
            
            if(isFlat())
            {

                if (TradeSide == Side.Buy)
                {
                    lvls = PriceLevleLine.LevleData.FindAll(l => l.Value < _TradeSessions.MaxSessionPrice && l.Value>LastSessionEndPrice);
                    if(lvls != null && lvls.Count > 1)
                    {
                        lvls.Sort((a, b) => decimal.Compare(a.Value, b.Value));
                        for( int i = 1; i < lvls.Count; i++)
                        {
                            result.Add(lvls[i]);
                        }
                    }
                }
                else
                {
                    lvls = PriceLevleLine.LevleData.FindAll(l => l.Value > _TradeSessions.MinSessionPrice && l.Value < LastSessionEndPrice);
                    if (lvls != null && lvls.Count > 1)
                    {
                        lvls.Sort((a, b) => decimal.Compare(a.Value, b.Value));
                        for (int i = 0; i < lvls.Count-1; i++)
                        {
                            result.Add(lvls[i]);
                        }
                    }
                }

            }
            
            
            return result;
        }
        private bool isFlat()
        {
            bool result = false;
            if (maVolume!=null && maVolume.Values.Count>0 && maVolumeSlow != null && maVolumeSlow.Values.Count > 0)
            {
                result = maVolume.Values[maVolume.Values.Count - 1] < maVolumeSlow.Values[maVolumeSlow.Values.Count - 1];
            }
            return result;
        }
        private void _tab_delta_CandleFinishedEvent(List<Candle> candles)
        {
            if (!ValidateParams())
            {
                return;
            }
            //CloseByPattern(candles);

            List<IIndicator> indicators = new List<IIndicator>();
            indicators.Add(delta_delta);
            indicators.Add(Volume_delta);
            // открытие позиций по патерну
            List<string> patterns = new List<string>();
            patterns.Add("Signal_pattern"); //сигналка
            //patterns.Add("Metla_pattern");
            OpenByPattrn(indicators, candles, patterns);
        }
        private void _tab_pattern_CandleFinishedEvent(List<Candle> candles)
        {
        //    LogicClosePositions(candles);
            if (!ValidateParams())
            {
                return;
            }
            List<IIndicator> indicators = new List<IIndicator>();
            indicators.Add(delta_pattern);
            indicators.Add(Volume_pattern);
            // открытие позиций по патерну
            List<string> patterns = new List<string>();
            patterns.Add("Metla_pattern"); //Метелка
            //patterns.Add("Trap_pattern");
            OpenByPattrn(indicators, candles, patterns);
        }

        private void DeltaStepCheck()
        {
            if (maVolume.Values.Count == 0 || maVolume.Values[maVolume.Values.Count - 1] == 0)
            {
                _tab_delta.Connector.TimeFrameBuilder.DeltaPeriods = (int)_tab.CandlesAll[_tab.CandlesAll.Count - 1].Volume / DeltaSizeK.ValueInt;
                return;
            }
            if (_tab_delta.Connector.TimeFrameBuilder.DeltaPeriods != (int)maVolume.Values[maVolume.Values.Count - 1] / DeltaSizeK.ValueInt)
            {
                _tab_delta.Connector.TimeFrameBuilder.DeltaPeriods = (int)maVolume.Values[maVolume.Values.Count - 1] / DeltaSizeK.ValueInt;
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
        private Decimal LastPositionVolume_All;
        private bool NeadStepOpen;
        private void OpenPosition(Side side, decimal price,string Signal)
        {
            Slipage = _Slipage.ValueInt * _tab.Securiti.PriceStep;

            decimal _Vol;
            LastStop = GetStopLevel(side, price);
         //   LastStop = GetStopByPattern(side, price, Signal);
            if (LastStop == 0)
            {
                return;
            }
            decimal VollAll = leverage.ValueInt * (_tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked) / GetPrice(price);
            
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

            LastPositionVolume_All = _Vol;
            if (_Vol > 0)
            {

                if (StepCount.ValueInt > 1 
                    && ((side == Side.Buy && price > (_TradeSessions.MinSessionPrice + _TradeSessions.MaxSessionPrice) / 2) ||
                        (side == Side.Sell && price < (_TradeSessions.MinSessionPrice + _TradeSessions.MaxSessionPrice) / 2)))
                {
                    decimal v = GetVol(_Vol / GetPieceCount());

                    if (v == 0 && _Vol > 0)
                    {
                        v = 1;
                    }
                    if (side == Side.Buy)
                    {
                        _tab.BuyAtMarket(v, Signal);
                    }
                    else
                    {
                        _tab.SellAtMarket(v, Signal);
                    }
                    NeadStepOpen = true;
                }
                else
                {
                    if (side == Side.Buy)
                    {
                        _tab.BuyAtMarket(_Vol, Signal);
                    }
                    else
                    {
                        _tab.SellAtMarket(_Vol, Signal);
                    }
                }
            }


        }
        private void OpenStepLimit(Position pos)
        {
            NeadStepOpen = false;
            decimal _volOst = LastPositionVolume_All - pos.OpenVolume;
            decimal v = pos.OpenVolume;
            decimal price = pos.EntryPrice;
            decimal step = Math.Abs(pos.EntryPrice - LastStop) / (StepCount.ValueInt);
            for (int i = 1; i < StepCount.ValueInt; i++) { 
                v = GetVol(Math.Min(_volOst, v + 2 * v));
                if (v > 0)
                {
                    if (pos.Direction == Side.Buy)
                    {
                        price = price - step;
                        if (price > LastStop)
                        {
                            _tab.BuyAtLimitToPosition(pos, price, v);
                        }
                    }
                    if (pos.Direction == Side.Sell)
                    {
                        price = price + step;
                        if (price < LastStop)
                        {
                            _tab.SellAtLimitToPosition(pos, price, v);
                        }
                    }
                }
                _volOst = _volOst - v;
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
        /// <summary>
        /// Получить количество долей для лесенки
        /// </summary>
        /// <returns></returns>
        private int GetPieceCount()
        {
            int result = 1;
            for (int i = 1; i < StepCount.ValueInt; i++)
            {
                result = result +result * 2;
            }
            return result;
        }
        private void _tab_CandleUpdateEvent(List<Candle> candles)
        {
        }


        private bool ValidateParams()
        {
            if (Regime.ValueString == "Off")
            {
                return false;
            }
            if (_tab.CandlesAll == null || _tab.CandlesAll.Count < 2)
            {
                return false;
            }
            if (LastSessionEndPrice == 0)
            {
                return false;
            }
            if (!_TradeSessions.CanTrade)
            {
                //    return false;
            }
            
            if (RiskOnDay < -1 * MaxStop.ValueDecimal / 100)
            {
             //  return false;
            }
            if (_TradeSessions.MinSessionPrice == 0 || (_TradeSessions.MaxSessionPrice - _TradeSessions.MinSessionPrice) / _TradeSessions.MinSessionPrice < MaxStop.ValueDecimal / 100)
            {
                //return false;
            }
            
            if (_TradeSessions.DayChanel / _TradeSessions.MaxSessionPrice < 0.02m)
            {
                if (_tab.CandlesAll[_tab.CandlesAll.Count - 1].Close > _TradeSessions.MinSessionPrice + 0.1m * _TradeSessions.DayChanel
                    && TradeSide == Side.Buy
                      && _TradeSessions.MaxSessionPrice > _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close
                    )
                {
                    return false;
                }
                if (_tab.CandlesAll[_tab.CandlesAll.Count - 1].Close < _TradeSessions.MaxSessionPrice - 0.1m * _TradeSessions.DayChanel
                    && TradeSide == Side.Sell
                         && _TradeSessions.MinSessionPrice < _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close
                     )
                {
                    return false;
                }
            }
            
                return true;
        }
        private bool CanOpenPosition()
        {
            if (_tab.PositionsOpenAll == null)
            {
                return true;
            }
            return _tab.PositionsOpenAll.Count == 0;
        }
        private void LogicClosePositions(List<Candle> candles)
        {
            if (CanOpenPosition())
            {
                return;
            }
            List<Position> openPositions = _tab.PositionsOpenAll;


            for (int i = 0; i < openPositions.Count && candles.Count > 1; i++)
            {
                /*
                if (openPositions[i].TimeOpen.AddHours(1) < _tab.TimeServerCurrent &&
                    openPositions[i].ProfitPortfolioPunkt<0)
                {
                    _tab.CloseAllAtMarket();
                }
                */
                decimal stop = GetTrailingStopPrice(openPositions[i]);
                    if(openPositions[i].EntryPrice == 0)
                    {
                        continue;
                    }
                    if(openPositions[i].Direction == Side.Buy && stop < openPositions[i].EntryPrice)
                    {
                        continue;
                    }
                    if(openPositions[i].Direction == Side.Sell && stop > openPositions[i].EntryPrice)
                    {
                        continue;
                    }
                    bool canClose = false;
                    decimal _profit = (stop - openPositions[i].EntryPrice) * 100 / openPositions[i].EntryPrice;
                    if (openPositions[i].Direction == Side.Sell)
                    {
                        _profit = -1 * _profit;
                    }
                    if (_profit >= MinProfitTraling.ValueDecimal)
                    {
                        canClose = true;
                    }
                    if (canClose)
                    {
                        _tab.CloseAtServerTrailingStop(openPositions[i], stop, stop);
                        NeedBreakeven = false;
                    }

            }

        }
        private void CloseByPattern(List<Candle> candles)
        {

            List<Position> positions = _tab.PositionsOpenAll;
            if (_tab.PositionsLast != null && _tab.PositionsLast.State == PositionStateType.Open)
            {
                List<IIndicator> indicators = new List<IIndicator>();
                indicators.Add(delta_delta);
                indicators.Add(Volume_delta);
                List<string> patterns = new List<string>();
                patterns.Add("Signal_pattern"); //сигналка

                // Закрытие в случае конт паттерна
                List<Pattern> signal = Pattern.GetValidatePatterns(candles, indicators, patterns);
                if (signal.Count > 0 && signal[0].isPattern)
                {
                    if (signal.Count != 0 && signal[0].isPattern)
                    {
                        if (signal[0].Side != _tab.PositionsLast.Direction && CanCloseByPattern(candles))
                        {
                            _tab.SetNewLogMessage("Закрытие по патерну " + signal[0].GetType().Name, LogMessageType.Signal);
                            _tab.CloseAtServerTrailingStop(_tab.PositionsLast, candles[candles.Count - 1].Close, candles[candles.Count - 1].Close);
                        }
                    }
                }

            }
        }
        private bool CanCloseByPattern(List<Candle> candles)
        {
            List<Position> openPositions = _tab.PositionsOpenAll;


            for (int i = 0; i < openPositions.Count && candles.Count > 1; i++)
            {
                decimal stop = candles[candles.Count - 1].Close;
                if (openPositions[i].EntryPrice == 0)
                {
                    continue;
                }
                if (openPositions[i].Direction == Side.Buy && stop < openPositions[i].EntryPrice)
                {
                    continue;
                }
                if (openPositions[i].Direction == Side.Sell && stop > openPositions[i].EntryPrice)
                {
                    continue;
                }
                bool canClose = false;
                decimal _profit = (stop - openPositions[i].EntryPrice) * 100 / openPositions[i].EntryPrice;
                if (openPositions[i].Direction == Side.Sell)
                {
                    _profit = -1 * _profit;
                }
                if (_profit >= MinProfitTraling.ValueDecimal)
                {
                    canClose = true;
                }
                if (canClose)
                {
                    return true;
                }

            }
            return false;
        }
        private void CloseAtBreakeven(List<Candle> candles)
        {
            if (CanOpenPosition())
            {
                return;
            }
            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; i < openPositions.Count && candles.Count > 1; i++)
            {
                decimal stop = GetTrailingStopPrice(openPositions[i]);
                if (openPositions[i].EntryPrice == 0)
                {
                    continue;
                }
                if (openPositions[i].Direction == Side.Buy && stop < openPositions[i].EntryPrice)
                {
                    continue;
                }
                if (openPositions[i].Direction == Side.Sell && stop > openPositions[i].EntryPrice)
                {
                    continue;
                }
                bool canClose = false;
                decimal _profit = (stop - openPositions[i].EntryPrice) * 100 / openPositions[i].EntryPrice;
                if (openPositions[i].Direction == Side.Sell)
                {
                    _profit = -1 * _profit;
                }
                if (_profit >= 0.2m)
                {
                    canClose = true;
                }
                if (canClose)
                {
                    _tab.CloseAtServerTrailingStop(openPositions[i], stop, stop);
                    NeedBreakeven = false;
                }

            }

        }

        private decimal GetTrailingStopPrice(Position position)
        {
            if (position != null)
            {
                List<decimal> result = new List<decimal>();
                if ((position.Direction == Side.Buy && mA.Values[mA.Values.Count - 1] < _tab.Trades[_tab.Trades.Count - 1].Price) ||
                    (position.Direction == Side.Sell && mA.Values[mA.Values.Count - 1] > _tab.Trades[_tab.Trades.Count - 1].Price)
                    )
                    result.Add(mA.Values[mA.Values.Count - 1]);
                result.Add((position.EntryPrice + _tab.Trades[_tab.Trades.Count - 1].Price) / 2);
                result.Sort((a, b) => decimal.Compare(a, b));
                if (position.Direction == Side.Buy)
                {
                    return result[result.Count - 1];
                }
                else
                {
                    return result[0];
                }
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// Найдем последний уровень
        /// </summary>
        /// <param name="position">Позиция</param>
        /// <returns></returns>
        private Decimal GetStop(Side side, decimal price)
        {
            // берем последние 20 свечек
            List<Candle> LastCandles = _tab.CandlesAll.GetRange(_tab.CandlesAll.Count - 21, 20);
            LastCandles.Sort((a, b) => decimal.Compare(a.ClasterData.maxPrice, b.ClasterData.maxPrice));
            if (side == Side.Buy)
            {
                List<Candle> lvl = LastCandles.FindAll(x => x.ClasterData.maxPrice < price);
                if (lvl != null && lvl.Count > 1)
                {
                    return lvl[lvl.Count - 2].ClasterData.maxPrice - Slipage;
                }
            }
            if (side == Side.Sell)
            {
                List<Candle> lvl = LastCandles.FindAll(x => x.ClasterData.maxPrice > price);
                if (lvl != null && lvl.Count > 1)
                {
                    return lvl[1].ClasterData.maxPrice + Slipage;
                }
            }
            if (side == Side.Buy)
            {
                return price - price * MaxStop.ValueDecimal / 100;
            }
            else
            {
                return price + price * MaxStop.ValueDecimal / 100;
            }

        }
        private Decimal GetStopLevel(Side side, decimal price)
        {
            

            if (side == Side.Buy)
            {
                List<PriceLevleLine.levlel> lvl = PriceLevleLine.LevleData.FindAll(x => x.Value < price);
                if (lvl != null)
                {
                    lvl.Sort((a, b) => decimal.Compare(a.Value, b.Value));
                    /*
                    if (lvl.Count > 2)
                    {
                        return lvl[lvl.Count - 3].Value - Slipage;
                    }*/
                    if (lvl.Count > 1)
                    {
                        return lvl[lvl.Count - 2].Value - Slipage;
                    }
                    
                    if (lvl.Count > 0)
                    {
                        return lvl[lvl.Count - 1].Value - Slipage;
                    }

                }

            }
            else
            {
                List<PriceLevleLine.levlel> lvl = PriceLevleLine.LevleData.FindAll(x => x.Value > price);
                if (lvl != null)
                {
                    lvl.Sort((a, b) => decimal.Compare(a.Value, b.Value));
                    /*
                    if (lvl != null && lvl.Count > 2)
                    {
                        return lvl[2].Value + Slipage;
                    }*/
                    if (lvl != null && lvl.Count > 1)
                    {
                        return lvl[1].Value + Slipage;
                    }
                    
                    if (lvl != null && lvl.Count > 0)
                    {
                        return lvl[0].Value + Slipage;
                    }
                }


            }
            return GetStop(side, price);


        }
        private Decimal GetStopByPattern(Side side, decimal price, string Signal)
        {
            if (Signal == "Signal_pattern")
            {
                if (
                    (side == Side.Buy && _tab.CandlesAll[_tab.CandlesAll.Count-1].ClasterData.maxPrice<price) ||
                    (side == Side.Sell && _tab.CandlesAll[_tab.CandlesAll.Count - 1].ClasterData.maxPrice > price)
                    )
                {
                    return _tab.CandlesAll[_tab.CandlesAll.Count - 1].ClasterData.maxPrice;
                }
            }
            return GetStopLevel(side, price);
        }
        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {
            if(
               (obj.Direction == Side.Buy && LastStop > obj.EntryPrice) ||
               (obj.Direction == Side.Sell && LastStop < obj.EntryPrice) ||
               LastStop ==0
              )
            {
                LastStop = GetStopLevel(obj.Direction, obj.EntryPrice);
            }
            //выставим новые стопы
            
            _tab.CloseAtServerTrailingStop(obj, LastStop, LastStop);
            if (NeadStepOpen)
            {
                OpenStepLimit(obj);
            }
    //        _tab.CloseAtProfit(obj, 2*(obj.EntryPrice - LastStop)+ obj.EntryPrice, 2 * (obj.EntryPrice - LastStop) + obj.EntryPrice);

            //_tab.CloseAtTrailingStop(obj, LastStop, LastStop);
            if (UseSafe.ValueBool)
            {
                decimal fixPOs = 2 * obj.EntryPrice - LastStop;
                decimal vol = obj.OpenVolume/2;
                if (isContract.ValueBool)
                {
                    vol = (int)vol;
                }
                _tab.CloseAtLimit(obj, fixPOs, vol);
            }
            if (Breakeven.ValueBool)
            {
                NeedBreakeven = true;
            }
        }
        private void OpenAtLevel()
        {
            if (!CanOpenPosition())
            {
                return;
            }
            PriceLevleLine.levlel lvl = PriceLevleLine.LevleData.FindLast(x => x.levlSide == TradeSide);
            if (lvl != null)
            {
                if (TradeSide == Side.Buy)
                {
                    // покупаем по возвращению цены
                    _tab.BuyAtStop(_Volume.ValueDecimal, lvl.Value, lvl.Value, StopActivateType.LowerOrEqyal);
                }
                if (TradeSide == Side.Sell)
                {
                    // Продаем по возвращению цены
                    _tab.SellAtStop(_Volume.ValueDecimal, lvl.Value, lvl.Value, StopActivateType.HigherOrEqual);
                }

            }

            lvl = PriceLevleLine.LevleData.FindLast(x => x.levlSide != TradeSide);
            if (lvl != null)
            {
                if (TradeSide == Side.Buy)
                {
                    // покупаем попробитию последнего максимума
                    _tab.BuyAtStop(_Volume.ValueDecimal, lvl.Value + lvl.Value * 0.005m, lvl.Value, StopActivateType.LowerOrEqyal);
                }
                if (TradeSide == Side.Sell)
                {
                    // Проадем попробитию последнего минимума
                    _tab.SellAtStop(_Volume.ValueDecimal, lvl.Value - lvl.Value * 0.005m, lvl.Value, StopActivateType.HigherOrEqual);
                }
            }

        }
        private void _tab_CandleFinishedEvent(List<Candle> candles)
        {
            LogicClosePositions(candles);
            DeltaStepCheck();
            ///Отрисовка линий
            if (PaintLevels.ValueBool)
            {
                PriceLevleLine.PaintLevleData(TabsSimple);
            }
            /*
            if(TrendMA.Values[TrendMA.Values.Count - 1] == 0)
            {
                return;
            }
            
            LastSessionEndPrice = TrendMA.Values[TrendMA.Values.Count - 1];
            */
             
            //Определяем направление торговли по прошлой сессии
            if (_TradeSessions.Values.Count > 0)
            {
                LastSessionEndPrice = _TradeSessions.Values[_TradeSessions.Values.Count - 1];//_TradeSessions.LastSessionEndPrice;
            }
            else
            {
                return;
            }
            
            //LastSessionEndPrice = TradeSessions.LastSessionEndPrice(_tab.CandlesAll, candles[candles.Count - 1].TimeStart);
            if (LastSessionEndPrice > 0)
            {
                Side oldTradeSide = TradeSide;
                if (LastSessionEndPrice < candles[candles.Count - 1].Close)
                {
                    TradeSide = Side.Buy;
                }
                else
                {
                    TradeSide = Side.Sell;
                }
                /*
                if(_TradeSessions.MaxSessionPrice>LastSessionEndPrice 
                    && _TradeSessions.MinSessionPrice< LastSessionEndPrice)
                {
                    if (LastSessionEndPrice < candles[candles.Count - 1].Close)
                    {
                        TradeSide = Side.Sell;
                    }
                    else
                    {
                        TradeSide = Side.Buy;
                    }
                }
                */
                if (oldTradeSide != TradeSide)
                {
                    _tab.SetNewLogMessage("Направление торговли " + TradeSide, LogMessageType.Signal);
                }
                oldTradeSide = TradeSide;
            }
            if (candles.Count > 1 && candles[candles.Count - 1].TimeStart.DayOfYear != candles[candles.Count - 2].TimeStart.DayOfYear)
            {
                RiskOnDay = 0;
            }

            for (int i = _tab.PositionsCloseAll.Count - 1; i >= 0; i--)
            {
                if(_tab.PositionsCloseAll[i].State != PositionStateType.Done ||
                    _tab.PositionsCloseAll[i].EntryPrice==0)
                {
                    continue;
                }
                if (_tab.PositionsCloseAll[i].TimeClose.DayOfYear == candles[candles.Count - 1].TimeStart.DayOfYear)
                {
                    int koeff = 1;
                    if (_tab.PositionsCloseAll[i].Direction == Side.Sell)
                    {
                        koeff = -1;
                    }
                    RiskOnDay += koeff * (_tab.PositionsCloseAll[i].ClosePrice - _tab.PositionsCloseAll[i].EntryPrice) / _tab.PositionsCloseAll[i].EntryPrice;
                }
                else
                {
                    break;
                }
            }


            if (!ValidateParams())
            {
                return;
            }

            if (!CanOpenPosition())
            {
                return;
            }
            /*
            if (_TradeSessions.DayChanel / _TradeSessions.MaxSessionPrice < 0.02m)
            {
                _tab.SellAtStop(1, _TradeSessions.MaxSessionPrice, _TradeSessions.MaxSessionPrice, StopActivateType.HigherOrEqual);
                _tab.BuyAtStop(1, _TradeSessions.MinSessionPrice, _TradeSessions.MinSessionPrice, StopActivateType.LowerOrEqyal);
            }
            else
            {
                _tab.BuyAtStopCanсel();
                _tab.SellAtStopCanсel();
            }
            */

            // если трендовый день то открываемся сразу
            /*
            if (_TradeSessions.TypeOfDay == TradeSessions.DayType.TrendDay)
            {
                OpenPosition(_TradeSessions.TradeSide[0], candles[candles.Count - 1].Close);
            }
            */
            //OpenAtLevel();
        }


    }

}
