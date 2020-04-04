/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OsEngine.Entity;
using System.Linq;
using OsEngine.Indicators;

namespace OsEngine.Charts.CandleChart.Indicators
{
    /// <summary>
    ///  Volume. Объём свечек. Индикатор
    /// </summary>
    public class MarketProfile: IIndicator
    {

        /// <summary>
        /// конструктор с параметрами. Индикатор будет сохраняться
        /// </summary>
        /// <param name="uniqName">уникальное имя</param>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public MarketProfile(string uniqName,bool canDelete)
        {
            Name = uniqName;
            TypeIndicator = IndicatorChartPaintType.Line;
            ColorBase = Color.DarkOrange;
            LenCount = 6;
            CandlesCount =10;
            /*
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            */

            PaintOn = true;
            CanDelete = canDelete;
            initValues();
            Load();
        }

        /// <summary>
        /// конструктор без параметров. Индикатор не будет сохраняться
        /// используется ТОЛЬКО для создания составных индикаторов
        /// не используйте его из слоя создания роботов!
        /// </summary>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public MarketProfile(bool canDelete)
        {
            Name = Guid.NewGuid().ToString();
            TypeIndicator = IndicatorChartPaintType.Line;
            ColorBase = Color.DarkOrange;
            LenCount = 6;
            CandlesCount = 10;
            /*
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            */
            PaintOn = true;
            CanDelete = canDelete;
            initValues();
        }
        /// <summary>
        /// Нужное количество максимальных объекмов
        /// </summary>
        private int LenCount;
        /// <summary>
        /// Количество обсчитываемых свечек
        /// </summary>
        public int CandlesCount;
        /// <summary>
        /// Обсчитанные сделки
        /// </summary>
        private List<Trade> trades = new List<Trade>();
        /// <summary>
        /// объемы по ценам
        /// </summary>
        public Dictionary<Decimal, Decimal> Data = new Dictionary<decimal, decimal>();
        /// <summary>
        /// все значения индикатора
        /// </summary>
        private List<List<decimal>> Values;
        /// <summary>
        /// Последняя обработаная сделка
        /// </summary>
        private int _lastTradeIndex;
        List<List<decimal>> IIndicator.ValuesToChart
        {
            get
            {
                if (Values == null)
                {
                    initValues();
                    return Values;//new List<List<decimal>>();
                }
                return Values;
            }
        }

        /// <summary>
        /// цвета для индикатора
        /// </summary>
        List<Color> IIndicator.Colors
        {
            get
            {
                /*
                List<Color> colors = new List<Color>();
                colors.Add(ColorBase);
                
                colors.Add(ColorUp);
                colors.Add(ColorDown);
                */
                List<Color> colors = initColors();
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
        public IndicatorChartPaintType TypeIndicator
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
                    /*
                    writer.WriteLine(ColorUp.ToArgb());
                    writer.WriteLine(ColorDown.ToArgb());
                    */
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
                    /*
                    ColorUp = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    ColorDown = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    */
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
        public event Action<IIndicator> NeadToReloadEvent;

// вычисления

        /// <summary>
        /// прогрузить индикатор свечками
        /// </summary>
        public void Process(List<Candle> candles)
        {
            if (Values != null &&
                           Values[0].Count + 1 == candles.Count)
            {
                ProcessOneCandle(candles);
            }
            else if (Values != null &&
                Values[0].Count == candles.Count)
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
            if (ColorSeries == null)
            {
                ColorSeries = new List<Color>();
            }
            _lastTradeIndex = 0;
            UpdateDate(candles[candles.Count - 1]);
            RemoveOldData(candles);
            List<decimal> _l = GetValues();
            for (int i = 0; i < LenCount; i++)
            {
                Values[i].Add(_l[i]);
            }
            ColorSeries.Add(GetColor(candles[candles.Count-1].ClasterData));
        }

        /// <summary>
        /// прогрузить все свечи
        /// </summary>
        private void ProcessAllCandle(List<Candle> candles)
        {
            initValues();
            ColorSeries = new List<Color>();
            for (int i = 0; i < candles.Count; i++)
            {
                _lastTradeIndex = 0;
                UpdateDate(candles[i]);
                RemoveOldData(candles);
                List<decimal> _l = GetValues();
                for (int j = 0; j < LenCount; j++)
                {
                    Values[j].Add(_l[j]);
                }
                ColorSeries.Add(GetColor(candles[i].ClasterData));
            }
        }

        /// <summary>
        /// перегрузить последнюю свечу
        /// </summary>
        private void ProcessLastCanlde(List<Candle> candles)
        {
            UpdateDate(candles[candles.Count-1]);
            List<decimal> _l = GetValues();
            for (int j = 0; j < LenCount; j++)
            {
                Values[j][Values[j].Count - 1] = _l[j];
            }
            ColorSeries[ColorSeries.Count - 1] = GetColor(candles[candles.Count - 1].ClasterData);
        }
        private void UpdateDate(Candle candle)
        {
            for (int i = _lastTradeIndex; i < candle.Trades.Count; i++)
            {
                if (Data.ContainsKey(candle.Trades[i].Price))
                {
                    Data[candle.Trades[i].Price] += candle.Trades[i].Volume;
                }
                else
                {
                    Data.Add(candle.Trades[i].Price, candle.Trades[i].Volume);
                }
                trades.Add(candle.Trades[i]);
            }
            _lastTradeIndex = candle.Trades.Count;
        }
        private void RemoveOldData(List<Candle> candles)
        {
            if(candles.Count <= CandlesCount)
            {
                return;
            }
            DateTime _date = candles[candles.Count -1 - CandlesCount].TimeStart;
            List<Trade> _trades = trades.FindAll(x=>x.Time < _date);
            if (_trades != null)
            {
                foreach(Trade tr in _trades)
                {
                    if (Data.ContainsKey(tr.Price))
                    {
                        Data[tr.Price] = Data[tr.Price] - tr.Volume;
                        if (Data[tr.Price] == 0)
                        {
                            Data.Remove(tr.Price);
                        }
                    }
                    trades.Remove(tr);
                }
            }
        }
        private List<Decimal> GetValues()
        {
            List<Decimal> result = new List<decimal>();

            Data = Data.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);
            for(int i = 1; i <= LenCount; i++)
            {
                if(Data.Count > i)
                {
                    result.Add(Data.ElementAt(i - 1).Key);
                }
                else
                {
                    result.Add(0);
                }
            }
            result.Sort();    
            return result;
        }
        private void initValues()
        {
            if (Values == null) { 
            Values = new List<List<decimal>>();
            for (int i = 0; i < LenCount; i++)
            {
                Values.Add(new List<decimal>());
            }
            }

        }
        private List<Color> initColors()
        {
            List<Color> result = new List<Color>();
            for (int i = 0; i < LenCount; i++)
            {
                result.Add(ColorBase);
            }
            return result;

        }
        /// <summary>
        /// Получить цвет по значению
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private Color GetColor(ClasterData val)
        {
            return ColorBase;
            /*
            if (val.MaxData.side == Side.Buy)
            {
                return ColorUp;
            }
            return ColorDown;
            */
        }
    }
}
