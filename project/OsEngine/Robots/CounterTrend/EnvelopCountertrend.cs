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
using System.Threading;
using System.Linq;
using OsEngine.OsTrader;

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
            _tab.PositionClosingSuccesEvent += _tab_PositionClosingSuccesEvent;
            this.ParametrsChangeByUser += EnvelopCountertrend_ParametrsChangeByUser;


            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyClosePosition", "OnlyShort", "OnlyLong" });
            Slippage = CreateParameter("Slippage", 0, 0, 20, 1);
            EnvelopDeviation = CreateParameter("Envelop Deviation", 0.3m, 5m, 10, 0.3m);
            EnvelopMovingLength = CreateParameter("Envelop Moving Length", 10, 5, 200, 5);
            

            leverage = CreateParameter("Маржинальное плечо", 1m, 1m, 10, 0.1m);
            DepoCurrency = CreateParameter("DepoCurrency", "Currency2", new[] { "Currency1", "Currency2" });
            isContract = CreateParameter("Торгуем контрактами", false);
            MaxStop = CreateParameter("MaxStop", 1, 25, 30, 0.1m);
            SmaLength = CreateParameter("SmaLength", 10, 5, 150, 2);
            VolumeDecimals = CreateParameter("Volume Decimals", 0, 0, 20, 1);

            MinVolume = CreateParameter("MinVolume", 1, 1, 10000, 0.0001m);
            MaxPosition = CreateParameter("Макс. открытых позиций", 1, 1, 10, 1);

            _sma = IndicatorsFactory.CreateIndicatorByName("Sma", name + "Moving", false);
            _sma = (Aindicator)_tab.CreateCandleIndicator(_sma, "Prime");
            _sma.ParametersDigit[0].Value = SmaLength.ValueInt;
            _sma.Save();

            _envelop = new Envelops(name + "Envelop", false);
            _envelop = (Envelops)_tab.CreateCandleIndicator(_envelop, "Prime");
            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;
            _envelop.Save();

            DeleteEvent += Strategy_DeleteEvent;
            
            Thread worker = new Thread(Logic);
            worker.IsBackground = true;
            worker.Start();


        }
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
            _isDisposed = true;
        }
        private void _tab_PositionClosingSuccesEvent(Position obj)
        {
            //_tab.CloseAllOrderInSystem();
            /*
            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                if (openPositions[i].State == PositionStateType.Opening)
                {
                    _tab.GetJournal().DeletePosition(openPositions[i]);
                }

            }
            */
        }
        private void EnvelopCountertrend_ParametrsChangeByUser()
        {
            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;
            _envelop.Save();
            _envelop.Reload();

            _sma.ParametersDigit[0].Value = SmaLength.ValueInt;
            _sma.Save();
            _sma.Reload();

        }

        // public settings / настройки публичные

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
        /// regime
        /// режим работы
        /// </summary>
        public StrategyParameterString Regime;

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
        /// <summary>
        /// Количество знаков после запятой в объеме
        /// </summary>
        public StrategyParameterInt VolumeDecimals;
        /// <summary>
        /// Минимальный размер ордерар
        /// </summary>
        private StrategyParameterDecimal MinVolume;

        private decimal _lastUp;
        private decimal _lastDown;
        private decimal _lastMa;

        public StrategyParameterInt SmaLength;
        // indicators / индикаторы

        private Envelops _envelop;
        /// <summary>
        /// Машка для профита
        /// </summary>
        private Aindicator _sma;
        private bool _isDisposed;
        /// <summary>
        /// Максимальное количество одновременно открытых позиций
        /// </summary>
        private StrategyParameterInt MaxPosition;

        // trade logic
        private void Logic()
        {

            while (true)
            {
                Thread.Sleep(5 * 1000);
                if (!_tab.IsConnected) continue;
                if (_tab.Connector.MyServer.ServerType == ServerType.Tester ||
                    _tab.Connector.MyServer.ServerType == ServerType.Optimizer)
                {
                    return;
                }
                

                if (_isDisposed)
                {
                    return;
                }

                if (Regime.ValueString == "Off")
                {
                    continue;
                }

                if (_sma.DataSeries[0].Values == null ||
                    _sma.ParametersDigit[0].Value + 3 > _sma.DataSeries[0].Values.Count)
                {
                    continue;
                }
                
                if (_lastMa == 0 || _lastUp == 0 || _lastDown == 0)
                {
                    _envelop.Process(_tab.CandlesAll);
                    _lastUp = _envelop.ValuesUp[_envelop.ValuesUp.Count - 1];
                    _lastDown = _envelop.ValuesDown[_envelop.ValuesDown.Count - 1];
                    _lastMa = _sma.DataSeries[0].Values[_sma.DataSeries[0].Values.Count - 1];
                }
                if (_lastUp == 0 || _lastDown == 0)
                {
                    continue;
                }
                if(_tab.CandlesAll == null || _tab.CandlesAll.Count == 0)
                {
                    continue;
                }

                decimal spread = GetSpread();
                decimal minspread = GetMinSpread();
                
                if (spread > minspread)
                {
                    CanselAllOrders();
                }
                if (spread <= minspread)
                {
                    List<Position> positions = _tab.PositionsOpenAll;
                    if (positions == null || positions.Count == 0)
                    {
                        LogicOpenPosition(_tab.CandlesAll);
                    }
                }
            }
        }
        private decimal GetMinSpread()
        {
            decimal minspread = 0.4m;
            if (_tab.Connector.MyServer.ServerType == ServerType.BitMex)
            {
                minspread = 0.7m;
            }
            return minspread;
        }
        public decimal GetSpread()
        {
            decimal _lastprice = _tab.CandlesAll[_tab.CandlesAll.Count - 1].Close;
            decimal spread = 0;
            decimal spreadAver = (_lastUp + _lastDown) / 2;
            decimal spreadEnv = (_lastUp - _lastDown) / 2;
            
            if (_lastprice > spreadAver)
            {
                spread = (_lastUp - _lastprice) / spreadEnv;
            }
            else
            {
                spread = (_lastprice - _lastDown) / spreadEnv;
            }

            return spread;
        }
        private void _tab_PositionOpeningSuccesEvent(Position position)
        {
            
            
            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                
                decimal stop = GetStop(openPositions[i].Direction);
                _tab.CloseAtStop(openPositions[i], stop, stop);
                if (_tab.Connector.MyServer.ServerType != ServerType.Tester &&
                _tab.Connector.MyServer.ServerType != ServerType.Optimizer)
                {
                    _tab.CloseAtProfit(openPositions[i], _lastMa, _lastMa);
                }

                //_tab.CloseAtProfit(openPositions[i],_lastMa,_lastMa);
                /*
                if (openPositions[i].Direction == Side.Buy)
                {
                    _tab.SellAtStopCancel();
                }
                else
                {
                    _tab.BuyAtStopCancel();
                }
                */
                /*
                      if (openPositions[i].OpenOrders.Count<3 && openPositions[i].OpenOrders[openPositions[i].OpenOrders.Count-1].State == OrderStateType.Done)
                      {
                          if (openPositions[i].Direction == Side.Buy)
                          {
                              _tab.BuyAtLimitToPosition(openPositions[i], openPositions[i].EntryPrice - openPositions[i].EntryPrice * 0.02m, openPositions[i].OpenVolume);
                          }
                          else
                          {
                              _tab.SellAtLimitToPosition(openPositions[i], openPositions[i].EntryPrice + openPositions[i].EntryPrice * 0.02m, openPositions[i].OpenVolume);

                          }
                          _canGrid = false;
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
            _lastUp = _envelop.ValuesUp[_envelop.ValuesUp.Count - 1];
            _lastDown = _envelop.ValuesDown[_envelop.ValuesDown.Count - 1];
            _lastMa = _sma.DataSeries[0].Values[_sma.DataSeries[0].Values.Count - 1];
            if (_lastUp==0|| _lastDown == 0)
            {
                return;
            }
            CanselAllOrders();
            List<Position> positions = _tab.PositionsOpenAll;

            if(positions.Count != 0)
            {
                LogicClosePosition();
            }
 
            if (positions == null || positions.Count == 0)
            {
                if (_tab.Connector.MyServer.ServerType == ServerType.Tester ||
                    _tab.Connector.MyServer.ServerType == ServerType.Optimizer)
                {
                    decimal spread = GetSpread();
                    decimal minspread = GetMinSpread();
                    if (spread <= minspread)
                    {
                        LogicOpenPosition(candles);
                    }
                }
            }
        }
        private void CanselAllOrders()
        {
            List<Position> openPositions = _tab.PositionsOpenAll;

            Position[] poses = openPositions.ToArray();

            for (int i = 0; poses != null && i < poses.Length; i++)
            {
                if(poses[i].State == PositionStateType.ClosingFail){
                    poses[i].State = PositionStateType.Open;
                }
                if (poses[i].State == PositionStateType.Opening || poses[i].State == PositionStateType.OpeningFail)
                {
                    _tab.CloseAllOrderToPosition(poses[i]);
                    _tab.GetJournal().DeletePosition(poses[i]);
                }
            }
        }

        private void LogicClosePosition()
        {
            List<Position> openPositions = _tab.PositionsOpenAll;
            for (int i = 0; openPositions != null && i < openPositions.Count; i++)
            {
                _tab.CloseAtProfit(openPositions[i], _lastMa, _lastMa);
            }
            
        }
        private void LogicOpenPosition(List<Candle> candles)
        {
           
            if(_lastMa<_lastDown || _lastMa > _lastUp)
            {
                return;
            }
            if (Regime.ValueString == "OnlyClosePosition")
            {
                return;
            }
            List<Position> openPositions = _tab.PositionsOpenAll;
            if (openPositions == null || openPositions.Count == 0)
            {
                // long
                if (Regime.ValueString != "OnlyShort" && candles.Last().Close <= (_lastUp+_lastDown)/2)
                {
                    if (_tab.Connector.MyServer.ServerType != ServerType.Tester && _tab.Connector.MyServer.ServerType != ServerType.Optimizer) { Thread.Sleep(1000); }
                    decimal vol = GetVolume(Side.Buy);
                    if (vol > MinVolume.ValueDecimal)
                    {
                        _tab.BuyAtLimit(vol, _lastDown);
                    }
                    //_tab.BuyAtLimit(GetVolume(Side.Buy), _lastDown - _lastDown * EnvelopDeviation.ValueDecimal/100);
                    //   _tab.BuyAtStop(GetVolume(Side.Buy), _lastDown, _lastDown, StopActivateType.LowerOrEqyal);
                    //    _tab.BuyAtStop(GetVolume(Side.Buy), _lastDown   - _lastDown*0.02m, _lastDown - _lastDown * 0.02m, StopActivateType.LowerOrEqyal);
                    //    _tab.BuyAtStop(GetVolume(Side.Buy), _lastDown - _lastDown * 0.04m, _lastDown - _lastDown * 0.04m, StopActivateType.LowerOrEqyal);

                }

                // Short
                if (Regime.ValueString != "OnlyLong" && candles.Last().Close >= (_lastUp + _lastDown) / 2)
                {
                    if (_tab.Connector.MyServer.ServerType != ServerType.Tester && _tab.Connector.MyServer.ServerType != ServerType.Optimizer) { Thread.Sleep(1000); }
                    decimal vol = GetVolume(Side.Sell);
                    if (vol > MinVolume.ValueDecimal)
                    {
                        _tab.SellAtLimit(vol, _lastUp);
                    }
                    //_tab.SellAtLimit(GetVolume(Side.Sell), _lastUp + _lastUp * EnvelopDeviation.ValueDecimal / 100);
                    //    _tab.SellAtStop(GetVolume(Side.Sell), _lastUp, _lastUp, StopActivateType.HigherOrEqual);
                    //    _tab.SellAtStop(GetVolume(Side.Sell), _lastUp +_lastUp * 0.02m, _lastUp + _lastUp * 0.02m, StopActivateType.HigherOrEqual);
                    //    _tab.SellAtStop(GetVolume(Side.Sell), _lastUp + _lastUp * 0.04m, _lastUp + _lastUp * 0.04m, StopActivateType.HigherOrEqual);
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
            decimal VollAll = (GetBalance()) / GetPrice(priceEnter);

            decimal StopSize = Math.Abs((Laststop - priceEnter) / priceEnter);

            if (StopSize <= 0)
            {
                return 0;
            }
            decimal _Vol = (MaxStop.ValueDecimal / 100) * VollAll / (StopSize);
            if (_Vol > VollAll)
            {
                _Vol = VollAll;
            }
                
            //int _maxPositions = (int)(1 / leverage.ValueDecimal);
            int _posCountNaw = GetOpenPositionsCount();

            if (MaxPosition.ValueInt > 1)
            {
                
                if(_posCountNaw >= MaxPosition.ValueInt)
                {
                    return 0;
                }
                _Vol = _Vol / (MaxPosition.ValueInt - _posCountNaw);
            }
            //else
            //{
                _Vol = _Vol * leverage.ValueDecimal;
            //}
            _Vol = GetVol(_Vol);
            return _Vol;
        }
        private int GetOpenPositionsCount()
        {
            int result = 0;
            foreach(var panel in OsTraderMaster.Master._panelsArray)
            {
                if (panel.IsConnected && panel.GetNameStrategyType() == this.GetNameStrategyType())
                {
                    foreach(var tab in panel.TabsSimple)
                    {
                        if(tab.Connector.ServerType == _tab.Connector.ServerType 
                            && tab.Connector.PortfolioName == _tab.Connector.PortfolioName)
                        {
                            if (tab.PositionsOpenAll != null)
                            {
                                result += tab.PositionsOpenAll.Count;
                            }
                        }
                    }
                }
            }
            return result;
        }
        private decimal GetStop(Side side)
        {
            if(side == Side.Buy)
            {
                return _lastDown - _lastDown * MaxStop.ValueDecimal/100; 
            }
            else
            {
                return _lastUp + _lastUp * MaxStop.ValueDecimal / 100;
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
                decimal _v = (long)(v * (int)Math.Pow(10, VolumeDecimals.ValueInt));
                return _v / (decimal)Math.Pow(10, VolumeDecimals.ValueInt);
                //return Math.Round(v, VolumeDecimals.ValueInt, MidpointRounding.AwayFromZero);
            }
        }
        private decimal GetBalance()
        {
            if (_tab.Connector.MyServer.ServerType == ServerType.Tester ||
                _tab.Connector.MyServer.ServerType == ServerType.Optimizer)
            {
                if(_tab.Portfolio.ValueBlocked != 0)
                {
                    Console.WriteLine("Заблокировано "+ _tab.Portfolio.ValueBlocked);
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
            if(_tab.Connector.MyServer.ServerType == ServerType.Binance)
            {
                List<PositionOnBoard> bal = _tab.Portfolio.GetPositionOnBoard();
                if (bal != null && bal.Count > 0)
                {
                    PositionOnBoard b = bal.FindLast(x => x.SecurityNameCode == _tab.Securiti.NameClass);
                    if (b != null)
                    {
                        if(_tab.Connector.PortfolioName == "BinanceMargin" && b.ValueCurrent == 0 && b.ValueBlocked != 0)
                        {
                            int _posCountNaw = GetOpenPositionsCount();
                            if (_posCountNaw >= 3) //тк 3е плечо на маржиналке то ограничимся 3мя позициями
                            {
                                return 0;
                            }

                            return b.ValueBlocked / _posCountNaw;
                        }
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
        public class EnvelopCountertrendLocker
        {
            public struct robot
            {
                string Security;
                string Regime;
                decimal spread;
                decimal lastprise;
                decimal _lastUp;
                decimal _lastDown;
            }
            public List<robot> robots;
        }
    }
}
