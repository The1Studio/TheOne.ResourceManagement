#nullable enable
namespace TheOne.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using TheOne.Extensions;
    using TheOne.Logging;
    using UnityEngine;
    using UnityEngine.Scripting;
    using ILogger = TheOne.Logging.ILogger;
    using Object = UnityEngine.Object;
    #if THEONE_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public sealed class ResourceAssetsManager : AssetsManager
    {
        private readonly string? scope;
        private readonly ILogger logger;

        [Preserve]
        public ResourceAssetsManager(ILoggerManager loggerManager, string? scope = null) : base(loggerManager)
        {
            this.scope  = scope.NullIfWhiteSpace();
            this.logger = loggerManager.GetLogger(this);
        }

        private string GetScopedKey(string key) => this.scope is null ? key : $"{this.scope}/{key}";

        protected override T Load<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Asset key cannot be null or empty", nameof(key));
            
            var scopedKey = this.GetScopedKey(key);
            var asset = Resources.Load<T>(scopedKey);
            
            if (asset == null)
                throw new InvalidOperationException($"Asset '{key}' not found in Resources at path '{scopedKey}'");
            
            return asset;
        }

        protected override IEnumerable<T> LoadAll<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Asset key cannot be null or empty", nameof(key));
            
            return Resources.LoadAll<T>(this.GetScopedKey(key)) ?? Enumerable.Empty<T>();
        }

        protected override void Unload(Object asset)
        {
            if (asset != null)
                Resources.UnloadAsset(asset);
        }

        #if THEONE_UNITASK
        protected override UniTask<T> LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return Resources.LoadAsync<T>(this.GetScopedKey(key))
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(asset => 
                {
                    if (asset == null)
                        throw new InvalidOperationException($"Asset '{key}' not found in Resources at path '{this.GetScopedKey(key)}'");
                    return (T)asset;
                });
        }

        protected override UniTask<IEnumerable<T>> LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            this.logger.Warning("Unity does not support loading all from resources asynchronously");
            return UniTask.FromResult(this.LoadAll<T>(key));
        }
        #else
        protected override IEnumerator LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress)
        {
            var operation = Resources.LoadAsync<T>(this.GetScopedKey(key));
            yield return operation.ToCoroutine(progress: progress);
            if (operation.asset == null)
                throw new InvalidOperationException($"Asset '{key}' not found in Resources at path '{this.GetScopedKey(key)}'");
            callback((T)operation.asset);
        }

        protected override IEnumerator LoadAllAsync<T>(string key, Action<IEnumerable<T>> callback, IProgress<float>? progress)
        {
            this.logger.Warning("Unity does not support loading all from resources asynchronously");
            var assets = this.LoadAll<T>(key);
            callback(assets);
            yield break;
        }
        #endif
    }
}