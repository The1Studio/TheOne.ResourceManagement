#nullable enable
namespace TheOne.ResourceManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using TheOne.Extensions;
    using TheOne.Logging;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.Scripting;
    using ILogger = TheOne.Logging.ILogger;
    #if THEONE_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public sealed class ExternalAssetsManager : IExternalAssetsManager
    {
        #region Constructor

        private readonly ILogger logger;

        private readonly Dictionary<string, object> cache = new Dictionary<string, object>();

        [Preserve]
        public ExternalAssetsManager(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        #region Public

        #if THEONE_UNITASK
        async UniTask<string> IExternalAssetsManager.DownloadTextAsync(string url, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!cache) return (string)await DownloadTextAsync();
            return (string)await this.cache.GetOrAddAsync(url, DownloadTextAsync);

            async UniTask<object> DownloadTextAsync()
            {
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerBuffer();
                request.downloadHandler = downloadHandler;
                await this.DownloadAsync(request, progress, cancellationToken);
                return downloadHandler.text;
            }
        }

        async UniTask<byte[]> IExternalAssetsManager.DownloadBufferAsync(string url, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!cache) return (byte[])await DownloadBufferAsync();
            return (byte[])await this.cache.GetOrAddAsync(url, DownloadBufferAsync);

            async UniTask<object> DownloadBufferAsync()
            {
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerBuffer();
                request.downloadHandler = downloadHandler;
                await this.DownloadAsync(request, progress, cancellationToken);
                return downloadHandler.data;
            }
        }

        async UniTask<Texture2D> IExternalAssetsManager.DownloadTextureAsync(string url, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!cache) return (Texture2D)await DownloadTextureAsync();
            return (Texture2D)await this.cache.GetOrAddAsync(url, DownloadTextureAsync);

            async UniTask<object> DownloadTextureAsync()
            {
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerTexture();
                request.downloadHandler = downloadHandler;
                await this.DownloadAsync(request, progress, cancellationToken);
                return downloadHandler.texture;
            }
        }

        async UniTask<Sprite> IExternalAssetsManager.DownloadSpriteAsync(string url, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!cache) return (Sprite)await DownloadSpriteAsync();
            return (Sprite)await this.cache.GetOrAddAsync(url, DownloadSpriteAsync);

            async UniTask<object> DownloadSpriteAsync()
            {
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerTexture();
                request.downloadHandler = downloadHandler;
                await this.DownloadAsync(request, progress, cancellationToken);
                return downloadHandler.texture.CreateSprite();
            }
        }

        async UniTask IExternalAssetsManager.DownloadFileAsync(string url, string savePath, bool cache, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            if (!cache || !File.Exists(savePath))
            {
                this.logger.Debug($"Saving {url} to {savePath}");
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerFile(savePath);
                request.downloadHandler = downloadHandler;
                await this.DownloadAsync(request, progress, cancellationToken);
            }
        }

        private async UniTask DownloadAsync(UnityWebRequest request, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            this.logger.Debug($"Downloading {request.url}");
            await request.SendWebRequest().ToUniTask(progress: progress, cancellationToken: cancellationToken);
            this.logger.Debug($"Downloaded {request.url}");
        }
        #else
        IEnumerator IExternalAssetsManager.DownloadTextAsync(string url, Action<string> callback, bool cache, IProgress<float>? progress)
        {
            if (!cache) return DownloadTextAsync(callback);
            return this.cache.GetOrAddAsync(
                url,
                DownloadTextAsync,
                value => callback((string)value)
            );

            IEnumerator DownloadTextAsync(Action<string> callback)
            {
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerBuffer();
                request.downloadHandler = downloadHandler;
                yield return this.DownloadAsync(request, progress);
                callback(downloadHandler.text);
            }
        }

        IEnumerator IExternalAssetsManager.DownloadBufferAsync(string url, Action<byte[]> callback, bool cache, IProgress<float>? progress)
        {
            if (!cache) return DownloadBufferAsync(callback);
            return this.cache.GetOrAddAsync(
                url,
                DownloadBufferAsync,
                value => callback((byte[])value)
            );

            IEnumerator DownloadBufferAsync(Action<byte[]> callback)
            {
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerBuffer();
                request.downloadHandler = downloadHandler;
                yield return this.DownloadAsync(request, progress);
                callback(downloadHandler.data);
            }
        }

        IEnumerator IExternalAssetsManager.DownloadTextureAsync(string url, Action<Texture2D> callback, bool cache, IProgress<float>? progress)
        {
            if (!cache) return DownloadTextureAsync(callback);
            return this.cache.GetOrAddAsync(
                url,
                DownloadTextureAsync,
                value => callback((Texture2D)value)
            );

            IEnumerator DownloadTextureAsync(Action<Texture2D> callback)
            {
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerTexture();
                request.downloadHandler = downloadHandler;
                yield return this.DownloadAsync(request, progress);
                callback(downloadHandler.texture);
            }
        }

        IEnumerator IExternalAssetsManager.DownloadSpriteAsync(string url, Action<Sprite> callback, bool cache, IProgress<float>? progress)
        {
            if (!cache) return DownloadSpriteAsync(callback);
            return this.cache.GetOrAddAsync(
                url,
                DownloadSpriteAsync,
                value => callback((Sprite)value)
            );

            IEnumerator DownloadSpriteAsync(Action<Sprite> callback)
            {
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerTexture();
                request.downloadHandler = downloadHandler;
                yield return this.DownloadAsync(request, progress);
                callback(downloadHandler.texture.CreateSprite());
            }
        }

        IEnumerator IExternalAssetsManager.DownloadFileAsync(string url, string savePath, Action? callback, bool cache, IProgress<float>? progress)
        {
            if (!cache || !File.Exists(savePath))
            {
                this.logger.Debug($"Saving {url} to {savePath}");
                using var request         = new UnityWebRequest(url);
                using var downloadHandler = new DownloadHandlerFile(savePath);
                request.downloadHandler = downloadHandler;
                yield return this.DownloadAsync(request, progress);
            }
            callback?.Invoke();
        }

        private IEnumerator DownloadAsync(UnityWebRequest request, IProgress<float>? progress)
        {
            this.logger.Debug($"Downloading {request.url}");
            yield return request.SendWebRequest().ToCoroutine(progress: progress);
            this.logger.Debug($"Downloaded {request.url}");
        }
        #endif

        void IExternalAssetsManager.DeleteCache(string key)
        {
            if (this.cache.Remove(key))
            {
                this.logger.Debug($"Deleted {key}");
            }
            else if (File.Exists(key))
            {
                File.Delete(key);
                this.logger.Debug($"Deleted {key}");
            }
            else
            {
                this.logger.Warning($"Failed to delete {key}");
            }
        }

        #endregion
    }
}