/*
 * Your rights to use code governed by this license http://o-s-a.net/doc/license_simple_engine.pdf
 * Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using OsEngine.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using OsEngine.OsData;
namespace OsEngine.Entity
{
    public class PreSaveDataSet
    {
        public PreSaveDataSet(string _SetName, string _securityName)
        {
            SetName = _SetName;
            SecurityName = _securityName;
            init();
        }
        private string SetName;
        private string SecurityName;
        private void init()
        {
            if (!Directory.Exists("Data"))
            {
                Directory.CreateDirectory("Data");
            }
            if (!Directory.Exists("Data\\Temp\\"))
            {
                Directory.CreateDirectory("Data\\Temp\\");
            }

            if (!Directory.Exists("Data\\Temp\\" + SetName))
            {
                Directory.CreateDirectory("Data\\Temp\\" + SetName);
            }

            string s = SecurityName.Replace("/", "");

            if (!Directory.Exists("Data\\Temp\\" + SetName + "\\" + SecurityName.Replace("/", "").Replace("*", "")))
            {
               Directory.CreateDirectory("Data\\Temp\\" + SetName + "\\" + SecurityName.Replace("/", "").Replace("*", ""));
            }
        }
        public void SaveTrades(List<Trade> trades)
        {
            string pathToSet = "Data\\Temp\\" + SetName + "\\";
            string path = pathToSet + SecurityName.Replace("/", "").Replace("*", "");

            for (int i = 0; i < trades.Count; i++)
            {

                SaveThisTick(trades[i],
                    path, SecurityName.Replace("*", ""), null, path + "\\" + "Tick");
            }
            SendNewLogMessage("Загружены данные "+ SecurityName + " за "+trades[trades.Count-1].Time.ToString("yyyy-MM-dd HH:mm:ss"), LogMessageType.System);
        }
        public List<Trade> LoadTrades()
        {
            List<Trade> result = new List<Trade>();
            string pathToSet = "Data\\Temp\\" + SetName + "\\";
            string path = pathToSet + SecurityName.Replace("/", "").Replace("*", "")+ "\\" + "Tick\\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            if (_tradeSaveInfo == null)
            {
                _tradeSaveInfo = new List<TradeSaveInfo>();
            }

            TradeSaveInfo tradeSaveInfo =
             _tradeSaveInfo.Find(info => info.NameSecurity == SecurityName);

            if (tradeSaveInfo == null)
            {
                // if we save these trades for the first time, we try to pick them up from the file/если сохраняем эти тики в первый раз, пробуем поднять их из файла
                tradeSaveInfo = new TradeSaveInfo();
                tradeSaveInfo.NameSecurity = SecurityName;

                _tradeSaveInfo.Add(tradeSaveInfo);

                string[] files = Directory.GetFiles(path);
                if (files.Length != 0)
                {
                    try
                    {
                        using (StreamReader reader = new StreamReader(files[0]))
                        {
                            string str = "";
                            while (!reader.EndOfStream)
                            {
                                str = reader.ReadLine();
                            }
                            if (str != "")
                            {
                                Trade trade = new Trade();
                                trade.SetTradeFromString(str);
                                tradeSaveInfo.LastSaveObjectTime = trade.Time;
                                tradeSaveInfo.LastTradeId = trade.Id;
                                result.Add(trade);
                            }

                        }
                    }
                    catch (Exception error)
                    {
                        if (NewLogMessageEvent != null)
                        {
                            NewLogMessageEvent(error.ToString(), LogMessageType.Error);
                        }

                        return result;
                    }
                }
            }

            return result;
        }
        /// <summary>
        /// service information to save trades/сервисная информация для сохранения тиков
        /// </summary>
        private List<TradeSaveInfo> _tradeSaveInfo;

        
        /// <summary>
        /// save trades series/сохранить серию тиков
        /// </summary>
        /// <param name="tradeLast">trades/тики</param>
        /// <param name="pathToFolder">path/путь</param>
        /// <param name="securityName">security Name/имя бумаги</param>
        private void SaveThisTick(Trade tradeLast, string pathToFolder, string securityName, StreamWriter writer, string pathToFile)
    {
        if (!Directory.Exists(pathToFolder))
        {
            Directory.CreateDirectory(pathToFolder);
        }

        if (_tradeSaveInfo == null)
        {
            _tradeSaveInfo = new List<TradeSaveInfo>();
        }

        // take trades storage/берём хранилище тиков

        TradeSaveInfo tradeSaveInfo =
            _tradeSaveInfo.Find(info => info.NameSecurity == securityName);

        if (tradeSaveInfo == null)
        {
            // if we save these trades for the first time, we try to pick them up from the file/если сохраняем эти тики в первый раз, пробуем поднять их из файла
            tradeSaveInfo = new TradeSaveInfo();
            tradeSaveInfo.NameSecurity = securityName;

            _tradeSaveInfo.Add(tradeSaveInfo);

            string[] files = Directory.GetFiles(pathToFolder);

            if (files.Length != 0)
            {
                if (writer != null)
                {
                    writer.Close();
                    writer = null;
                }

                try
                {
                    using (StreamReader reader = new StreamReader(files[0]))
                    {
                        string str = "";
                        while (!reader.EndOfStream)
                        {

                            str = reader.ReadLine();

                        }
                        if (str != "")
                        {
                            Trade trade = new Trade();
                            trade.SetTradeFromString(str);
                            tradeSaveInfo.LastSaveObjectTime = trade.Time;
                            tradeSaveInfo.LastTradeId = trade.Id;
                        }

                    }
                }
                catch (Exception error)
                {
                    if (NewLogMessageEvent != null)
                    {
                        NewLogMessageEvent(error.ToString(), LogMessageType.Error);
                    }

                    return;
                }
            }
        }

        if (tradeSaveInfo.LastSaveObjectTime >
            tradeLast.Time ||
            (tradeLast.Id != null && tradeLast.Id == tradeSaveInfo.LastTradeId)
            )
        {
            // if we have old trades coincide with new ones./если у нас старые тики совпадают с новыми.
            return;
        }


        tradeSaveInfo.LastSaveObjectTime = tradeLast.Time;
        tradeSaveInfo.LastTradeId = tradeLast.Id;
        // write down/записываем

        try
        {
            if (writer != null)
            {
                writer.WriteLine(tradeLast.GetSaveString());
            }
            else
            {
                using (
               StreamWriter writer2 =
            new StreamWriter(pathToFile + "\\" + securityName.Replace("/", "") + ".txt", true))
                {
                    writer2.WriteLine(tradeLast.GetSaveString());

                }
            }
        }
        catch (Exception error)
        {
            if (NewLogMessageEvent != null)
            {
                NewLogMessageEvent(error.ToString(), LogMessageType.Error);
            }
        }

    }
        /// <summary>
        /// send a new message to the top/выслать новое сообщение на верх
        /// </summary>
        private void SendNewLogMessage(string message, LogMessageType type)
    {
        if (NewLogMessageEvent != null)
        {
            NewLogMessageEvent(message, type);
        }
        else
        {
            System.Windows.MessageBox.Show(message);
        }
    }

        /// <summary>
        /// send new message to log/выслать новое сообщение в лог
        /// </summary>
        public event Action<string, LogMessageType> NewLogMessageEvent;
    }
    /// <summary>
    /// information to save trades/информация для сохранения тиков
    /// </summary>
}
