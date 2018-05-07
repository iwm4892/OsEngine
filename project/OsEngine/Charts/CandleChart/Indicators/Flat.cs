/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OsEngine.Entity;


namespace OsEngine.Charts.CandleChart.Indicators
{

    /// <summary>
    /// линия построенная на основе массива значений decimal
    /// </summary>
    public class Flat : IIndicatorCandle
    {
        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="uniqName">уникальное имя</param>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public Flat(string uniqName, bool canDelete)
        {
            Name = uniqName;
            TypeIndicator = IndicatorOneCandleChartType.Line;
            ColorBase = Color.DodgerBlue;
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            Lenght = 30;
            PaintOn = true;
            CanDelete = canDelete;
            Load();
        }

        /// <summary>
        /// индикатор без параметнов. Не будет сохраняться
        /// используется ТОЛЬКО для создания составных индикаторов
        /// не используйте его из слоя создания роботов!
        /// </summary>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public Flat(bool canDelete)
        {
            Name = Guid.NewGuid().ToString();

            TypeIndicator = IndicatorOneCandleChartType.Line;
            ColorBase = Color.DodgerBlue;
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            Lenght = 30;
            PaintOn = true;
            CanDelete = canDelete;
        }

        /// <summary>
        /// все значения индикатора
        /// </summary>
        List<List<decimal>> IIndicatorCandle.ValuesToChart
        {
            get
            {
                List<List<decimal>> list = new List<List<decimal>>();
                list.Add(ValuesUp);
                list.Add(ValuesDown);
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

        /// <summary>
        /// длина расчёта индикатора
        /// </summary>
        public int Lenght
        { get; set; }

        /// <summary>
        /// цвет верхней серии данных
        /// </summary>
        public Color ColorUp
        { get; set; }

        /// <summary>
        /// цвет нижней серии данных
        /// </summary>
        public Color ColorDown
        { get; set; }

        /// <summary>
        /// верхняя линия 
        /// </summary>
        public List<decimal> ValuesUp
        { get; set; }

        /// <summary>
        /// нижняя линия
        /// </summary>
        public List<decimal> ValuesDown
        { get; set; }

        /// <summary>
        /// Минимальное значение
        /// </summary>
        private Decimal minValue;
        /// <summary>
        /// Максимальное значение
        /// </summary>
        private Decimal maxValue;

        /// <summary>
        /// Средняя свечка
        /// </summary>
        public Decimal AverageCandle;
        /// <summary>
        /// Максимальное Значение на предыдущую свечку
        /// </summary>
        public Decimal LastMax;
        /// <summary>
        /// Минимальное Значение на предыдущую свечку
        /// </summary>
        public Decimal LastMin;
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
                    writer.WriteLine(ColorUp.ToArgb());
                    writer.WriteLine(ColorDown.ToArgb());
                    writer.WriteLine(Lenght);

                    writer.WriteLine(ColorBase.ToArgb());
                    writer.WriteLine(PaintOn);

                    writer.WriteLine(ColorUp.ToArgb());
                    writer.WriteLine(ColorDown.ToArgb());
                    writer.WriteLine(Lenght);

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
                    ColorUp = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    ColorDown = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    Lenght = Convert.ToInt32(reader.ReadLine());

                    ColorBase = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    PaintOn = Convert.ToBoolean(reader.ReadLine());

                    ColorUp = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    ColorDown = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    Lenght = Convert.ToInt32(reader.ReadLine());

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
            if (ValuesUp != null)
            {
                ValuesUp.Clear();
            }

            if (ValuesDown != null)
            {
                ValuesDown.Clear();
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
        /// пересчитать индикатор. 
        /// </summary>
        /// <param name="candles">свечи</param>
        public void Process(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }

            if (minValue == 0 || maxValue == 0)
            {
                SetMinMaxDefault(candles[candles.Count - 1]);
            }
            if (ValuesDown == null || ValuesUp == null)
            {
                ValuesUp = new List<decimal>();
                ValuesDown = new List<decimal>();
            }


            if (ValuesDown.Count + 1 == candles.Count)
            {
                ProcessOne(candles);
            }
            else if (ValuesDown.Count == candles.Count)
            {
                ProcessLast(candles);
            }

            else
            {
                ProcessAll(candles);
            }
            // Запоминаем предыдущие значения
            if (ValuesUp.Count > 1)
            {
                LastMax = ValuesUp[ValuesUp.Count - 2];
                LastMin = ValuesDown[ValuesDown.Count - 2];
            }
            /*
            if(ValuesUp[ValuesUp.Count-1]>0 && ValuesDown[ValuesDown.Count - 1] > 0)
            {
                PaintOn = true;
            }
            else
            {
                PaintOn = false;
            }
            */

        }
        /// <summary>
        /// прогрузить только последнюю свечку
        /// </summary>
        private void ProcessOne(List<Candle> candles)
        {
            CalculateAverageCandle(candles);
            CalculateMinMax(candles);
            ValuesUp.Add(maxValue);
            ValuesDown.Add(minValue);
        }
        /// <summary>
        /// прогрузить с самого начала
        /// </summary>
        private void ProcessAll(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }
            ValuesUp = new List<decimal>();
            ValuesDown = new List<decimal>();

            for (int i = 0; i < candles.Count; i++)
            {
                ProcessOne(candles);
            }

        }
        /// <summary>
        /// перегрузить последнюю ячейку
        /// </summary>
        private void ProcessLast(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }
            ValuesUp[ValuesUp.Count - 1] = maxValue;
            ValuesDown[ValuesDown.Count - 1] = minValue;
        }


