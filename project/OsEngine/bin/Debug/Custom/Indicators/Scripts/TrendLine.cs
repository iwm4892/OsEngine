using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Entity;
using OsEngine.Indicators;

namespace CustomIndicators.Scripts
{
    class TrendLine : Aindicator
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
        /// Фрактал
        /// </summary>
        public Aindicator _fractial;
        public override void OnProcess(List<Candle> source, int index)
        {
            if (index <= _lenght.ValueInt)
            {
                return;
            }
            List<Point> pointsUp = GetPoints(_fractial.DataSeries.ByName("SeriesUp"));
            if (pointsUp.Count < 2)
            {
                _seriesUp.Values[index] = 0;
            }
            List<Point> pointsDown = GetPoints(_fractial.DataSeries.ByName("SeriesDown"));
            if (pointsDown.Count < 2)
            {
                _seriesDown.Values[index] = 0;
            }
            if(pointsUp.Count == 2 && pointsUp[1].Value > pointsUp[0].Value)
            {
                _seriesUp.Values[index] = 0;
            }
            if (pointsUp.Count == 2 && pointsUp[1].Value < pointsUp[0].Value)
            {
                for(int i = pointsUp[0].Index; i <= index; i++)
                {
                    _seriesUp.Values[i] = GetNewValue(pointsUp, i);
                }
                //_seriesUp.Values[index] = GetNewValue(pointsUp, index);
            }
            if (pointsDown.Count == 2 && pointsDown[1].Value < pointsDown[0].Value)
            {
                _seriesDown.Values[index] = 0;
            }
            if (pointsDown.Count == 2 && pointsDown[1].Value > pointsDown[0].Value)
            {
                for (int i = pointsDown[0].Index; i <= index; i++)
                {
                    _seriesDown.Values[i] = GetNewValue(pointsDown, i);
                }
                //_seriesDown.Values[index] = GetNewValue(pointsDown, index);
            }

        }
        private decimal GetNewValue(List<Point> points, int index)
        {
            //Сначала получим формулу линии потом посчетам значение
            //(y1 - y2)x + (x2 - x1)y + (x1y2 - x2y1) = 0
            //a = y1 - y2
            //b = x2 - x1
            //c = x1y2 - x2y1
            //y = - (c + a*x)/b
            decimal a = points[0].Value - points[1].Value;
            decimal b = points[1].Index - points[0].Index;
            decimal c = points[0].Index * points[1].Value - points[1].Index * points[0].Value;
            return -(c + a * index) / b;
        }
        private List<Point> GetPoints(List<decimal> values)
        {
            List<Point> result = new List<Point>();
            for(int i = values.Count - 1; i >= 0; i--)
            {
                if (values[i] != 0)
                {
                    Point p = new Point();
                    p.Index = i;
                    p.Value = values[i];
                    result.Add(p);
                }
                if (result.Count == 2)
                {
                    break;
                }
            }
            result.Reverse();
            return result;
        }
    public override void OnStateChange(IndicatorState state)
        {
            if (state == IndicatorState.Configure)
            {
                _lenght = CreateParameterInt("Length", 5);

                _seriesUp = CreateSeries("SeriesUp", Color.DarkRed, IndicatorChartPaintType.Point, true);
                _seriesDown = CreateSeries("SeriesDown", Color.DarkGreen, IndicatorChartPaintType.Point, true);

                _fractial = IndicatorsFactory.CreateIndicatorByName("Fractail_lenth", Name + "Fractail", false);
                ((IndicatorParameterInt)_fractial.Parameters[0]).Bind(_lenght);
                ProcessIndicator("Fractail", _fractial);

            }
        }
        class Point
        {
            public int Index;
            public decimal Value;
        } 
    }
}
