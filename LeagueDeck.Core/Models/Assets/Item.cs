using System.Collections.Generic;

namespace LeagueDeck.Models
{
    public class Item : Asset<Item>
    {
        public string Name { get; set; }
        public double AbilityHaste { get; set; }
        public int BaseCost { get; set; }
        public int TotalCost { get; set; }
        public List<int> ComponentIds { get; set; }
        public override Item SetDefault()
        {
            Id = "???";
            Name = "???";
            AbilityHaste = 0;
            BaseCost = 0;
            TotalCost = 0;
            ComponentIds = new List<int>();

            return this;
        }
    }
}
