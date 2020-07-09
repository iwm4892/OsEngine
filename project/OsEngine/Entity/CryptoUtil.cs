using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsEngine.Market;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;

namespace OsEngine.Entity
{
    class CryptoUtil
    {
        /// <summary>
        /// Округлить на нужное количество знаков
        /// </summary>
        /// <param name="volume">Округляемый объем</param>
        /// <param name="VolumeDecimals">Количество знаков</param>
        /// <returns></returns>
        public static decimal RoundVolume(decimal volume, int VolumeDecimals)
        {
            if (VolumeDecimals == 0)
            {
                return (int)volume;
            }
            else
            {
                CultureInfo culture = new CultureInfo("ru-RU");
                string[] _v = volume.ToString(culture).Split(',');
                return (_v[0] + "," + _v[1].Substring(0, Math.Min(VolumeDecimals, _v[1].Length))).ToDecimal();
            }
        }
        /// <summary>
        /// Получить количество десятичных знаков для панели
        /// </summary>
        /// <param name="tab">Панель </param>
        /// <returns></returns>
        public static int GetRoundVolumeDecimals(BotTabSimple tab)
        {
            if (tab.Connector.MyServer.ServerType == ServerType.BinanceFutures)
            {
                switch (tab.Securiti.Name)
                {
                    case "ETHUSDT": return 3;
                    case "EOSUSDT": return 1;
                    case "LINKUSDT": return 2;
                    case "XMRUSDT": return 3;
                    case "ATOMUSDT": return 2;
                    case "TRXUSDT": return 0;
                    case "ADAUSDT": return 0;
                    case "BNBUSDT": return 2;
                    case "BTCUSDT": return 3;
                    case "ETCUSDT": return 2;
                    case "BCHUSDT": return 3;
                    case "ZECUSDT": return 3;
                    case "LTCUSDT": return 3;
                    case "XTZUSDT": return 1;
                    case "XRPUSDT": return 1;
                    case "XLMUSDT": return 0;
                    case "ONTUSDT": return 1;
                    case "IOTAUSDT": return 1;
                    case "BATUSDT": return 1;
                    case "VETUSDT": return 0;
                    case "NEOUSDT": return 2;
                    default: tab.SetNewLogMessage("Для инструмента: " + tab.Securiti.Name + " необходимо описать округление ", Logging.LogMessageType.Error); break;
                }

            }
            return 0;

        }
        public static decimal GetRoundVolume(BotTabSimple tab,decimal volume)
        {
            return RoundVolume(volume, GetRoundVolumeDecimals(tab));
        }
        public static decimal GetBalance(BotTabSimple tab)
        {
            if (tab.Connector.MyServer.ServerType == ServerType.Tester ||
                tab.Connector.MyServer.ServerType == ServerType.Optimizer)
            {
                if (tab.Portfolio.ValueBlocked != 0)
                {
                    Console.WriteLine("Заблокировано " + tab.Portfolio.ValueBlocked);
                }
                return tab.Portfolio.ValueCurrent;
            }
            if (tab.Connector.MyServer.ServerType == ServerType.BinanceFutures)
            {
                List<PositionOnBoard> bal = tab.Portfolio.GetPositionOnBoard();
                if (bal != null && bal.Count > 0)
                {
                    PositionOnBoard b = bal.FindLast(x => x.SecurityNameCode == "USDT");
                    if (b != null)
                    {
                        return b.ValueCurrent;
                    }
                }
            }
            if (tab.Connector.MyServer.ServerType == ServerType.Binance)
            {
                List<PositionOnBoard> bal = tab.Portfolio.GetPositionOnBoard();
                if (bal != null && bal.Count > 0)
                {
                    PositionOnBoard b = bal.FindLast(x => x.SecurityNameCode == tab.Securiti.NameClass);
                    if (b != null)
                    {
                        return b.ValueCurrent;
                    }
                }
            }
            if (tab.Connector.MyServer.ServerType == ServerType.GateIoFutures)
            {
                List<PositionOnBoard> bal = tab.Portfolio.GetPositionOnBoard();
                if (bal != null && bal.Count > 0)
                {
                    PositionOnBoard b = bal.FindLast(x => x.SecurityNameCode == "USDT");
                    if (b != null)
                    {
                        return b.ValueCurrent;
                    }
                }
            }
            if (tab.Connector.MyServer.ServerType == ServerType.BitMex)
            {
                return tab.Portfolio.ValueCurrent - tab.Portfolio.ValueBlocked;
            }
            return 0;
        }

    }
}
