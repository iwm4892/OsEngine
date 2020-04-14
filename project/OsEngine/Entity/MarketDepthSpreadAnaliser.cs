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
            Tabs[Tabs.Count - 1].CandleFinishedEvent += MarketDepthSpreadAnaliser_CandleFinishedEvent;
            _Tabs _t = new _Tabs();
            
            _t._tab = tab;
            if (_tabs == null) _tabs = new List<_Tabs>(); 
            _tabs.Add(_t);
        }

        private void MarketDepthSpreadAnaliser_CandleFinishedEvent(List<Candle> candles)
        {
            CalcChanges();
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
                decimal NewSpread =0;
                decimal av = average;
                foreach (var el in _tabs)
                {
                    if (el.Change == 0) return;

                    if(el.Change > av)
                    {
                        el.side = Side.Sell;
                        NewSpread += el.Change;
                    }
                    else
                    {
                        el.side = Side.Buy;
                        NewSpread -= el.Change;
                    }
                }
                //    decimal NewSpread = 100*((Tabs[0].PriceBestBid - Tabs[1].PriceBestAsk) / Tabs[1].PriceBestAsk);
             
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
        private void CalcChanges()
        {
            foreach (var el in _tabs)
            {

                if (el._tab.PriceBestAsk > el._tab.CandlesAll.Last().Open)
                {
                    if (el._tab.PriceBestAsk == 0) return;
                    el.Change = 100 * (el._tab.PriceBestAsk - el._tab.CandlesAll.Last().Open) / el._tab.CandlesAll.Last().Open;
                }
                else
                {
                    if (el._tab.PriceBestBid == 0) return;
                    el.Change = 100 * (el._tab.PriceBestBid - el._tab.CandlesAll.Last().Open) / el._tab.CandlesAll.Last().Open;
                }
            }

        }
        public decimal average
        {
            get
            {
                decimal res = 0;
                foreach (var el in _tabs)
                {
                    res += el.Change;
                }
                return res / _tabs.Count;
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

        /// <summary>
        /// Таблица изменений цены в рамках одной свечи
        /// </summary>
        public class _Tabs
        {
            public BotTabSimple _tab;
            public decimal Change = 0;
            public Side side;
        }
        /// <summary>
        /// Список таблиц и их изменения
        /// </summary>
        public List<_Tabs> _tabs;

        public event Action<decimal> SpreadChangeEvent;
    }
}
