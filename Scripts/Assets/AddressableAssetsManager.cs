#if UNIT_ADDRESSABLES
#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UniT.Extensions;
    using UniT.Logging;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.ResourceManagement.ResourceLocations;
    using UnityEngine.Scripting;
    using Object = UnityEngine.Object;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public sealed class AddressableAssetsManager : IAssetsManager, IRemoteAssetsDownloader
    {
        #region Constructor

        private readonly string? scope;
        private readonly ILogger logger;

        private readonly Dictionary<string, Object>                      cache  = new Dictionary<string, Object>();
        private readonly Dictionary<string, IReadOnlyCollection<string>> keyMap = new Dictionary<string, IReadOnlyCollection<string>>();

        [Preserve]
        public AddressableAssetsManager(ILoggerManager loggerManager, string? scope = null)
        {
            this.scope  = scope.NullIfWhiteSpace();
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        #region Sync

        T IAssetsManager.Load<T>(string key) => this.Load<T>(key);

        IEnumerable<T> IAssetsManager.LoadAll<T>(string key)
        {
            var keys = this.keyMap.GetOrAdd(key, () =>
            {
                var resourceLocations = this.GetAllResourceLocationsInternal<T>(key).WaitForResultOrThrow();
                return this.GetAllKeys(resourceLocations);
            });
            this.logger.Debug($"Found {keys.Count} keys for {key}");
            return keys.Select(this.Load<T>).ToArray();
        }

        private T Load<T>(string key) where T : Object
        {
            return (T)this.cache.GetOrAdd(key, () =>
            {
                var asset = this.LoadInternal<T>(key).WaitForResultOrThrow();
                this.logger.Debug($"Loaded {key}");
                return asset;
            });
        }

        #endregion

        #region Async

        #if UNIT_UNITASK
        UniTask<T> IAssetsManager.LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken) => this.LoadAsync<T>(key, progress, cancellationToken);

        async UniTask<IEnumerable<T>> IAssetsManager.LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            var keys = await this.keyMap.GetOrAddAsync(key, async () =>
            {
                var resourceLocations = await this.GetAllResourceLocationsInternal<T>(key).ToUniTask(cancellationToken: cancellationToken);
                return this.GetAllKeys(resourceLocations);
            });
            this.logger.Debug($"Found {keys.Count} keys for {key}");
            return await keys.SelectAsync(this.LoadAsync<T>, progress, cancellationToken).ToArrayAsync();
        }

        UniTask IRemoteAssetsDownloader.DownloadAsync(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return this.DownloadInternal(key).ToUniTask(progress, cancellationToken);
        }

        async UniTask IRemoteAssetsDownloader.DownloadAllAsync(IProgress<float>? progress, CancellationToken cancellationToken)
        {
            var subProgresses = progress.CreateSubProgresses(2).ToArray();
            await InitializeInternal().ToUniTask(subProgresses[0], cancellationToken);
            await DownloadAllInternal().ToUniTask(subProgresses[1], cancellationToken);
        }

        private async UniTask<T> LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken) where T : Object
        {
            return (T)await this.cache.GetOrAddAsync(key, async () =>
            {
                var asset = await this.LoadInternal<T>(key).ToUniTask(progress, cancellationToken);
                this.logger.Debug($"Loaded {key}");
                return (Object)asset;
            });
        }
        #else
        IEnumerator IAssetsManager.LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress) => this.LoadAsync(key, callback, progress);

        IEnumerator IAssetsManager.LoadAllAsync<T>(string key, Action<IEnumerable<T>> callback, IProgress<float>? progress)
        {
            var keys = default(IReadOnlyCollection<string>)!;
            yield return this.keyMap.GetOrAddAsync(
                key,
                callback => this.GetAllResourceLocationsInternal<T>(key).ToCoroutine(resourceLocations => callback(this.GetAllKeys(resourceLocations))),
                result => keys = result
            );
            this.logger.Debug($"Found {keys.Count} keys for {key}");
            yield return keys.SelectAsync<string, T>(this.LoadAsync, result => callback(result.ToArray()), progress);
        }

        IEnumerator IRemoteAssetsDownloader.DownloadAsync(string key, Action? callback, IProgress<float>? progress)
        {
            return this.DownloadInternal(key).ToCoroutine(callback, progress);
        }

        IEnumerator IRemoteAssetsDownloader.DownloadAllAsync(Action? callback, IProgress<float>? progress)
        {
            var subProgresses = progress.CreateSubProgresses(2).ToArray();
            yield return InitializeInternal().ToCoroutine(progress: subProgresses[0]);
            yield return DownloadAllInternal().ToCoroutine(progress: subProgresses[1]);
            callback?.Invoke();
        }

        private IEnumerator LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress) where T : Object
        {
            return this.cache.GetOrAddAsync(
                key,
                callback => this.LoadInternal<T>(key).ToCoroutine(
                    asset =>
                    {
                        this.logger.Debug($"Loaded {key}");
                        callback(asset);
                    },
                    progress
                ),
                asset => callback((T)asset)
            );
        }
        #endif

        #endregion

        #region Finalizer

        void IAssetsManager.Unload(string key)
        {
            if (!this.cache.Remove(key, out var asset))
            {
                this.logger.Warning($"Trying to unload {key} that was not loaded");
                return;
            }
            Addressables.Release(asset);
            this.logger.Debug($"Unloaded {key}");
        }

        void IAssetsManager.UnloadAll(string key)
        {
            if (!this.keyMap.TryGetValue(key, out var keys))
            {
                this.logger.Warning($"Trying to unload all {key} that was not loaded");
                return;
            }
            keys.ForEach(((IAssetsManager)this).Unload);
        }

        private void Dispose()
        {
            this.cache.Clear(Addressables.Release);
            this.keyMap.Clear();
        }

        void IDisposable.Dispose()
        {
            this.Dispose();
            this.logger.Debug("Disposed");
        }

        ~AddressableAssetsManager()
        {
            this.Dispose();
            this.logger.Debug("Finalized");
        }

        #endregion

        #region Internal

        private string KeyPrefix => this.scope is null ? string.Empty : $"{this.scope}/";

        private string GetScopedKey(string key) => $"{this.KeyPrefix}{key}";

        private AsyncOperationHandle<T> LoadInternal<T>(string key)
        {
            return Addressables.LoadAssetAsync<T>(this.GetScopedKey(key));
        }

        private AsyncOperationHandle<IList<IResourceLocation>> GetAllResourceLocationsInternal<T>(string key)
        {
            return Addressables.LoadResourceLocationsAsync(this.GetScopedKey(key), typeof(T));
        }

        private AsyncOperationHandle DownloadInternal(string key)
        {
            return Addressables.DownloadDependenciesAsync(this.GetScopedKey(key), autoReleaseHandle: true);
        }

        private static AsyncOperationHandle DownloadAllInternal()
        {
            return Addressables.DownloadDependenciesAsync(Addressables.ResourceLocators.SelectMany(locator => locator.Keys), autoReleaseHandle: true);
        }

        private static AsyncOperationHandle InitializeInternal()
        {
            return Addressables.InitializeAsync(autoReleaseHandle: true);
        }

        private IReadOnlyCollection<string> GetAllKeys(IList<IResourceLocation> resourceLocations)
        {
            return resourceLocations.Select(resourceLocation => resourceLocation.PrimaryKey.TrimStart(this.KeyPrefix)).ToArray();
        }

        #endregion
    }
}
#endif