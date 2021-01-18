using Newtonsoft.Json;

namespace LeagueDeck.Models.Api
{
    public class ActivePlayer
    {
        [JsonProperty("abilities")]
        public Abilities Abilities { get; set; }

        [JsonProperty("championStats")]
        public ChampionStats Stats { get; set; }

        [JsonProperty("currentGold")]
        public double CurrentGold { get; set; }

        [JsonProperty("fullRunes")]
        public Runes Runes { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("summonerName")]
        public string Name { get; set; }
    }
}
