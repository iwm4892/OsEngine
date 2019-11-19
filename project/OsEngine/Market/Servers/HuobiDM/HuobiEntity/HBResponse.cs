using Newtonsoft.Json;
namespace OsEngine.Market.Servers.HuobiDM.HuobiEntity
{
    public class HBResponse<T> where T : new()
    {
        public string Status { get; set; }
        [JsonProperty(PropertyName = "Data")]
        public T Data { get; set; }
    }
    public class HBNullData
    {

    }
    class HBError
    {
        public string status;
        public long err_code;
        public string err_msg;
    }
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
    class HBCandle
    {
        public decimal close;
        public decimal high;
        public decimal low;
        public decimal open;
        public long vol;
    }
}
