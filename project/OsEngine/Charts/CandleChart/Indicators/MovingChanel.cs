/*
 * Your rights to use code governed by this license http://o-s-a.net/doc/license_simple_engine.pdf
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OsEngine.Entity;

namespace OsEngine.Charts.CandleChart.Indicators
{
    public class MovingChanel: IIndicatorCandle
    {
        /// <summary>
        /// constructor with unique name. Indicator will be saved
        /// конструктор с уникальным именем. Индикатор будет сохраняться
        /// </summary>
        /// <param name="uniqName">unique name/уникальное имя</param>
        /// <param name="canDelete">whether user can remove indicator from chart manually/можно ли пользователю удалить индикатор с графика вручную</param>
        public MovingChanel(string uniqName,bool canDelete)
        {
            Name = uniqName;
            TypeIndicator = IndicatorOneCandleChartType.Line;
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            PaintOn = true;
            Deviation = 100;
            MaLenth = 9;
            if (!File.Exists(@"Engine\" + Name + @".txt"))
            {
                // if this is our first download.
                // если у нас первая загрузка
                MovingShadowBody = new MovingAverage(uniqName + "maSignal", false) { Lenght = 9, TypeCalculationAverage = MovingAverageTypeCalculation.Simple, TypePointsToSearch = PriceTypePoints.ShadowBody };
                MovingUp = new MovingAverage(uniqName + "MovingUp", false) { Lenght = 9, TypeCalculationAverage = MovingAverageTypeCalculation.Simple, TypePointsToSearch = PriceTypePoints.High };
                MovingDown = new MovingAverage(uniqName + "MovingDown", false) { Lenght = 9, TypeCalculationAverage = MovingAverageTypeCalculation.Simple, TypePointsToSearch = PriceTypePoints.Low};
            }
            else
            {
                MovingShadowBody = new MovingAverage(uniqName + "maSignal", false);
                MovingUp = new MovingAverage(uniqName + "MovingUp", false);
                MovingDown = new MovingAverage(uniqName + "MovingDown", false);
            }
            CanDelete = canDelete;
            Load();
        }

        /// <summary>
        /// constructor without parameters.Indicator will not saved/конструктор без параметров. Индикатор не будет сохраняться
        /// used ONLY to create composite indicators/используется ТОЛЬКО для создания составных индикаторов
        /// Don't use it from robot creation layer/не используйте его из слоя создания роботов!
        /// </summary>
        /// <param name="canDelete">whether user can remove indicator from chart manually/можно ли пользователю удалить индикатор с графика вручную</param>
        public MovingChanel(bool canDelete)
        {
            Name = Guid.NewGuid().ToString();

            TypeIndicator = IndicatorOneCandleChartType.Line;
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            PaintOn = true;
            Deviation = 100;
            MaLenth = 9;
            MovingShadowBody = new MovingAverage(false){Lenght = 9, TypeCalculationAverage = MovingAverageTypeCalculation.Simple, TypePointsToSearch = PriceTypePoints.ShadowBody };
            MovingUp = new MovingAverage(false) { Lenght = 9, TypeCalculationAverage = MovingAverageTypeCalculation.Simple, TypePointsToSearch = PriceTypePoints.High };
            MovingDown = new MovingAverage(false) { Lenght = 9, TypeCalculationAverage = MovingAverageTypeCalculation.Simple, TypePointsToSearch = PriceTypePoints.Low };
            CanDelete = canDelete;
        }

        /// <summary>
        /// designer with ready MA. Will line up based on parameters specified in it
        /// конструктор с готовой машкой. Будет выстраиваться исходя из заданных в ней параметров
        /// </summary>
        /// <param name="moving">moving average for calculation/скользящая средняя для расчёта</param>
        /// <param name="uniqName">unique name/уникальное имя</param>
        public MovingChanel(MovingAverage moving, string uniqName)
        {
            Name = uniqName;
            TypeIndicator = IndicatorOneCandleChartType.Line;
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            PaintOn = true;
            Deviation = 100;

            MovingShadowBody = moving;
            MovingShadowBody.Name = uniqName + "maSignal";


            Load();
        }

        /// <summary>
        /// all indicator values
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
        /// indicator colors
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
        /// whether indicator can be removed from chart. This is necessary so that robots can't be removed /можно ли удалить индикатор с графика. Это нужно для того чтобы у роботов нельзя было удалить 
        /// indicators he needs in trading/индикаторы которые ему нужны в торговле
        /// </summary>
        public bool CanDelete { get; set; }

        /// <summary>
        /// indicator drawing type
        /// тип прорисовки индикатора
        /// </summary>
        public IndicatorOneCandleChartType TypeIndicator { get; set; }

        /// <summary>
        /// name of data series on which indicator will be drawn
        /// имя серии данных на которой будет прорисован индикатор
        /// </summary>
        public string NameSeries { get; set; }

        /// <summary>
        /// name of data area where indicator will be drawn
        /// имя области данных на которой будет прорисовываться индикатор
        /// </summary>
        public string NameArea { get; set; }

        /// <summary>
        /// channel upper limit
        /// верхняя граница канала
        /// </summary>
        public List<decimal> ValuesUp { get; set; }

        /// <summary>
        /// channel bottom edge
        /// нижняя граница канала
        /// </summary>
        public List<decimal> ValuesDown { get; set; }

        /// <summary>
        /// unique indicator name
        /// уникальное имя индикатора
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// color of central data series
        /// цвет верхней серии данных
        /// </summary>
        public Color ColorUp { get; set; }

        /// <summary>
        /// lower data color
        /// цвет нижней серии данных
        /// </summary>
        public Color ColorDown { get; set; }

        /// <summary>
        /// is indicator tracing enabled
        /// включена ли прорисовка индикатора
        /// </summary>
        public bool PaintOn { get; set; }

       public List<Color> ColorSeries { get; set; }

        /// <summary>
        /// deviation for indicator calculation
        /// отклонение для расчёта индикатора
        /// </summary>
        public decimal Deviation;

        /// <summary>
        /// save settings to file
        /// сохранить настройки в файл
        /// </summary>
        public void Save()
        {
            MovingShadowBody.Save();
            MovingUp.Save();
            MovingDown.Save();
            try
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    return;
                }

                using (StreamWriter writer = new StreamWriter(@"Engine\" + Name + @".txt", false))
                {
                    writer.WriteLine(ColorUp.ToArgb());
                    writer.WriteLine(ColorDown.ToArgb());
                    writer.WriteLine(PaintOn);
                    writer.WriteLine(Deviation);
                    writer.WriteLine(MaLenth);
                    writer.Close();
                }
            }
            catch (Exception)
            {
                // send to log
                // отправить в лог
            }
        }

        /// <summary>
        /// upload settings from file
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
                    ColorUp = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    ColorDown = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    PaintOn = Convert.ToBoolean(reader.ReadLine());
                    Deviation = Convert.ToDecimal(reader.ReadLine());
                    MaLenth = Convert.ToInt16(reader.ReadLine());
                    reader.ReadLine();

                    reader.Close();
                }


            }
            catch (Exception)
            {
                // send to log
                // отправить в лог
            }
        }

        /// <summary>
        /// delete file with settings
        /// удалить файл с настройками
        /// </summary>
        public void Delete()
        {
            if (File.Exists(@"Engine\" + Name + @".txt"))
            {
                File.Delete(@"Engine\" + Name + @".txt");
            }
            MovingShadowBody.Delete();
            MovingUp.Delete();
            MovingDown.Delete();
        }

        /// <summary>
        /// delete data
        /// удалить данные
        /// </summary>
        public void Clear()
        {
            if (ValuesUp != null)
            {
                ValuesUp.Clear();
                ValuesDown.Clear();
            }
            _myCandles = null;
        }

        /// <summary>
        /// display settings window
        /// показать окно с настройками
        /// </summary>
        public void ShowDialog()
        {
        }

        /// <summary>
        /// reload indicator
        /// перезагрузить индикатор
        /// </summary>
        public void Reload()
        {
            if (_myCandles == null)
            {
                return;
            }
            ProcessAll(_myCandles);

            if (NeadToReloadEvent != null)
            {
                NeadToReloadEvent(this);
            }
        }

        /// <summary>
        /// Show signal settings MA
        /// показать настройки сигнальной машки
        /// </summary>
        public void ShowMaSignalDialog()
        {
        }
        // calculation
        // расчёт

        /// <summary>
        /// candles to calculate indicator
        /// свечи для рассчёта индикатора
        /// </summary>
        private List<Candle> _myCandles;

        /// <summary>
        /// signal MA
        /// полный размер свечи машка
        /// </summary>
        public MovingAverage MovingShadowBody;
        /// <summary>
        /// Машка по максимуму свечи
        /// </summary>
        public MovingAverage MovingUp;
        /// <summary>
        /// Машка по минимуму свечи
        /// </summary>
        public MovingAverage MovingDown;
        /// <summary>
        /// Размер Машек
        /// </summary>
        public int MaLenth {
            get { return _maLenth; }
            set {
                _maLenth = value;
                config();                
                }
        }
        private void config ()
        {
            if (MovingUp != null)
            {
                MovingUp.Lenght = _maLenth;
                MovingDown.Lenght = _maLenth;
                MovingShadowBody.Lenght = _maLenth;
            }
        }
        private int _maLenth;
        /// <summary>
        /// calculate indicator
        /// рассчитать индикатор
        /// </summary>
        /// <param name="candles">candles/свечи</param>
        public void Process(List<Candle> candles)
        {

            _myCandles = candles;

            MovingShadowBody.Process(candles);
            MovingUp.Process(candles);
            MovingDown.Process(candles);
            if (ValuesDown != null &&
                ValuesDown.Count + 1 == candles.Count)
            {
                ProcessOne(candles);
            }
            else if (ValuesDown != null &&
                     ValuesDown.Count == candles.Count)
            {
                ProcessLast(candles);
            }
            else
            {
                ProcessAll(candles);
            }

        }

        /// <summary>
        /// indicator needs to be redrawn
        /// индикатор нужно перерисовать
        /// </summary>
        public event Action<IIndicatorCandle> NeadToReloadEvent;

        /// <summary>
        /// load only last candle
        /// прогрузить только последнюю свечку
        /// </summary>
        private void ProcessOne(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }

            if (ValuesUp == null)
            {
                ValuesUp = new List<decimal>();
                ValuesDown= new List<decimal>();
            }

            ValuesUp.Add(GetUpValue(candles.Count-1));
            ValuesDown.Add(GetDownValue(candles.Count - 1));
        }

        /// <summary>
        /// to upload from the beginning
        /// прогрузить с самого начала
        /// </summary>
        private void ProcessAll(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }

            MovingShadowBody.Values = null;
            MovingShadowBody.Process(candles);
            MovingUp.Values = null;
            MovingUp.Process(candles);
            MovingDown.Values = null;
            MovingDown.Process(candles);

            ValuesUp = new List<decimal>();
            ValuesDown= new List<decimal>();

            for (int i = 0; i < candles.Count; i++)
            {
                ValuesUp.Add(GetUpValue(i));
                ValuesDown.Add(GetDownValue(i));
            }
        }

        /// <summary>
        /// overload last value
        /// перегрузить последнее значение
        /// </summary>
        private void ProcessLast(List<Candle> candles)
        {
            if (candles == null)
            {
                return;
            }

            ValuesUp[ValuesUp.Count - 1] = GetUpValue(candles.Count-1);
            ValuesDown[ValuesDown.Count - 1] = GetDownValue(candles.Count - 1);
        }

        private decimal GetUpValue(int index)
        {
            if (MovingShadowBody.Values.Count <= index)
            {
                index = MovingShadowBody.Values.Count - 1;
            }
            return Math.Round(MovingUp.Values[index] + MovingShadowBody.Values[index]*(Deviation/100),5);
         //   return Math.Round(MovingUp.Values[index] + MovingUp.Values[index] * (Deviation / 100), 5);
        }

        private decimal GetDownValue(int index)
        {
            if (MovingShadowBody.Values.Count <= index)
            {
                index = MovingShadowBody.Values.Count - 1;
            }
            return Math.Round(MovingDown.Values[index] - MovingShadowBody.Values[index] * (Deviation / 100),5);
        //    return Math.Round(MovingDown.Values[index] - MovingDown.Values[index] * (Deviation / 100), 5);
        }
    }
}