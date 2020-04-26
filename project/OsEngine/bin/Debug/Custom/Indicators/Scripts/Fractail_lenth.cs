using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.Indicators;

namespace CustomIndicators.Scripts
{
    class Fractail_lenth : Aindicator
    {
        /// <summary>
        /// количество свечей для анализа
        /// </summary>
        public IndicatorParameterInt _lenght;
        /// <summary>
        /// Верхние уровни
        /// </summary>
        public IndicatorDataSeries _seriesUp;
        /// <summary>
        /// Нижние уровни
        /// </summary>
        public IndicatorDataSeries _seriesDown;
        /// <summary>
        /// центр длины
        /// </summary>
        private int _center
        {
            get { return (int)(_lenght.ValueInt / 2) + 1; }
        }
        public override void OnProcess(List<Candle> candles, int index)
        {
            decimal _up=0;
            decimal _downe=0;

            if (index -_lenght.ValueInt >= 0)
            {
                bool canUp = true;
                bool canDown = true;
                for (int i = index - _lenght.ValueInt; i < index; i++)
                {
                    if(i == index - _center)
                    {
                        continue;
                    }

                    if (candles[i].GetPoint("High") > candles[index - _center].GetPoint("High"))
                    {
                        canUp = false;
                    }
                    if (candles[i].GetPoint("Low") < candles[index - _center].GetPoint("Low"))
                    {
                        canDown = false;
                    }
                }
                if (canUp)
                {
                    _up = candles[index - _center].GetPoint("High");
                }
                if (canDown)
                {
                    _downe = candles[index - _center].GetPoint("Low");
                }
                _seriesUp.Values[index - _center] = _up;
                _seriesDown.Values[index - _center] = _downe;
            }
            else
            {
                _seriesUp.Values[index] = 0;
                _seriesDown.Values[index] = 0;
            }
        }

        public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _lenght = CreateParameterInt("Length", 5);
                _seriesUp = CreateSeries("SeriesUp", Color.Blue, IndicatorChartPaintType.Point, true);
                _seriesDown = CreateSeries("SeriesDown", Color.DarkRed, IndicatorChartPaintType.Point, true);
            }
        }
    }
}
