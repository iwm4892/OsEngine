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
    class HBOrder
    {
        /// <summary>
        /// Variety code
        /// </summary>
        public string symbol;
        /// <summary>
        /// Тип контракта ("this_week", "next_week", "quarter")
        /// </summary>
        public string contract_type;
        /// <summary>
        /// объем
        /// </summary>
        public decimal volume;
        /// <summary>
        /// Зафиксированная цена
        /// </summary>
        public decimal price;
        /// <summary>
        /// Тип цены ордера [фиксированная цена，цена оппонента，рыночная цена]
        /// </summary>
        public string order_price_type;
        /// <summary>
        /// Направление транзакции
        /// </summary>
        public string direction;
        /// <summary>
        /// "open": "close"
        /// </summary>
        public string offset;
        /// <summary>
        /// Статус ордера ( 3. Have sumbmitted the orders; 4. Orders partially matched; 5. Orders cancelled with  partially matched; 6. Orders fully matched; 7. Orders cancelled)
        /// </summary>
        public int status;
        /// <summary>
        /// Плечо
        /// </summary>
        public int lever_rate;
        /// <summary>
        /// Ордер ID
        /// </summary>
        public string order_id;
        /// <summary>
        /// ID ордера клиента
        /// </summary>
        public string client_order_id;
        /// <summary>
        /// Время транзакции
        /// </summary>
        public long created_at;

        /// <summary>
        /// Комиссия
        /// </summary>
        public decimal fee;

        /// <summary>
        /// Количество транзакций
        /// </summary>
        public decimal trade_volume;
        /// <summary>
        /// Совокупная сумма транзакций
        /// </summary>
        public decimal trade_turnover;
        /// <summary>
        /// Сделки по ордеру
        /// </summary>
        public List<HBOrderTrade> trade;

    }
    class HBOrderTrade
    {
        /// <summary>
        /// id соответсвующего результата
        /// </summary>
        public long trade_id;

        /// <summary>
        /// Соответствующая цена
        /// </summary>
        public decimal trade_price;

        /// <summary>
        /// Количество транзакций
        /// </summary>
        public decimal trade_volume;
        /// <summary>
        /// Совокупная сумма транзакций
        /// </summary>
        public decimal trade_turnover;
        /// <summary>
        /// Время транзакции
        /// </summary>
        public long created_at;

    }
}
