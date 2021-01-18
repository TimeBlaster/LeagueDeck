using Newtonsoft.Json;
using System.Collections.Generic;

namespace LeagueDeck.Models.Api
{
    public class Runes
    {
        [JsonProperty("generalRunes")]
        public List<Rune> General { get; set; }

        [JsonProperty("keystone")]
        public Rune Keystone { get; set; }

        [JsonProperty("primaryRuneTree")]
        public Rune PrimaryRuneTree { get; set; }

        [JsonProperty("secondaryRuneTree")]
        public Rune SecondaryRuneTree { get; set; }
    }

    public class Rune
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("displayName")]
        public string Name { get; set; }

        [JsonProperty("rawDescription")]
        public string RawDescription { get; set; }

        [JsonProperty("rawDisplayName")]
        public string RawDisplayName { get; set; }
    }
}
