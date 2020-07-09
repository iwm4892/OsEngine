using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsEngine.Market;
using OsEngine.Market.Servers;

namespace OsEngine.Entity
{
    class ServerTyped
    {
        public ServerType Type;
        public IServer Server;
        public ServerConnectStatus ServerStatus;
        public bool isActive = false;
        public List<Portfolio> Portfolios;

        public ServerTyped(IServer server)
        {
            Server = server;
            Type = server.ServerType;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            Server.ConnectStatusChangeEvent += Serv_ConnectStatusChangeEvent;
            Server.NeadToReconnectEvent += Serv_NeadToReconnectEvent;
            Server.PortfoliosChangeEvent += Serv_PortfoliosChangeEvent;
            Server.SecuritiesChangeEvent += Serv_SecuritiesChangeEvent;
            Server.NewMyTradeEvent += Serv_NewMyTradeEvent;
            Server.NewOrderIncomeEvent += Serv_NewOrderIncomeEvent;
            Server.NewBidAscIncomeEvent += Serv_NewBidAscIncomeEvent;
            Server.ConnectStatusChangeEvent += Server_ConnectStatusChangeEvent;
        }

        private void Server_ConnectStatusChangeEvent(string obj)
        {
            ServerStatus = Server.ServerStatus;
        }

        private void Serv_NewBidAscIncomeEvent(decimal arg1, decimal arg2, Security arg3)
        {
            NewBidAscIncomeEvent(Type, arg1, arg2, arg3);
        }

        private void Serv_NewOrderIncomeEvent(Order obj)
        {
            NewOrderIncomeEvent(Type, obj);
        }

        private void Serv_NewMyTradeEvent(MyTrade obj)
        {
            NewMyTradeEvent(Type, obj);
        }

        private void Serv_SecuritiesChangeEvent(List<Security> obj)
        {
            SecuritiesChangeEvent(Type, obj);
        }

        private void Serv_PortfoliosChangeEvent(List<Portfolio> obj)
        {
            PortfoliosChangeEvent(Type, obj);
        }

        private void Serv_NeadToReconnectEvent()
        {
            NeadToReconnectEvent(Type);
        }

        private void Serv_ConnectStatusChangeEvent(string obj)
        {
            ConnectStatusChangeEvent(Type, obj);
        }

        public event Action<ServerType, decimal, decimal, Security> NewBidAscIncomeEvent;
        public event Action<ServerType, Order> NewOrderIncomeEvent;
        public event Action<ServerType, MyTrade> NewMyTradeEvent;
        public event Action<ServerType, List<Security>> SecuritiesChangeEvent;
        public event Action<ServerType, List<Portfolio>> PortfoliosChangeEvent;
        public event Action<ServerType> NeadToReconnectEvent;
        public event Action<ServerType, string> ConnectStatusChangeEvent;

        public void StopServer()
        {
            Server.StopServer();
        }

        public void StartServer()
        {
            Server.StartServer();
        }
    }
}
