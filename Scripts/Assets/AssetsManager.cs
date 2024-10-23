#nullable enable
namespace TheOne.ResourceManagement
{
    using System;
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

        private readonly Dictionary<string, Object>   cacheSingle   = new Dictionary<string, Object>();
        private readonly Dictionary<string, Object[]> cacheMultiple = new Dictionary<string, Object[]>();

        protected AssetsManager(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        #region Sync

        T IAssetsManager.Load<T>(string key)
        {
            return (T)this.cacheSingle.GetOrAdd(key, () =>
            {
                var asset = this.Load<T>(key);
                this.logger.Debug($"Loaded {key}");
                return asset;
            });
        }

        IEnumerable<T> IAssetsManager.LoadAll<T>(string key)
        {
            return this.cacheMultiple.GetOrAdd(key, () =>
            {
                var assets = this.LoadAll<T>(key);
                this.logger.Debug($"Loaded {key}");
                return assets.ToArray<Object>();
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
            if (this.cacheSingle.Remove(key, out var asset))
            {
                this.Unload(asset);
                this.logger.Debug($"Unloaded {key}");
                return;
            }
            if (this.cacheMultiple.Remove(key, out var assets))
            {
                assets.ForEach(this.Unload);
                this.logger.Debug($"Unloaded {key}");
                return;
            }
            this.logger.Warning($"Trying to unload {key} that was not loaded");
        }

        protected abstract void Unload(Object asset);

        private void Dispose()
        {
            this.cacheSingle.Clear(this.Unload);
            this.cacheMultiple.Clear(assets => assets.ForEach(this.Unload));
        }

        void IDisposable.Dispose()
        {
            this.Dispose();
            this.logger.Debug("Disposed");
        }

        ~AssetsManager()
        {
            this.Dispose();
            this.logger.Debug("Finalized");
        }

        #endregion
    }
}