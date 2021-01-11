using Newtonsoft.Json;

namespace LeagueDeck
{
    public class SpellTimerSettings
    {
        public static SpellTimerSettings CreateDefaultSettings()
        {
            return new SpellTimerSettings
            {
                SendMessageInChat = false,
                ShowMinutesAndSeconds = false,
                ShowAbilityName = false,
                Offset = 0,
                ChatFormat = EChatFormat.GameTime,
                Summoner = ESummoner.Summoner1,
                Spell = ESpell.SummonerSpell1,
            };
        }

        [JsonProperty(PropertyName = "sendMessageInChat")]
        public bool SendMessageInChat { get; set; }

        [JsonProperty(PropertyName = "showMinutesAndSeconds")]
        public bool ShowMinutesAndSeconds { get; set; }

        [JsonProperty(PropertyName = "showAbilityName")]
        public bool ShowAbilityName { get; set; }

        [JsonProperty(PropertyName = "offset")]
        public int Offset { get; set; }

        [JsonProperty(PropertyName = "chatFormat")]
        public EChatFormat ChatFormat { get; set; }

        [JsonProperty(PropertyName = "summoner")]
        public ESummoner Summoner { get; set; }

        [JsonProperty(PropertyName = "spell")]
        public ESpell Spell { get; set; }
    }
}
