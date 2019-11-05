using Newtonsoft.Json;

namespace OsEngine.Market.Servers.HuobiDM.HuobiEntity
{
    public class Account
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public string State { get; set; }
        [JsonProperty(PropertyName = "user-id")]
        public long UserId { get; set; }
    }
}
