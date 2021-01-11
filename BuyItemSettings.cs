using Newtonsoft.Json;
using System.Collections.Generic;

namespace LeagueDeck
{
    public class BuyItemSettings
    {
        public static BuyItemSettings CreateDefaultSettings()
        {
            return new BuyItemSettings
            {
                Items = new List<Item>(),
                ItemId = -1,
                DisplayFormat = EBuyItemDisplayFormat.None,
            };
        }

        [JsonProperty(PropertyName = "items")]
        public List<Item> Items { get; set; }

        [JsonProperty(PropertyName = "itemId")]
        public int ItemId { get; set; }

        [JsonProperty(PropertyName = "displayFormat")]
        public EBuyItemDisplayFormat DisplayFormat { get; set; }
    }
}
