using Newtonsoft.Json;

namespace LeagueDeck.Models.Api
{
    public class Abilities
    {
        [JsonProperty("Passive")]
        public Ability Passive { get; set; }

        [JsonProperty("Q")]
        public Ability Q { get; set; }

        [JsonProperty("W")]
        public Ability W { get; set; }

        [JsonProperty("E")]
        public Ability E { get; set; }

        [JsonProperty("R")]
        public Ability R { get; set; }
    }

    public class Ability : BaseSpell
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("abilityLevel")]
        public string Level { get; set; }
    }
}
