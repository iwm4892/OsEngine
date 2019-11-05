using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.HuobiDM.HuobiEntity
{
    class HBContractInfo
    {
        public string symbol;
        public string contract_code;
        public string contract_type;
        public decimal contract_size;
        public decimal price_tick;
        public string delivery_date;
        public string create_date;
        public int contract_status;

    }
}
