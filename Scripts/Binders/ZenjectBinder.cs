#if UNIT_ZENJECT
#nullable enable
namespace UniT.ResourceManagement
{
    using Zenject;

    public static class ZenjectBinder
    {
        public static void BindResourceManagers(this DiContainer container)
        {
            #if UNIT_ADDRESSABLES
            container.BindInterfacesTo<AddressableScenesManager>().AsSingle();
            container.BindInterfacesTo<AddressableAssetsManager>().AsSingle();
            #else
            container.BindInterfacesTo<ResourceScenesManager>().AsSingle();
            container.BindInterfacesTo<ResourceAssetsManager>().AsSingle();
            #endif
            container.BindInterfacesTo<ExternalAssetsManager>().AsSingle();
        }
    }
}
#endif