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
            data = new List<PriseData>();
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
            decimal max=0;

            for(int i = 0; i < data.Count; i++)
            {
                if (data[i].volume > max)
                {
                    MaxData = data[i];
                    max = data[i].volume;
                       
                }
            }
        }
        public System.Data.DataTable dataTable;

        private void MakeDataTable()
        {
            dataTable = new DataTable();
            DataColumn column = new DataColumn("Price", typeof(decimal));
            column.Unique = true;
            dataTable.Columns.Add(column);

            dataTable.Columns.Add(new DataColumn("Volume", typeof(decimal)));
            dataTable.Columns.Add(new DataColumn("Side", typeof(Side)));
            dataTable.Columns.Add(new DataColumn("volumeBuy", typeof(decimal)));
            dataTable.Columns.Add(new DataColumn("volumeSell", typeof(decimal)));

            DataColumn[] PrimaryKeyColumns = new DataColumn[1];
            PrimaryKeyColumns[0] = dataTable.Columns["Price"];
            dataTable.PrimaryKey = PrimaryKeyColumns;
        }
        /// <summary>
        /// данные объемов по ценам свечи
        /// </summary>
        public List<PriseData> data;
        /// <summary>
        /// Добавление данных в колекцию
        /// </summary>
        public void add(Trade trade)
        {
            /*
            DataRow row = dataTable.Rows.Find(trade.Price);
            if (row == null)
            {
                row = dataTable.Rows.Add();
                row["Price"] = trade.Price;
            }
            
            //запоминаем объемы
            if (trade.Side == Side.Buy)
            {
                row["volumeBuy"] = (decimal)row["volumeBuy"] + trade.Volume;
            }
            else
            {
                row["volumeSell"] = (decimal)row["volumeSell"] + trade.Volume;
            }
            // берем максимальный объем
            row["volume"] = Math.Max((decimal)row["volumeBuy"], (decimal)row["volumeSell"]);
            
            //определяем сторону объема
            if (row["volume"] == row["volumeBuy"])
            {
                row["side"] = Side.Buy;
            }
            else
            {
                row["side"] = Side.Sell;
            }
            */
            //дозаполняем накопленные цены
            PriseData pd = data.Find(x => x.Price == trade.Price);
            if (pd.Price != 0)
            {
                pd.Add(trade);
                return;
            }
            // добавляем новые цены
            pd = new PriseData();
            pd.Price = trade.Price;
            pd.Add(trade);
            data.Add(pd);
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
