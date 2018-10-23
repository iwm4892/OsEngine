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
    public class PriceLevleLine : IIndicatorCandle
    {
        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="uniqName">уникальное имя</param>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public PriceLevleLine(string uniqName, bool canDelete)
        {
            Name = uniqName;
            CanDelete = canDelete;
            init();
            Load();
        }

        /// <summary>
        /// индикатор без параметнов. Не будет сохраняться
        /// используется ТОЛЬКО для создания составных индикаторов
        /// не используйте его из слоя создания роботов!
        /// </summary>
        /// <param name="canDelete">можно ли пользователю удалить индикатор с графика вручную</param>
        public PriceLevleLine(bool canDelete)
        {
            Name = Guid.NewGuid().ToString();
            CanDelete = canDelete;
            init();
        }
        private void init()
        {
            TypeIndicator = IndicatorOneCandleChartType.Line;
            ColorBase = Color.DodgerBlue;
            PaintOn = true;
            linewidth = 0.02m;
        }
        /// <summary>
        /// Толщина линии (пример 0.01 = 1%)
        /// </summary>
        public Decimal linewidth;
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
                colors.Add(ColorBase);
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

        public List<Color> ColorSeries { get; set; }

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
                    writer.WriteLine(ColorBase.ToArgb());
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
                    ColorBase = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    PaintOn = Convert.ToBoolean(reader.ReadLine());
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
            // ignored. Этот тип индикатора настраивается и создаётся только из кода
        }

        /// <summary>
        /// индикатор нужно перерисовать
        /// </summary>
        public event Action<IIndicatorCandle> NeadToReloadEvent;

        #region Кластера
        public List<ClasterData> data;
        #endregion 

        private DateTime LastDay;
        /// <summary>
        /// прогрузить индикатор свечками
        /// </summary>
        public void Process(List<Candle> candles)
        {
            if (Values == null)
            {
                Values = new List<decimal>();
            }
            if (data != null &&
                           data.Count + 1 == candles.Count)
            {
                ProcessOneCandle(candles);
            }
            else if (data != null &&
                data.Count == candles.Count)
            {
                ProcessLastCanlde(candles);
            }
            else
            {
                ProcessAllCandle(candles);
            }


        }
        public List<levlel> LevleData = new List<levlel>();

        private bool updateLevelData(levlel lvl)
        {
            levlel findlvl = LevleData.Find(x => x.Value * (1 + linewidth / 2) > lvl.Value && x.Value * (1 - linewidth / 2) < lvl.Value);
            if (findlvl != null)
            {
                findlvl.levlSide = lvl.levlSide;
                findlvl.Value = lvl.Value;
                return true;
            }
            return false;
        }
        private void add(levlel el)
        {

            LevleData.Add(el);
            LevleData.Sort((a, b) => decimal.Compare(a.Value, b.Value));
            if (LevleData.Count > 6)
            {
                decimal avg = (LevleData[LevleData.Count - 1].Value + LevleData[0].Value) / 2;
                if (el.Value > avg)
                {
                    LevleData.RemoveAt(0);
                }
                if (el.Value < avg)
                {
                    LevleData.RemoveAt(LevleData.Count - 1);
                }
            }

        }
        private void ProcessValue()
        {
            if (data.Count > 2)
            {
                if (data[data.Count - 1].MaxData.Price <= data[data.Count - 2].MaxData.Price
                    && data[data.Count - 3].MaxData.Price <= data[data.Count - 2].MaxData.Price
                    && data[data.Count - 1].MaxData.Price != data[data.Count - 3].MaxData.Price
                    )
                {
                    levlel el = new levlel();
                    el.levlSide = Side.Sell;
                    el.Value = data[data.Count - 2].MaxData.Price;

                    if (!updateLevelData(el))
                    {
                        add(el);
                    }
                    Values.Add(data[data.Count - 2].MaxData.Price);
                }

                if (data[data.Count - 1].MaxData.Price >= data[data.Count - 2].MaxData.Price
                    && data[data.Count - 3].MaxData.Price >= data[data.Count - 2].MaxData.Price
                    && data[data.Count - 1].MaxData.Price != data[data.Count - 3].MaxData.Price
                    )
                {
                    levlel el = new levlel();
                    el.levlSide = Side.Buy;
                    el.Value = data[data.Count - 2].MaxData.Price;

                    if (!updateLevelData(el))
                    {
                        add(el);
                    }

                    Values.Add(data[data.Count - 2].MaxData.Price);

                }
            }


            /*
            for (int i = LevleData.Count - 1; i >= 4; i--)
            {
                if (i > LevleData.Count - 1)
                {
                    i = LevleData.Count - 1;
                }
                if (LevleData[i].levlSide == Side.Buy

                    && LevleData[i - 1].levlSide == Side.Sell
                    && LevleData[i - 2].levlSide == Side.Buy
                    && LevleData[i - 3].levlSide == Side.Sell
                    && LevleData[i].Value < LevleData[i - 2].Value
                    && LevleData[i - 1].Value < LevleData[i - 3].Value
                    )
                {
                    LevleData.RemoveAt(i - 2);
                    LevleData.RemoveAt(i - 2);
                }
                else
                {
                    if (LevleData[i].levlSide == Side.Sell
                    && LevleData[i - 1].levlSide == Side.Buy
                    && LevleData[i - 2].levlSide == Side.Sell
                    && LevleData[i - 3].levlSide == Side.Buy

                    && LevleData[i].Value > LevleData[i - 2].Value
                    && LevleData[i - 1].Value > LevleData[i - 3].Value
                    )
                    {
                        LevleData.RemoveAt(i - 2);
                        LevleData.RemoveAt(i - 2);
                    }
                }

            }

            // исправление однонаправленности экстремумов

            for (int i = 1; i < LevleData.Count; i++)
            {
                if (LevleData[i].levlSide == LevleData[i - 1].levlSide)
                {
                    LevleData.RemoveAt(i - 1);
                    i--;
                }

            }
            */
        }
        /// <summary>
        /// прогрузить только последнюю свечку
        /// </summary>
        private void ProcessOneCandle(List<Candle> candles)
        {
            if (data == null)
            {
                data = new List<ClasterData>();

            }
            if (LastDay != candles[candles.Count - 1].TimeStart.Date)
            {
                LevleData = new List<levlel>();
                LastDay = candles[candles.Count - 1].TimeStart.Date;
            }
            ProcessValue();
            ClasterData clasterData = GetValue(candles, candles.Count - 1);
            data.Add(clasterData);

        }

        /// <summary>
        /// прогрузить все свечи
        /// </summary>
        private void ProcessAllCandle(List<Candle> candles)
        {
            data = new List<ClasterData>();
            for (int i = 0; i < candles.Count; i++)
            {
                if (LastDay != candles[i].TimeStart.Date)
                {
                    LevleData = new List<levlel>();
                    LastDay = candles[i].TimeStart.Date;
                }
                ClasterData clasterData = GetValue(candles, i);
                data.Add(clasterData);
                ProcessValue();
            }
        }

        /// <summary>
        /// перегрузить последнюю свечу
        /// </summary>
        private void ProcessLastCanlde(List<Candle> candles)
        {
            data[data.Count - 1].update(candles[candles.Count - 1].Trades);
        }

        /// <summary>
        /// взять значение индикаторм по индексу
        /// </summary>
        private ClasterData GetValue(List<Candle> candles, int index)
        {
            if (index > candles.Count - 1)
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


        /// <summary>
        /// прогрузить новыми значениями
        /// </summary>
        /// <param name="decimals"></param>
        public void ProcessDesimals(List<Decimal> decimals)
        {
            Values = decimals;
        }
        public class levlel
        {
            public Side levlSide;
            public decimal Value;
        }

        /// <summary>
        /// Список Горизонтальных линий для визуализации уровней
        /// </summary>
        private List<Elements.LineHorisontal> lines;

        public void PaintLevleData(List<OsTrader.Panels.Tab.BotTabSimple> _tabs)
        {
            if(lines == null)
            {
                lines = new List<Elements.LineHorisontal>();
            }
            for (int i = 0; i < LevleData.Count; i++)
            {
                Elements.LineHorisontal line = lines.Find(x => x.Value == LevleData[i].Value);
                if (line != null)
                {
                }
                else
                {
                    line = new Elements.LineHorisontal("line" + LevleData[i].Value.ToString(), "Prime", false)
                    {
                        Color = Color.Yellow,
                        Value = LevleData[i].Value,
                    };
                    lines.Add(line);
                    for (int ind=0; ind < _tabs.Count; ind++) {
                        _tabs[ind].SetChartElement(line);
                    }

                }
            }
            for (int i = 0; i < lines.Count; i++)
            {
                levlel lvl = LevleData.Find(x => x.Value == lines[i].Value);
                if (lvl == null)
                {
                    for (int ind = 0; ind < _tabs.Count; ind++)
                    {
                        _tabs[ind].DeleteChartElement(lines[i]);
                    }
                    lines.RemoveAt(i);
                }
                else
                {
                    lines[i].Refresh();
                }

            }

            

        }
    }
}