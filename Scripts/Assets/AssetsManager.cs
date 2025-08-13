#nullable enable
namespace TheOne.ResourceManagement
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using TheOne.Extensions;
    using TheOne.Logging;
    using ILogger = TheOne.Logging.ILogger;
    using Object = UnityEngine.Object;
    #if THEONE_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public abstract class AssetsManager : IAssetsManager
    {
        #region Constructor

        private readonly ILogger logger;

        private readonly ConcurrentDictionary<string, Object>   cacheSingle   = new ConcurrentDictionary<string, Object>();
        private readonly ConcurrentDictionary<string, Object[]> cacheMultiple = new ConcurrentDictionary<string, Object[]>();
        private readonly object loadLock = new object();
        private bool disposed;

        protected AssetsManager(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        #region Sync

        T IAssetsManager.Load<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Asset key cannot be null or empty", nameof(key));
            
            if (this.disposed)
                throw new ObjectDisposedException(nameof(AssetsManager));
            
            return (T)this.cacheSingle.GetOrAdd(key, k =>
            {
                lock (this.loadLock)
                {
                    if (this.cacheSingle.TryGetValue(k, out var existing))
                        return existing;
                    
                    var asset = this.Load<T>(k);
                    if (asset == null)
                        throw new InvalidOperationException($"Failed to load asset with key: {k}");
                    
                    this.logger.Debug($"Loaded {k}");
                    return asset;
                }
            });
        }

        IEnumerable<T> IAssetsManager.LoadAll<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Asset key cannot be null or empty", nameof(key));
            
            if (this.disposed)
                throw new ObjectDisposedException(nameof(AssetsManager));
            
            return this.cacheMultiple.GetOrAdd(key, k =>
            {
                lock (this.loadLock)
                {
                    if (this.cacheMultiple.TryGetValue(k, out var existing))
                        return existing;
                    
                    var assets = this.LoadAll<T>(k);
                    this.logger.Debug($"Loaded all {k}");
                    return assets?.ToArray<Object>() ?? Array.Empty<Object>();
                }
            }).Cast<T>();
        }

        void IAssetsManager.Download(string key) => this.Download(key);

        void IAssetsManager.DownloadAll() => this.DownloadAll();

        protected abstract T Load<T>(string key) where T : Object;

        protected abstract IEnumerable<T> LoadAll<T>(string key) where T : Object;

        protected virtual void Download(string key) { }

        protected virtual void DownloadAll() { }

        #endregion

        #region Async

        #if THEONE_UNITASK
        async UniTask<T> IAssetsManager.LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (T)await this.cacheSingle.GetOrAddAsync(key, async () =>
            {
                var asset = await this.LoadAsync<T>(key, progress, cancellationToken);
                this.logger.Debug($"Loaded {key}");
                return (Object)asset;
            });
        }

        async UniTask<IEnumerable<T>> IAssetsManager.LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (await this.cacheMultiple.GetOrAddAsync(key, async () =>
            {
                var assets = await this.LoadAllAsync<T>(key, progress, cancellationToken);
                this.logger.Debug($"Loaded {key}");
                return assets.ToArray<Object>();
            })).Cast<T>();
        }

        UniTask IAssetsManager.DownloadAsync(string key, IProgress<float>? progress, CancellationToken cancellationToken) => this.DownloadAsync(key, progress, cancellationToken);

        UniTask IAssetsManager.DownloadAllAsync(IProgress<float>? progress, CancellationToken cancellationToken) => this.DownloadAllAsync(progress, cancellationToken);

        protected abstract UniTask<T> LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken) where T : Object;

        protected abstract UniTask<IEnumerable<T>> LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken) where T : Object;

        protected virtual UniTask DownloadAsync(string key, IProgress<float>? progress, CancellationToken cancellationToken) => UniTask.CompletedTask;

        protected virtual UniTask DownloadAllAsync(IProgress<float>? progress, CancellationToken cancellationToken) => UniTask.CompletedTask;
        #else
        IEnumerator IAssetsManager.LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress)
        {
            return this.cacheSingle.GetOrAddAsync(
                key,
                callback => this.LoadAsync<T>(
                    key,
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

        IEnumerator IAssetsManager.LoadAllAsync<T>(string key, Action<IEnumerable<T>> callback, IProgress<float>? progress)
        {
            return this.cacheMultiple.GetOrAddAsync(
                key,
                callback => this.LoadAllAsync<T>(
                    key,
                    assets =>
                    {
                        this.logger.Debug($"Loaded {key}");
                        callback(assets.ToArray<Object>());
                    },
                    progress
                ),
                assets => callback(assets.Cast<T>())
            );
        }

        IEnumerator IAssetsManager.DownloadAsync(string key, Action? callback, IProgress<float>? progress) => this.DownloadAsync(key, callback, progress);

        IEnumerator IAssetsManager.DownloadAllAsync(Action? callback, IProgress<float>? progress) => this.DownloadAllAsync(callback, progress);

        protected abstract IEnumerator LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress) where T : Object;

        protected abstract IEnumerator LoadAllAsync<T>(string key, Action<IEnumerable<T>> callback, IProgress<float>? progress) where T : Object;

        protected virtual IEnumerator DownloadAsync(string key, Action? callback, IProgress<float>? progress)
        {
            yield break;
        }

        protected virtual IEnumerator DownloadAllAsync(Action? callback, IProgress<float>? progress)
        {
            yield break;
        }
        #endif

        #endregion

        #region Finalizer

        void IAssetsManager.Unload(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;
            
            if (this.cacheSingle.TryRemove(key, out var asset))
            {
                if (asset != null)
                    this.Unload(asset);
                this.logger.Debug($"Unloaded {key}");
                return;
            }
            if (this.cacheMultiple.TryRemove(key, out var assets))
            {
                if (assets != null)
                    assets.Where(a => a != null).ForEach(this.Unload);
                this.logger.Debug($"Unloaded {key}");
                return;
            }
            this.logger.Warning($"Trying to unload {key} that was not loaded");
        }

        protected abstract void Unload(Object asset);

        private void Dispose(bool disposing)
        {
            if (this.disposed)
                return;
            
            if (disposing)
            {
                foreach (var kvp in this.cacheSingle)
                {
                    if (kvp.Value != null)
                        this.Unload(kvp.Value);
                }
                this.cacheSingle.Clear();
                
                foreach (var kvp in this.cacheMultiple)
                {
                    if (kvp.Value != null)
                        kvp.Value.Where(a => a != null).ForEach(this.Unload);
                }
                this.cacheMultiple.Clear();
            }
            
            this.disposed = true;
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
            this.logger.Debug("Disposed");
        }

        ~AssetsManager()
        {
            this.Dispose(false);
        }

        #endregion
    }
}