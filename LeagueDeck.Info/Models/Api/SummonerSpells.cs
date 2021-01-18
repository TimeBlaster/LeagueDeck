using Newtonsoft.Json;

namespace LeagueDeck.Models.Api
{
    public class SummonerSpells
    {
        [JsonProperty("summonerSpellOne")]
        public SummonerSpell Spell1 { get; set; }

        [JsonProperty("summonerSpellTwo")]
        public SummonerSpell Spell2 { get; set; }
    }


    public class SummonerSpell : BaseSpell
    {
        public string Id => RawDisplayName.Replace("GeneratedTip_SummonerSpell_", string.Empty).Replace("_DisplayName", string.Empty);
    }
}
