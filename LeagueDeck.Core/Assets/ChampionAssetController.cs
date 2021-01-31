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
    public class ChampionAssetController : AssetController<Champion>
    {
        private const string cChampionsDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion.json";
        private const string cChampionDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion/{1}.json";
        private const string cChampionImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/champion/{1}.png";

        public Champion GetChampion(string championName)
        {
            var id = _assets.FirstOrDefault(x => x.Name == championName)?.Id ?? string.Empty;
            return GetAsset(id);
        }

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
                var url = string.Format(cChampionImageUrl, version, champion.Id);
                var imgPath = Path.Combine(_imageFolder, $"{champion.Id}.png");

                var response = await hc.GetAsync(url);
                using (var fs = new FileStream(imgPath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }

                _updateProgressReporter.IncrementCurrent();
            }

            var json = JsonConvert.SerializeObject(champions);
            File.WriteAllText(jsonPath, json);
        }

        private async Task<List<Champion>> GetChampions(HttpClient hc, string version, CancellationToken ct)
        {
            var url = string.Format(cChampionsDataUrl, version);

            var championsJson = await hc.GetStringAsync(url);
            var champions = JsonConvert.DeserializeObject<JObject>(championsJson);
            var data = champions.GetValue("data", StringComparison.OrdinalIgnoreCase);
            var children = data.Children<JProperty>();

            // 2 requests:
            // data, square
            _updateProgressReporter.Total += (uint)children.Count() * 2;

            return (List<Champion>)children.Select(async (x) =>
            {
                var id = x.Name;
                url = string.Format(cChampionDataUrl, version, id);

                var championJson = await LiveClientApiController.GetApiResponse(url, ct);
                var champion = JsonConvert.DeserializeObject<JObject>(championJson);

                var detailedData = champion.GetValue("data", StringComparison.OrdinalIgnoreCase).First.First;

                var name = detailedData["name"].Value<string>();
                var spells = JsonConvert.DeserializeObject<List<Spell>>(detailedData["spells"].ToString());

                _updateProgressReporter.IncrementCurrent();

                return new Champion
                {
                    Id = id,
                    Name = name,
                    Spells = spells,
                };
            });
        }
    }
}
