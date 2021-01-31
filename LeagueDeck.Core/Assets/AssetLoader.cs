using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck
{
    public interface IAssetLoader
    {
        Task LoadAssets(HttpClient client, string version, CancellationToken ct);
        Task DownloadAssets(HttpClient client, string version, CancellationToken ct, bool forceUpdate);
    }

    public class AssetLoader
    {
        private List<IAssetLoader> _assetLoaders = new List<IAssetLoader>();
        private HttpClient _client;

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
            if (_client == null)
                _client = new HttpClient();

            await Task.WhenAll(_assetLoaders.Select(x => x.DownloadAssets(_client, version, ct, force)));
        }

        public async Task LoadData(CancellationToken ct, string version = null)
        {
            if (_client == null)
                _client = new HttpClient();

            await Task.WhenAll(_assetLoaders.Select(x => x.LoadAssets(_client, version, ct)));
        }
    }
}
