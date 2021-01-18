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
    public class ItemAssetController : AssetController<Item>, IAssetLoader
    {
        private const string cItemDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/item.json";
        private const string cItemImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/item/{1}.png";

        public async Task LoadAssets(string version, CancellationToken ct)
        {
            InitDirectories(version);

            var jsonPath = await GetJsonPath(version, ct);
            if (!File.Exists(jsonPath))
                await DownloadAssets(version, ct, true);

            var json = File.ReadAllText(jsonPath);
            _assets = JsonConvert.DeserializeObject<List<Item>>(json);
        }

        public async Task DownloadAssets(string version, CancellationToken ct, bool force = false)
        {
            InitDirectories(version);

            var jsonPath = await GetJsonPath(version, ct);
            if (File.Exists(jsonPath) && !force)
                return;

            var items = await GetItems(version, ct);

            using (var wc = new WebClient())
            {
                foreach (var item in items)
                {
                    var url = string.Format(cItemImageUrl, version, item.Id);
                    var imgPath = Path.Combine(_imageFolder, $"{item.Id}.png");
                    await wc.DownloadFileTaskAsync(url, imgPath);

                    _updateProgressReporter.IncrementCurrent();
                }
            }

            var json = JsonConvert.SerializeObject(items);
            File.WriteAllText(jsonPath, json);
        }

        private async Task<List<Item>> GetItems(string version, CancellationToken ct)
        {
            var itemList = new List<Item>();

            var url = string.Format(cItemDataUrl, version);

            var itemJson = await ApiController.GetApiResponse(url, ct);
            var items = JsonConvert.DeserializeObject<JObject>(itemJson);
            var data = items.GetValue("data", StringComparison.OrdinalIgnoreCase);
            var children = data.Children<JProperty>();

            // 1 request:
            // image
            _updateProgressReporter.Total += (uint)children.Count();

            foreach (var child in children)
            {
                var id = child.Name;

                var item = child.First;

                var name = item["name"].Value<string>();
                var from = item["from"]?.Values<int>();

                var gold = item["gold"];
                var baseCost = gold["base"].Value<int>();
                var totalCost = gold["total"].Value<int>();

                var stats = item["stats"];
                var modifier = stats.Cast<JProperty>().FirstOrDefault(x => x.Name == "AbilityHasteMod");

                var abilityHaste = 0d;
                if (modifier != null)
                    abilityHaste = modifier.First.Value<double>();

                itemList.Add(new Item
                {
                    Id = id,
                    Name = name,
                    AbilityHaste = abilityHaste,
                    ComponentIds = from?.ToList() ?? new List<int>(),
                    BaseCost = baseCost,
                    TotalCost = totalCost,
                });
            }

            return itemList;
        }
    }
}
