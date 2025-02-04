#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using UniT.Extensions;
    using UnityEngine;
    using Object = UnityEngine.Object;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public interface IAssetsManager : IDisposable
    {
        #region Sync

        public T Load<T>(string key) where T : Object;

        public T[] LoadAll<T>(string key) where T : Object;

        public void Download(string key);

        public void DownloadAll();

        #region Default Implementation

        public bool TryLoad<T>(string key, [MaybeNullWhen(false)] out T asset) where T : Object
        {
            try
            {
                asset = this.Load<T>(key);
                return true;
            }
            catch
            {
                asset = null;
                return false;
            }
        }

        public T LoadComponent<T>(string key) => this.Load<GameObject>(key).GetComponentOrThrow<T>();

        public T[] LoadAllComponents<T>(string key) => GetAllComponents<T>(this.LoadAll<GameObject>(key));

        public bool TryLoadComponent<T>(string key, [MaybeNullWhen(false)] out T component)
        {
            component = default;
            return this.TryLoad<GameObject>(key, out var gameObject) && gameObject.TryGetComponent(out component);
        }

        #endregion

        #region Implicit Key

        public T Load<T>() where T : Object => this.Load<T>(typeof(T).GetKey());

        public T[] LoadAll<T>() where T : Object => this.LoadAll<T>(typeof(T).GetKey());

        public void Download<T>() => this.Download(typeof(T).GetKey());

        public bool TryLoad<T>([MaybeNullWhen(false)] out T asset) where T : Object => this.TryLoad(typeof(T).GetKey(), out asset);

        public T LoadComponent<T>() => this.LoadComponent<T>(typeof(T).GetKey());

        public T[] LoadAllComponents<T>() => this.LoadAllComponents<T>(typeof(T).GetKey());

        public bool TryLoadComponent<T>([MaybeNullWhen(false)] out T component) => this.TryLoadComponent(typeof(T).GetKey(), out component);

        #endregion

        #endregion

        #region Async

        #if UNIT_UNITASK
        public UniTask<T> LoadAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;

        public UniTask<T[]> LoadAllAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;

        public UniTask DownloadAsync(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask DownloadAllAsync(IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        #region Default Implementation

        public async UniTask<(bool IsSucceeded, T Asset)> TryLoadAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object
        {
            try
            {
                return (true, await this.LoadAsync<T>(key, progress, cancellationToken));
            }
            catch
            {
                return (false, null!);
            }
        }

        public UniTask<T> LoadComponentAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.LoadAsync<GameObject>(key, progress, cancellationToken).ContinueWith(gameObject => gameObject.GetComponentOrThrow<T>());

        public UniTask<T[]> LoadAllComponentsAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.LoadAllAsync<GameObject>(key, progress, cancellationToken).ContinueWith(GetAllComponents<T>);

        public UniTask<(bool IsSucceeded, T Component)> TryLoadComponentAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default)
        {
            return this.TryLoadAsync<GameObject>(key, progress, cancellationToken)
                .ContinueWith((isSucceeded, asset) =>
                {
                    var component = default(T)!;
                    return (isSucceeded && asset.TryGetComponent<T>(out component), component);
                });
        }

        #endregion

        #region Implicit Key

        public UniTask<T> LoadAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.LoadAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask<T[]> LoadAllAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.LoadAllAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask DownloadAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.DownloadAsync(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask<(bool IsSucceeded, T Asset)> TryLoadAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object => this.TryLoadAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask<T> LoadComponentAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.LoadComponentAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask<T[]> LoadAllComponentsAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.LoadAllComponentsAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        public UniTask<(bool IsSucceeded, T Component)> TryLoadComponentAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.TryLoadComponentAsync<T>(typeof(T).GetKey(), progress, cancellationToken);

        #endregion

        #else
        public IEnumerator LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress = null) where T : Object;

        public IEnumerator LoadAllAsync<T>(string key, Action<T[]> callback, IProgress<float>? progress = null) where T : Object;

        public IEnumerator DownloadAsync(string key, Action? callback = null, IProgress<float>? progress = null);

        public IEnumerator DownloadAllAsync(Action? callback = null, IProgress<float>? progress = null);

        #region Default Implementation

        public IEnumerator TryLoadAsync<T>(string key, Action<(bool IsSucceeded, T Asset)> callback, IProgress<float>? progress = null) where T : Object
        {
            return this.LoadAsync<T>(
                key,
                asset => callback((true, asset)),
                progress
            ).Catch(() => callback((false, null!)));
        }

        public IEnumerator LoadComponentAsync<T>(string key, Action<T> callback, IProgress<float>? progress = null) => this.LoadAsync<GameObject>(key, gameObject => callback(gameObject.GetComponentOrThrow<T>()), progress);

        public IEnumerator LoadAllComponentsAsync<T>(string key, Action<T[]> callback, IProgress<float>? progress = null) => this.LoadAllAsync<GameObject>(key, gameObjects => callback(GetAllComponents<T>(gameObjects)), progress);

        public IEnumerator TryLoadComponentAsync<T>(string key, Action<(bool IsSucceeded, T Component)> callback, IProgress<float>? progress = null)
        {
            return this.TryLoadAsync<GameObject>(
                key,
                result =>
                {
                    var component = default(T)!;
                    callback((result.IsSucceeded && result.Asset.TryGetComponent<T>(out component), component));
                },
                progress
            );
        }

        #endregion

        #region Implicit Key

        public IEnumerator LoadAsync<T>(Action<T> callback, IProgress<float>? progress = null) where T : Object => this.LoadAsync(typeof(T).GetKey(), callback, progress);

        public IEnumerator LoadAllAsync<T>(Action<T[]> callback, IProgress<float>? progress = null) where T : Object => this.LoadAllAsync(typeof(T).GetKey(), callback, progress);

        public IEnumerator DownloadAsync<T>(Action? callback = null, IProgress<float>? progress = null) => this.DownloadAsync(typeof(T).GetKey(), callback, progress);

        public IEnumerator TryLoadAsync<T>(Action<(bool IsSucceeded, T Asset)> callback, IProgress<float>? progress = null) where T : Object => this.TryLoadAsync(typeof(T).GetKey(), callback, progress);

        public IEnumerator LoadComponentAsync<T>(Action<T> callback, IProgress<float>? progress = null) => this.LoadComponentAsync(typeof(T).GetKey(), callback, progress);

        public IEnumerator LoadAllComponentsAsync<T>(Action<T[]> callback, IProgress<float>? progress = null) => this.LoadAllComponentsAsync(typeof(T).GetKey(), callback, progress);

        public IEnumerator TryLoadComponentAsync<T>(Action<(bool IsSucceeded, T Component)> callback, IProgress<float>? progress = null) => this.TryLoadComponentAsync(typeof(T).GetKey(), callback, progress);

        #endregion

        #endif

        #endregion

        public void Unload(string key);

        public void Unload<T>() => this.Unload(typeof(T).GetKey());

        private static T[] GetAllComponents<T>(GameObject[] gameObjects) => gameObjects.Select(gameObject => gameObject.GetComponent<T>()).OfType<T>().ToArray();
    }
}