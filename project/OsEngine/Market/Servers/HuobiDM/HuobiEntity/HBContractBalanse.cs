using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.Market.Servers.HuobiDM.HuobiEntity
{
    class HBContractBalanse
    {
        /// <summary>
        /// Variety code	"BTC","ETH"...
        /// </summary>
        public string symbol;
        /// <summary>
        /// Account rights
        /// </summary>
        public decimal margin_balance;
        /// <summary>
        /// Position Margin
        /// </summary>
        public decimal margin_position;
        /// <summary>
        /// Freeze margin
        /// </summary>
        public decimal margin_frozen;
        /// <summary>
        /// Available margin
        /// </summary>
        public decimal margin_available;
        /// <summary>
        /// Realized profit
        /// </summary>
        public decimal profit_real;
        /// <summary>
        /// Unrealized profit
        /// </summary>
        public decimal profit_unreal;
        /// <summary>
        /// risk rate
        /// </summary>
//        public decimal risk_rate;
        /// <summary>
        /// Estimated liquidation price
        /// </summary>
//        public decimal liquidation_price;
        /// <summary>
        /// Available withdrawal
        /// </summary>
        public decimal withdraw_available;
        /// <summary>
        /// Leverage Rate
        /// </summary>
        public decimal lever_rate;
        /// <summary>
        /// Adjustment Factor
        /// </summary>
        public decimal adjust_factor;

    }
}
