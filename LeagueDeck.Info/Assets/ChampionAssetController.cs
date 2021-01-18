using LeagueDeck.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck.Core
{
    public class ChampionAssetController : AssetController<Champion>, IAssetLoader
    {
        private const string cAssetName = "Champions";

        private const string cChampionsDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion.json";
        private const string cChampionDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/champion/{1}.json";
        private const string cChampionImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/champion/{1}.png";

        private readonly string _championImageFolder = Path.Combine(Environment.CurrentDirectory, cImageFolderName, cAssetName);

        private List<Champion> _champions = new List<Champion>();

        public async Task LoadAssets(string version, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(version))
                version = await GetLatestVersion(ct);

            var leagueDeckPatchFolder = Path.Combine(_leagueDeckDataFolder, version);
            var jsonPath = Path.Combine(leagueDeckPatchFolder, $"{cAssetName}.json");

            if (!File.Exists(jsonPath))
                await DownloadAssets(version, ct, true);

            var json = File.ReadAllText(jsonPath);
            _champions = JsonConvert.DeserializeObject<List<Champion>>(json);
        }

        public async Task DownloadAssets(string version, CancellationToken ct, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(version))
                version = await GetLatestVersion(ct);

            Directory.CreateDirectory(_championImageFolder);

            var leagueDeckPatchFolder = Path.Combine(_leagueDeckDataFolder, version);
            Directory.CreateDirectory(leagueDeckPatchFolder);

            var jsonPath = Path.Combine(leagueDeckPatchFolder, $"{cAssetName}.json");

            if (File.Exists(jsonPath) && !force)
                return;

            var champions = await GetChampions(version, ct);

            using (var wc = new WebClient())
            {
                foreach (var champion in champions)
                {
                    var url = string.Format(cChampionImageUrl, version, champion.Id);
                    var imgPath = Path.Combine(_championImageFolder, $"{champion.Id}.png");
                    await wc.DownloadFileTaskAsync(url, imgPath);

                    _updateProgressReporter.IncrementCurrent();
                }
            }

            var json = JsonConvert.SerializeObject(champions);
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

            // 2 requests:
            // data, square
            _updateProgressReporter.Total += (uint)children.Count() * 2;

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

        #region Overrides

        public override Champion GetAsset(string id)
        {
            var champion = _champions.FirstOrDefault(x => x.Id == id);
            if(champion == null)
            {
                champion = Champion.Default;
                // TODO: log
            }
            return champion;
        }

        public override IReadOnlyList<Champion> GetAssets()
        {
            return _champions.AsReadOnly();
        }

        public override Image GetImage(string id)
        {
            var path = Path.Combine(_championImageFolder, $"{id}.png");

            if (!File.Exists(path))
            {
                id = _missingImageId;
                path = Path.Combine(_championImageFolder, $"{id}.png");
            }

            return Image.FromFile(path);
        }

        #endregion
    }
}
