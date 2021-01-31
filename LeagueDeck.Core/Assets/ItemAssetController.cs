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
    public class ItemAssetController : AssetController<Item>
    {
        private const string cItemDataUrl = "https://ddragon.bangingheads.net/cdn/{0}/data/en_US/item.json";
        private const string cItemImageUrl = "https://ddragon.bangingheads.net/cdn/{0}/img/item/{1}.png";

        public override async Task DownloadAssets(HttpClient hc, string version, CancellationToken ct, bool force = false)
        {
            if (string.IsNullOrWhiteSpace(version))
                version = await GetLatestVersion(ct);

            InitDirectories(version);

            var jsonPath = await GetJsonPath(version, ct);
            if (File.Exists(jsonPath) && !force)
                return;

            var items = await GetItems(hc, version, ct);

            foreach (var item in items)
            {
                var url = string.Format(cItemImageUrl, version, item.Id);
                var imgPath = Path.Combine(_imageFolder, $"{item.Id}.png");

                var response = await hc.GetAsync(url);
                using(var fs = new FileStream(imgPath, FileMode.Create))
                {
                    await response.Content.CopyToAsync(fs);
                }

                _updateProgressReporter.IncrementCurrent();
            }

            var json = JsonConvert.SerializeObject(items);
            File.WriteAllText(jsonPath, json);
        }

        private async Task<List<Item>> GetItems(HttpClient hc, string version, CancellationToken ct)
        {
            var itemList = new List<Item>();

            var url = string.Format(cItemDataUrl, version);

            var itemJson = await hc.GetStringAsync(url);
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
