﻿/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Globalization;
using System.Collections.Generic;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Indicators;

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
//        public Volume Volume;
        public Delta Delta;
//        public Claster Claster;


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

        public void Fill(List<Candle> candles, List<IIndicator> indicators)
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
                 //   case "Volume": Volume = (Volume)indicators[i]; break;
                 //   case "Claster": Claster = (Claster)indicators[i]; break;
                }
            }

        }
        public void Check()
        {
            throw new NotImplementedException();
        }

        public bool Validate()
        {
            bool result = true;
            if (Candles == null || Candles.Count < CandlesCount)
            {
                result = false;
            }
            if(Delta !=null && Delta.Values.Count < CandlesCount)
            {
                result = false;
            }
            return result;
        }
        static Pattern GetPattern(string name, List<Candle> _candles, List<IIndicator> _indicators)
        {
            
            string BaseClassName = "OsEngine.Entity." + name;
            Type type = Type.GetType(BaseClassName);
            return (Pattern)Activator.CreateInstance(type, new object[] { _candles, _indicators });
        }
        public static List<Pattern> GetValidatePatterns(List<Candle> candles, List<IIndicator> indicators)
        {
            List<string> patterns = new List<string>();
            patterns.Add("B_pattern");
            patterns.Add("P_pattern");
        //    patterns.Add("Signal_pattern");
            patterns.Add("Metla_pattern");
            patterns.Add("Trap_pattern");

            return GetValidatePatterns(candles,indicators,patterns);
        }
        public static List<Pattern> GetValidatePatterns(List<Candle> candles, List<IIndicator> indicators, List<string> patterns)
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
        /// <summary>
        /// Данные кластера свечи
        /// </summary>
        public ClasterData ClasterData;

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
            ClasterData = candle.ClasterData;
        }
    }
    public class Trap_pattern : Pattern
    {
        public Trap_pattern(List<Candle> candles, List<IIndicator> indicators)
        {
            CandlesCount = 1;
            Fill(candles, indicators);
            if (!Validate())
            {
                return;
            }
            Check();
        }
        public new void  Check()
        {
            CandleData cData = new CandleData(Candles[Candles.Count - 1]);
            if (Candles[Candles.Count - 1].IsUp 
                && Candles[Candles.Count - 1].Open > Candles[Candles.Count - 1].ClasterData.MaxData.Price
                && Candles[Candles.Count - 1].ClasterData.MaxData.side == Side.Sell
                && Delta.Values[Delta.Values.Count - 1] > 0
                )
            {
                isPattern = true;
                Side = Side.Buy;
            }
            if (Candles[Candles.Count - 1].IsDown
                && Candles[Candles.Count - 1].Open < Candles[Candles.Count - 1].ClasterData.MaxData.Price
                && Candles[Candles.Count - 1].ClasterData.MaxData.side == Side.Buy
                && Delta.Values[Delta.Values.Count - 1] < 0
                )
            {
                isPattern = true;
                Side = Side.Sell;
            }
        }

    }

    public class B_pattern : Pattern
    {
        public B_pattern(List<Candle> candles, List<IIndicator> indicators)
        {
            CandlesCount = 2;
            Fill(candles, indicators);
            if (!Validate())
            {
                return;
            }
            Check();
        }
        public new void Check()
        {

            if (Candles[Candles.Count - 2].IsUp && Candles[Candles.Count - 1].IsDown
                && Delta.Values[Delta.Values.Count - 1] < 0
                && Candles[Candles.Count - 1].ClasterData.MaxData.Price <= (Candles[Candles.Count - 1].High - Candles[Candles.Count - 1].Low) / 3 + Candles[Candles.Count - 1].Low)
            {
                isPattern = true;
                Side = Side.Sell;
            }
        }

    }
    public class P_pattern : Pattern
    {
        public P_pattern(List<Candle> candles, List<IIndicator> indicators)
        {
            CandlesCount = 2;
            Fill(candles, indicators);
            if (!Validate())
            {
                return;
            }
            Check();
        }
        public new void Check()
        {
            if (Candles[Candles.Count - 2].IsDown && Candles[Candles.Count - 1].IsUp
                && Delta.Values[Delta.Values.Count - 1] > 0
                && Candles[Candles.Count - 1].ClasterData.MaxData.Price >= (+Candles[Candles.Count - 1].High - Candles[Candles.Count - 1].High - Candles[Candles.Count - 1].Low) / 3)
            {
                isPattern = true;
                Side = Side.Buy;
            }
        }

    }

    public class Signal_pattern  : Pattern
    {
        public Signal_pattern(List<Candle> candles, List<IIndicator> indicators)
        {
            CandlesCount = 2;
            Fill(candles, indicators);
            if (!Validate())
            {
                return;
            }
            Check();
        }
        public new void Check()
        {

            CandleData cData = new CandleData(Candles[Candles.Count - 1]);
            //предположим что тени нет если размер тени меньше 10% размера свечи
            // предполагаем что длинная тень это больше 30% размера свечи
            if (Candles[Candles.Count - 1].IsUp
                && cData.hiShadow < cData.candleSize * (decimal)0.05
          //      && cData.lowShadow > cData.candleSize * (decimal)0.3
                && cData.ClasterData.MaxData.Price <= Candles[Candles.Count - 1].Open +(cData.candleBody * (decimal)0.3)
                && Delta.Values[Delta.Values.Count - 1] > 0
                && Candles[Candles.Count - 2].Volume<= Candles[Candles.Count - 1].Volume
                )
            {
                isPattern = true;
                Side = Side.Buy;
            }
            if (Candles[Candles.Count - 1].IsDown
                && cData.lowShadow < cData.candleSize * (decimal)0.05
             //   && cData.hiShadow > cData.candleSize * (decimal)0.3
                && cData.ClasterData.MaxData.Price >= Candles[Candles.Count - 1].Open - (cData.candleBody * (decimal)0.3)
                && Delta.Values[Delta.Values.Count - 1] < 0
            //        && Candles[Candles.Count - 2].Volume<= Candles[Candles.Count - 1].Volume
            )
            {
                isPattern = true;
                Side = Side.Sell;
            }

        }

    }

    public class Metla_pattern : Pattern
    {
        public Metla_pattern(List<Candle> candles, List<IIndicator> indicators)
        {
            CandlesCount = 2;
            Fill(candles, indicators);
            if (!Validate())
            {
                return;
            }
            Check();
        }
        public new void Check()
        {

            CandleData cData1 = new CandleData(Candles[Candles.Count - 2]);
            CandleData cData2 = new CandleData(Candles[Candles.Count - 1]);
            //Разные направления
            if (Candles[Candles.Count - 1].IsUp != Candles[Candles.Count - 2].IsUp)
            {
                if(Candles[Candles.Count - 2].IsUp
                    && cData1.ClasterData.MaxData.Price >= Candles[Candles.Count - 2].Close - (cData1.candleBody * (decimal)0.3)
                    && cData2.ClasterData.MaxData.Price <= Candles[Candles.Count - 1].Close + (cData2.candleBody * (decimal)0.3)
                    && Delta.Values[Delta.Values.Count - 2] > 0
                    && Delta.Values[Delta.Values.Count - 1] < 0
                      && Candles[Candles.Count - 2].Volume <= Candles[Candles.Count - 1].Volume
                    //    && Volume.Values[Volume.Values.Count - 2] >= Volume.Values[Volume.Values.Count - 1]
                    //    && cData1.candleSize <= cData2.candleSize
                    )
                {
                    isPattern = true;
                    Side = Side.Sell;
                }
                if (Candles[Candles.Count - 1].IsUp
                    && cData1.ClasterData.MaxData.Price <= Candles[Candles.Count - 2].Close + (cData1.candleBody * (decimal)0.3)
                    && cData2.ClasterData.MaxData.Price >= Candles[Candles.Count - 1].Close - (cData2.candleBody * (decimal)0.3)
                    && Delta.Values[Delta.Values.Count - 2] < 0
                    && Delta.Values[Delta.Values.Count - 1] > 0
                     && Candles[Candles.Count - 2].Volume <= Candles[Candles.Count - 1].Volume
                    //   && Volume.Values[Volume.Values.Count - 2] >= Volume.Values[Volume.Values.Count - 1]
                    //    && cData1.candleSize <= cData2.candleSize
                    )
                {
                    isPattern = true;
                    Side = Side.Buy;
                }
            }

        }

    }

}
