#if UNIT_ADDRESSABLES
#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Linq;
    using UniT.Extensions;
    using UniT.Logging;
    using UnityEngine.AddressableAssets;
    using UnityEngine.Scripting;
    using Object = UnityEngine.Object;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    using UnityEngine.AddressableAssets.ResourceLocators;
    #endif

    public sealed class AddressableAssetsManager : AssetsManager
    {
        private readonly string? scope;

        [Preserve]
        public AddressableAssetsManager(ILoggerManager loggerManager, string? scope = null) : base(loggerManager)
        {
            this.scope = scope.NullIfWhitespace();
        }

        private string GetScopedKey(string key) => this.scope is null ? key : $"{this.scope}/{key}";

        protected override void Initialize()
        {
            var resourceLocator = Addressables.InitializeAsync().WaitForCompletion();
            Addressables.DownloadDependenciesAsync(resourceLocator.AllLocations.ToArray()).WaitForCompletion();
        }

        protected override T? Load<T>(string key) where T : class
        {
            return Addressables.LoadAssetAsync<T>(this.GetScopedKey(key)).WaitForCompletion();
        }

        protected override T[] LoadAll<T>(string key)
        {
            return Addressables.LoadAssetsAsync<T>(this.GetScopedKey(key)).WaitForCompletion().ToArray();
        }

        protected override void Unload(Object asset)
        {
            Addressables.Release(asset);
        }

        #if UNIT_UNITASK
        protected override async UniTask InitializeAsync(IProgress<float>? progress, CancellationToken cancellationToken)
        {
            var subProgresses   = progress.CreateSubProgresses(2).ToArray();
            var resourceLocator = await Addressables.InitializeAsync().ToUniTask(progress: subProgresses[0], cancellationToken: cancellationToken);
            await Addressables.DownloadDependenciesAsync(resourceLocator.AllLocations.ToArray()).ToUniTask(progress: subProgresses[1], cancellationToken: cancellationToken);
        }

        protected override UniTask<T?> LoadAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken) where T : class
        {
            return Addressables.LoadAssetAsync<T>(this.GetScopedKey(key))
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(asset => (T?)asset);
        }

        protected override UniTask<T[]> LoadAllAsync<T>(string key, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return Addressables.LoadAssetsAsync<T>(this.GetScopedKey(key))
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(assets => assets as T[] ?? assets.ToArray());
        }
        #else
        protected override IEnumerator InitializeAsync(Action? callback, IProgress<float>? progress)
        {
            var subProgresses   = progress.CreateSubProgresses(2).ToArray();
            var resourceLocator = default(IResourceLocator)!;
            yield return Addressables.InitializeAsync().ToCoroutine(result => resourceLocator = result, subProgresses[0]);
            yield return Addressables.DownloadDependenciesAsync(resourceLocator.AllLocations.ToArray()).ToCoroutine(progress: subProgresses[1]);
        }

        protected override IEnumerator LoadAsync<T>(string key, Action<T?> callback, IProgress<float>? progress) where T : class
        {
            return Addressables.LoadAssetAsync<T>(this.GetScopedKey(key)).ToCoroutine(callback, progress);
        }

        protected override IEnumerator LoadAllAsync<T>(string key, Action<T[]> callback, IProgress<float>? progress)
        {
            return Addressables.LoadAssetsAsync<T>(this.GetScopedKey(key)).ToCoroutine(assets => callback(assets as T[] ?? assets.ToArray()), progress);
        }
        #endif
    }
}
#endif