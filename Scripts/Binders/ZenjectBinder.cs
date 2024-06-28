#if UNIT_ZENJECT
#nullable enable
namespace UniT.ResourceManagement
{
    using UniT.Logging;
    using Zenject;

    public static class ZenjectBinder
    {
        public static void BindResourceManagers(this DiContainer container)
        {
            if (container.HasBinding<IAssetsManager>()) return;
            container.BindLoggerManager();
            #if UNIT_ADDRESSABLES
            container.BindInterfacesTo<AddressableAssetsManager>().AsSingle();
            container.BindInterfacesTo<AddressableScenesManager>().AsSingle();
            #else
            container.BindInterfacesTo<ResourceAssetsManager>().AsSingle();
            container.BindInterfacesTo<ResourceScenesManager>().AsSingle();
            #endif
            container.BindInterfacesTo<ExternalAssetsManager>().AsSingle();
        }
    }
}
#endif