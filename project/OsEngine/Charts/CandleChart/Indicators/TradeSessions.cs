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

    public class TradeSessions : IIndicatorCandle
    {
        public List<TS> Sessions;
        private List<TS> SessionsAll;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sessionTypes">Список используемых сессий</param>
        public TradeSessions(List<SessionType> sessionTypes)
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
        }
        public TradeSessions()
        {
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
            

        }
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
                colors.Add(ColorBase);
                return colors;
            }

        }

        /// <summary>
        /// можно ли удалить индикатор с графика. Это нужно для того чтобы у роботов нельзя было удалить 
        /// индикаторы которые ему нужны в торговле
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// тип прорисовки индикатора
        /// </summary>
        public IndicatorOneCandleChartType TypeIndicator
        { get; set; }

        /// <summary>
        /// имя серии на которой индикатор прорисовывается
        /// </summary>
        public string NameSeries
        { get; set; }

        /// <summary>
        /// имя области на котророй индикатор прорисовывается
        /// </summary>
        public string NameArea
        { get; set; }

        /// <summary>
        /// значение индикатора
        /// </summary>
        public List<decimal> Values
        { get; set; }

        /// <summary>
        /// уникальное имя индикатора
        /// </summary>
        public string Name
        { get; set; }

        /// <summary>
        /// цвет для прорисовки базовой точки данных
        /// </summary>
        public Color ColorBase
        { get; set; }

        /// <summary>
        /// включена ли прорисовка индикатора
        /// </summary>
        public bool PaintOn
        { get; set; }

        public List<Color> ColorSeries { get; set; }

        /// <summary>
        /// сохранить настройки
        /// </summary>
        public void Save()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                return;
            }
            try
            {
                using (StreamWriter writer = new StreamWriter(@"Engine\" + Name + @".txt", false))
                {
                    writer.WriteLine(ColorBase.ToArgb());
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
        /// загрузить настройки
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
                    ColorBase = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    PaintOn = Convert.ToBoolean(reader.ReadLine());
                    reader.ReadLine();

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
            // ignored. Этот тип индикатора настраивается и создаётся только из кода
        }

        /// <summary>
        /// индикатор нужно перерисовать
        /// </summary>
        public event Action<IIndicatorCandle> NeadToReloadEvent;
        /// <summary>
        /// прогрузить индикатор свечками
        /// </summary>
        public void Process(List<Candle> candles)
        {
            if (Values == null)
            {
                Values = new List<decimal>();
            }
            if (data != null &&
                           data.Count + 1 == candles.Count)
            {
                ProcessOneCandle(candles);
            }
            else if (data != null &&
                data.Count == candles.Count)
            {
                ProcessLastCanlde(candles);
            }
            else
            {
                ProcessAllCandle(candles);
            }


        }
        private void ProcessOneCandle(List<Candle> candles)
        {

        }
        /// <summary>
        /// прогрузить все свечи
        /// </summary>
        private void ProcessAllCandle(List<Candle> candles)
        {

        }
        /// <summary>
        /// перегрузить последнюю свечу
        /// </summary>
        private void ProcessLastCanlde(List<Candle> candles)
        {
            data[data.Count - 1].update(candles[candles.Count - 1].Trades);
        }


        /// <summary>
        /// Проверяем началась ли каянибудь торговая сессия на указанную дату
        /// </summary>
        /// <param name="date">дата для проверки</param>
        /// <returns></returns>
        public static bool itsSessionStart(DateTime date)
        {
            var tradesession = new TradeSessions();
            TS ts = tradesession.Sessions.Find(x => x.Open == new DateTime(1,1,1,date.Hour,date.Minute,date.Second));

            return (ts !=null);
        }
        /// <summary>
        /// Получение даты окончания прошлой сесии
        /// </summary>
        /// <param name="date">дата для анализа</param>
        /// <returns></returns>
        public static DateTime GetLastSessionEnd(DateTime date)
        {
            var tradesession = new TradeSessions();
            return tradesession.LastSessionEnd(date);
        }
        /// <summary>
        /// Получение даты окончания прошлой сесии
        /// </summary>
        /// <param name="date">дата для анализа</param>
        /// <returns></returns>
        public DateTime LastSessionEnd(DateTime date)
        {

            TS ts;
            ts = Sessions.Find(x => x.SessionType == SessionOnTime(date));
            if (ts == null)
            {
                // ошибка сессия не найдена
                return date.AddDays(-1);
            }
            DateTime testDate = new DateTime(date.Year, date.Month, date.Day, ts.Close.Hour, ts.Close.Minute, ts.Close.Second);

            return testDate.AddDays(-1);
        }
        public static Decimal LastSessionEndPrice(List<Candle> candles,DateTime date)
        {
            var tradesession = new TradeSessions();
            DateTime LastDate = tradesession.LastSessionEnd(date);
            int ind = candles.FindIndex(x => x.TimeStart > LastDate);
            if (ind>0 && candles[ind-1].TimeStart <= LastDate)
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
        public SessionType SessionOnTime(DateTime date)
        {
            TS result = new TS();
            DateTime testDateOpen; 
            foreach(var ts in Sessions)
            {
                testDateOpen = new DateTime(date.Year, date.Month, date.Day, ts.Open.Hour, ts.Open.Minute, ts.Open.Second);
                if (testDateOpen <= date)  
                {
                    if(result.Open == new DateTime() || result.Open <= ts.Open)
                    {
                        result = ts;
                    }
                }
            }
            return result.SessionType;
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
    }

}
