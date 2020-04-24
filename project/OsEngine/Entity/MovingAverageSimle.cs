using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Entity
{
    class MovingAverageSimle
    {
        private decimal koef {
            get {
                if (Lenth == 0) return 0;
                return (2 / (1 + Lenth));
                }
            }
        public int Lenth;
        public decimal lastMa = 0;
        private List<decimal> Values = new List<decimal>();
        private List<decimal> oldValues = new List<decimal>();
        public void Add(decimal el)
        {
            if (Values.Count==0 && oldValues.Count < Lenth)
            {
                oldValues.Add(el);
            }
            if(Values.Count == 0 && oldValues.Count == Lenth)
            {
                decimal sum = 0;
                foreach(var m in oldValues)
                {
                    sum += m;
                }
                lastMa = sum / Lenth;
                Values.Add(lastMa);
            }
            if (Values.Count > 0)
            {
                Values.Add(lastMa + (koef * (el - lastMa)));
                lastMa = Values[Values.Count - 1];
            }
            if (Values.Count > Lenth)
            {
                Values.RemoveAt(0);
            }
        }
    }
}
