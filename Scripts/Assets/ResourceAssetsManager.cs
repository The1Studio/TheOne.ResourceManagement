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
            return Resources.Load<T>(this.GetScopedKey(key)) ?? throw new ArgumentOutOfRangeException($"{key} not found in resources");
        }

        protected override IEnumerable<T> LoadAll<T>(string key)
        {
            return Resources.LoadAll<T>(this.GetScopedKey(key));
        }

        protected override void Unload(Object asset)
        {
            Resources.UnloadAsset(asset);
        }

        #if THEONE_UNITASK
        protected override UniTask<T> LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return Resources.LoadAsync<T>(this.GetScopedKey(key))
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(asset => (T)asset ?? throw new ArgumentOutOfRangeException($"{key} not found in resources"));
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
            callback((T)operation.asset ?? throw new ArgumentOutOfRangeException($"{key} not found in resources"));
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