using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LeagueDeck
{
    public class SummonerData
    {
        [JsonProperty("summonerName")]
        public string SummonerName { get; set; }

        [JsonProperty("team")]
        public string Team { get; set; }

        [JsonProperty("position")]
        public string Position { get; set; }

        [JsonProperty("championName")]
        public string ChampionName { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("isBot")]
        public bool IsBot { get; set; }

        [JsonProperty("isDead")]
        public bool IsDead { get; set; }

        [JsonProperty("respawnTimer")]
        public double RespawnTimer { get; set; }

        [JsonProperty("items")]
        public IEnumerable<ItemData> Items { get; set; }

        [JsonProperty("summonerSpells")]
        public SummonerSpellData SummonerSpells { get; set; }

        public string GetSummonerSpell(ESummonerSpell spell)
        {
            switch (spell)
            {
                case ESummonerSpell.Spell1:
                    return SummonerSpells.Spell1.Name;

                case ESummonerSpell.Spell2:
                    return SummonerSpells.Spell2.Name;

                default:
                    throw new ArgumentOutOfRangeException(nameof(spell));
            }
        }
    }

    public class ItemData
    {
        [JsonProperty("itemID")]
        public int Id { get; set; }

        [JsonProperty("displayName")]
        public string Name { get; set; }

        [JsonProperty("price")]
        public int Price { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("slot")]
        public int Slot { get; set; }

        [JsonProperty("canUse")]
        public bool CanUse { get; set; }

        [JsonProperty("consumable")]
        public bool IsConsumable { get; set; }
    }

    public class RuneData
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("displayName")]
        public string Name { get; set; }
    }

    public class SummonerSpellData
    {
        [JsonProperty("summonerSpellOne")]
        public SummonerSpell Spell1 { get; set; }

        [JsonProperty("summonerSpellTwo")]
        public SummonerSpell Spell2 { get; set; }
    }

    public class SummonerSpell
    {
        [JsonProperty("displayName")]
        public string Name { get; set; }
    }

    public class GameData
    {
        [JsonProperty("gameMode")]
        public string GameMode { get; set; }

        [JsonProperty("gameTime")]
        public double Time { get; set; }

        [JsonProperty("mapName")]
        public string MapName { get; set; }

        [JsonProperty("mapNumber")]
        public int MapNumber { get; set; }

        [JsonProperty("mapTerrain")]
        public string MapTerrain { get; set; }
    }

    public class GameEvent
    {
        [JsonProperty("EventID")]
        public int Id { get; set; }

        [JsonProperty("EventName")]
        public string Name { get; set; }

        [JsonProperty("EventTime")]
        public double Time { get; set; }
    }
}
