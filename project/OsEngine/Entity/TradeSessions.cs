/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Globalization;
using System.Collections.Generic;

namespace OsEngine.Entity
{
public class TradeSessions
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
