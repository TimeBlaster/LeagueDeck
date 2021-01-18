using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeagueDeck.Models
{
    public class Item
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double AbilityHaste { get; set; }
        public int BaseCost { get; set; }
        public int TotalCost { get; set; }
        public List<int> ComponentIds { get; set; }

        public static Item Default = new Item
        {
            Id = "???",
            Name = "???",
            AbilityHaste = 0,
        };
    }
}
