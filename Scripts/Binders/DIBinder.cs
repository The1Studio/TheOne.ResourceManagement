#if UNIT_DI
#nullable enable
namespace UniT.ResourceManagement
{
    using UniT.DI;
    using UniT.Logging;

    public static class DIBinder
    {
        public static void AddResourceManagers(this DependencyContainer container)
        {
            if (container.Contains<IAssetsManager>()) return;
            container.AddLoggerManager();
            #if UNIT_ADDRESSABLES
            container.AddInterfaces<AddressableAssetsManager>();
            container.AddInterfaces<AddressableScenesManager>();
            #else
            container.AddInterfaces<ResourceAssetsManager>();
            container.AddInterfaces<ResourceScenesManager>();
            #endif
            container.AddInterfaces<ExternalAssetsManager>();
        }
    }
}
#endif