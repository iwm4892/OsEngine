﻿/*
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

    /// <summary>
    /// Fractals. Indicator Fractal bu Bill Williams
    /// Fractal. индикатор фрактал. В интерпритации Билла Вильямса
    /// </summary>
    public class Fractail_lenth : IIndicatorCandle
    {

        /// <summary>
        /// constructor
        /// конструктор
        /// </summary>
        /// <param name="uniqName">unique name/уникальное имя</param>
        /// <param name="canDelete">whether user can remove indicator from chart manually/можно ли пользователю удалить индикатор с графика вручную</param>
        public Fractail_lenth(string uniqName,bool canDelete)
        {
            Name = uniqName;

            TypeIndicator = IndicatorOneCandleChartType.Point;
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;
            PaintOn = true;
            CanDelete = canDelete;
            Lenght = 11;
            Load();
        }

        /// <summary>
        /// constructor without parameters.Indicator will not saved/конструктор без параметров. Индикатор не будет сохраняться
        /// used ONLY to create composite indicators/используется ТОЛЬКО для создания составных индикаторов
        /// Don't use it from robot creation layer/не используйте его из слоя создания роботов!
        /// </summary>
        /// <param name="canDelete">whether user can remove indicator from chart manually/можно ли пользователю удалить индикатор с графика вручную</param>
        public Fractail_lenth(bool canDelete) 
        {
            Name = Guid.NewGuid().ToString();

            TypeIndicator = IndicatorOneCandleChartType.Point;
            ColorUp = Color.DodgerBlue;
            ColorDown = Color.DarkRed;

            PaintOn = true;
            CanDelete = canDelete;
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
        /// тип индикатора
        /// </summary>
        public IndicatorOneCandleChartType TypeIndicator { get; set; }

        /// <summary>
        ///  name of data series on which indicator will be drawn
        /// имя серии данных на которой будет прорисовываться индикатор
        /// </summary>
        public string NameSeries { get; set; }

        /// <summary>
        /// name of data area where indicator will be drawn
        /// имя области данных на которой будет прорисовываться индикатор
        /// </summary>
        public string NameArea
        { get; set; }

        /// <summary>
        /// indicator name
        /// имя индикатора
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// color of upper data series
        /// цвет верхней серии данных
        /// </summary>
        public Color ColorUp { get; set; }

        /// <summary>
        /// color of lower data series
        /// цвет нижней серии данных
        /// </summary>
        public Color ColorDown { get; set; }

        /// <summary>
        /// is indicator tracing enabled
        /// включена ли прорисовка индикаторов
        /// </summary>
        public bool PaintOn { get; set; }
        /// <summary>
        /// Свечей для анализа
        /// </summary>
        public int Lenght { get; set; }
        
        private int center { get { return (int)(Lenght / 2) + 1; } }
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
                    PaintOn = Convert.ToBoolean(reader.ReadLine());
                    ColorUp = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    ColorDown = Color.FromArgb(Convert.ToInt32(reader.ReadLine()));
                    Lenght = Convert.ToInt32(reader.ReadLine());
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
        /// save settings to file
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
                    writer.WriteLine(PaintOn);
                    writer.WriteLine(ColorUp.ToArgb());
                    writer.WriteLine(ColorDown.ToArgb());
                    writer.WriteLine(Lenght);

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
        /// delete file with settings
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
        }

        /// <summary>
        /// display settings window
        /// показать окно настроек
        /// </summary>
        public void ShowDialog()
        {
        }

        /// <summary>
        /// upper fractals
        /// верхние фракталы
        /// </summary>
        public List<decimal> ValuesUp { get; set; }

        /// <summary>
        /// bottom fractals
        /// нижние фракталы
        /// </summary>
        public List<decimal> ValuesDown { get; set; }

       public List<Color> ColorSeries { get; set; }

        /// <summary>
        /// to upload new candles
        /// прогрузить новыми свечками
        /// </summary>
        public void Process(List<Candle> candles)
        {
            if (candles.Count <= Lenght || ValuesUp == null)
            {
                ValuesUp = new List<decimal>();
                ValuesDown = new List<decimal>();

                for (int i = 0; i < candles.Count; i++)
                {
                    ValuesUp.Add(0);
                    ValuesDown.Add(0);
                }
                ProcessAll(candles);
                return;
            }

            if (ValuesUp != null &&
                ValuesUp.Count + 1 == candles.Count)
            {
                ProcessOne(candles);
            }
            else if (ValuesUp != null && ValuesUp.Count != candles.Count)
            {
                ProcessAll(candles);
            }
        }

        /// <summary>
        /// it's necessary to redraw indicator
        /// необходимо перерисовать индикатор
        /// </summary>
        public event Action<IIndicatorCandle> NeadToReloadEvent;

        /// <summary>
        /// load only last candle
        /// прогрузить только последнюю свечку
        /// </summary>
        private void ProcessOne(List<Candle> candles)
        {
            if (ValuesUp == null)
            {
                ValuesUp = new List<decimal>();
                ValuesDown = new List<decimal>();
                ValuesUp.Add(GetValueUp(candles, candles.Count - 1));
                ValuesDown.Add(GetValueDown(candles, candles.Count - 1));
            }
            else
            {
                ValuesUp.Add(0);
                ValuesDown.Add(0);
               

                ValuesUp[ValuesUp.Count - center] = (GetValueUp(candles, candles.Count - 1));
                ValuesDown[ValuesDown.Count - center] = (GetValueDown(candles, candles.Count - 1));

                if (ValuesDown[ValuesDown.Count - center] != 0)
                {
                    for (int i = center+1; i <= Lenght; i++)
                    {
                        ValuesDown[ValuesDown.Count - i] = 0;
                    }
                }

                if (ValuesUp[ValuesUp.Count- center] != 0)
                {
                    for (int i = center+1; i <= Lenght; i++)
                    {
                        ValuesUp[ValuesUp.Count - i] = 0;
                    }
                }
            }
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
            ValuesUp = new List<decimal>();
            ValuesDown= new List<decimal>();
            for (int i = 0; i < candles.Count; i++)
            {
                ValuesUp.Add(0);
                ValuesDown.Add(0);
            }

            for (int i = center; i < candles.Count; i++)
            {

                    ValuesUp[i - center] = GetValueUp(candles, i);
                    if (ValuesUp[i - center] != 0)
                    {
                        for(int j = 1; j <= center; j++)
                        {
                            ValuesUp[i - center - j] = 0;
                        }
                    }

                    ValuesDown[i - center] = GetValueDown(candles, i);
                    if (ValuesDown[i - center] != 0)
                    {
                        for (int j = 1; j <= center; j++)
                        {
                            ValuesDown[i - center - j] = 0;
                        }
                    }
            }
        }

        /// <summary>
        ///  take upper value of indicator by index
        /// взять верхнее значение индикатора по индексу
        /// </summary>
        private decimal GetValueUp(List<Candle> candles, int index)
        {
            // fractal considered to be formed only after two candles have passed
            // фрактал у нас считается сформированным только после прошедших уже двух свечей
            // looking at trird candle from index
            // т.ч. смотрим трейтью свечу от индекса
            if (index - Lenght <= 0)
            {
                return 0;
            }
            bool canHigh = true;
            for(int i = 0; i < Lenght; i++)
            {
                if (i == center-1) { continue; }
                if(candles[index-center+1].High < candles[index - i].High)
                {
                    canHigh = false;
                }
            }
            if (canHigh)
            {
                return candles[index - center+1].High;
            }

            return 0;
        }

        /// <summary>
        /// take lower value of indicator by index
        /// взять нижнее значение индикатора по индексу
        /// </summary>
        private decimal GetValueDown(List<Candle> candles, int index)
        {
            // fractal considered to be formed only after two candles have passed
            // фрактал у нас считается сформированным только после прошедших уже двух свечей
            // looking at trird candle from index
            // т.ч. смотрим трейтью свечу от индекса
            if (index - Lenght <= 0)
            {
                return 0;
            }

            bool canLow = true;
            for (int i = 0; i < Lenght; i++)
            {
                if (i == center-1) { continue; }
                if (candles[index - center+1].Low > candles[index - i].Low)
                {
                    canLow = false;
                }
            }
            if (canLow)
            {
                return candles[index - center+1].Low;
            }
            return 0;
        }

    }
}
