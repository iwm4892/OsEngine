using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Robots.MarketMaker
{
    public class ArbitrageTest : BotPanel
    {
        public ArbitrageTest(string name, StartProgram startProgram) : base(name, startProgram)
        {
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Simple);
            TabCreate(BotTabType.Index);

            _tabToTrade1 = TabsSimple[0];
            _tabToTrade2 = TabsSimple[1];

            _tabIndex = TabsIndex[0];

        //    _ma = new MovingAverage(name + "MA", false);
        //    _ma = (MovingAverage)_tabIndex.CreateCandleIndicator(_ma, "Prime");
        //    _ma.Save();

         //   _atr = new Atr(name + "Atr", false);
         //   _atr = (Atr)_tabIndex.CreateCandleIndicator(_atr, "Second");
         //   _atr.Save();

        }

        private StrategyParameterInt _lengthMa;


        private MovingAverage ma;

        private Atr atr;

        private BotTabSimple _tabToTrade1;

        private BotTabSimple _tabToTrade2;

        private BotTabIndex _tabIndex;

        public override string GetNameStrategyType()
        {
            return "ArbitrageIWM";

        }

        public override void ShowIndividualSettingsDialog()
        {
            //ignore
        }
    }
}