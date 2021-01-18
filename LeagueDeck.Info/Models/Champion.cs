using System.Collections.Generic;

namespace LeagueDeck.Models
{
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
}
