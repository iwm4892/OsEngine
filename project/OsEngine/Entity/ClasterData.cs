/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OsEngine.Entity
{
    /// <summary>
    /// Данные одной свечи
    /// </summary>
    public class ClasterData
    {
        public ClasterData(List<Trade> trades)
        {
            init();
            update(trades);
        }
        public ClasterData()
        {
            init();
        }
        private void init()
        {
            data = new List<PriseData>();

            locker = new Object();
            Trades_id = new List<string>();
            MaxData = new PriseData();
            minPrice = Decimal.MaxValue;


        }
        /// <summary>
        /// Максимальная цена в кластере
        /// </summary>
        public Decimal maxPrice;
        /// <summary>
        /// Минимальная цена в кластере
        /// </summary>
        public Decimal minPrice;
        /// <summary>
        /// Расстояние между минимум и максимум скластера
        /// </summary>
        public Decimal ClasterBody;
        /// <summary>
        /// Последняя обработаная сделка
        /// </summary>
        private int _lastTradeIndex;

        private long new_trade_num {
            get
            {
                _last_trade_num++;
                return _last_trade_num;
            }
        }
        private long _last_trade_num;
        private Object locker;
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
            if (trade.Id == ""|| Trades_id.IndexOf(trade.Id) == -1)
            {
                return true;
            }

            return false;
        }
        public void update(List<Trade> trades)
        {
            
            if (trades == null || trades.Count == 0)
            {
                return;
            }
            
            for(int i= _lastTradeIndex;i < trades.Count; i++)
            {
                addTrade(trades[i]);
            }
            
            /*
            Parallel.For(_lastTradeIndex, trades.Count, i =>
                {
                    add(trades[i]);
                });
            */
            
            _lastTradeIndex = trades.Count;
            
        }
        public void addTrades(List<Trade> trades)
        {
            foreach(Trade trade in trades)
            {
                addTrade(trade);
            }
            _lastTradeIndex = _lastTradeIndex + trades.Count;
        }

        /// <summary>
        /// данные объемов по ценам свечи
        /// </summary>
        public List<PriseData> data; // { get; set; }  = new List<PriseData>();

        /// <summary>
        /// Добавление данных в колекцию
        /// </summary>
        public void addTrade(Trade trade)
        {
            /*
            lock (locker)
            {
                if (trade.Id == "")
                {
                    trade.Id = new_trade_num.ToString();
                    //запоминаем id сделки
                    Trades_id.Add(trade.Id);
                }
                else if (!isNewTrade(trade))
                {
                    return;
                }
            }
            */
            //дозаполняем накопленные цены
            PriseData pd;
            lock (locker)
            {
                pd = data.Find(x => x.Price == trade.Price);
            }
            if (pd == null)
            {
                pd = new PriseData();
                pd.Price = trade.Price;
                lock (locker)
                {
                    data.Add(pd);
                }
            }
            pd.Add(trade);
            lock (locker)
            {
                if (MaxData.volume < pd.volume)
                {
                    MaxData = pd;
                }
                if (pd.Price > maxPrice)
                {
                    maxPrice = pd.Price;
                }
                if (pd.Price < minPrice)
                {
                    minPrice = pd.Price;
                }
                ClasterBody = maxPrice - minPrice;
            }

        }
        /// <summary>
        /// Направление максимального объема Buy/Sell
        /// </summary>
        public PriseData MaxData;

        /// <summary>
        /// данные конкретной цены
        /// </summary>

        public class PriseData
        {
            public PriseData()
            {
            }
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
                lock (this)
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
}
