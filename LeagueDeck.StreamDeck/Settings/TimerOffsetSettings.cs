using Newtonsoft.Json;

namespace LeagueDeck.Settings
{
    public class TimerOffsetSettings
    {
        public static TimerOffsetSettings CreateDefaultSettings()
        {
            return new TimerOffsetSettings
            {
                Toggle = true,
                Offset = 10,
            };
        }

        [JsonProperty(PropertyName = "toggle")]
        public bool Toggle { get; set; }

        [JsonProperty(PropertyName = "offset")]
        public int Offset { get; set; }
    }
}
