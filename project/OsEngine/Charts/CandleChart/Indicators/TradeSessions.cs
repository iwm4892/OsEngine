/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OsEngine.Entity;
using OsEngine.Indicators;

namespace OsEngine.Charts.CandleChart.Indicators
{
    /// <summary>
    ///  Индикатор торговой сессии
    /// </summary>
    public class TradeSessions:IIndicator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionTypes"></param>
        public TradeSessions(string uniqName, bool canDelete, List<SessionType> sessionTypes)
        {

            init();
            FillSessions(sessionTypes);
            Name = uniqName;
            CanDelete = canDelete;
            
            Load();
        }

        /// <summary>
        /// конструктор без параметров. Индикатор не будет сохраняться
        /// используется ТОЛЬКО для создания составных индикаторов
        /// не используйте его из слоя создания роботов!
        /// </summary>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public TradeSessions(string uniqName, bool canDelete)
        {
            Name = Guid.NewGuid().ToString();
            CanDelete = canDelete;
            init();
            Sessions = new List<TS>();
            foreach (TS s in SessionsAll)
            {
                Sessions.Add(s);
            }

        }
        public void FillSessions(List<SessionType> sessionTypes)
        {
            Sessions = new List<TS>();
            foreach (SessionType s in sessionTypes)
            {
                TS tS = SessionsAll.Find(x => x.SessionType == s);
                if (tS != null)
                {
                    Sessions.Add(tS);
                }
            }
            Sessions.Sort((a, b) => DateTime.Compare(a.Close, b.Close));

        }
        private void init()
        {
            if (SessionsAll == null)
            {
                SessionsAll = new List<TS>();
            }

            TS _ts = new TS();
            
            _ts.Name = "Азия";
            _ts.SessionType = SessionType.Asia;
            _ts.Open = new DateTime(1,1,1,3,0,0);
            _ts.Close = new DateTime(1,1,1,11,0,0);
            SessionsAll.Add(_ts);

            _ts = new TS();
            _ts.Name = "Европа";
            _ts.SessionType = SessionType.EU;
            _ts.Open = new DateTime(1, 1, 1, 9, 0, 0);
            _ts.Close = new DateTime(1, 1, 1, 17, 0, 0);
            SessionsAll.Add(_ts);

            _ts = new TS();
            _ts.Name = "Америка";
            _ts.SessionType = SessionType.USA;
            _ts.Open = new DateTime(1, 1, 1,16, 30, 0);
            _ts.Close = new DateTime(1, 1, 1, 23, 0, 0);
            SessionsAll.Add(_ts);
            
            
            _ts = new TS();
            _ts.Name = "Мосбиржа";
            _ts.SessionType = SessionType.RUS;
            _ts.Open = new DateTime(1, 1, 1, 10, 0, 0);
            _ts.Close = new DateTime(1, 1, 1, 19, 0, 0);
            SessionsAll.Add(_ts);

            _ts = new TS();
            _ts.Name = "Сутки";
            _ts.SessionType = SessionType.Day;
            _ts.Open = new DateTime(1, 1, 1, 0, 0, 0);
            _ts.Close = new DateTime(1, 1, 1, 23, 59, 59);
            SessionsAll.Add(_ts);

            color = Color.Blue;
            TypeIndicator = IndicatorChartPaintType.Line;
            PaintOn = true;


        }
        /// <summary>
        /// Описание торговой сессии
        /// </summary>
        public class TS
        {
            /// <summary>
            /// Название торговой сессии
            /// </summary>
            public string Name;
            /// <summary>
            /// Время открытия
            /// </summary>
            public DateTime Open;
            /// <summary>
            /// Время закрытия
            /// </summary>
            public DateTime Close;

