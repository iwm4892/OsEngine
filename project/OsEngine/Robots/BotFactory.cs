/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System.Collections.Generic;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots.CounterTrend;
using OsEngine.Robots.High_Frequency;
using OsEngine.Robots.MarketMaker;
using OsEngine.Robots.Patterns;
using OsEngine.Robots.Trend;
using OsEngine.Robots.VSA;

namespace OsEngine.Robots
{
    public class BotFactory
    {
        /// <summary>
        /// list robots name / 
        /// список доступных роботов
        /// </summary>
        public static List<string> GetNamesStrategy()
        {
            List<string> result = new List<string>();

            result.Add("MarketMakerBot");
            result.Add("PatternTrader");
            result.Add("HighFrequencyTrader");
            result.Add("Bollinger");
            result.Add("EnvelopTrend");
            result.Add("Williams Band");
            result.Add("TwoLegArbitrage");
            result.Add("ThreeSoldier");
            result.Add("PriceChannelTrade");
            result.Add("SmaStochastic");
            result.Add("ClusterCountertrend");
            result.Add("PairTraderSpreadSma");
            result.Add("WilliamsRangeTrade");
            result.Add("ParabolicSarTrade");
            result.Add("PivotPointsRobot");
            result.Add("RsiContrtrend");
            result.Add("PinBarTrade");
            //+++
            result.Add("PriceLavelBot");
            result.Add("EnvelopTrendBitmex");
            result.Add("EnvelopFlatBitmex");
            result.Add("FastDelta");
            result.Add("FastDelta_2");
            result.Add("MovingChanelFlat");
            result.Add("ArbitrageIndex");
            result.Add("ArbitrageFutures");
            result.Add("PriceChanel_work");
            return result;
        }

        /// <summary>
        /// create robot
        /// создать робота
        /// </summary>
        public static BotPanel GetStrategyForName(string nameClass, string name, StartProgram startProgram)
        {
            BotPanel bot = null;
            // примеры и бесплатные боты
            
            if (nameClass == "EnvelopTrend")
            {
                bot = new EnvelopTrend(name, startProgram);
            }
            if (nameClass == "ClusterCountertrend")
            {
                bot = new ClusterCountertrend(name, startProgram);
            }
            if (nameClass == "PatternTrader")
            {
                bot = new PatternTrader(name, startProgram);
            }
            if (nameClass == "HighFrequencyTrader")
            {
                bot = new HighFrequencyTrader(name, startProgram);
            }
            if (nameClass == "PivotPointsRobot")
            {
                bot = new PivotPointsRobot(name, startProgram);
            }
            if (nameClass == "Williams Band")
            {
                bot = new StrategyBillWilliams(name, startProgram);
            }
            if (nameClass == "MarketMakerBot")
            {
                bot = new MarketMakerBot(name, startProgram);
            }
            if (nameClass == "Bollinger")
            {
                bot = new StrategyBollinger(name, startProgram);
            }
            if (nameClass == "ParabolicSarTrade")
            {
                bot = new ParabolicSarTrade(name, startProgram);
            }
            if (nameClass == "PriceChannelTrade")
            {
                bot = new PriceChannelTrade(name, startProgram);
            }
            if (nameClass == "WilliamsRangeTrade")
            {
                bot = new WilliamsRangeTrade(name, startProgram);
            }
            if (nameClass == "SmaStochastic")
            {
                bot = new SmaStochastic(name, startProgram);
            }
            if (nameClass == "PinBarTrade")
            {
                bot = new PinBarTrade(name, startProgram);
            }
            if (nameClass == "TwoLegArbitrage")
            {
                bot = new TwoLegArbitrage(name, startProgram);
            }
            if (nameClass == "ThreeSoldier")
            {
                bot = new ThreeSoldier(name, startProgram);
            }
            if (nameClass == "RsiContrtrend")
            {
                bot = new RsiContrtrend(name, startProgram);
            }
            if (nameClass == "PairTraderSpreadSma")
            {
                bot = new PairTraderSpreadSma(name, startProgram);
            }
            //++++
            if (nameClass == "PriceLavelBot")
            {
                bot = new PriceLavelBot(name, startProgram);
            }
            if (nameClass == "EnvelopTrendBitmex")
            {
                bot = new EnvelopTrendBitmex(name, startProgram);
            }
            if (nameClass == "EnvelopFlatBitmex")
            {
                bot = new EnvelopFlatBitmex(name, startProgram);
            }
            if (nameClass == "FastDelta")
            {
                bot = new FastDelta(name, startProgram);
            }
            if (nameClass == "GridBot")
            {
                bot = new GridBot(name, startProgram);
            }
            if (nameClass == "FastDelta_2")
            {
                bot = new FastDelta_2(name, startProgram);
            }
            if (nameClass == "MovingChanelFlat")
            {
                bot = new MovingChanelFlat(name, startProgram);
            }
            if (nameClass == "ArbitrageIndex")
            {
                bot = new ArbitrageIndex(name, startProgram);
            }
            if (nameClass == "ArbitrageFutures")
            {
                bot = new ArbitrageFutures(name, startProgram);
            }
            if (nameClass == "PriceChanel_work")
            {
                bot = new PriceChanel_work(name, startProgram);
            }
            return bot;
        }
    }
}
