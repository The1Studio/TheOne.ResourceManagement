#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using UniT.Extensions;
    using UniT.Logging;
    using ILogger = UniT.Logging.ILogger;
    using Object = UnityEngine.Object;
    #if UNIT_UNITASK
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

        void IAssetsManager.Initialize() => this.Initialize();

        T IAssetsManager.Load<T>(string key)
        {
            try
            {
                return (T)this.cacheSingle.GetOrAdd(key, () =>
                {
                    var asset = this.Load<T>(key);
                    if (asset is null) throw new NullReferenceException($"{key} is null");
                    this.logger.Debug($"Loaded {key}");
                    return asset;
                });
            }
            catch (Exception inner)
            {
                throw new ArgumentOutOfRangeException($"Failed to load {key}", inner);
            }
        }

        T[] IAssetsManager.LoadAll<T>(string key)
        {
            try
            {
                return (T[])this.cacheMultiple.GetOrAdd(key, () =>
                {
                    var assets = this.LoadAll<T>(key);
                    this.logger.Debug($"Loaded {key}");
                    return assets;
                });
            }
            catch (Exception inner)
            {
                throw new ArgumentOutOfRangeException($"Failed to load {key}", inner);
            }
        }

        protected virtual void Initialize() { }

        protected abstract T? Load<T>(string key) where T : Object;

        protected abstract T[] LoadAll<T>(string key) where T : Object;

        #endregion

        #region Async

        #if UNIT_UNITASK
        UniTask IAssetsManager.InitializeAsync(IProgress<float>? progress, CancellationToken cancellationToken) => this.InitializeAsync(progress, cancellationToken);

        async UniTask<T> IAssetsManager.LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            try
            {
                return (T)await this.cacheSingle.GetOrAddAsync(key, async () =>
                {
                    var asset = await this.LoadAsync<T>(key, progress, cancellationToken);
                    if (asset is null) throw new NullReferenceException($"{key} is null");
                    this.logger.Debug($"Loaded {key}");
                    return (Object)asset;
                });
            }
            catch (Exception inner)
            {
                throw new ArgumentOutOfRangeException($"Failed to load {key}", inner);
            }
        }

        async UniTask<T[]> IAssetsManager.LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            try
            {
                return (T[])await this.cacheMultiple.GetOrAddAsync(key, async () =>
                {
                    var assets = await this.LoadAllAsync<T>(key, progress, cancellationToken);
                    this.logger.Debug($"Loaded {key}");
                    return (Object[])assets;
                });
            }
            catch (Exception inner)
            {
                throw new ArgumentOutOfRangeException($"Failed to load {key}", inner);
            }
        }

        protected virtual UniTask InitializeAsync(IProgress<float>? progress, CancellationToken cancellationToken) => UniTask.CompletedTask;

        protected abstract UniTask<T?> LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken) where T : Object;

        protected abstract UniTask<T[]> LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken) where T : Object;
        #else
        IEnumerator IAssetsManager.InitializeAsync(Action? callback, IProgress<float>? progress) => this.InitializeAsync(callback, progress);

        IEnumerator IAssetsManager.LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress)
        {
            return this.cacheSingle.GetOrAddAsync(
                key,
                callback => this.LoadAsync<T>(
                    key,
                    asset =>
                    {
                        if (asset is null) throw new NullReferenceException($"{key} is null");
                        this.logger.Debug($"Loaded {key}");
                        callback(asset);
                    },
                    progress
                ),
                asset => callback((T)asset)
            ).Catch(inner => throw new ArgumentOutOfRangeException($"Failed to load {key}", inner));
        }

        IEnumerator IAssetsManager.LoadAllAsync<T>(string key, Action<T[]> callback, IProgress<float>? progress)
        {
            return this.cacheMultiple.GetOrAddAsync(
                key,
                callback => this.LoadAllAsync<T>(
                    key,
                    assets =>
                    {
                        this.logger.Debug($"Loaded {key}");
                        callback(assets);
                    },
                    progress
                ),
                assets => callback((T[])assets)
            ).Catch(inner => throw new ArgumentOutOfRangeException($"Failed to load {key}", inner));
        }

        protected virtual IEnumerator InitializeAsync(Action? callback, IProgress<float>? progress)
        {
            progress?.Report(1);
            callback?.Invoke();
            yield break;
        }

        protected abstract IEnumerator LoadAsync<T>(string key, Action<T?> callback, IProgress<float>? progress) where T : Object;

        protected abstract IEnumerator LoadAllAsync<T>(string key, Action<T[]> callback, IProgress<float>? progress) where T : Object;
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