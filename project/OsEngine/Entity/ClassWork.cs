/*
 *Ваши права на использование кода регулируются данной лицензией http://o-s-a.net/doc/license_simple_engine.pdf
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Charts.CandleChart.Indicators;
using System.Reflection;
namespace OsEngine.Entity
{
    /// <summary>
    /// Свеча
    /// </summary>
    public class ClassWork
    {
        public System.Reflection.ConstructorInfo GetConstructor(string name, Type[] args)
        {
            try
            {

                Type TestType = Type.GetType(name, false, true);

                    //получаем конструктор
                    System.Reflection.ConstructorInfo ci = TestType.GetConstructor(new Type[] {  });
                    return ci;
                    //вызываем конструтор
                   // object Obj = ci.Invoke(new object[] { });
            }
            catch (Exception error)
            {
                SendErrorMessage(error);
                return null;
            }

        }
        public static object GetInstance(string name, object[] Args)
        {
            try
            {
                Type type = Type.GetType(name, ResolveAssembly, ResolveType);
                return Activator.CreateInstance(type, Args, null);
            }
            catch (Exception error)
            {
                Message(error.ToString());
                return null;
            }
        }

        private static Assembly ResolveAssembly(AssemblyName assemblyName)
        {
            if (assemblyName.Name.Equals(assemblyName.FullName))
                return Assembly.LoadWithPartialName(assemblyName.Name);
            return Assembly.Load(assemblyName);
        }

        private static Type ResolveType(Assembly assembly, string typeName, bool ignoreCase)
        {
            return assembly != null
                ? assembly.GetType(typeName, false, ignoreCase)
                : Type.GetType(typeName, false, ignoreCase);
        }
        static void Message(string mes)
        {
            System.Windows.MessageBox.Show(mes);
        }
        public static string GetFullNameIndicator(string name)
        {
            return "OsEngine.Charts.CandleChart.Indicators." + name;
        }       
        /// <summary>
        /// выслать наверх сообщение об ошибке
        /// </summary>
        private void SendErrorMessage(Exception error)
        {
            if (LogMessageEvent != null)
            {
                LogMessageEvent(error.ToString(), LogMessageType.Error);
            }
            else
            { // если никто на нас не подписан и происходит ошибка
                System.Windows.MessageBox.Show(error.ToString());
            }
        }
        /// <summary>
        /// исходящее сообщение для лога
        /// </summary>
        public event Action<string, LogMessageType> LogMessageEvent;

    }
}
