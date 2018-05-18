/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Globalization;
using System.Collections.Generic;
using OsEngine.Charts.CandleChart.Indicators;


namespace OsEngine.Entity
{
    /// <summary>
    /// Паттерн
    /// </summary>
    public interface Patterninterface
    {
        /// <summary>
        /// Проверяемые свечи
        /// </summary>
        List<Candle> Candles{ get; set; }

        /// <summary>
        /// Необходимое количество свечек для формирования паттерна
        /// </summary>
        int CandlesCount { get; set; }
        /// <summary>
        /// Направление паттерна
        /// </summary>
        Side Side {get;}
        /// <summary>
        /// Условия формирования выполены
        /// </summary>
        bool isPattern { get; }

        /// <summary>
        /// Проверка условий формирования паттерна
        /// </summary>
        void Check();

    }
    public class Pattern : Patterninterface
    {
        public Pattern()
        {

        }
        public List<Candle> Candles
        {
            get
            {
                return Candles;
            }
            set
            {
                if (Candles != value)
                {
                    Candles = value;
                }
            }
        }
        public int CandlesCount {
            get { return CandlesCount; } set { CandlesCount = value; }
        }

        public Side Side { get { return Side; } }

        public bool isPattern { get { return isPattern; } }

        private void Fill(List<Candle> candles)
        {
            this.Candles = new List<Candle>();
            for (int i = candles.Count - 1-CandlesCount; i < candles.Count; i++)
            {
                this.Candles.Add(candles[i]);
            }
        }
        public void Check()
        {
            throw new NotImplementedException();
        }

        public bool Validate()
        {
            if (Candles.Count < CandlesCount)
            {
                return false;
            }
            return true;
        }
        static Pattern GetPattern(string name)
        {
            string BaseClassName = "OsEngine.Entity.Pattern." + name;
            Type type = Type.GetType(BaseClassName);
            return (Pattern)Activator.CreateInstance(type);
        }
        public class B_pattern : Pattern
        {
            private Volume Volume;
            private Delta Delta;
            private ClasterData ClasterData;

            public B_pattern(List<Candle> candles, List<IIndicatorCandle> indicators)
            {
                CandlesCount = 1;
                Fill(candles);
                for(int i = 0; i < indicators.Count; i++)
                {

                }
            }
                    
        }
    }
}
