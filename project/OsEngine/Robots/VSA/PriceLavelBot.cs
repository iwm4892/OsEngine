using System;
using System.Collections.Generic;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.OsTrader.Panels.Tab;
using MessageBox = System.Windows.MessageBox;
using OsEngine.OsTrader.Panels;
using System.Threading;

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
        /// Индикатор кластера цен
        /// </summary>
        private Claster Claster;
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
        /// коэффицент для расчета размера дельты
        /// </summary>
        private StrategyParameterInt DeltaSizeK;

        private MovingAverage maVolume;

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

            Claster = new Claster(name + "Claster", false);
            Claster = (Claster)_tab.CreateCandleIndicator(Claster, "Prime");
            Claster.Save();


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

            mA = new MovingAverage(name + "mA", false) { Lenght = 9 };
            mA = (MovingAverage)_tab.CreateCandleIndicator(mA, "Prime");
            mA.Save();

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On" });
            UseSafe = CreateParameter("Использовать сейф", true);
            _Volume = CreateParameter("Volume", 1, 0.00m, 100, 1);
            MinProfitTraling = CreateParameter("Минимальный профит для трэйлинга",0.2m, 0.2m, 2, 0.1m);

            MaxStop = CreateParameter("MaxStop", 1, 1, 10, 0.1m);

            _Slipage = CreateParameter("_Slipage", 1, 1, 50, 1);

            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });

            isContract = CreateParameter("Торгуем контрактами", false);

            DeltaSizeK = CreateParameter("Делитель основного ТФ", 6, 1, 40, 1);

            _tab.CandleFinishedEvent += _tab_CandleFinishedEvent;
            _tab.CandleUpdateEvent += _tab_CandleUpdateEvent;
            _tab.PositionOpeningSuccesEvent += _tab_PositionOpeningSuccesEvent;

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
            while (true)
            {
                Thread.Sleep(1000);

                if (MainWindow.ProccesIsWorked == false)
                {
                    return;
                }
                /*
                for (int i = 0; i < _tab.PositionsCloseAll.Count; i++)
                {
                    if (_tab.PositionsCloseAll[i].State != PositionStateType.ClosingFail)
                    {
                        continue;
                    }
                    _tab.CloseAtMarket(_tab.PositionsCloseAll[i], _tab.PositionsCloseAll[i].OpenVolume);
                }
                */
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
        private void OpenByPattrn(List<IIndicatorCandle> indicators, List<Candle> candles, List<string> patterns)
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
                    CanFindPattern = true;
                }
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
                        OpenPosition(signal[0].Side, candles[candles.Count - 1].Close);
                    }
                }
            }

        }
        private void _tab_delta_CandleFinishedEvent(List<Candle> candles)
        {
            if (!ValidateParams())
            {
                return;
            }
            List<IIndicatorCandle> indicators = new List<IIndicatorCandle>();
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
            if (!ValidateParams())
            {
                return;
            }
            List<IIndicatorCandle> indicators = new List<IIndicatorCandle>();
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
        private void OpenPosition(Side side, decimal price)
        {
            Slipage = _Slipage.ValueInt * _tab.Securiti.PriceStep;

            decimal _Vol;
            LastStop = GetStopLevel(side, price);
            if (LastStop == 0)
            {
                return;
            }
            decimal VollAll = (_tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked) / GetPrice(price);
            
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
            if (isContract.ValueBool)
            {
                _Vol = (int)_Vol;
            }
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
               return false;
            }
            if (_TradeSessions.MinSessionPrice == 0 || (_TradeSessions.MaxSessionPrice - _TradeSessions.MinSessionPrice) / _TradeSessions.MinSessionPrice < MaxStop.ValueDecimal / 100)
            {
                //return false;
            }
            
            if(Math.Abs(_TradeSessions.MaxSessionPrice -_tab.CandlesAll[_tab.CandlesAll.Count-1].Close)<
                Math.Abs(_TradeSessions.MinSessionPrice - _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close) 
                && TradeSide == Side.Buy
                && _TradeSessions.MaxSessionPrice > _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close)
            {
                return false;
            }
            if (Math.Abs(_TradeSessions.MaxSessionPrice - _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close) >
                Math.Abs(_TradeSessions.MinSessionPrice - _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close) 
                && TradeSide == Side.Sell
                 && _TradeSessions.MinSessionPrice < _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close)
            {
                return false;
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
            List<Position> openPositions = _tab.PositionsOpenAll;
            if (CanOpenPosition())
            {
                return;
            }


            for (int i = 0; i < openPositions.Count && candles.Count > 1; i++)
            {
                List<decimal> stop = GetTrailingStopPrice(openPositions[i]);
                foreach (var _st in stop)
                {
                    if(openPositions[i].EntryPrice == 0)
                    {
                        continue;
                    }
                    if(openPositions[i].Direction == Side.Buy && _st < openPositions[i].EntryPrice)
                    {
                        continue;
                    }
                    if(openPositions[i].Direction == Side.Sell && _st > openPositions[i].EntryPrice)
                    {
                        continue;
                    }
                    bool canClose = false;
                    decimal _profit = (_st - openPositions[i].EntryPrice) * 100 / openPositions[i].EntryPrice;
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
                        _tab.CloseAtTrailingStop(openPositions[i], _st, _st);
                    }
                }


            }

        }
        private List<decimal> GetTrailingStopPrice(Position position)
        {
            List<decimal> result = new List<decimal>();
            result.Add(mA.Values[mA.Values.Count - 1]);
            result.Add((position.EntryPrice + _tab.Trades[_tab.Trades.Count-1].Price) / 2);
            return result;
        }
        /// <summary>
        /// Найдем последний уровень
        /// </summary>
        /// <param name="position">Позиция</param>
        /// <returns></returns>
        private Decimal GetStop(Side side, decimal price)
        {
            // берем последние 6 свечек
            List<decimal> maxVolumes = Claster.Values.GetRange(Claster.Values.Count - 21, 20);
            maxVolumes.Sort((a, b) => decimal.Compare(a, b));
            if (side == Side.Buy)
            {
                List<decimal> lvl = maxVolumes.FindAll(x => x < price);
                if (lvl != null && lvl.Count > 1)
                {
                    return lvl[lvl.Count - 2] - Slipage;
                }
            }
            if (side == Side.Sell)
            {
                List<decimal> lvl = maxVolumes.FindAll(x => x > price);
                if (lvl != null && lvl.Count > 1)
                {
                    return lvl[1] + Slipage;
                }
            }
            /*
            if (side == Side.Buy)
            {
                List<ClasterData> lvls = Claster.data.FindAll(x => x.MaxData.Price < price);
                lvls.Sort((a, b) => decimal.Compare(a.MaxData.Price, b.MaxData.Price));
                if (lvls != null && lvls.Count > 1)
                {
                    return lvls[lvls.Count - 2].MaxData.Price;
                }
            }
            if (side == Side.Sell)
            {
                List<ClasterData> lvls = Claster.data.FindAll(x => x.MaxData.Price > price);
                lvls.Sort((a, b) => decimal.Compare(a.MaxData.Price, b.MaxData.Price));
                if (lvls != null && lvls.Count > 1)
                {
                    return lvls[1].MaxData.Price;
                }
            }
            */
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
        private void _tab_PositionOpeningSuccesEvent(Position obj)
        {

            //выставим новые стопы
            _tab.CloseAtTrailingStop(obj, LastStop, LastStop);
            if (UseSafe.ValueBool)
            {
                decimal fixPOs = 2 * obj.EntryPrice - LastStop;
                decimal vol = obj.OpenVolume/2;
                if (isContract.ValueBool)
                {
                    vol = (int)vol;
                }
                
                if (obj.Direction == Side.Buy)
                {
                    _tab.SellAtAcebergToPosition(obj, fixPOs, vol, 1);    
                }
                else
                {
                    _tab.BuyAtAcebergToPosition(obj, fixPOs, vol, 1);
                }
                
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
            DeltaStepCheck();
            ///Отрисовка линий
            PriceLevleLine.PaintLevleData(TabsSimple);
            //Определяем направление торговли по прошлой сессии
            LastSessionEndPrice = _TradeSessions.Values[_TradeSessions.Values.Count - 1];//_TradeSessions.LastSessionEndPrice;


            //LastSessionEndPrice = TradeSessions.LastSessionEndPrice(_tab.CandlesAll, candles[candles.Count - 1].TimeStart);
            if (LastSessionEndPrice > 0)
            {
                Side oldTradeSide = TradeSide;
                if (LastSessionEndPrice < candles[candles.Count - 1].Open)
                {
                    TradeSide = Side.Buy;
                }
                else
                {
                    TradeSide = Side.Sell;
                }
                if (oldTradeSide != TradeSide)
                {
                    _tab.SetNewLogMessage("Направление торговли " + TradeSide, LogMessageType.Signal);
                }
            }
            if (candles.Count > 1 && candles[candles.Count - 1].TimeStart.DayOfYear != candles[candles.Count - 2].TimeStart.DayOfYear)
            {
                RiskOnDay = 0;
            }

            for (int i = _tab.PositionsCloseAll.Count - 1; i >= 0; i--)
            {

                if (_tab.PositionsCloseAll[i].TimeClose.DayOfYear == candles[candles.Count - 1].TimeStart.DayOfYear)
                {
                    /*
                    if (_tab.PositionsCloseAll[i].ProfitOperationPersent < 0)
                    {
                        RiskOnDay += -1*_tab.PositionsCloseAll[i].ProfitPortfolioPersent;
                    }
                    */
                    RiskOnDay += _tab.PositionsCloseAll[i].ProfitPortfolioPersent;
                }
                else
                {
                    break;
                }
            }

            LogicClosePositions(candles);

            if (!ValidateParams())
            {
                return;
            }

            if (!CanOpenPosition())
            {
                return;
            }
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
