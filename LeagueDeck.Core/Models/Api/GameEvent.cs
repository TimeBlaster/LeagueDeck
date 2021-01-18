using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace LeagueDeck.Models.Api
{
    public class GameEvent
    {
        [JsonProperty("EventID")]
        public int Id { get; set; }

        [JsonProperty("EventName")]
        [JsonConverter(typeof(StringEnumConverter))]
        public EEventType Type { get; set; }

        [JsonProperty("EventTime")]
        public double Time { get; set; }

        [JsonProperty("KillerName")]
        public string KillerName { get; set; }

        [JsonProperty("Assisters")]
        public List<string> Assisters { get; set; }

        [JsonProperty("Stolen")]
        public bool Stolen { get; set; }

        [JsonProperty("DragonType")]
        public string DragonType { get; set; }

        [JsonProperty("VictimName")]
        public string VictimName { get; set; }

        [JsonProperty("KillStreak")]
        public int KillStreak { get; set; }

        [JsonProperty("Acer")]
        public string Acer { get; set; }

        [JsonProperty("AcingTeam")]
        public string AcingTeam { get; set; }

        [JsonProperty("InhibKilled")]
        public string InhibKilled { get; set; }

        [JsonProperty("TurretKilled")]
        public string TurretKilled { get; set; }
    }
}
