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
    using UnityEngine.Scripting;
    using Object = UnityEngine.Object;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public sealed class AddressableAssetsManager : AssetsManager
    {
        private readonly string? scope;

        [Preserve]
        public AddressableAssetsManager(ILoggerManager loggerManager, string? scope = null) : base(loggerManager)
        {
            this.scope = scope.NullIfWhitespace();
        }

        #region Sync

        protected override T? Load<T>(string key) where T : class
        {
            return this.LoadInternal<T>(key).WaitForCompletion();
        }

        protected override T[] LoadAll<T>(string key)
        {
            return this.LoadAllInternal<T>(key).WaitForCompletion().ToArray();
        }

        protected override void Download(string key)
        {
            this.DownloadInternal(key).WaitForCompletion();
        }

        protected override void DownloadAll()
        {
            InitializeInternal().WaitForCompletion();
            DownloadAllInternal().WaitForCompletion();
        }

        protected override void Unload(Object asset)
        {
            Addressables.Release(asset);
        }

        #endregion

        #region Async

        #if UNIT_UNITASK
        protected override UniTask<T?> LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken) where T : class
        {
            return this.LoadInternal<T>(key)
                .ToUniTask(progress: progress, cancellationToken: cancellationToken, autoReleaseWhenCanceled: true)
                .ContinueWith(asset => (T?)asset);
        }

        protected override UniTask<T[]> LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return this.LoadAllInternal<T>(key)
                .ToUniTask(progress: progress, cancellationToken: cancellationToken, autoReleaseWhenCanceled: true)
                .ContinueWith(assets => assets.ToArray());
        }

        protected override UniTask DownloadAsync(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return this.DownloadInternal(key).ToUniTask(progress: progress, cancellationToken: cancellationToken, autoReleaseWhenCanceled: true);
        }

        protected override async UniTask DownloadAllAsync(IProgress<float>? progress, CancellationToken cancellationToken)
        {
            await InitializeInternal().ToUniTask(progress: progress, cancellationToken: cancellationToken, autoReleaseWhenCanceled: true);
            await DownloadAllInternal().ToUniTask(progress: progress, cancellationToken: cancellationToken, autoReleaseWhenCanceled: true);
        }
        #else
        protected override IEnumerator LoadAsync<T>(string key, Action<T?> callback, IProgress<float>? progress) where T : class
        {
            return this.LoadInternal<T>(key).ToCoroutine(callback, progress);
        }

        protected override IEnumerator LoadAllAsync<T>(string key, Action<T[]> callback, IProgress<float>? progress)
        {
            return this.LoadAllInternal<T>(key).ToCoroutine(assets => callback(assets.ToArray()), progress);
        }

        protected override IEnumerator DownloadAsync(string key, Action? callback, IProgress<float>? progress)
        {
            return this.DownloadInternal(key).ToCoroutine(callback, progress);
        }

        protected override IEnumerator DownloadAllAsync(Action? callback, IProgress<float>? progress)
        {
            yield return InitializeInternal().ToCoroutine(progress: progress);
            yield return DownloadAllInternal().ToCoroutine(progress: progress);
            callback?.Invoke();
        }
        #endif

        #endregion

        #region Internal

        private string GetScopedKey(string key) => this.scope is null ? key : $"{this.scope}/{key}";

        private AsyncOperationHandle<T> LoadInternal<T>(string key)
        {
            return Addressables.LoadAssetAsync<T>(this.GetScopedKey(key));
        }

        private AsyncOperationHandle<IList<T>> LoadAllInternal<T>(string key)
        {
            return Addressables.LoadAssetsAsync<T>(this.GetScopedKey(key));
        }

        private AsyncOperationHandle DownloadInternal(string key)
        {
            return Addressables.DownloadDependenciesAsync(this.GetScopedKey(key), true);
        }

        private static AsyncOperationHandle DownloadAllInternal()
        {
            return Addressables.DownloadDependenciesAsync(Addressables.ResourceLocators.SelectMany(locator => locator.Keys), true);
        }

        private static AsyncOperationHandle InitializeInternal()
        {
            return Addressables.InitializeAsync();
        }

        #endregion
    }
}
#endif