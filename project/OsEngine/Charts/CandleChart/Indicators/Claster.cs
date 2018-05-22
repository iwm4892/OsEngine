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
    ///  Volume. Объём свечек. Индикатор
    /// </summary>
    public class Claster:IIndicatorCandle
    {

        /// <summary>
        /// конструктор с параметрами. Индикатор будет сохраняться
        /// </summary>
        /// <param name="uniqName">уникальное имя</param>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public Claster(string uniqName,bool canDelete)
        {
            Name = uniqName;
            TypeIndicator = IndicatorOneCandleChartType.Column;
            ColorBase = Color.DeepSkyBlue;
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            PaintOn = false;
            CanDelete = canDelete;
            Load();
        }

        /// <summary>
        /// конструктор без параметров. Индикатор не будет сохраняться
        /// используется ТОЛЬКО для создания составных индикаторов
        /// не используйте его из слоя создания роботов!
        /// </summary>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public Claster(bool canDelete)
        {
            Name = Guid.NewGuid().ToString();
            TypeIndicator = IndicatorOneCandleChartType.Column;
            ColorBase = Color.DeepSkyBlue;
            
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            
            PaintOn = false;
            CanDelete = canDelete;
        }
        
        /// <summary>
        /// Данные кластеров
        /// </summary>
        public List<ClasterData> data;
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
                //colors.Add(ColorBase);
                
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
        /// тип индикатора
        /// </summary>
        public IndicatorOneCandleChartType TypeIndicator
        { get; set; }

        /// <summary>
        /// имя серии данных на которой будет прорисовываться индикатор
        /// </summary>
        public string NameSeries
        { get; set; }

        /// <summary>
        /// имя области данных на которой будет прорисовываться индикатор
        /// </summary>
        public string NameArea
        { get; set; }

        /// <summary>
        /// объём
        /// </summary>
        public List<decimal> Values
        { get; set; }

        /// <summary>
        /// уникальное имя
        /// </summary>
        public string Name
        { get; set; }

        
        /// <summary>
        /// цвет растущего объёма
        /// </summary>
        public Color ColorUp
        { get; set; }

        /// <summary>
        /// цвет падающего объёма
        /// </summary>
        public Color ColorDown
        { get; set; }
        

        /// <summary>
        /// цвет линии индикатора
        /// </summary>
        public Color ColorBase { get; set; }

        /// <summary>
        /// включена ли прорисовка индикатора на чарте
        /// </summary>
        public bool PaintOn
        { get; set; }
        /// <summary>
        /// Цвета значений
        /// </summary>
        public List<Color> ColorSeries { get; set; }

        /// <summary>
        /// сохранить настройки в файл
        /// </summary>
        public void Save()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    return;
                }
                using (StreamWriter writer = new StreamWriter(@"Engine\" + Name + @".txt", false))
                {
                    writer.WriteLine(ColorBase.ToArgb());
                    
                    writer.WriteLine(ColorUp.ToArgb());
                    writer.WriteLine(ColorDown.ToArgb());
                    
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
                    ColorBase = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    
                    ColorUp = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    ColorDown = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    
                    PaintOn = Convert.ToBoolean(reader.ReadLine());
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
        /*    VolumeUi ui = new VolumeUi(this);
            ui.ShowDialog();

            if (ui.IsChange)
            {
                if (NeadToReloadEvent != null)
                {
                    NeadToReloadEvent(this);
                }
            }
            */
        }

        /// <summary>
        /// нужно перерисовать индикатор
        /// </summary>
        public event Action<IIndicatorCandle> NeadToReloadEvent;

// вычисления

        /// <summary>
        /// прогрузить индикатор свечками
        /// </summary>
        public void Process(List<Candle> candles)
        {
            if (Values != null &&
                           Values.Count + 1 == candles.Count)
            {
                ProcessOneCandle(candles);
            }
            else if (Values != null &&
                Values.Count == candles.Count)
            {
                ProcessLastCanlde(candles);
            }
            else
            {
                ProcessAllCandle(candles);
            }
        }

        /// <summary>
        /// прогрузить только последнюю свечку
        /// </summary>
        private void ProcessOneCandle(List<Candle> candles)
        {
            if (Values == null)
            {
                Values = new List<decimal>();
                ColorSeries = new List<Color>();
                data = new List<ClasterData>();

            }
            ClasterData clasterData = GetValue(candles, candles.Count - 1);
            data.Add(clasterData);
            Values.Add(clasterData.MaxData.prise);
            ColorSeries.Add(GetColor(clasterData));
        }

        /// <summary>
        /// прогрузить все свечи
        /// </summary>
        private void ProcessAllCandle(List<Candle> candles)
        {
            Values = new List<decimal>();
            ColorSeries = new List<Color>();
            data = new List<ClasterData>();
            for (int i = 0; i < candles.Count; i++)
            {
                ClasterData clasterData = GetValue(candles, i);
                data.Add(clasterData);

                Values.Add(clasterData.MaxData.prise);
                ColorSeries.Add(GetColor(clasterData));
            }
        }

        /// <summary>
        /// перегрузить последнюю свечу
        /// </summary>
        private void ProcessLastCanlde(List<Candle> candles)
        {
            ClasterData clasterData = GetValue(candles, candles.Count - 1);
            data[data.Count - 1] = clasterData;

            Values[Values.Count-1]  = clasterData.MaxData.prise;
            ColorSeries[ColorSeries.Count - 1] = GetColor(clasterData);
        }
        /// <summary>
        /// Получить цвет по значению
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private Color GetColor(ClasterData val)
        {
            if (val.MaxData.side == Side.Buy)
            {
                return ColorUp;
            }
            return ColorDown;
        }
        /// <summary>
        /// взять значение индикаторм по индексу
        /// </summary>
        private ClasterData GetValue(List<Candle> candles, int index)
        {
            if (index > candles.Count-1)
            {
                return new ClasterData();
            }

            List<Trade> trades = candles[index].Trades;

            if (trades == null ||
                trades.Count == 0)
            {
                return new ClasterData();
            }

            ClasterData data = new ClasterData(trades);
            return data;        
        }

    }
}
