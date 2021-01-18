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
    public class SummonerSpellAssetController : AssetController<Spell>, IAssetLoader
    {
        private const string cAssetName = "SummonerSpells";

        private const string cSummonerSpellDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/summoner.json";
        private const string cSpellImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/spell/{1}.png";

        private readonly string _summonerSpellImageFolder = Path.Combine(Environment.CurrentDirectory, cImageFolderName, cAssetName);

        private List<Spell> _summonerSpells = new List<Spell>();

        public async Task LoadAssets(string version, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(version))
                version = await GetLatestVersion(ct);

            var leagueDeckPatchFolder = Path.Combine(_leagueDeckDataFolder, version);
            var jsonPath = Path.Combine(leagueDeckPatchFolder, $"{cAssetName}.json");

            if (!File.Exists(jsonPath))
                await DownloadAssets(version, ct, true);

            var json = File.ReadAllText(jsonPath);
            _summonerSpells = JsonConvert.DeserializeObject<List<Spell>>(json);
        }

        public async Task DownloadAssets(string version, CancellationToken ct, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(version))
                version = await GetLatestVersion(ct);

            Directory.CreateDirectory(_summonerSpellImageFolder);

            var leagueDeckPatchFolder = Path.Combine(_leagueDeckDataFolder, version);
            Directory.CreateDirectory(leagueDeckPatchFolder);

            var jsonPath = Path.Combine(leagueDeckPatchFolder, $"{cAssetName}.json");

            if (File.Exists(jsonPath) && !force)
                return;

            var summonerSpells = await GetSummonerSpells(version, ct);

            using (var wc = new WebClient())
            {
                foreach (var summonerSpell in summonerSpells)
                {
                    var url = string.Format(cSpellImageUrl, version, summonerSpell.Id);
                    var imgPath = Path.Combine(_summonerSpellImageFolder, $"{summonerSpell.Id}.png");
                    await wc.DownloadFileTaskAsync(url, imgPath);

                    _updateProgressReporter.IncrementCurrent();
                }
            }

            var json = JsonConvert.SerializeObject(summonerSpells);
            File.WriteAllText(jsonPath, json);
        }

        private async Task<List<Spell>> GetSummonerSpells(string version, CancellationToken ct)
        {
            var summonerSpellList = new List<Spell>();

            var url = string.Format(cSummonerSpellDataUrl, version);

            var summonerSpellsJson = await ApiController.GetApiResponse(url, ct);
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

        public override Spell GetAsset(string id)
        {
            var spell = _summonerSpells.FirstOrDefault(x => x.Id == id);
            if (spell == null)
            {
                spell = Spell.Default;
                // TODO: log
            }
            return spell;
        }

        public override IReadOnlyList<Spell> GetAssets()
        {
            return _summonerSpells.AsReadOnly();
        }

        public override Image GetImage(string id)
        {
            var path = Path.Combine(_summonerSpellImageFolder, $"{id}.png");

            if (!File.Exists(path))
            {
                id = _missingImageId;
                path = Path.Combine(_summonerSpellImageFolder, $"{id}.png");
            }

            return Image.FromFile(path);
        }
    }
}
