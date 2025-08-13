#if THEONE_ADDRESSABLES
#nullable enable
namespace TheOne.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TheOne.Extensions;
    using TheOne.Logging;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;
    using UnityEngine.Scripting;
    using Object = UnityEngine.Object;
    #if THEONE_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public sealed class AddressableAssetsManager : AssetsManager
    {
        private readonly string? scope;
        private readonly ILogger logger;

        [Preserve]
        public AddressableAssetsManager(ILoggerManager loggerManager, string? scope = null) : base(loggerManager)
        {
            this.scope = scope.NullIfWhiteSpace();
            this.logger = loggerManager.GetLogger(this);
        }

        #region Sync

        protected override T Load<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Asset key cannot be null or empty", nameof(key));
            
            var handle = this.LoadInternal<T>(key);
            var result = handle.WaitForResultOrThrow();
            
            if (result == null)
                throw new InvalidOperationException($"Asset '{key}' loaded but was null");
            
            return result;
        }

        protected override IEnumerable<T> LoadAll<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Asset key cannot be null or empty", nameof(key));
            
            try
            {
                var result = this.LoadAllInternal<T>(key).WaitForResultOrThrow();
                return result ?? Enumerable.Empty<T>();
            }
            catch (Exception ex)
            {
                this.logger.Warning($"Failed to load all assets with key '{key}': {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }

        protected override void Download(string key)
        {
            this.DownloadInternal(key).WaitForResultOrThrow();
        }

        protected override void DownloadAll()
        {
            InitializeInternal().WaitForResultOrThrow();
            DownloadAllInternal().WaitForResultOrThrow();
        }

        protected override void Unload(Object asset)
        {
            if (asset != null)
            {
                try
                {
                    Addressables.Release(asset);
                }
                catch (Exception ex)
                {
                    this.logger.Warning($"Failed to unload asset: {ex.Message}");
                }
            }
        }

        #endregion

        #region Async

        #if THEONE_UNITASK
        protected override UniTask<T> LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return this.LoadInternal<T>(key).ToUniTask(progress, cancellationToken);
        }

        protected override async UniTask<IEnumerable<T>> LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Asset key cannot be null or empty", nameof(key));
            
            try
            {
                var result = await this.LoadAllInternal<T>(key).ToUniTask(progress, cancellationToken);
                return result ?? Enumerable.Empty<T>();
            }
            catch (Exception ex)
            {
                this.logger.Warning($"Failed to load all assets with key '{key}': {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }

        protected override UniTask DownloadAsync(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return this.DownloadInternal(key).ToUniTask(progress, cancellationToken);
        }

        protected override async UniTask DownloadAllAsync(IProgress<float>? progress, CancellationToken cancellationToken)
        {
            var subProgresses = progress.CreateSubProgresses(2).ToArray();
            await InitializeInternal().ToUniTask(subProgresses[0], cancellationToken);
            await DownloadAllInternal().ToUniTask(subProgresses[1], cancellationToken);
        }
        #else
        protected override IEnumerator LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress)
        {
            return this.LoadInternal<T>(key).ToCoroutine(callback, progress);
        }

        protected override IEnumerator LoadAllAsync<T>(string key, Action<IEnumerable<T>> callback, IProgress<float>? progress)
        {
            return this.LoadAllInternal<T>(key).ToCoroutine(callback, progress)
                .Catch(() => callback(Enumerable.Empty<T>()));
        }

        protected override IEnumerator DownloadAsync(string key, Action? callback, IProgress<float>? progress)
        {
            return this.DownloadInternal(key).ToCoroutine(callback, progress);
        }

        protected override IEnumerator DownloadAllAsync(Action? callback, IProgress<float>? progress)
        {
            var subProgresses = progress.CreateSubProgresses(2).ToArray();
            yield return InitializeInternal().ToCoroutine(progress: subProgresses[0]);
            yield return DownloadAllInternal().ToCoroutine(progress: subProgresses[1]);
            callback?.Invoke();
        }
        #endif

        #endregion

        #region Internal

        private string GetScopedKey(string key) => this.scope is null ? key : $"{this.scope}/{key}";

        private AsyncOperationHandle<T> LoadInternal<T>(string key)
        {
            var scopedKey = this.GetScopedKey(key);
            return Addressables.LoadAssetAsync<T>(scopedKey);
        }

        private AsyncOperationHandle<IList<T>> LoadAllInternal<T>(string key)
        {
            return Addressables.LoadAssetsAsync<T>(this.GetScopedKey(key));
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

        #endregion
    }
}
#endif