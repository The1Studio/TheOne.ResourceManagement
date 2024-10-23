﻿#if THEONE_ADDRESSABLES
#nullable enable
namespace TheOne.ResourceManagement
{
    using System;
    using TheOne.Extensions;
    using TheOne.Logging;
    using UnityEngine.AddressableAssets;
    using UnityEngine.SceneManagement;
    using UnityEngine.Scripting;
    #if THEONE_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public sealed class AddressableScenesManager : IScenesManager
    {
        #region Constructor

        private readonly ILogger logger;

        [Preserve]
        public AddressableScenesManager(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        void IScenesManager.LoadScene(string sceneName, LoadSceneMode loadMode)
        {
            Addressables.LoadSceneAsync(sceneName, loadMode).WaitForCompletion();
            this.logger.Debug($"Loaded {sceneName}");
        }

        #if THEONE_UNITASK
        UniTask IScenesManager.LoadSceneAsync(string sceneName, LoadSceneMode loadMode, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return Addressables.LoadSceneAsync(sceneName, loadMode)
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(_ => this.logger.Debug($"Loaded {sceneName}"));
        }
        #else
        IEnumerator IScenesManager.LoadSceneAsync(string sceneName, LoadSceneMode loadMode, Action? callback, IProgress<float>? progress)
        {
            return Addressables.LoadSceneAsync(sceneName, loadMode).ToCoroutine(_ =>
            {
                this.logger.Debug($"Loaded {sceneName}");
                callback?.Invoke();
            }, progress);
        }
        #endif
    }
}
#endif