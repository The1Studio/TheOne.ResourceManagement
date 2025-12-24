#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using UniT.Extensions;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public interface IRemoteAssetsDownloader
    {
        #if UNIT_UNITASK
        public UniTask DownloadAsync(object key, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask DownloadAllAsync(IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask DownloadAsync<T>(IProgress<float>? progress = null, CancellationToken cancellationToken = default) => this.DownloadAsync(typeof(T).GetKey(), progress, cancellationToken);
        #else
        public IEnumerator DownloadAsync(object key, Action? callback = null, IProgress<float>? progress = null);

        public IEnumerator DownloadAllAsync(Action? callback = null, IProgress<float>? progress = null);

        public IEnumerator DownloadAsync<T>(Action? callback = null, IProgress<float>? progress = null) => this.DownloadAsync(typeof(T).GetKey(), callback, progress);
        #endif
    }
}