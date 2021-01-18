using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LeagueDeck.Models.Api
{
    public class Participant
    {
        [JsonProperty("summonerName")]
        public string SummonerName { get; set; }

        [JsonProperty("team")]
        public string Team { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("championName")]
        public string ChampionName { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("isBot")]
        public bool IsBot { get; set; }

        [JsonProperty("isDead")]
        public bool IsDead { get; set; }

        [JsonProperty("respawnTimer")]
        public double RespawnTimer { get; set; }

        [JsonProperty("items")]
        public IEnumerable<Item> Items { get; set; }

        [JsonProperty("summonerSpells")]
        public SummonerSpells SummonerSpells { get; set; }
    }
}
