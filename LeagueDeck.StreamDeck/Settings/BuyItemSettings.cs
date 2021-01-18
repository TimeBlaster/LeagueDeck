using LeagueDeck.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LeagueDeck.Settings
{
    public class BuyItemSettings
    {
        public static BuyItemSettings CreateDefaultSettings()
        {
            return new BuyItemSettings
            {
                Items = new List<Item>(),
                ItemId = string.Empty,
                DisplayFormat = EBuyItemDisplayFormat.None,
            };
        }

        [JsonProperty(PropertyName = "items")]
        public IReadOnlyList<Item> Items { get; set; }

        [JsonProperty(PropertyName = "itemId")]
        public string ItemId { get; set; }

        [JsonProperty(PropertyName = "displayFormat")]
        public EBuyItemDisplayFormat DisplayFormat { get; set; }
    }

    public enum EBuyItemDisplayFormat
    {
        None = 0,
        RemainingCost = 1,
        TotalCost = 2,
    }
}
