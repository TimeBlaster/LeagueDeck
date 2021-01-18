using System.Collections.Generic;

namespace LeagueDeck.Models
{
    public class Champion : Asset<Champion>
    {
        public string Name { get; set; }
        public List<Spell> Spells { get; set; }
        public override Champion SetDefault()
        {
            Id = "???";
            Name = "???";
            Spells = new List<Spell>
            {
                new Spell().SetDefault(),
                new Spell().SetDefault(),
                new Spell().SetDefault(),
                new Spell().SetDefault(),
                new Spell().SetDefault(),
            };

            return this;
        }
    }
}
