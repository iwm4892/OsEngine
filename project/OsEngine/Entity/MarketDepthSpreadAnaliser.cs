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
            maFast = new MovingAverageSimle() { Lenth = 5 };
            maSlow = new MovingAverageSimle() { Lenth = 20 };
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
            Tabs[Tabs.Count - 1].PositionOpeningSuccesEvent += MarketDepthSpreadAnaliser_PositionOpeningSuccesEvent;
            Tabs[Tabs.Count - 1].PositionNetVolumeChangeEvent += MarketDepthSpreadAnaliser_PositionOpeningSuccesEvent;

            this.ProfitChangeEvent += MarketDepthSpreadAnaliser_ProfitChangeEvent;
            this.SpreadChangeEvent += MarketDepthSpreadAnaliser_SpreadChangeEvent;
            _Tabs _t = new _Tabs();
            
            _t._tab = tab;
            if(tab.PositionsOpenAll != null && tab.PositionsOpenAll.Count > 0)
            {
                _t.Open = tab.PositionsLast.EntryPrice;
                _t.side = tab.PositionsLast.Direction;
            }
            if (_tabs == null) _tabs = new List<_Tabs>(); 
            _tabs.Add(_t);
        }

        private void MarketDepthSpreadAnaliser_SpreadChangeEvent(decimal obj)
        {
            if (!CanChange) return;
            if (obj != _LastPriceInd)
            {
                _LastPriceInd = obj;
                maFast.Add(obj);
                maSlow.Add(obj);
            }
        }

        private void MarketDepthSpreadAnaliser_ProfitChangeEvent(decimal obj)
        {
            if (CanChange) return;
            if (obj != _LastPriceInd)
            {
                _LastPriceInd = obj;
                maFast.Add(obj);
                maSlow.Add(obj);
            }
        }

        private void MarketDepthSpreadAnaliser_PositionOpeningSuccesEvent(Position obj)
        {
            foreach (var el in _tabs)
            {
                Position p = el._tab.PositionsOpenAll.FindLast(x => x.Number == obj.Number);
                if (p != null)
                {
                    el.Open = obj.EntryPrice;
                }
                
            }
            CalcChanges();
            maFast = new MovingAverageSimle() { Lenth = 5 };
            maSlow = new MovingAverageSimle() { Lenth = 20 };

        }

        private void MarketDepthSpreadAnaliser_CandleFinishedEvent(List<Candle> candles)
        {
            CalcChanges();
            if (CanChange)
            {
                maFast = new MovingAverageSimle() { Lenth = 5 };
                maSlow = new MovingAverageSimle() { Lenth = 20 };
            }
        }
        private decimal _LastPriceInd = 0;
        private void MarketDepthSpreadAnaliser_MarketDepthUpdateEvent(MarketDepth obj)
        {
            if (Tabs.Count < 2)
            {
                return;
            }
            foreach(var tab in Tabs)
            {
                if (!tab.IsConnected) return;
                if (tab.MarketDepth == null) return;
                if (tab.MarketDepth.Asks == null || tab.MarketDepth.Asks.Count == 0) return;
                if (tab.MarketDepth.Bids == null || tab.MarketDepth.Bids.Count == 0) return;
                if (tab.PriceBestBid == 0 || tab.PriceBestAsk == 0) return;
                if (tab.CandlesAll == null || tab.CandlesAll.Count == 0) return;
                //если сильный спрэд то возможно ошибка в данных
                if ((tab.PriceBestAsk - tab.PriceBestBid) / tab.PriceBestBid > 0.005m) return;
                if ((tab.PriceBestAsk - tab.CandlesAll[tab.CandlesAll.Count-1].Close)/ tab.CandlesAll[tab.CandlesAll.Count - 1].Close > MaxSpread) return;
                if ((tab.CandlesAll[tab.CandlesAll.Count - 1].Close - tab.PriceBestBid) / tab.PriceBestBid > MaxSpread) return;
            }
            lock (_locker)
            {
                //CalcIndex();
                CalcChanges();
                decimal NewSpread = 0;
                foreach (var el in _tabs)
                {
                    if (el.Change == 0) return;

                    if(el.Slow && el.side == Side.Sell && el._tab.CandlesAll.Last().IsUp)
                    {
                        NewSpread += el.Change;
                    }
                    else if(el.Slow && el.side == Side.Sell && el._tab.CandlesAll.Last().IsDown)
                    {
                        NewSpread -= el.Change;
                    }
                    else if (el.Slow && el.side == Side.Buy  && el._tab.CandlesAll.Last().IsDown)
                    {
                        NewSpread += el.Change;
                    }
                    else if (el.Slow && el.side == Side.Buy && el._tab.CandlesAll.Last().IsUp)
                    {
                        NewSpread -= el.Change;
                    }
                    else if (el.Fast)
                    {
                        NewSpread += el.Change;
                    }
                }

                NewSpread = Math.Round(NewSpread, 2);
                if (Spread != NewSpread)
                {
                    if (SpreadChangeEvent != null)
                    {
                        SpreadChangeEvent(NewSpread);
                    }
                    Spread = NewSpread;
                }
                decimal NewProfit = 0;
                if (!CanChange)
                {
                    NewProfit = GetProfitPersent();
                    NewProfit = Math.Round(NewProfit, 2);
                    if (Profit != NewProfit)
                    {
                        if (SpreadChangeEvent != null)
                        {
                            ProfitChangeEvent(NewProfit);
                        }
                        Profit = NewProfit;
                    }
                }
            }
        }
        private void CalcIndex()
        {
            /*
            decimal ind = 0;
            decimal Last1 = _tabs[0]._tab.CandlesAll.Last().Close;
            decimal Last2 = _tabs[1]._tab.CandlesAll.Last().Close;
            if (_tabs[0].Fast)
            {
                ind = Last1 / Last2;
            }
            else
            {
                ind = Last2 / Last1;
            }
            ind = Math.Round(ind, 8);
            if(ind != _LastPriceInd)
            {
                _LastPriceInd = ind;
                maFast.Add(ind);
                maSlow.Add(ind);
            }
            */
        }
        private void CalcChanges()
        {
            lock (_locker)
            {
                bool _CanChange = CanChange;
                //Сначала расчитаем кто быстрее движется
                //Расчитываем отклонение
                foreach (var el in _tabs)
                {
                    if (_CanChange)
                    {
                        el.Open = el._tab.CandlesAll.Last().Open;
                    }
                    el.Change = CalcProfitPersent(el.Open, el._tab.PriceCenterMarketDepth);
                }
                decimal _averege = average;
                //выясняем кто быстрее (если выше средней то продажа, иначе покупка)
                if (_CanChange)
                {
                    foreach (var el in _tabs)
                    {
                        if (el.Change > _averege)
                        {
                            el.Fast = true;
                            el.Slow = false;
                        }
                        else
                            {
                                el.Slow = true;
                                el.Fast = false;
                            }
                    }
                    
                }
                List<_Tabs> Fast = _tabs.FindAll(x=>x.Fast);
                List<_Tabs> Slow = _tabs.FindAll(x => x.Slow);

                List<BotTabSimple> BuyTab = new List<BotTabSimple>();
                List<BotTabSimple> SellTab = new List<BotTabSimple>();

                foreach(var el in Fast)
                {
                    if (el._tab.CandlesAll.Last().IsUp)
                    {
                        SellTab.Add(el._tab);
                    }
                    else
                    {
                        BuyTab.Add(el._tab);
                    }
                }
                if (SellTab.Count > 0 && BuyTab.Count > 0)
                {
                    //не знаю как быть в таком случае
                    return;
                }
                foreach(var el in Slow)
                {
                    if (SellTab.Count > 0)
                    {
                        BuyTab.Add(el._tab);
                    }
                    else
                    {
                        SellTab.Add(el._tab);
                    }
                }
                if (_CanChange)
                {
                    foreach (var el in _tabs)
                    {
                        if (SellTab.FindLast(x => x.Equals(el._tab)) != null)
                        {
                            el.side = Side.Sell;
                        }
                        if (BuyTab.FindLast(x => x.Equals(el._tab)) != null)
                        {
                            el.side = Side.Buy;
                        }
                    }
                }
                //пересчитываем изменения с учетом направленности 
                foreach (var el in _tabs)
                {
                        if (el.side == Side.Buy)
                        {
                            el.Change = CalcProfitPersent(el.Open, el._tab.MarketDepth.Asks[0].Price);
                        }
                        else
                        {
                            el.Change = CalcProfitPersent(el.Open, el._tab.MarketDepth.Bids[0].Price);
                        }
                }

            }

        }
        /// <summary>
        /// Рсчет отклонения между точками
        /// </summary>
        /// <param name="open">Открытие</param>
        /// <param name="close">Закрытие</param>
        /// <returns></returns>
        private decimal CalcProfitPersent(decimal open, decimal close)
        {
            if (close > open)
            {
                return CalcProfitPersent(open, close, Side.Buy);
            }
            else
            {
                return CalcProfitPersent(open, close, Side.Sell);
            }
        }

        /// <summary>
        /// Рсчет отклонения между точками
        /// </summary>
        /// <param name="open">Открытие</param>
        /// <param name="close">Закрытие</param>
        /// <param name="side">Напрвление</param>
        /// <returns></returns>
        private decimal CalcProfitPersent(decimal open, decimal close, Side side)
        {
            if (side == Side.Buy)
            {
                return close / open * 100 - 100;
            }
            else
            {
                return -(close / open * 100 - 100);
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
        public bool CanChange
        {
            get
            {
                foreach (var el in Tabs)
                {
                    if(el.PositionsOpenAll == null || el.PositionsOpenAll.Count != 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        
        public decimal GetProfitPersent()
        {
            decimal result = 0;
            foreach(var el in _tabs)
            {
                List<Position> pos = el._tab.PositionsOpenAll;
                if (pos == null || pos.Count == 0) return 0;
                foreach (var p in pos)
                {
                    if (p.OpenVolume != 0)
                    {
                        decimal ClosePrice = 0;
                        if (el.side == Side.Buy)
                        {
                            ClosePrice = GetPrice(el._tab.MarketDepth.Bids, p);
                        }
                        if (el.side == Side.Sell)
                        {
                            ClosePrice = GetPrice(el._tab.MarketDepth.Asks, p);
                        }
                        result += CalcProfitPersent(el.Open, ClosePrice, el.side);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Расчет средневзвешеной цены
        /// </summary>
        /// <param name="marketDepthLevels"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private decimal GetPrice(List<MarketDepthLevel> marketDepthLevels, Position p)
        {
            decimal ost = p.OpenVolume;
            decimal sum=0;
            foreach(var el in marketDepthLevels)
            {
                decimal min = 0;
                if (p.Direction == Side.Buy)
                {
                    min = Math.Min(el.Bid, ost);
                }
                else
                {
                    min = Math.Min(el.Ask, ost);
                }
                sum += min * el.Price;
                ost -= min;

                if (ost == 0) break;
            }

            return sum / p.OpenVolume;
        }

        private object _locker = new object();
        private object _locker2 = new object();

        /// <summary>
        /// Вкладки данных
        /// </summary>
        public List<BotTabSimple> Tabs;
        /// <summary>
        /// Спред в % между вкладками
        /// </summary>
        public decimal Spread;

        public decimal Profit;
        /// <summary>
        /// Максимальный спред внутри одного инструмента(если больше то считаем что пришли ошибочные данные)
        /// </summary>
        public decimal MaxSpread;

        public bool CanTrade
        {
            get
            {
                bool _result = false;
                if (maFast.lastMa == 0 || maSlow.lastMa == 0) return _result;
                if(
                    (maSlow.lastMa>0 && maFast.lastMa < maSlow.lastMa)
                    ||(maSlow.lastMa < 0 && maFast.lastMa > maSlow.lastMa)
                    )
                {
                    _result = true;
                }
                return _result;
            }
        }

        private MovingAverageSimle maFast;
        private MovingAverageSimle maSlow;
        /// <summary>
        /// Таблица изменений цены в рамках одной свечи
        /// </summary>
        public class _Tabs
        {
            public BotTabSimple _tab;
            public decimal Change = 0;
            public decimal Open;
            /// <summary>
            /// Коэффт для сложения спрэдов
            /// </summary>
            public int Koeff;
            public Side side;
            public bool Fast;
            public bool Slow;
        }
        /// <summary>
        /// Список таблиц и их изменения
        /// </summary>
        public List<_Tabs> _tabs;

        public event Action<decimal> SpreadChangeEvent;

        public event Action<decimal> ProfitChangeEvent;
    }
}
