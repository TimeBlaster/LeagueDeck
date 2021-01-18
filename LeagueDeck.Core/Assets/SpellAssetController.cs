using LeagueDeck.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck.Core
{
    public class SpellAssetController : AssetController<Spell>, IAssetLoader
    {
        private const string cChampionsDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion.json";
        private const string cChampionDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion/{1}.json";
        private const string cSpellImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/spell/{1}.png";

        public async Task LoadAssets(string version, CancellationToken ct)
        {
            InitDirectories(version);

            var jsonPath = await GetJsonPath(version, ct);
            if (!File.Exists(jsonPath))
                await DownloadAssets(version, ct, true);

            var json = File.ReadAllText(jsonPath);
            _assets = JsonConvert.DeserializeObject<List<Spell>>(json);
        }

        public async Task DownloadAssets(string version, CancellationToken ct, bool force = false)
        {
            InitDirectories(version);

            var jsonPath = await GetJsonPath(version, ct);
            if (File.Exists(jsonPath) && !force)
                return;

            var champions = await GetChampions(version, ct);

            using (var wc = new WebClient())
            {
                foreach (var champion in champions)
                {
                    foreach (var spell in champion.Spells)
                    {
                        var url = string.Format(cSpellImageUrl, version, spell.Id);
                        var imgPath = Path.Combine(_imageFolder, $"{spell.Id}.png");
                        await wc.DownloadFileTaskAsync(url, imgPath);

                        _updateProgressReporter.IncrementCurrent();
                    }
                }
            }

            var json = JsonConvert.SerializeObject(champions.SelectMany(x=>x.Spells));
            File.WriteAllText(jsonPath, json);
        }

        private async Task<List<Champion>> GetChampions(string version, CancellationToken ct)
        {
            var championList = new List<Champion>();

            var url = string.Format(cChampionsDataUrl, version);

            var championsJson = await ApiController.GetApiResponse(url, ct);
            var champions = JsonConvert.DeserializeObject<JObject>(championsJson);
            var data = champions.GetValue("data", StringComparison.OrdinalIgnoreCase);
            var children = data.Children<JProperty>();

            // 5 requests:
            // data, q, w, e, r
            _updateProgressReporter.Total += (uint)children.Count() * 5;

            foreach (var x in children)
            {
                var id = x.Name;
                url = string.Format(cChampionDataUrl, version, id);

                var championJson = await ApiController.GetApiResponse(url, ct);
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
    }
}
