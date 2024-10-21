#nullable enable
namespace UniT.ResourceManagement
{
    using System;
    using UniT.Extensions;
    using UniT.Logging;
    using UnityEngine.SceneManagement;
    using UnityEngine.Scripting;
    #if UNIT_UNITASK
    using System.Threading;
    using Cysharp.Threading.Tasks;
    #else
    using System.Collections;
    #endif

    public sealed class ResourceScenesManager : IScenesManager
    {
        #region Constructor

        private readonly ILogger logger;

        [Preserve]
        public ResourceScenesManager(ILoggerManager loggerManager)
        {
            this.logger = loggerManager.GetLogger(this);
            this.logger.Debug("Constructed");
        }

        #endregion

        void IScenesManager.LoadScene(string sceneName, LoadSceneMode loadMode)
        {
            SceneManager.LoadScene(sceneName, loadMode);
            this.logger.Debug($"Loaded {sceneName}");
        }

        #if UNIT_UNITASK
        UniTask IScenesManager.LoadSceneAsync(string sceneName, LoadSceneMode loadMode, IProgress<float>? progress, CancellationToken cancellationToken)
        {
            return SceneManager.LoadSceneAsync(sceneName, loadMode)
                .ToUniTask(progress: progress, cancellationToken: cancellationToken)
                .ContinueWith(() => this.logger.Debug($"Loaded {sceneName}"));
        }
        #else
        IEnumerator IScenesManager.LoadSceneAsync(string sceneName, LoadSceneMode loadMode, Action? callback, IProgress<float>? progress)
        {
            return SceneManager.LoadSceneAsync(sceneName, loadMode)!.ToCoroutine(() =>
            {
                this.logger.Debug($"Loaded {sceneName}");
                callback?.Invoke();
            }, progress: progress);
        }
        #endif
    }
}