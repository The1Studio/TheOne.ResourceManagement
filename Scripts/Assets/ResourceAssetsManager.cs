#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UniT.Extensions;
    using UniT.Logging;
    using UnityEngine;
    using UnityEngine.Scripting;
    using ILogger = UniT.Logging.ILogger;
    using Object = UnityEngine.Object;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public sealed class ResourceAssetsManager : IAssetsManager
    {
        #region Constructor

        private readonly string? scope;
        private readonly ILogger logger;

        private readonly Dictionary<string, Object>   cacheSingle   = new Dictionary<string, Object>();
        private readonly Dictionary<string, Object[]> cacheMultiple = new Dictionary<string, Object[]>();

        [Preserve]
        public ResourceAssetsManager(ILoggerManager loggerManager, string? scope = null)
        {
            this.scope  = scope.NullIfWhiteSpace();
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        #region Sync

        private string GetScopedKey(string key) => this.scope is null ? key : $"{this.scope}/{key}";

        T IAssetsManager.Load<T>(string key)
        {
            return (T)this.cacheSingle.GetOrAdd(key, () =>
            {
                var asset = Resources.Load<T>(this.GetScopedKey(key))
                    ?? throw new ArgumentOutOfRangeException($"{key} not found in resources");
                this.logger.Debug($"Loaded {key}");
                return asset;
            });
        }

        IEnumerable<T> IAssetsManager.LoadAll<T>(string key) => this.LoadAll<T>(key);

        private IEnumerable<T> LoadAll<T>(string key) where T : Object
        {
            return this.cacheMultiple.GetOrAdd(key, () =>
            {
                var assets = Resources.LoadAll<T>(this.GetScopedKey(key));
                this.logger.Debug($"Loaded {key}");
                return assets.ToArray<Object>();
            }).Cast<T>();
        }

        #endregion

        #region Async

        #if UNIT_UNITASK
        async UniTask<T> IAssetsManager.LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return (T)await this.cacheSingle.GetOrAddAsync(key, async () =>
            {
                var asset = await Resources.LoadAsync<T>(this.GetScopedKey(key)).ToUniTask(progress: progress, cancellationToken: cancellationToken)
                    ?? throw new ArgumentOutOfRangeException($"{key} not found in resources");
                this.logger.Debug($"Loaded {key}");
                return asset;
            });
        }

        UniTask<IEnumerable<T>> IAssetsManager.LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            this.logger.Warning("Unity does not support loading all from resources asynchronously");
            return UniTask.FromResult(this.LoadAll<T>(key));
        }
        #else
        IEnumerator IAssetsManager.LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress)
        {
            return this.cacheSingle.GetOrAddAsync(
                key,
                callback =>
                {
                    var operation = Resources.LoadAsync<T>(this.GetScopedKey(key));
                    return operation.ToCoroutine(
                        () => callback(operation.asset ?? throw new ArgumentOutOfRangeException($"{key} not found in resources")),
                        progress
                    );
                },
                asset => callback((T)asset)
            );
        }

        IEnumerator IAssetsManager.LoadAllAsync<T>(string key, Action<IEnumerable<T>> callback, IProgress<float>? progress)
        {
            this.logger.Warning("Unity does not support loading all from resources asynchronously");
            callback(this.LoadAll<T>(key));
            yield break;
        }
        #endif

        #endregion

        #region Finalizer

        void IAssetsManager.Unload(string key)
        {
            if (!this.cacheSingle.Remove(key, out var asset))
            {
                this.logger.Warning($"Trying to unload {key} that was not loaded");
                return;
            }
            Resources.UnloadAsset(asset);
            this.logger.Debug($"Unloaded {key}");
        }

        void IAssetsManager.UnloadAll(string key)
        {
            if (!this.cacheMultiple.Remove(key, out var assets))
            {
                this.logger.Warning($"Trying to unload all {key} that was not loaded");
                return;
            }
            assets.ForEach(Resources.UnloadAsset);
            this.logger.Debug($"Unloaded {key}");
        }

        private void Dispose()
        {
            this.cacheSingle.Clear(Resources.UnloadAsset);
            this.cacheMultiple.Clear(assets => assets.ForEach(Resources.UnloadAsset));
        }

        void IDisposable.Dispose()
        {
            this.Dispose();
            this.logger.Debug("Disposed");
        }

        ~ResourceAssetsManager()
        {
            this.Dispose();
            this.logger.Debug("Finalized");
        }

        #endregion
    }
}