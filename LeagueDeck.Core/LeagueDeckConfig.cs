using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace LeagueDeck.Core
{
    public class LeagueDeckConfig
    {
        [JsonConverter(typeof(VersionConverter))]
        public Version LeagueDeckVersion { get; set; }
        public string ApiKey { get; set; }

        public static LeagueDeckConfig CreateDefaultConfig()
        {
            return new LeagueDeckConfig
            {
                LeagueDeckVersion = Utilities.GetLeagueDeckVersion(),
                ApiKey = "YOUR_API_KEY",
            };
        }
    }
}
