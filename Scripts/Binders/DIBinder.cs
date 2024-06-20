#if UNIT_DI
#nullable enable
namespace UniT.ResourceManagement
{
    using UniT.DI;

    public static class DIBinder
    {
        public static void AddResourceManagers(this DependencyContainer container)
        {
            #if UNIT_ADDRESSABLES
            container.AddInterfaces<AddressableScenesManager>();
            container.AddInterfaces<AddressableAssetsManager>();
            #else
            container.AddInterfaces<ResourceScenesManager>();
            container.AddInterfaces<ResourceAssetsManager>();
            #endif
            container.AddInterfaces<ExternalAssetsManager>();
        }
    }
}
#endif