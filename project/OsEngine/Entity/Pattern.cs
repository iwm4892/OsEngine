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
        //List<Candle> Candles{ get; set; }

        /// <summary>
        /// Необходимое количество свечек для формирования паттерна
        /// </summary>
        //int CandlesCount { get; set; }
        /// <summary>
        /// Направление паттерна
        /// </summary>
        //Side Side {get;}
        /// <summary>
        /// Условия формирования выполены
        /// </summary>
        //bool isPattern { get; }

        /// <summary>
        /// Проверка условий формирования паттерна
        /// </summary>
        //void Check();

    }
    public class Pattern : Patterninterface
    {
        public Volume Volume;
        public Delta Delta;
        public Claster Claster;


        public Pattern()
        {

        }
        public List<Candle> Candles;
            /*
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
        */
        public int CandlesCount;

        public Side Side;// { get { return Side; } }

        public bool isPattern;// { get { return isPattern; } }

        public void Fill(List<Candle> candles, List<IIndicatorCandle> indicators)
        {
            this.Candles = new List<Candle>();
            Candles = candles;
/*            for (int i = candles.Count - CandlesCount; i < candles.Count; i++)
            {
                this.Candles.Add(candles[i]);
            }
*/
            for (int i = 0; i < indicators.Count; i++)
            {
                switch (indicators[i].GetType().Name)
                {
                    case "Delta": Delta = (Delta)indicators[i]; break;
                    case "Volume": Volume = (Volume)indicators[i]; break;
                    case "Claster": Claster = (Claster)indicators[i]; break;
                }
            }

        }
        public void Check()
        {
            throw new NotImplementedException();
        }

        public bool Validate()
        {
            if (Candles == null || Candles.Count < CandlesCount)
            {
                return false;
            }
            return true;
        }
        static Pattern GetPattern(string name, List<Candle> _candles, List<IIndicatorCandle> _indicators)
        {
            
            string BaseClassName = "OsEngine.Entity." + name;
            Type type = Type.GetType(BaseClassName);
            return (Pattern)Activator.CreateInstance(type, new object[] { _candles, _indicators });
        }
        public static List<Pattern> GetValidatePatterns(List<Candle> candles, List<IIndicatorCandle> indicators)
        {
            List<Pattern> result = new List<Pattern>();

            List<string> patterns = new List<string>();

            patterns.Add("B_pattern");
            patterns.Add("P_pattern");

            for (int i = 0; i < patterns.Count; i++)
            {
                Pattern p = Pattern.GetPattern(patterns[i], candles, indicators);
                
                if (p.isPattern)
                {
                    result.Add(p);
                }
            }
            return result;
        }
    }
    public class B_pattern : Pattern
    {
        public B_pattern(List<Candle> candles, List<IIndicatorCandle> indicators)
        {
            CandlesCount = 2;
            Fill(candles, indicators);
            if (!Validate())
            {
                return;
            }
            Check();
        }
        public void Check()
        {

            if (Candles[Candles.Count - 2].IsUp && Candles[Candles.Count - 1].IsDown
                && Delta.Values[Delta.Values.Count - 1] < 0
                && Claster.data[Claster.data.Count - 1].MaxData.prise <= (Candles[Candles.Count - 1].High - Candles[Candles.Count - 1].Low) / 3 + Candles[Candles.Count - 1].Low)
            {
                isPattern = true;
                Side = Side.Sell;
            }
        }

    }
    public class P_pattern : Pattern
    {
        public P_pattern(List<Candle> candles, List<IIndicatorCandle> indicators)
        {
            CandlesCount = 2;
            Fill(candles, indicators);
            if (!Validate())
            {
                return;
            }
            Check();
        }
        public void Check()
        {
            if (Candles[Candles.Count - 2].IsDown && Candles[Candles.Count - 1].IsUp
                && Delta.Values[Delta.Values.Count - 1] > 0
                && Claster.data[Claster.data.Count - 1].MaxData.prise >= (+Candles[Candles.Count - 1].High - Candles[Candles.Count - 1].High - Candles[Candles.Count - 1].Low) / 3)
            {
                isPattern = true;
                Side = Side.Buy;
            }
        }

    }
}
