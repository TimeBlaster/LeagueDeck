using Newtonsoft.Json;

namespace LeagueDeck.Models.Api
{
    public class ChampionStats
    {
        [JsonProperty("abilityPower")]
        public double AbilityPower { get; set; }

        [JsonProperty("armor")]
        public double Armor { get; set; }

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
}