        /// <summary>
        /// прогрузить новыми значениями
        /// </summary>
        /// <param name="decimals"></param>
        public void ProcessDesimals(List<Decimal> decimals)
        {
            //   Values = decimals;
        }
        /// <summary>
        /// Установка Начальных значений минимум и максимум
        /// </summary>
        /// <param name="candle">Свеча</param>
        private void SetMinMaxDefault(Candle candle)
        {
            if (candle.IsUp)
            {
                minValue = candle.High;
                maxValue = candle.High;
            }
            else
            {
                minValue = candle.Low;
                maxValue = candle.Low;
            }
        }
        /// <summary>
        /// Вычисляем минимум и максимум
        /// </summary>
        /// <param name="candles">Свечи</param>
        private void CalculateMinMax(List<Candle> candles)
        {

            if (candles.Count > 1 && GetCandleBody(candles[candles.Count - 2])[0] > AverageCandle)
            {
                SetMinMaxDefault(candles[candles.Count - 2]);
            }
            for (int i = candles.Count - 1; i > 0 && candles.Count - i - 1 < Lenght; i--)
            {
                if (GetCandleBody(candles[i])[0] <= AverageCandle)
                {
                    maxValue = Math.Max(maxValue, candles[i].High);
                    minValue = Math.Min(minValue, candles[i].Low);
                }
                else
                {
                    //    SetMinMaxDefault(candles[i]);
                    break;
                }
            }
        }
        /// <summary>
        /// Вычисляет средний размер свечи
        /// </summary>
        /// <param name="candles">Свечи</param>
        private void CalculateAverageCandle(List<Candle> candles)
        {
            Decimal sum = 0;
            Decimal vol = 0;
            Decimal sum1 = 0;
            int count = 0;
            for (int i = candles.Count - Lenght - 1; i > 0 && i < candles.Count; i++)
            {
                decimal[] data = GetCandleBody(candles[i]);
                sum += data[0] * data[1];
                vol += data[1];
                sum1 += data[0];
                count++;
            }
            if (vol != 0)
            {
                AverageCandle = sum / vol;
            }
            else if (count != 0)
            {

                AverageCandle = sum1 / count;
            }
            else
            {
                AverageCandle = 0;
            }
        }
        /// <summary>
        /// Получает размер свечи и объем
        /// </summary>
        /// <param name="candle">Свеча</param>
        /// <returns></returns>
        private decimal[] GetCandleBody(Candle candle)
        {
            decimal[] result = new Decimal[2];
            result[0] = Math.Abs(candle.High - candle.Low);
            result[1] = candle.Volume;
            return result;
        }

    }
}
