#nullable enable
namespace TheOne.ResourceManagement
{
    using System;
    using UnityEngine.SceneManagement;
    #if THEONE_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public interface IScenesManager
    {
        public void Load(string name, LoadSceneMode mode = LoadSceneMode.Single);

        #if THEONE_UNITASK
        public UniTask LoadAsync(string name, LoadSceneMode mode = LoadSceneMode.Single, IProgress<float>? progress = null, CancellationToken cancellationToken = default);

        public UniTask UnloadAsync(string name, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
        #else
        public IEnumerator LoadAsync(string name, LoadSceneMode mode = LoadSceneMode.Single, Action? callback = null, IProgress<float>? progress = null);

        public IEnumerator UnloadAsync(string name, Action? callback = null, IProgress<float>? progress = null);
        #endif
    }
}