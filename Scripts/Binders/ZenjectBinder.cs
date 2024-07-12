#if UNIT_ZENJECT
#nullable enable
namespace UniT.ResourceManagement
{
    using UniT.Logging;
    using Zenject;

    public static class ZenjectBinder
    {
        public static void BindResourceManagers(this DiContainer container, string? scope = null)
        {
            if (container.HasBinding<IAssetsManager>()) return;
            container.BindLoggerManager();
            #if UNIT_ADDRESSABLES
            container.BindInterfacesTo<AddressableAssetsManager>().AsSingle().WithArguments(scope);
            container.BindInterfacesTo<AddressableScenesManager>().AsSingle();
            #else
            container.BindInterfacesTo<ResourceAssetsManager>().AsSingle().WithArguments(scope);
            container.BindInterfacesTo<ResourceScenesManager>().AsSingle();
            #endif
            container.BindInterfacesTo<ExternalAssetsManager>().AsSingle();
        }
    }
}
#endif