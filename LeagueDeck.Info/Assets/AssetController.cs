using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck.Core
{
    public interface IAssetLoader
    {
        Task LoadAssets(string version, CancellationToken ct);
        Task DownloadAssets(string version, CancellationToken ct, bool force);
    }

    public class AssetLoader
    {
        private List<IAssetLoader> _assetLoaders = new List<IAssetLoader>();

        public void Add<T>() where T : IAssetLoader, new()
        {
            var assetLoader = new T();
            this.Add(assetLoader);
        }

        public void Add(IAssetLoader assetLoader)
        {
            if (_assetLoaders.Any(x => x.GetType() == assetLoader.GetType()))
                return;

            _assetLoaders.Add(assetLoader);
        }

        public async Task UpdateData(CancellationToken ct, string version = null, bool force = false)
        {
            foreach (var loader in _assetLoaders)
            {
                await loader.DownloadAssets(version, ct, force);
            }
        }

        public async Task LoadData(CancellationToken ct, string version = null)
        {
            foreach (var loader in _assetLoaders)
            {
                await loader.LoadAssets(version, ct);
            }
        }
    }

    public abstract class AssetController<T>
    {
        public const string cImageFolderName = "Images";
        public const string cLeagueDeckDataFolderName = "LeagueDeckData";

        private const string cVersionsUrl = "https://ddragon.bangingheads.net/api/versions.json";
        private string _latestVersion;

        protected readonly UpdateProgressReporter _updateProgressReporter = UpdateProgressReporter.GetInstance();

        protected readonly string _leagueDeckDataFolder = Path.Combine(Environment.CurrentDirectory, cLeagueDeckDataFolderName);
        protected readonly string _missingImageId = "Missing";

        public abstract T GetAsset(string id);

        public abstract IReadOnlyList<T> GetAssets();

        public abstract Image GetImage(string id);

        protected async Task<string> GetLatestVersion(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_latestVersion))
            {
                var versionsJson = await ApiController.GetApiResponse(cVersionsUrl, ct);
                var versions = JsonConvert.DeserializeObject<List<string>>(versionsJson);

                _latestVersion = versions.FirstOrDefault();
            }

            return _latestVersion;
        }
    }
}
