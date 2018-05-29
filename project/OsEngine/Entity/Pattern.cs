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
            List<string> patterns = new List<string>();
            patterns.Add("B_pattern");
            patterns.Add("P_pattern");
        //    patterns.Add("Signal_pattern");
            patterns.Add("Metla_pattern");

            return GetValidatePatterns(candles,indicators,patterns);
        }
        public static List<Pattern> GetValidatePatterns(List<Candle> candles, List<IIndicatorCandle> indicators, List<string> patterns)
        {
            List<Pattern> result = new List<Pattern>();

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
    public class CandleData
    {
        /// <summary>
        /// Размер тела свечи
        /// </summary>
        public decimal candleBody;
        /// <summary>
        /// Размер свечи(включая тени)
        /// </summary>
        public decimal candleSize;
        /// <summary>
        /// Размер верхней тени
        /// </summary>
        public decimal hiShadow;
        /// <summary>
        /// Размер нижней тени
        /// </summary>
        public decimal lowShadow;

        public CandleData(Candle candle)
        {
            candleSize = candle.High - candle.Low;
            if (candle.IsUp)
            {
                candleBody = candle.Close - candle.Open;
                hiShadow = candle.High - candle.Close;
                lowShadow = candle.Open - candle.Low ;
            }
            else
            {
                candleBody = candle.Open - candle.Close;
                hiShadow = candle.High - candle.Open;
                lowShadow = candle.Low - candle.Close;
            }
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
                && Claster.data[Claster.data.Count - 1].MaxData.Price <= (Candles[Candles.Count - 1].High - Candles[Candles.Count - 1].Low) / 3 + Candles[Candles.Count - 1].Low)
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
                && Claster.data[Claster.data.Count - 1].MaxData.Price >= (+Candles[Candles.Count - 1].High - Candles[Candles.Count - 1].High - Candles[Candles.Count - 1].Low) / 3)
            {
                isPattern = true;
                Side = Side.Buy;
            }
        }

    }

    public class Signal_pattern  : Pattern
    {
        public Signal_pattern(List<Candle> candles, List<IIndicatorCandle> indicators)
        {
            CandlesCount = 6;
            Fill(candles, indicators);
            if (!Validate())
            {
                return;
            }
            Check();
        }
        public void Check()
        {
            /*
            // проверим тренд из последних CandlesCount Свечеек
            // и посчетаем средний спрэд
            decimal trend =0;
            decimal sredspread=0;
            for (int i= Candles.Count-CandlesCount;i< Candles.Count; i++)
            {
                trend += Candles[i].Close-Candles[i].Open;
                sredspread += Candles[i].High - Candles[i].Low;
            }
            sredspread = sredspread / CandlesCount;
            if (Math.Abs(trend) > sredspread * (decimal)0.1)
            {
                return;
            }
            */
            CandleData cData = new CandleData(Candles[Candles.Count - 1]);
            //предположим что тени нет если размер тени меньше 10% размера свечи
            // предполагаем что длинная тень это больше 30% размера свечи
            if (Candles[Candles.Count - 1].IsUp
                && cData.hiShadow ==0//< cData.candleSize * (decimal)0.1
                && cData.lowShadow > cData.candleSize * (decimal)0.3
                && Claster.data[Claster.data.Count - 1].MaxData.Price <= Candles[Candles.Count - 1].Open +(cData.candleBody * (decimal)0.3)
                && Delta.Values[Delta.Values.Count - 1] > 0
                && Volume.Values[Volume.Values.Count-2]<= Volume.Values[Volume.Values.Count - 1]
                )
            {
                isPattern = true;
                Side = Side.Buy;
            }
            if (Candles[Candles.Count - 1].IsDown
                && cData.lowShadow ==0//< cData.candleSize * (decimal)0.1
                && cData.hiShadow > cData.candleSize * (decimal)0.3
                && Claster.data[Claster.data.Count - 1].MaxData.Price >= Candles[Candles.Count - 1].Open - (cData.candleBody * (decimal)0.3)
                && Delta.Values[Delta.Values.Count - 1] < 0
                && Volume.Values[Volume.Values.Count - 2] <= Volume.Values[Volume.Values.Count - 1])
            {
                isPattern = true;
                Side = Side.Sell;
            }

        }

    }

    public class Metla_pattern : Pattern
    {
        public Metla_pattern(List<Candle> candles, List<IIndicatorCandle> indicators)
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
            /*
            // проверим тренд из последних CandlesCount Свечеек
            // и посчетаем средний спрэд
            decimal trend =0;
            decimal sredspread=0;
            for (int i= Candles.Count-CandlesCount;i< Candles.Count; i++)
            {
                trend += Candles[i].Close-Candles[i].Open;
                sredspread += Candles[i].High - Candles[i].Low;
            }
            sredspread = sredspread / CandlesCount;
            if (Math.Abs(trend) > sredspread * (decimal)0.1)
            {
                return;
            }
            */
            CandleData cData1 = new CandleData(Candles[Candles.Count - 2]);
            CandleData cData2 = new CandleData(Candles[Candles.Count - 1]);
            //Разные направления
            if (Candles[Candles.Count - 1].IsUp != Candles[Candles.Count - 2].IsUp)
            {
                if(Candles[Candles.Count - 2].IsUp
                    && Claster.data[Claster.data.Count - 2].MaxData.Price >= Candles[Candles.Count - 2].Close - (cData1.candleBody * (decimal)0.3)
                    && Claster.data[Claster.data.Count - 1].MaxData.Price <= Candles[Candles.Count - 1].Close + (cData2.candleBody * (decimal)0.3)
                    && Delta.Values[Delta.Values.Count - 2] > 0
                    && Delta.Values[Delta.Values.Count - 1] < 0
                    && Volume.Values[Volume.Values.Count - 2] <= Volume.Values[Volume.Values.Count - 1]
                    )
                {
                    isPattern = true;
                    Side = Side.Sell;
                }
                if (Candles[Candles.Count - 1].IsUp
                    && Claster.data[Claster.data.Count - 2].MaxData.Price <= Candles[Candles.Count - 2].Close + (cData1.candleBody * (decimal)0.3)
                    && Claster.data[Claster.data.Count - 1].MaxData.Price >= Candles[Candles.Count - 1].Close - (cData2.candleBody * (decimal)0.3)
                    && Delta.Values[Delta.Values.Count - 2] < 0
                    && Delta.Values[Delta.Values.Count - 1] > 0
                    && Volume.Values[Volume.Values.Count - 2] <= Volume.Values[Volume.Values.Count - 1]
                    )
                {
                    isPattern = true;
                    Side = Side.Buy;
                }
            }

        }

    }

}
