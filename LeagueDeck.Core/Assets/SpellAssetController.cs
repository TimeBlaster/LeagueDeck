using LeagueDeck.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck.Core
{
    public class SpellAssetController : AssetController<Spell>
    {
        private const string cChampionsDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion.json";
        private const string cChampionDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion/{1}.json";

        private const string cSummonerSpellDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/summoner.json";

        private const string cSpellImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/spell/{1}.png";

        public override async Task DownloadAssets(HttpClient hc, string version, CancellationToken ct, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(version))
                version = await GetLatestVersion(ct);

            InitDirectories(version);

            var jsonPath = await GetJsonPath(version, ct);
            if (File.Exists(jsonPath) && !force)
                return;

            var champions = await GetChampions(hc, version, ct);

            foreach (var champion in champions)
            {
                foreach (var spell in champion.Spells)
                {
                    var url = string.Format(cSpellImageUrl, version, spell.Id);
                    var imgPath = Path.Combine(_imageFolder, $"{spell.Id}.png");

                    var response = await hc.GetAsync(url);
                    using (var fs = new FileStream(imgPath, FileMode.Create))
                    {
                        await response.Content.CopyToAsync(fs);
                    }

                    _updateProgressReporter.IncrementCurrent();
                }
            }

            var summonerSpells = await GetSummonerSpells(hc, version, ct);

            foreach (var summonerSpell in summonerSpells)
            {
                var url = string.Format(cSpellImageUrl, version, summonerSpell.Id);
                var imgPath = Path.Combine(_imageFolder, $"{summonerSpell.Id}.png");

                var response = await hc.GetAsync(url);
                using (var fs = new FileStream(imgPath, FileMode.CreateNew))
                {
                    await response.Content.CopyToAsync(fs);
                }

                _updateProgressReporter.IncrementCurrent();
            }

            var spells = champions.SelectMany(x => x.Spells).Concat(summonerSpells);
            var json = JsonConvert.SerializeObject(spells);
            File.WriteAllText(jsonPath, json);
        }

        private async Task<List<Champion>> GetChampions(HttpClient hc, string version, CancellationToken ct)
        {
            var championList = new List<Champion>();

            var url = string.Format(cChampionsDataUrl, version);

            var championsJson = await hc.GetStringAsync(url);
            var champions = JsonConvert.DeserializeObject<JObject>(championsJson);
            var data = champions.GetValue("data", StringComparison.OrdinalIgnoreCase);
            var children = data.Children<JProperty>();

            // 5 requests:
            // data, q, w, e, r
            // TODO: passive
            _updateProgressReporter.Total += (uint)children.Count() * 5;

            foreach (var x in children)
            {
                var id = x.Name;
                url = string.Format(cChampionDataUrl, version, id);

                var championJson = await LiveClientApiController.GetApiResponse(url, ct);
                var champion = JsonConvert.DeserializeObject<JObject>(championJson);

                var detailedData = champion.GetValue("data", StringComparison.OrdinalIgnoreCase).First.First;

                var name = detailedData["name"].Value<string>();
                var spells = JsonConvert.DeserializeObject<List<Spell>>(detailedData["spells"].ToString());

                championList.Add(new Champion
                {
                    Id = id,
                    Name = name,
                    Spells = spells,
                });

                _updateProgressReporter.IncrementCurrent();
            }

            return championList;
        }

        private async Task<List<Spell>> GetSummonerSpells(HttpClient hc, string version, CancellationToken ct)
        {
            var summonerSpellList = new List<Spell>();

            var url = string.Format(cSummonerSpellDataUrl, version);

            var summonerSpellsJson = await hc.GetStringAsync(url);
            var summonerSpells = JsonConvert.DeserializeObject<JObject>(summonerSpellsJson);
            var data = summonerSpells.GetValue("data", StringComparison.OrdinalIgnoreCase);
            var children = data.Children<JProperty>().Select(x => x.First);

            // 1 request:
            // image
            _updateProgressReporter.Total += (uint)children.Count();

            foreach (var summonerSpell in children)
            {
                var spell = JsonConvert.DeserializeObject<Spell>(summonerSpell.ToString());
                summonerSpellList.Add(spell);
            }

            return summonerSpellList;
        }
    }
}
