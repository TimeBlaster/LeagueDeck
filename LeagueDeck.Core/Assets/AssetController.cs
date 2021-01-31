using LeagueDeck.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck.Core
{
    public abstract class AssetController<T> : IAssetLoader where T : Asset<T>, new()
    {
        #region vars

        public const string cImageFolderName = "Images";
        public const string cLeagueDeckDataFolderName = "LeagueDeckData";

        private const string cVersionsUrl = "https://ddragon.bangingheads.net/api/versions.json";
        private string _latestVersion;

        protected readonly UpdateProgressReporter _updateProgressReporter = UpdateProgressReporter.GetInstance();

        protected readonly string _leagueDeckDataFolder = Path.Combine(Environment.CurrentDirectory, cLeagueDeckDataFolderName);
        protected readonly string _imageFolder = Path.Combine(Environment.CurrentDirectory, cImageFolderName, typeof(T).Name);
        protected const string cMissingImageId = "Missing";

        protected List<T> _assets;

        protected async Task<string> GetJsonPath(string version, CancellationToken ct) =>
            Path.Combine(
                Environment.CurrentDirectory,
                cLeagueDeckDataFolderName,
                string.IsNullOrWhiteSpace(version) ? await GetLatestVersion(ct) : version,
                $"{typeof(T).Name}.json");

        #endregion

        public abstract Task DownloadAssets(HttpClient client, string version, CancellationToken ct, bool force = false);

        #region Public Methods

        public async Task LoadAssets(HttpClient client, string version, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(version))
                version = await GetLatestVersion(ct);

            InitDirectories(version);

            var jsonPath = await GetJsonPath(version, ct);
            if (!File.Exists(jsonPath))
            {
                await DownloadAssets(client, version, ct, true);
            }

            var json = File.ReadAllText(jsonPath);
            _assets = JsonConvert.DeserializeObject<List<T>>(json);
        }

        public T GetAsset(string id)
        {
            var asset = _assets.FirstOrDefault(x => x.Id == id);
            if (asset == null)
            {
                asset = new T().SetDefault();
                // TODO: log
            }
            return asset;
        }

        public IReadOnlyList<T> GetAssets()
        {
            return _assets.AsReadOnly();
        }

        public Image GetImage(string id)
        {
            var path = Path.Combine(_imageFolder, $"{id}.png");

            if (!File.Exists(path))
            {
                id = cMissingImageId;
                path = Path.Combine(_imageFolder, $"{id}.png");
            }

            return Image.FromFile(path);
        }

        #endregion

        #region Protected Methods

        protected async Task<string> GetLatestVersion(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(_latestVersion))
            {
                var versionsJson = await LiveClientApiController.GetApiResponse(cVersionsUrl, ct);
                var versions = JsonConvert.DeserializeObject<List<string>>(versionsJson);

                _latestVersion = versions.FirstOrDefault();
            }

            return _latestVersion;
        }

        protected void InitDirectories(string version)
        {
            var leagueDeckPatchFolder = Path.Combine(_leagueDeckDataFolder, version);
            Directory.CreateDirectory(leagueDeckPatchFolder);
            Directory.CreateDirectory(_imageFolder);
        }

        #endregion
    }
}
