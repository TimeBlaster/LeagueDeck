using Newtonsoft.Json;

namespace LeagueDeck.Models.Api
{
    public class Item
    {
        [JsonProperty("itemID")]
        public int Id { get; set; }

        [JsonProperty("displayName")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("slot")]
        public int Slot { get; set; }

        [JsonProperty("canUse")]
        public bool CanUse { get; set; }

        [JsonProperty("consumable")]
        public bool IsConsumable { get; set; }
    }
}
