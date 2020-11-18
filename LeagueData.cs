using System.Collections.Generic;

namespace LeagueDeck
{
    public class LeagueData
    {
        public List<Champion> Champions { get; set; }
        public List<Spell> SummonerSpells { get; set; }
        public List<Item> Items { get; set; }
    }

    public class Champion
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Spell> Spells { get; set; }
    }

    public class Spell
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int MaxRank { get; set; }
        public List<double> Cooldown { get; set; }
    }

    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double AbilityHaste { get; set; }
    }
}
