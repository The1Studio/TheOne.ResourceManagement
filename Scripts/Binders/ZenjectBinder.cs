#if UNIT_ZENJECT
#nullable enable
namespace UniT.ResourceManagement
{
    using UniT.Logging;
    using Zenject;

    public static class ZenjectBinder
    {
        public static void BindAssetsManager(this DiContainer container, string? scope = null)
        {
            if (container.HasBinding<IAssetsManager>()) return;
            container.BindLoggerManager();
            #if UNIT_ADDRESSABLES
            container.BindInterfacesTo<AddressableAssetsManager>().AsSingle().WithArguments(scope);
            #else
            container.BindInterfacesTo<ResourceAssetsManager>().AsSingle().WithArguments(scope);
            #endif
        }

        public static void BindScenesManager(this DiContainer container)
        {
            if (container.HasBinding<IScenesManager>()) return;
            container.BindLoggerManager();
            #if UNIT_ADDRESSABLES
            container.BindInterfacesTo<AddressableScenesManager>().AsSingle();
            #else
            container.BindInterfacesTo<ResourceScenesManager>().AsSingle();
            #endif
        }

        public static void BindExternalAssetsManager(this DiContainer container)
        {
            if (container.HasBinding<IExternalAssetsManager>()) return;
            container.BindLoggerManager();
            container.BindInterfacesTo<ExternalAssetsManager>().AsSingle();
        }
    }
}
#endif