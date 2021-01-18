using System.Collections.Generic;

namespace LeagueDeck.Models
{
    public class Spell : Asset<Spell>
    {
        public string Name { get; set; }
        public int MaxRank { get; set; }
        public List<double> Cooldown { get; set; }
        public override Spell SetDefault()
        {
            Id = "???";
            Name = "???";
            MaxRank = 6;
            Cooldown = new List<double> { 0, 0, 0, 0, 0, 0 };

            return this;
        }
    }
}
