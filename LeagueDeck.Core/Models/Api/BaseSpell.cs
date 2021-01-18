using Newtonsoft.Json;

namespace LeagueDeck.Models.Api
{
    public abstract class BaseSpell
    {
        [JsonProperty("displayName")]
        public string LocalizedName { get; set; }

        [JsonProperty("rawDescription")]
        public string RawDescription { get; set; }

        [JsonProperty("rawDisplayName")]
        public string RawDisplayName { get; set; }
    }
}
