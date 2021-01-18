using System.Collections.Generic;

namespace LeagueDeck.Models
{
    public class Champion : Asset<Champion>
    {
        public string Name { get; set; }
        public List<Spell> Spells { get; set; }

        public Champion()
        {
            Id = "???";
            Name = "???";
            Spells = new List<Spell>
            {
                Spell.Default,
                Spell.Default,
                Spell.Default,
                Spell.Default,
                Spell.Default,
            };
        }
    }
}