            public SessionType SessionType;
            /// <summary>
            /// Кластера сессий
            /// </summary>
            public List<ClasterData> clasterDatas = new List<ClasterData>();
            /// <summary>
            /// Максимальные Объемы по дням
            /// </summary>
            public List<decimal> Values = new List<decimal>();
        }
        /// <summary>
        /// Перечисление видов сессий
        /// </summary>
        public enum SessionType
        {
            /// <summary>
            /// Америка
            /// </summary>
            USA,
            /// <summary>
            /// Европа
            /// </summary>
            EU,
            /// <summary>
            /// Азия
            /// </summary>
            Asia,
            /// <summary>
            /// Мосбиржа
            /// </summary>
            RUS,
            /// <summary>
            /// Без сессий (0-24)
            /// </summary>
            Day
        }
        public enum DayType
        {
            /// <summary>
            /// Тип дня не определен
            /// </summary>
            UnKnown,
            /// <summary>
            /// Нетрендовый день
            /// </summary>
            NontrendDay,
            /// <summary>
            /// Нормальный день
            /// </summary>
            NormalDay,
            /// <summary>
            /// Нормальное изменение нормального дня
            /// </summary>
            NormalVariationOfNormalDay,
            /// <summary>
            /// Трендовый день
            /// </summary>
            TrendDay,
            /// <summary>
            /// Нейтральный день
            /// </summary>
            NeutralDay
        }

        public List<TS> Sessions;
        private List<TS> SessionsAll;

        /// <summary>
        /// Цена закрытия прошлой сессии
        /// </summary>
        public decimal LastSessionEndPrice;
        /// <summary>
        /// Время окончания прошлой сессии
        /// </summary>
        public DateTime LastSessionEndDate;
        /// <summary>
        /// Текущая Сессия
        /// </summary>
        public TS SessionNow;
        /// <summary>
        /// Можно открывать сделки
        /// </summary>
        public bool CanTrade;
        /// <summary>
        /// Тип дня (для текушей сессии)
        /// </summary>
        public DayType TypeOfDay;
        /// <summary>
        /// Направления торговли
        /// </summary>
        public List<Side> TradeSide;

        /// <summary>
        /// Максимальный уровень по сесии
        /// </summary>
        public decimal MaxSessionPrice;
        /// <summary>
        /// Минимальный уровень по сессии
        /// </summary>
        public decimal MinSessionPrice;
        /// <summary>
        /// Цвет индикатора
        /// </summary>
        public Color color;
        /// <summary>
        /// все значения индикатора
        /// </summary>
        List<List<decimal>> IIndicator.ValuesToChart
        {
            get
            {
                List<List<decimal>> list = new List<List<decimal>>();
                list.Add(Values);
                return list;
            }
        }

        /// <summary>
        /// цвета для индикатора
        /// </summary>
        List<Color> IIndicator.Colors
        {
            get
            {
                List<Color> colors = new List<Color>();
                colors.Add(color);
                return colors;
            }

        }

        /// <summary>
        /// можно ли удалить индикатор с графика. Это нужно для того чтобы у роботов нельзя было удалить 
        /// индикаторы которые ему нужны в торговле
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// тип индикатора
        /// </summary>
        public IndicatorChartPaintType TypeIndicator
        { get; set; }

        /// <summary>
        /// имя серии данных на которой будет прорисовываться индикатор
        /// </summary>
        public string NameSeries
        { get; set; }

        /// <summary>
        /// имя области данных на которой будет прорисовываться индикатор
        /// </summary>
        public string NameArea
        { get; set; }

        /// <summary>
        /// объём
        /// </summary>
        public List<decimal> Values
        { get; set; }

        /// <summary>
        /// уникальное имя
        /// </summary>
        public string Name
        { get; set; }

        /// <summary>
        /// цвет растущего объёма
        /// </summary>
        public Color ColorUp
        { get; set; }

        /// <summary>
        /// цвет падающего объёма
        /// </summary>
        public Color ColorDown
        { get; set; }

        /// <summary>
        /// включена ли прорисовка индикатора на чарте
        /// </summary>
        public bool PaintOn
        { get; set; }
        /// <summary>
        /// Размер канала (максимум - минимум) за последние сутки
        /// </summary>
        public Decimal DayChanel;

       public List<Color> ColorSeries { get; set; }

