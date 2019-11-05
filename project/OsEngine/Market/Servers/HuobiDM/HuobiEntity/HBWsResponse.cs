using Newtonsoft.Json;
using System.Collections.Generic;

namespace OsEngine.Market.Servers.HuobiDM.HuobiEntity
{
    public class HBWsResponse<T> where T : new()
    {
        public string ch { get; set; }
        public long ts { get; set; }

        [JsonProperty(PropertyName = "tick")]
        public T tick { get; set; }
        [JsonProperty(PropertyName = "data")]
        public T data { get; set; }
    }
    public class HBTick
    {
        public string id { get; set; }
        public List<HBTickData> data { get; set; }
    }
    public class HBTickData
    {
        public long amount;
        public long ts;
        public string id;
        public decimal price;
        public string direction;
    }

    public class HBDepth
    {
        public List<List<decimal>> bids = new List<List<decimal>>();
        public List<List<decimal>> asks = new List<List<decimal>>();
        public string id;
        public long ts;
    }

}
