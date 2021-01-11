using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace LeagueDeck
{
    public class LeagueData
    {
        [JsonConverter(typeof(VersionConverter))]
        public Version LeagueDeckVersion { get; set; }

        public List<Champion> Champions { get; set; }
        public List<Spell> SummonerSpells { get; set; }
        public List<Item> Items { get; set; }
    }

    public class Champion
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Spell> Spells { get; set; }

        public static Champion Default = new Champion
        {
            Id = "???",
            Name = "???",
            Spells = new List<Spell>
            {
                Spell.Default,
                Spell.Default,
                Spell.Default,
                Spell.Default,
                Spell.Default,
            }
        };
    }

    public class Spell
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int MaxRank { get; set; }
        public List<double> Cooldown { get; set; }

        public static Spell Default = new Spell
        {
            Id = "???",
            Name = "???",
            MaxRank = 6,
            Cooldown = new List<double> { 0, 0, 0, 0, 0, 0 }
        };
    }

    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double AbilityHaste { get; set; }
        public int BaseCost { get; set; }
        public int TotalCost { get; set; }
        public List<int> ComponentIds { get; set; }

        public static Item Default = new Item
        {
            Id = -1,
            Name = "???",
            AbilityHaste = 0,
        };
    }
}
