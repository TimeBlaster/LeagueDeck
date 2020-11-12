﻿using Newtonsoft.Json;

namespace LeagueDeck
{
    public class LeagueDeckSettings
    {
        public static LeagueDeckSettings CreateDefaultSettings()
        {
            LeagueDeckSettings instance = new LeagueDeckSettings();
            instance.SendMessageInChat = false;
            instance.Summoner = ESummoner.Summoner1;
            instance.SummonerSpell = ESummonerSpell.Spell1;

            return instance;
        }

        [JsonProperty(PropertyName = "sendMessageInChat")]
        public bool SendMessageInChat { get; set; }

        [JsonProperty(PropertyName = "summoner")]
        public ESummoner Summoner { get; set; }

        [JsonProperty(PropertyName = "summonerSpell")]
        public ESummonerSpell SummonerSpell { get; set; }
    }
}
