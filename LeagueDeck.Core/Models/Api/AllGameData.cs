using Newtonsoft.Json;
using System.Collections.Generic;

namespace LeagueDeck.Models.Api
{
    public class AllGameData
    {
        [JsonProperty("activePlayer")]
        public ActivePlayer ActivePlayer { get; set; }

        [JsonProperty("allPlayers")]
        public List<Participant> Participants { get; set; }

        [JsonProperty("events")]
        public GameEvents GameEvents { get; set; }

        [JsonProperty("gameData")]
        public GameData GameData { get; set; }
    }
}
