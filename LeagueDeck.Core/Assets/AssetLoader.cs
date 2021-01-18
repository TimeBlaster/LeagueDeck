using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LeagueDeck
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
}
