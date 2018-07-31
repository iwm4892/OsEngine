/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Data;
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
            //MakeDataTable();
            if (data == null)
            {
                data = new List<PriseData>();
            }
            update(trades);
        }
        public ClasterData()
        {
            //MakeDataTable();
            data = new List<PriseData>();
        }
        /// <summary>
        /// отработанные идентификаторы сделок
        /// </summary>
        private List<String> Trades_id;
        /// <summary>
        /// проверка что сделка еще не общитана
        /// </summary>
        /// <param name="trade">сделка</param>
        /// <returns></returns>
        private bool isNewTrade(Trade trade)
        {
            if (Trades_id == null)
            {
                Trades_id = new List<string>();
            }
            if (trade.Id == ""|| Trades_id.IndexOf(trade.Id) == -1)
            {
                return true;
            }
            return false;
        }
        public void update(List<Trade> trades)
        {
            if (trades==null || trades.Count == 0)
            {
                return;
            }
            if (trades[0].Id == "")
            {
                data = new List<PriseData>();
            }
            //data.Clear(); //= new List<PriseData>();
            for (int i = 0; i < trades.Count; i++)
            {
                if (isNewTrade(trades[i]))
                {
                    add(trades[i]);
                }
            }
        }
        public System.Data.DataTable dataTable;

        /// <summary>
        /// данные объемов по ценам свечи
        /// </summary>
        public List<PriseData> data;
        /// <summary>
        /// Добавление данных в колекцию
        /// </summary>
        public void add(Trade trade)
        {
            //дозаполняем накопленные цены
            PriseData pd = data.Find(x => x.Price == trade.Price);
            if (pd.Price != 0)
            {
                pd.Add(trade);
                if (MaxData.volume < pd.volume)
                {
                    MaxData = pd;
                }
                return;
            }
            // добавляем новые цены
            pd = new PriseData();
            pd.Price = trade.Price;
            pd.Add(trade);
            data.Add(pd);

            if (MaxData.volume < pd.volume)
            {
                MaxData = pd;
            }
            //запоминаем id сделки
            if (trade.Id != "")
            {
                Trades_id.Add(trade.Id);
            }
            
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
            public decimal Price;
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
                if (volumeBuy > volumeSell)
                {
                    side = Side.Buy;
                }
                else
                {
                    side = Side.Sell;
                }
                volume += trade.Volume;

            }

        }
    }
}
