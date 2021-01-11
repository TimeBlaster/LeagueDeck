using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace LeagueDeck.ApiResponse
{
    public class Summoner
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
        public IEnumerable<Item> Items { get; set; }

        [JsonProperty("summonerSpells")]
        public SummonerSpells SummonerSpells { get; set; }

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

    public class ChampionStats
    {
        [JsonProperty("abilityPower")]
        public double AbilityPower { get; set; }

        [JsonProperty("armor")]
        public double Armor{ get; set; }

        [JsonProperty("armorPenetrationFlat")]
        public double ArmorPenetrationFlat { get; set; }

        [JsonProperty("armorPenetrationPercent")]
        public double ArmorPenetrationPercent { get; set; }

        [JsonProperty("attackDamage")]
        public double AttackDamage { get; set; }

        [JsonProperty("attackRange")]
        public double AttackRange { get; set; }

        [JsonProperty("attackSpeed")]
        public double AttackSpeed { get; set; }

        [JsonProperty("bonusArmorPenetrationPercent")]
        public double BonusArmorPenetrationPercent { get; set; }

        [JsonProperty("bonusMagicPenetrationPercent")]
        public double BonusMagicPenetrationPercent { get; set; }

        [JsonProperty("cooldownReduction")]
        public double CooldownReduction { get; set; }

        [JsonProperty("critChance")]
        public double CritChance { get; set; }

        [JsonProperty("critDamage")]
        public double CritDamage { get; set; }

        [JsonProperty("currentHealth")]
        public double CurrentHealth { get; set; }

        [JsonProperty("healthRegenRate")]
        public double HealthRegenRate { get; set; }

        [JsonProperty("lifeSteal")]
        public double LifeSteal { get; set; }

        [JsonProperty("magicLethality")]
        public double MagicLethality { get; set; }

        [JsonProperty("magicPenetrationFlat")]
        public double MagicPenetrationFlat { get; set; }

        [JsonProperty("magicPenetrationPercent")]
        public double MagicPenetrationPercent { get; set; }

        [JsonProperty("magicResist")]
        public double MagicResist { get; set; }

        [JsonProperty("maxHealth")]
        public double MaxHealth { get; set; }

        [JsonProperty("moveSpeed")]
        public double MoveSpeed { get; set; }

        [JsonProperty("physicalLethality")]
        public double PhysicalLethality { get; set; }

        [JsonProperty("resourceMax")]
        public double ResourceMax { get; set; }

        [JsonProperty("resourceRegenRate")]
        public double ResourceRegenRate { get; set; }

        [JsonProperty("resourceValue")]
        public double ResourceValue { get; set; }

        [JsonProperty("spellVamp")]
        public double SpellVamp { get; set; }

        [JsonProperty("tenacity")]
        public double Tenacity { get; set; }
    }

    public class Item
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

    public class Runes
    {
        [JsonProperty("generalRunes")]
        public List<Rune> General { get; set; }

        [JsonProperty("keystone")]
        public Rune Keystone { get; set; }

        [JsonProperty("primaryRuneTree")]
        public Rune PrimaryRuneTree { get; set; }

        [JsonProperty("secondaryRuneTree")]
        public Rune SecondaryRuneTree { get; set; }
    }

    public class Rune
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("displayName")]
        public string Name { get; set; }

        [JsonProperty("rawDescription")]
        public string RawDescription { get; set; }

        [JsonProperty("rawDisplayName")]
        public string RawDisplayName { get; set; }
    }

    public class SummonerSpells
    {
        [JsonProperty("summonerSpellOne")]
        public SummonerSpell Spell1 { get; set; }

        [JsonProperty("summonerSpellTwo")]
        public SummonerSpell Spell2 { get; set; }
    }

    public abstract class BaseSpell
    {
        [JsonProperty("displayName")]
        public string LocalizedName { get; set; }

        [JsonProperty("rawDescription")]
        public string RawDescription { get; set; }

        [JsonProperty("rawDisplayName")]
        public string RawDisplayName { get; set; }
    }

    public class SummonerSpell : BaseSpell
    {
        public string Id => RawDisplayName.Replace("GeneratedTip_SummonerSpell_", string.Empty).Replace("_DisplayName", string.Empty);
    }

    public class Ability : BaseSpell
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("abilityLevel")]
        public string Level { get; set; }
    }

    public class Abilities
    {
        [JsonProperty("Passive")]
        public Ability Passive { get; set; }

        [JsonProperty("Q")]
        public Ability Q { get; set; }

        [JsonProperty("W")]
        public Ability W { get; set; }

        [JsonProperty("E")]
        public Ability E { get; set; }

        [JsonProperty("R")]
        public Ability R { get; set; }
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

    public class ActivePlayer
    {
        [JsonProperty("abilities")]
        public Abilities Abilities { get; set; }

        [JsonProperty("championStats")]
        public ChampionStats Stats { get; set; }

        [JsonProperty("currentGold")]
        public double CurrentGold { get; set; }

        [JsonProperty("fullRunes")]
        public Runes Runes { get; set; }

        [JsonProperty("level")]
        public int Level { get; set; }

        [JsonProperty("summonerName")]
        public string Name { get; set; }
    }
}
