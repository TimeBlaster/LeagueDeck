using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace LeagueDeck.Models
{
    public class LeagueData
    {
        [JsonConverter(typeof(VersionConverter))]
        public Version LeagueDeckVersion { get; set; }

        public List<Champion> Champions { get; set; }
        public List<Spell> SummonerSpells { get; set; }
        public List<Item> Items { get; set; }
    }
}
