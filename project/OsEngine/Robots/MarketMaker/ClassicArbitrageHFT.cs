/*
 * Your rights to use code governed by this license https://github.com/AlexWan/OsEngine/blob/master/LICENSE
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Drawing;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Market.Servers;
using System.Linq;

namespace OsEngine.Robots.MarketMaker
{
    public class ClassicArbitrageHFT : BotPanel
    {
        public ClassicArbitrageHFT(string name, StartProgram startProgram)
            : base(name, startProgram)
        {
            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;
            ParametrsChangeByUser += ArbitrageIndex_ParametrsChangeByUser;

        }
        private List<ServerTyped> serversList;
        private void ServerMaster_ServerCreateEvent(IServer server)
        {
            if (serversList != null && serversList.Count > 0)
                foreach (var serv in serversList)
                    if (serv == server)
                        return;

            serversList.Add(new ServerTyped(server) { });
            serversList.Last().ConnectStatusChangeEvent += ClassicArbitrageHFT_ConnectStatusChangeEvent;
            serversList.Last().NeadToReconnectEvent += ClassicArbitrageHFT_NeadToReconnectEvent;
        }

        private void ClassicArbitrageHFT_ConnectStatusChangeEvent(ServerType type, string obj)
        {
            foreach (var serv in serversList.Where(x => x.isActive == false && x.Type == type))
                if (serv.ServerStatus == ServerConnectStatus.Connect)
                {
                    serv.isActive = true;
                    serv.PortfoliosChangeEvent += Serv_PortfoliosChangeEvent;
                    serv.SecuritiesChangeEvent += Serv_SecuritiesChangeEvent;
                    serv.NewMyTradeEvent += Serv_NewMyTradeEvent;
                    serv.NewOrderIncomeEvent += Serv_NewOrderIncomeEvent;
                    serv.NewBidAscIncomeEvent += Serv_NewBidAscIncomeEvent;
                }
        }

        private void Serv_NewBidAscIncomeEvent(ServerType arg1, decimal arg2, decimal arg3, Security arg4)
        {
            throw new NotImplementedException();
        }

        private void Serv_NewOrderIncomeEvent(ServerType arg1, Order arg2)
        {
            throw new NotImplementedException();
        }

        private void Serv_NewMyTradeEvent(ServerType arg1, MyTrade arg2)
        {
            throw new NotImplementedException();
        }

        private void Serv_PortfoliosChangeEvent(ServerType arg1, List<Portfolio> arg2)
        {
            throw new NotImplementedException();
        }

        private void Serv_SecuritiesChangeEvent(ServerType arg1, List<Security> arg2)
        {
            throw new NotImplementedException();
        }

        private void ClassicArbitrageHFT_NeadToReconnectEvent(ServerType type)
        {
            foreach (var serv in serversList.Where(x => x.Type == type))
            {
                serv.StopServer();
                serv.StartServer();
            }
        }

        private void PositionClosingSuccesEvent(Position obj)
        {
            _CountTrade.ValueInt = 0;
        }
        private DateTime _lastTime;

        private PositionStateType Status;


        /// <summary>
        /// user change params
        /// пользователь изменил параметр
        /// </summary>
        void ArbitrageIndex_ParametrsChangeByUser()
        {
        }

        /// <summary>
        /// name bot
        /// взять уникальное имя
        /// </summary>
        public override string GetNameStrategyType()
        {
            return "ClassicArbitrageHFT";
        }


        public override void ShowIndividualSettingsDialog()
        {

        }

        /// <summary>
        /// slippage / проскальзывание
        /// </summary>
        public StrategyParameterInt Slippage;

        /// <summary>
        /// regime
        /// режим работы робота
        /// </summary>
        public StrategyParameterString Regime;
        
        /// <summary>
        /// Используемый процент Депозита
        /// </summary>
        public StrategyParameterInt Volume;

        public StrategyParameterDecimal minSpread;

        public StrategyParameterDecimal minProfit;

        private MarketDepthSpreadAnaliser Analiser;
        /// <summary>
        /// торгуем контрактами
        /// </summary>
        private StrategyParameterBool isContract;
        /// <summary>
        /// Принудительно закрывать сделки по времени
        /// </summary>
        private StrategyParameterBool autoCanselTrades;

        /// <summary>
        /// Плечо
        /// </summary>
        private StrategyParameterDecimal leverage;
        
        /// <summary>
        /// Максимальное количество доборов позиции
        /// </summary>
        public StrategyParameterInt MaxTrade;

        /// <summary>
        /// Текущее количество сделок
        /// </summary>
        private StrategyParameterInt _CountTrade;
        
        /// <summary>
        /// Прикаком спреде добирать позицию
        /// </summary>
        public StrategyParameterDecimal minSpreadAdd; 

        private decimal GetPrice(decimal price,BotTabSimple _tab)
        {
            if (_tab.Connector.MyServer.ServerType == ServerType.BitMex)
            {
                if (_tab.Securiti.Name == "ETHUSD")
                {
                    return price * 0.000001m;
                }
            }

            return price;
        }
        private decimal GetVol(decimal v,BotTabSimple _tab)
        {
            if (isContract.ValueBool)
            {
                return (int)v;
            }
            else
            {
                switch (_tab.Securiti.Name)
                {
                    case "ETHUSDT": return Math.Round(v, 3);
                    case "EOSUSDT": return Math.Round(v, 1);
                    case "LINKUSDT": return Math.Round(v, 2);
                    case "XMRUSDT": return Math.Round(v, 3);
                    case "ATOMUSDT": return Math.Round(v, 2);
                    case "TRXUSDT": return Math.Round(v, 0);
                    case "ADAUSDT": return Math.Round(v, 0);
                    case "BNBUSDT": return Math.Round(v, 2);
                    case "BTCUSDT": return Math.Round(v, 3);
                    case "ETCUSDT": return Math.Round(v, 2);
                    case "BCHUSDT": return Math.Round(v, 3);
                    case "ZECUSDT": return Math.Round(v, 3);
                    case "LTCUSDT": return Math.Round(v, 3);
                    case "XTZUSDT": return Math.Round(v, 1);
                    case "XRPUSDT": return Math.Round(v, 1);
                    case "XLMUSDT": return Math.Round(v, 0);
                    case "ONTUSDT": return Math.Round(v, 1);
                    case "IOTAUSDT": return Math.Round(v, 1);
                    case "BATUSDT": return Math.Round(v, 1);
                    case "VETUSDT": return Math.Round(v, 0);
                    case "NEOUSDT": return Math.Round(v, 2);

                        

                }
                return Math.Round(v, 3);
            }
        }
        private decimal GetBalance(BotTabSimple _tab)
        {
            //return 300;
            if (_tab.Connector.MyServer.ServerType == ServerType.Tester ||
                _tab.Connector.MyServer.ServerType == ServerType.Optimizer)
            {
                return _tab.Portfolio.ValueCurrent;
            }
            if (_tab.Connector.MyServer.ServerType == ServerType.BinanceFutures)
            {
                List<PositionOnBoard> bal = _tab.Portfolio.GetPositionOnBoard();
                if (bal != null && bal.Count > 0)
                {
                    PositionOnBoard b = bal.FindLast(x => x.SecurityNameCode == "USDT");
                    if (b != null)
                    {
                        return b.ValueCurrent;
                    }
                }
            }
            if (_tab.Connector.MyServer.ServerType == ServerType.BitMex)
            {
                return _tab.Portfolio.ValueCurrent - _tab.Portfolio.ValueBlocked;
            }
            return 0;
        }


    }
}
