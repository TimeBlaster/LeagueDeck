using System.Collections.Generic;

namespace LeagueDeck.Models
{
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
}
