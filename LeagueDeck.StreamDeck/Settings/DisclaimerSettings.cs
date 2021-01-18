using Newtonsoft.Json;

namespace LeagueDeck.Settings
{
    public class DisclaimerSettings
    {
        public static DisclaimerSettings CreateDefaultSettings()
        {
            return new DisclaimerSettings
            {
                ChatFormat = EChatFormat.GameTime,
                Message = string.Empty,
            };
        }

        [JsonProperty(PropertyName = "chatFormat")]
        public EChatFormat ChatFormat { get; set; }

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }
    }
}