        /// <summary>
        /// сохранить настройки в файл
        /// </summary>
        public void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    return;
                }
                using (StreamWriter writer = new StreamWriter(@"Engine\" + Name + @".txt", false))
                {
                    writer.WriteLine(PaintOn);
                    writer.Close();
                }
            }
            catch (Exception)
            {
                // отправить в лог
            }
        }

        /// <summary>
        /// загрузить настройки из файла
        /// </summary>
        public void Load()
        {
            if (!File.Exists(@"Engine\" + Name + @".txt"))
            {
                return;
            }
            try
            {
                using (StreamReader reader = new StreamReader(@"Engine\" + Name + @".txt"))
                {
                    PaintOn = Convert.ToBoolean(reader.ReadLine());
                    reader.Close();
                }
            }
            catch (Exception)
            {
                // отправить в лог
            }
        }

        /// <summary>
        /// удалить файл с настройками
        /// </summary>
        public void Delete()
        {
            if (File.Exists(@"Engine\" + Name + @".txt"))
            {
                File.Delete(@"Engine\" + Name + @".txt");
            }
        }

        /// <summary>
        /// удалить данные
        /// </summary>
        public void Clear()
        {
            if (Values != null)
            {
                Values.Clear();
            }
        }

        /// <summary>
        /// показать окно настроек
        /// </summary>
        public void ShowDialog()
        {

        }

        /// <summary>
        /// нужно перерисовать индикатор
        /// </summary>
        public event Action<IIndicator> NeadToReloadEvent;
        /// <summary>
        /// Проверить можно ли сейчас торговать
        /// </summary>
        /// <returns></returns>
        private bool GetCanTrade(List<Candle> candles, int i)
        {
            if (SessionNow.Name == null)
            {
                return false;
            }
            DateTime testDate = new DateTime(1, 1, 1, candles[i].TimeStart.Hour, candles[i].TimeStart.Minute, candles[i].TimeStart.Second);
            // не торгуем первые полтора часа сессии
            DateTime endOfStartSession = SessionNow.Open.AddHours(1).AddMinutes(30);
            if (testDate >= SessionNow.Open && testDate<= endOfStartSession)
            {
                TypeOfDay = DayType.UnKnown;
                return false;
            }
            // не торгуем последний час сесии
            if(testDate >= SessionNow.Close.AddHours(-1))
            {
                return false;
            }
            return true;
        }

        // вычисления
        /// <summary>
        /// Проверяем началась ли каянибудь торговая сессия на указанную дату
        /// </summary>
        /// <param name="date">дата для проверки</param>
        /// <returns></returns>
        public TS itsSessionStart(DateTime date)
        {
            TS ts =  Sessions.Find(x => x.Open == new DateTime(1, 1, 1, date.Hour, date.Minute, date.Second));
            return ts;
        }
        /// <summary>
        /// Получение даты окончания прошлой сесии
        /// </summary>
        /// <param name="date">дата для анализа</param>
        /// <returns></returns>
        public DateTime GetLastSessionEndDate(DateTime date)
        {
            if (SessionNow == null)
            {
                SessionNow = SessionOnTime(date);
            }
            if (SessionNow.Open==DateTime.MinValue)
            {
                // ошибка сессия не найдена
                return DateTime.MinValue;
            }
            DateTime testDate = new DateTime(date.Year, date.Month, date.Day, SessionNow.Close.Hour, SessionNow.Close.Minute, SessionNow.Close.Second);

            return testDate.AddDays(-1);
        }
        private Decimal GetLastSessionEndPrice(List<Candle> candles, DateTime date)
        {
            if (LastSessionEndDate == DateTime.MinValue)
            {
                LastSessionEndDate = GetLastSessionEndDate(date);
            }
           
            int ind = candles.FindIndex(x => x.TimeStart > LastSessionEndDate);
            if (ind > 0 && candles[ind - 1].TimeStart <= LastSessionEndDate)
            {
                return candles[ind].Open;
            }
            return 0;
        }
        private DayType GetDayType(List<Candle> candles, int i)
        {

            if(TypeOfDay == DayType.UnKnown)
            {
                DateTime OpenSessionStart = new DateTime(candles[i].TimeStart.Year, candles[i].TimeStart.Month, candles[i].TimeStart.Day, SessionNow.Open.Hour, SessionNow.Open.Minute, SessionNow.Open.Second);
                // Окончание открытия сессии
                DateTime EndOfStartSession = OpenSessionStart.AddHours(1).AddMinutes(30);
                    
                if(candles[i].TimeStart >= EndOfStartSession)
                {
                    List<Candle> StartSessionCandles = candles.FindAll(x=>x.TimeStart>= OpenSessionStart && x.TimeStart < EndOfStartSession);
                    if (StartSessionCandles != null && StartSessionCandles.Count > 2)
                    {
                    if (
                            (StartSessionCandles[0].Close< StartSessionCandles[1].Close
                            && StartSessionCandles[1].Close< StartSessionCandles[2].Close)
                            || (StartSessionCandles[0].Close > StartSessionCandles[1].Close
                            && StartSessionCandles[1].Close > StartSessionCandles[2].Close)
                            )

                        {
                            return DayType.TrendDay;
                        }
                    }
                }
                return TypeOfDay;
            }
            
            return DayType.UnKnown;
        }
        public List<Side> GetTradeSyde(List<Candle> candles, int i)
        {
            List<Side> result = new List<Side>();
            if (TypeOfDay == DayType.TrendDay)
            {
                if (candles[i].Close > LastSessionEndPrice)
                {
                    result.Add(Side.Buy);
                }
                else
                {
                    result.Add(Side.Sell);
                }
            }
            return result;
        }
        /// <summary>
        /// Получение текущей сесии
        /// </summary>
        /// <param name="date">дата для анализа</param>
        /// <returns></returns>
        public TS SessionOnTime(DateTime date)
        {
            TS result = new TS();
            DateTime testDateOpen;
            DateTime testDateClose;

            foreach (var ts in Sessions)
            {
                testDateOpen = new DateTime(date.Year, date.Month, date.Day, ts.Open.Hour, ts.Open.Minute, ts.Open.Second);
                testDateClose = new DateTime(date.Year, date.Month, date.Day, ts.Close.Hour, ts.Close.Minute, ts.Close.Second);
                if (testDateOpen <= date && testDateClose > date)
                {
                    result = ts;
                }
            }
            return result;
        }

        /// <summary>
        /// прогрузить индикатор свечками
        /// </summary>
        public void Process(List<Candle> candles)
        {
            
            if (Values != null &&
                           Values.Count + 1 == candles.Count)
            {
                ProcessOneCandle(candles);
            }
            else if (Values != null &&
                Values.Count == candles.Count)
            {
                ProcessLastCanlde(candles);
            }
            else
            {
                ProcessAllCandle(candles);
            }
        }

        /// <summary>
        /// прогрузить только последнюю свечку
        /// </summary>
        private void ProcessOneCandle(List<Candle> candles)
        {
            if (Values == null)
            {
                Values = new List<decimal>();
                ColorSeries = new List<Color>();
            }
            UpdateDate(candles, candles.Count - 1);
            Values.Add(GetValue(candles, candles.Count - 1));
            updateNullValue();
            ColorSeries.Add(color);

        }

        /// <summary>
        /// прогрузить все свечи
        /// </summary>
        private void ProcessAllCandle(List<Candle> candles)
        {
            Values = new List<decimal>();
            ColorSeries = new List<Color>();

            for(int i = 0; i < candles.Count; i++)
            {
                UpdateDate(candles, i);
                Values.Add(GetValue(candles, i));
                updateNullValue();
                ColorSeries.Add(color);
            }
        }

        /// <summary>
        /// перегрузить последнюю свечу
        /// </summary>
        private void ProcessLastCanlde(List<Candle> candles)
        {
            UpdateDate(candles, candles.Count - 1);
            Values[Values.Count - 1] = GetValue(candles, candles.Count - 1);
            updateNullValue();
        }
        private void updateNullValue()
        {
            if (Values!=null && Values.Count > 1 && Values[Values.Count - 1] == 0)
            {
                Values[Values.Count - 1] = Values[Values.Count - 2];
            }
        }
        private decimal GetValue(List<Candle> candles, int i)
        {
            SessionNow = SessionOnTime(candles[i].TimeStart);
            if (SessionNow != null && SessionNow.Values.Count>1)
            {
                return SessionNow.Values[SessionNow.Values.Count - 2];
            }
            return 0;
        }
        private void UpdateSessions(List<Candle> candles, int i)
        {
            TS ns = itsSessionStart(candles[i].TimeStart);
            if(ns!=null)
            {
                if (ns.Values == null)
                {
                    ns.Values = new List<decimal>();
                }
                ns.Values.Add(candles[i].ClasterData.MaxData.Price);
                /*
                if (ns.clasterDatas.Count > 0)
                {
                    ns.Values.Add(ns.clasterDatas[ns.clasterDatas.Count - 1].MaxData.Price);
                    ns.clasterDatas[ns.clasterDatas.Count - 1] = null;
                    ns.clasterDatas.Add(new ClasterData());
                }
                */
            }
            foreach (var ts in Sessions)
            {
                DateTime date = candles[i].TimeStart;
                DateTime testDateOpen = new DateTime(date.Year, date.Month, date.Day, ts.Open.Hour, ts.Open.Minute, ts.Open.Second);
                DateTime testDateClose = new DateTime(date.Year, date.Month, date.Day, ts.Close.Hour, ts.Close.Minute, ts.Close.Second);

                if (date >= testDateOpen && date < testDateClose)
                {
                    /*
                    if (ts.clasterDatas.Count == 0)
                    {
                        ts.clasterDatas.Add(new ClasterData());
                    }
                    */
                    if (ts.Values.Count == 0)
                    {
                        ts.Values.Add(0);
                    }
                 //   ts.clasterDatas[ts.clasterDatas.Count - 1].update(candles[i].Trades);
                 //   ts.Values[ts.Values.Count - 1] = ts.clasterDatas[ts.clasterDatas.Count - 1].MaxData.Price;
                    ts.Values[ts.Values.Count - 1] = candles[i].ClasterData.MaxData.Price;
                }
            }
        }
        private void UpdateDate(List<Candle> candles,int i)
        {
            UpdateSessions(candles, i);
            /*
            SessionNow = SessionOnTime(candles[i].TimeStart);
            CanTrade = GetCanTrade(candles, i);
            TypeOfDay = GetDayType(candles, i);
            LastSessionEndDate = GetLastSessionEndDate(candles[i].TimeStart);
            LastSessionEndPrice = GetLastSessionEndPrice(candles, candles[i].TimeStart);
            TradeSide = GetTradeSyde(candles, i);
            */
            UpdateMinMax(candles, i);
        }
        private void UpdateMinMax(List<Candle> candles, int i)
        {
            if (i > 0)
            {
                MaxSessionPrice = 0;
                MinSessionPrice = decimal.MaxValue;

                DateTime endDate = candles[i].TimeStart;
                DateTime startDate = endDate.AddDays(-1);
                List<Candle> analizCandles = candles.FindAll(o=>o.TimeStart>=startDate && o.TimeStart <= endDate);
                foreach(var candle in analizCandles)
                {
                    if (candle.ClasterData.MaxData.Price > MaxSessionPrice)
                    {
                        MaxSessionPrice = candle.ClasterData.MaxData.Price;
                    }
                    if (candle.ClasterData.MaxData.Price < MinSessionPrice)
                    {
                        MinSessionPrice = candle.ClasterData.MaxData.Price;
                    }

                }
                DayChanel = MaxSessionPrice - MinSessionPrice;
                /*
                if (candles[i].TimeStart.Date != candles[i - 1].TimeStart.Date)
                {
                    MaxSessionPrice = candles[i].ClasterData.MaxData.Price;
                    MinSessionPrice = candles[i].ClasterData.MaxData.Price;
                }

                if (candles[i].ClasterData.MaxData.Price > MaxSessionPrice)
                {
                    MaxSessionPrice = candles[i].ClasterData.MaxData.Price;
                }
                if (candles[i].ClasterData.MaxData.Price < MinSessionPrice)
                {
                    MinSessionPrice = candles[i].ClasterData.MaxData.Price;
                }
                */

            }
            /*
            if (itsSessionStart(candles[i].TimeStart))
            {
                MaxSessionPrice = LastSessionEndPrice;
                MinSessionPrice = LastSessionEndPrice;
            }
            else
            {
                if (candles[i].ClasterData.MaxData.Price > MaxSessionPrice)
                {
                    MaxSessionPrice = candles[i].ClasterData.MaxData.Price;
                }
                if (candles[i].ClasterData.MaxData.Price < MinSessionPrice)
                {
                    MinSessionPrice = candles[i].ClasterData.MaxData.Price;
                }

            }
            */
        }

    }
}
