using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Entity
{
    class MarketDepthSpreadAnaliser
    {
        public MarketDepthSpreadAnaliser()
        {
            Tabs = new List<BotTabSimple>();
            MaxSpread = 0.005m;
        }
        public MarketDepthSpreadAnaliser(decimal _maxSpread)
        {
            Tabs = new List<BotTabSimple>();
            MaxSpread = _maxSpread;
        }
        public void addTab(BotTabSimple tab)
        {
            Tabs.Add(tab);
            Tabs[Tabs.Count - 1].MarketDepthUpdateEvent += MarketDepthSpreadAnaliser_MarketDepthUpdateEvent;
        }

        private void MarketDepthSpreadAnaliser_MarketDepthUpdateEvent(MarketDepth obj)
        {
            if (Tabs.Count < 2)
            {
                return;
            }
            foreach(var tab in Tabs)
            {
                if (!tab.IsConnected) return;
                if (tab.PriceBestBid == 0 || tab.PriceBestAsk == 0) return;
                if (tab.CandlesAll == null || tab.CandlesAll.Count == 0) return;
                //если сильный спрэд то возможно ошибка в данных
                if ((tab.PriceBestAsk - tab.PriceBestBid) / tab.PriceBestBid > 0.005m) return;
                if ((tab.PriceBestAsk - tab.CandlesAll[tab.CandlesAll.Count-1].Close)/ tab.CandlesAll[tab.CandlesAll.Count - 1].Close > MaxSpread) return;
                if ((tab.CandlesAll[tab.CandlesAll.Count - 1].Close - tab.PriceBestBid) / tab.PriceBestBid > MaxSpread) return;
            }
            lock (_locker)
            {
                if (Tabs[1].PriceBestAsk == 0) return;
                decimal NewSpread = 100*((Tabs[0].PriceBestBid - Tabs[1].PriceBestAsk) / Tabs[1].PriceBestAsk);
                NewSpread = Math.Round(NewSpread, 2);
                if (Spread != NewSpread)
                {
                    if (SpreadChangeEvent != null)
                    {
                        SpreadChangeEvent(NewSpread);
                    }
                    Spread = NewSpread;
                }
            }
        }
        private object _locker = new object();
        /// <summary>
        /// Вкладки данных
        /// </summary>
        public List<BotTabSimple> Tabs;
        /// <summary>
        /// Спред в % между вкладками
        /// </summary>
        public decimal Spread;
        /// <summary>
        /// Максимальный спред внутри одного инструмента(если больше то считаем что пришли ошибочные данные)
        /// </summary>
        public decimal MaxSpread;

        public event Action<decimal> SpreadChangeEvent;
    }
}
