/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OsEngine.Entity;

namespace OsEngine.Charts.CandleChart.Indicators
{
    /// <summary>
    ///  Индикатор торговой сессии
    /// </summary>
    public class TradeSessions:IIndicatorCandle
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionTypes"></param>
        public TradeSessions(string uniqName, bool canDelete, List<SessionType> sessionTypes)
        {

            init();
            Sessions = new List<TS>();
            foreach (SessionType s in sessionTypes)
            {
                TS tS = SessionsAll.Find(x => x.SessionType == s);
                if (tS != null)
                {
                    Sessions.Add(tS);
                }
            }

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
        private void init()
        {
            if (SessionsAll == null)
            {
                SessionsAll = new List<TS>();
            }

            TS _ts = new TS();
            /*
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
            _ts.Open = new DateTime(1, 1, 1,15, 30, 0);
            _ts.Close = new DateTime(1, 1, 1, 23, 0, 0);
            SessionsAll.Add(_ts);
            */
            _ts = new TS();
            _ts.Name = "Мосбиржа";
            _ts.SessionType = SessionType.RUS;
            _ts.Open = new DateTime(1, 1, 1, 10, 0, 0);
            _ts.Close = new DateTime(1, 1, 1, 19, 0, 0);
            SessionsAll.Add(_ts);

            color = Color.Blue;
            TypeIndicator = IndicatorOneCandleChartType.Line;
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
            RUS
        }
        public enum DayType
        {
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
        /// Цвет индикатора
        /// </summary>
        public Color color;
        /// <summary>
        /// все значения индикатора
        /// </summary>
        List<List<decimal>> IIndicatorCandle.ValuesToChart
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
        List<Color> IIndicatorCandle.Colors
        {
            get
            {
                List<Color> colors = new List<Color>();
                colors.Add(ColorUp);
                colors.Add(ColorDown);
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
        public IndicatorOneCandleChartType TypeIndicator
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
        public event Action<IIndicatorCandle> NeadToReloadEvent;

        // вычисления
        /// <summary>
        /// Проверяем началась ли каянибудь торговая сессия на указанную дату
        /// </summary>
        /// <param name="date">дата для проверки</param>
        /// <returns></returns>
        public bool itsSessionStart(DateTime date)
        {
            TS ts =  Sessions.Find(x => x.Open == new DateTime(1, 1, 1, date.Hour, date.Minute, date.Second));
            return (ts != null);
        }
        /// <summary>
        /// Получение даты окончания прошлой сесии
        /// </summary>
        /// <param name="date">дата для анализа</param>
        /// <returns></returns>
        public DateTime GetLastSessionEndDate(DateTime date)
        {

            TS ts = SessionOnTime(date);
           
            if (ts.Open==DateTime.MinValue)
            {
                // ошибка сессия не найдена
                return DateTime.MinValue;
            }
            DateTime testDate = new DateTime(date.Year, date.Month, date.Day, ts.Close.Hour, ts.Close.Minute, ts.Close.Second);

            return testDate.AddDays(-1);
        }
        public Decimal GetLastSessionEndPrice(List<Candle> candles, DateTime date)
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
        /// <summary>
        /// Получение текущей сесии
        /// </summary>
        /// <param name="date">дата для анализа</param>
        /// <returns></returns>
        public TS SessionOnTime(DateTime date)
        {
            TS result = new TS();
            DateTime testDateOpen;
            foreach (var ts in Sessions)
            {
                testDateOpen = new DateTime(date.Year, date.Month, date.Day, ts.Open.Hour, ts.Open.Minute, ts.Open.Second);
                if (testDateOpen <= date)
                {
                    if (result.Open == new DateTime() || result.Open <= ts.Open)
                    {
                        result = ts;
                    }
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
                Values.Add(candles[candles.Count - 1].Volume);
                ColorSeries.Add(color);
        }

        /// <summary>
        /// прогрузить все свечи
        /// </summary>
        private void ProcessAllCandle(List<Candle> candles)
        {
            Values = new List<decimal>();
            ColorSeries = new List<Color>();

            for (int i = 0; i < candles.Count; i++)
            {
                Values.Add(candles[i].Volume);
                ColorSeries.Add(color);
            }
        }

        /// <summary>
        /// перегрузить последнюю свечу
        /// </summary>
        private void ProcessLastCanlde(List<Candle> candles)
        {
            Values[Values.Count-1] = (candles[candles.Count - 1].Volume);
            ColorSeries[ColorSeries.Count - 1] = color;
        }

        private void UpdateDate(List<Candle> candles,int i)
        {
            LastSessionEndDate = GetLastSessionEndDate(candles[i].TimeStart);
            LastSessionEndPrice = GetLastSessionEndPrice(candles, candles[i].TimeStart);
        }

    }
}
