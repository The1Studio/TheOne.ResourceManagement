#if UNIT_DI
#nullable enable
namespace UniT.ResourceManagement
{
    using UniT.DI;
    using UniT.Logging;

    public static class DIBinder
    {
        public static void AddResourceManagers(this DependencyContainer container, string? scope = null)
        {
            if (container.Contains<IAssetsManager>()) return;
            container.AddLoggerManager();
            var loggerManager = container.Get<ILoggerManager>();
            #if UNIT_ADDRESSABLES
            container.AddInterfaces(new AddressableAssetsManager(loggerManager, scope));
            container.AddInterfaces<AddressableScenesManager>();
            #else
            container.AddInterfaces(new ResourceAssetsManager(loggerManager, scope));
            container.AddInterfaces<ResourceScenesManager>();
            #endif
            container.AddInterfaces<ExternalAssetsManager>();
        }
    }
}
#endif