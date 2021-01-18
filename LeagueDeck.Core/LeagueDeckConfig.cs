using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace LeagueDeck.Core
{
    public class LeagueDeckConfig
    {
        [JsonConverter(typeof(VersionConverter))]
        public Version LeagueDeckVersion { get; set; }
    }
}
