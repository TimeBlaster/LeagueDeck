using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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

        public string GetSummonerSpell(ESpell spell)
        {
            switch (spell)
            {
                case ESpell.SummonerSpell1:
                    return SummonerSpells.Spell1.Id;

                case ESpell.SummonerSpell2:
                    return SummonerSpells.Spell2.Id;

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
        public string Id => RawDisplayName.Replace("GeneratedTip_SummonerSpell_", string.Empty).Replace("_DisplayName", string.Empty);

        [JsonProperty("displayName")]
        public string LocalizedName { get; set; }

        [JsonProperty("rawDisplayName")]
        public string RawDisplayName { get; set; }
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
