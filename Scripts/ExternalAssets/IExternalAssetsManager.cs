#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using UnityEngine;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public interface IExternalAssetsManager
    {
        #if UNIT_UNITASK
        public UniTask<string> DownloadTextAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<byte[]> DownloadBufferAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask<Texture2D> DownloadTextureAsync(string url, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask DownloadFileAsync(string url, string savePath, bool cache = true, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
        #else
        public IEnumerator DownloadTextAsync(string url, Action<string> callback, bool cache = true, IProgress<float>? progress = null);

        public IEnumerator DownloadBufferAsync(string url, Action<byte[]> callback, bool cache = true, IProgress<float>? progress = null);

        public IEnumerator DownloadTextureAsync(string url, Action<Texture2D> callback, bool cache = true, IProgress<float>? progress = null);

        public IEnumerator DownloadFileAsync(string url, string savePath, Action? callback = null, bool cache = true, IProgress<float>? progress = null);
        #endif

        public void DeleteCache(string key);
    }
}