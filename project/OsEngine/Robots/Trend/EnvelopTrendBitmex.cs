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
using System.Net;
using System.Net.Sockets;
using System.Text;
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
            this.DeleteEvent += Strategy_DeleteEvent;

            Regime = CreateParameter("Regime", "Off", new[] { "Off", "On", "OnlyClosePosition", "OnlyShort", "OnlyLong" });
            RegimeML = CreateParameter("RegimeML", "Off", new[] { "Off", "Parser", "Client" });

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
            _name = name;

            #region ML Region
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            Load();
            if (RegimeML.ValueString != "Off") // создаем файл(ы) если несуществуют и очищаем если существуют
            {
                initML();
            }
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            #endregion
        }


        private void initML()
        {
            for (int i = 0; i < 3; i++) // инициализируем списки индикаторов снимка состояния рынка
            {
                bearsPowersList.Add((BearsPower)_tab.CreateCandleIndicator(new BearsPower(_name + "BearsPower" + i.ToString(), false), "Prime"));
                bearsPowersList[i].PaintOn = false;
                bearsPowersList[i].Period = 10 + i * 10;
                bearsPowersList[i].Save();

                bullsPowersList.Add((BullsPower)_tab.CreateCandleIndicator(new BullsPower(_name + "BullsPower" + i.ToString(), false), "Prime"));
                bullsPowersList[i].PaintOn = false;
                bullsPowersList[i].Period = 10 + i * 10;
                bullsPowersList[i].Save();
                    
                atrList.Add((Atr)_tab.CreateCandleIndicator(new Atr(_name + "Atr" + i.ToString(), false), "Prime"));
                atrList[i].PaintOn = false;
                atrList[i].Lenght = 10 + i * 10;
                atrList[i].Save();
            }
        }
        private void DisposeML()
        {
            foreach(var ind in bearsPowersList)
            {
                _tab.Indicators.Remove(ind);
            }
            foreach (var ind in bullsPowersList)
            {
                _tab.Indicators.Remove(ind);
            }
            foreach (var ind in atrList)
            {
                _tab.Indicators.Remove(ind);
            }

            bearsPowersList.Clear();
            bullsPowersList.Clear();
            atrList.Clear();
        }
        private void EnvelopTrendBitmex_ParametrsChangeByUser()
        {
            _envelop.Deviation = EnvelopDeviation.ValueDecimal;
            _envelop.MovingAverage.Lenght = EnvelopMovingLength.ValueInt;
            _envelop.Save();

            if (RegimeML.ValueString != "Off")
            {
                initML();
            }
            else
            {
                DisposeML();
            }
        }
        /// <summary>
        /// load settings
        /// загрузить настройки
        /// </summary>
        private void Load()
        {
            #region ML Region
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + $@"\DataSource\")) // создаем папку для моделей если не существует
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Personal) + $@"\DataSource\");

            fname_data_firstmulti = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + $@"\DataSource\data-multi.csv";

            if (!File.Exists(fname_data_firstmulti))
            {
                File.Create(fname_data_firstmulti);
            }
            else if (File.Exists(fname_data_firstmulti))
            {
                File.Delete(fname_data_firstmulti);
                File.Create(fname_data_firstmulti);
            }
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            #endregion
        }
        /// <summary>
        /// delete save file
        /// удаление файла с сохранением
        /// </summary>
        private void Strategy_DeleteEvent()
        {
            if (File.Exists(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt"))
            {
                File.Delete(@"Engine\" + NameStrategyUniq + @"SettingsBot.txt");
            }
        }
        private void _tab_PositionClosingSuccesEvent(Position obj)
        {
            Filltabel_multi(obj);
        }
        private string _name;
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

        #region ML Region
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        private string delim = ";"; // разделитель значений индикаторов для записи в csv

        private string fname_data_firstmulti; // файл в который будут сохраняться значения

        private string IpAdress = "127.0.0.1"; // ip сокета

        private int Port = 8020; // порт сокета

        private List<Atr> atrList = new List<Atr>(); // списки индикаторов снимка состояния рынка
        private List<BearsPower> bearsPowersList = new List<BearsPower>(); // списки индикаторов снимка состояния рынка
        private List<BullsPower> bullsPowersList = new List<BullsPower>(); // списки индикаторов снимка состояния рынка

        private Dictionary<string, string> dealsDataDictionary = new Dictionary<string, string>(); // промежуточная база данных в виде словаря
        private List<string> tabel_multi = new List<string>(); // конечная база данных под запись в файл
        private List<string> ValueList = new List<string>(); // список значений индикаторов
        private int dealID;
        /// <summary>
        /// Режим МЛ
        /// </summary>
        public StrategyParameterString RegimeML;
        
        /// <summary>
        /// Перечень открываемых позиций
        /// </summary>
        private List<pos> stopPositions = new List<pos>();
        private struct pos
        {
            public Side Side;
            public long dealID;
        }
        private string CollectData()
        {
            ValueList.Clear();
            if (RegimeML.ValueString != "Off")
            {
                for (int k = 0; k < 3; k++)
                {
                    ValueList.Add(bearsPowersList[k].Values[bearsPowersList[k].Values.Count - 1].ToString(CultureInfo.InvariantCulture));
                    ValueList.Add(bullsPowersList[k].Values[bullsPowersList[k].Values.Count - 1].ToString(CultureInfo.InvariantCulture));
                    ValueList.Add(atrList[k].Values[atrList[k].Values.Count - 1].ToString(CultureInfo.InvariantCulture));
                }

                return string.Join(delim, ValueList.ToArray());
            }
            else
                return "null";
        }
        IPEndPoint ipPoint;
        public string WebClient(string message)
        {
            try
            {
                ipPoint = new IPEndPoint(IPAddress.Parse(IpAdress), Port);
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                // подключаемся к удаленному хосту
                socket.Connect(ipPoint);
                byte[] data = Encoding.Unicode.GetBytes(message);
                socket.Send(data);

                // получаем ответ
                data = new byte[1024];
                // буфер для ответа
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                // количество полученных байт
                do
                {
                    bytes = socket.Receive(data, data.Length, 0);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                } while (socket.Available > 0);

                // закрываем сокет
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
                //Print(builder.ToString());
                return builder.ToString();

            }
            catch
            {
                return "";
            }
        }

        private void Filltabel_multi(Position position)
        {
            if (position != null)
            {
                if (position.ProfitPortfolioPersent > 0 && position.Direction == Side.Buy)
                {
                    pos p = stopPositions.FindLast(x => x.Side == Side.Buy);
                    if (dealsDataDictionary.ContainsKey(p.dealID.ToString()))
                    {
                        tabel_multi.Add("0" + delim + dealsDataDictionary[p.dealID.ToString()]);
                        dealsDataDictionary.Remove(p.dealID.ToString());
                    }
                }
                else if (position.ProfitPortfolioPersent < 0 && position.Direction == Side.Buy)
                {
                    pos p = stopPositions.FindLast(x => x.Side == Side.Buy);
                    if (dealsDataDictionary.ContainsKey(p.dealID.ToString()))
                    {
                        tabel_multi.Add("1" + delim + dealsDataDictionary[p.dealID.ToString()]);
                        dealsDataDictionary.Remove(p.dealID.ToString());
                    }
                }
                else if (position.ProfitPortfolioPersent > 0 && position.Direction == Side.Sell)
                {
                    pos p = stopPositions.FindLast(x => x.Side == Side.Sell);
                    if (dealsDataDictionary.ContainsKey(p.dealID.ToString()))
                    {
                        tabel_multi.Add("3" + delim + dealsDataDictionary[p.dealID.ToString()]);
                        dealsDataDictionary.Remove(p.dealID.ToString());
                    }
                }
                else if (position.ProfitPortfolioPersent < 0 && position.Direction == Side.Sell)
                {
                    pos p = stopPositions.FindLast(x => x.Side == Side.Sell);
                    if (dealsDataDictionary.ContainsKey(p.dealID.ToString()))
                    {
                        tabel_multi.Add("4" + delim + dealsDataDictionary[p.dealID.ToString()]);
                        dealsDataDictionary.Remove(p.dealID.ToString());
                    }
                }
            }
            else
            {
                foreach(var el in stopPositions)
                {
                    if(el.Side == Side.Buy && dealsDataDictionary.ContainsKey(el.dealID.ToString()))
                    {
                        tabel_multi.Add("1" + delim + dealsDataDictionary[el.dealID.ToString()]);
                        dealsDataDictionary.Remove(el.dealID.ToString());
                    }
                    if (el.Side == Side.Sell && dealsDataDictionary.ContainsKey(el.dealID.ToString()))
                    {
                        tabel_multi.Add("4" + delim + dealsDataDictionary[el.dealID.ToString()]);
                        dealsDataDictionary.Remove(el.dealID.ToString());
                    }
                }
            }
            stopPositions.Clear();
        }
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        #endregion


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
            if (position.Direction == Side.Buy)
            {
                _tab.CloseAtProfit(position, position.EntryPrice + 2 * position.EntryPrice * TrailStop.ValueDecimal / 100, position.EntryPrice + 2 * position.EntryPrice * TrailStop.ValueDecimal / 100);
            }
            else
            {
                _tab.CloseAtProfit(position, position.EntryPrice - 2 * position.EntryPrice * TrailStop.ValueDecimal / 100, position.EntryPrice - 2 * position.EntryPrice * TrailStop.ValueDecimal / 100);

            }

        }
        private bool ValidateParams()
        {
            #region ML Region
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

            if (RegimeML.ValueString != "Off")
            {
                for (int i = 0; i < 3; i++)
                {
                    if (atrList[i].Values.Count < atrList[i].Lenght * 2)
                        return false;
                    if (bearsPowersList[i].Values.Count < bearsPowersList[i].Period * 2)
                        return false;
                    if (bullsPowersList[i].Values.Count < bullsPowersList[i].Period * 2)
                        return false;
                }
            }

            if (RegimeML.ValueString == "Parser")
            {
                if (tabel_multi.Count >= 200)
                {
                    // tabel_multi.ForEach(x => x = x.Replace(",", ".")); 
                    List<string> newTable = new List<string>(); // эта конструкция меняет разделитель на нужный
                    foreach (string str in tabel_multi)
                    {
                        newTable.Add(str.Replace(",", "."));
                    }

                    File.AppendAllLines(fname_data_firstmulti, newTable);
                    tabel_multi.Clear();
                }
            }

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
            #endregion

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
            if(stopPositions.Count != 0)
            {
                Filltabel_multi(null);
            }
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
                if (CanBuy())
                {
                    decimal priceEnter = _lastUp;
                    _tab.BuyAtStopMarket(GetVolume(Side.Buy), priceEnter + Slippage.ValueInt, priceEnter, StopActivateType.HigherOrEqual, 1);
                    if(RegimeML.ValueString != "Client")
                    {
                        dealID++;
                        stopPositions.Add(new pos{ Side = Side.Buy, dealID = dealID});
                        dealsDataDictionary.Add(dealID.ToString(), CollectData()); // кидаем снимок в предварительную базу
                    }
                }

                // Short
                if (CanSell())
                {
                    decimal priceEnter = _lastDown;
                    _tab.SellAtStopMarket(GetVolume(Side.Sell), priceEnter - Slippage.ValueInt, priceEnter, StopActivateType.LowerOrEqyal, 1);
                    if (RegimeML.ValueString != "Client")
                    {
                        dealID++;
                        stopPositions.Add(new pos{ Side = Side.Sell, dealID = dealID});
                        dealsDataDictionary.Add(dealID.ToString(), CollectData()); // кидаем снимок в предварительную базу
                    }

                }
            }
        }
        private bool CanBuy()
        {
            
            if (Regime.ValueString == "OnlyShort")
            {
                return false;
            }
            if (FastMA.Values[FastMA.Values.Count - 1] < SlowMA.Values[SlowMA.Values.Count - 1])
            {
                return false;
            }
            if(RegimeML.ValueString == "Client") 
            {
                string answer = WebClient(CollectData() + delim + "multi");
                if (answer != "0")
                {
                    return false;
                }
            }
            return true;
        }
        private bool CanSell()
        {

            if (Regime.ValueString == "OnlyLong")
            {
                return false;
            }
            if (FastMA.Values[FastMA.Values.Count - 1] > SlowMA.Values[SlowMA.Values.Count - 1])
            {
                return false;
            }
            if (RegimeML.ValueString == "Client")
            {
                string answer = WebClient(CollectData() + delim + "multi");
                if (answer != "3")
                {
                    return false;
                }
            }
            return true;

            return true;
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
