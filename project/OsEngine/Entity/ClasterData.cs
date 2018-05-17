/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using OsEngine.Market.Servers.Entity;
using System.Collections.Generic;

namespace OsEngine.Entity
{
    /// <summary>
    /// Данные одной свечи
    /// </summary>
    public class ClasterData
    {
        public ClasterData(List<Trade> trades)
        {
            update(trades);
        }
        public ClasterData()
        {
        }
        public void update(List<Trade> trades)
        {
            data.Clear();
            for (int i = 0; i < trades.Count; i++)
            {
                add(trades[i]);
            }
            decimal max=0;

            for(int i = 0; i < data.Count; i++)
            {
                if (data[i].volume > max)
                {
                    MaxData = data[i];
                }
            }
        }
        /// <summary>
        /// данные объемов по ценам свечи
        /// </summary>
        List<PriseData> data;
        /// <summary>
        /// Добавление данных в колекцию
        /// </summary>
        public void add(Trade trade)
        {
            //дозаполняем накопленные цены
            for (int i = 0;i<data.Count;i++)
            {
                if (data[i].prise == trade.Price)
                {
                    data[i].Add(trade);
                    return;
                }
            }
            // добавляем новые цены
            PriseData _dt = new PriseData();
            _dt.prise = trade.Price;
            _dt.Add(trade);
            data.Add(_dt);
        }
        /// <summary>
        /// Направление максимального объема Buy/Sell
        /// </summary>
        public PriseData MaxData;

        /// <summary>
        /// данные конкретной цены
        /// </summary>
        public struct PriseData
        {
            /// <summary>
            /// цена
            /// </summary>
            public decimal prise;
            /// <summary>
            /// объем
            /// </summary>
            public decimal volume;
            /// <summary>
            /// направление
            /// </summary>
            public Side side;
            /// <summary>
            /// объем на покупку
            /// </summary>
            private decimal volumeBuy;
            /// <summary>
            /// объем на продажу
            /// </summary>
            private decimal volumeSell;


            public void Add(Trade trade)
            {
                if (trade.Side == Side.Buy)
                {
                    volumeBuy += trade.Volume;
                }
                else
                {
                    volumeSell += trade.Volume;
                }
                volume = Math.Max(volumeBuy, volumeSell);
                if(volume== volumeBuy) {
                    side = Side.Buy;
                }
                else
                {
                    side = Side.Sell;
                }

            }

        }
    }
}
