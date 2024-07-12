#if UNIT_DI
#nullable enable
namespace UniT.ResourceManagement
{
    using UniT.DI;
    using UniT.Logging;

    public static class DIBinder
    {
        public static void AddAssetsManager(this DependencyContainer container, string? scope = null)
        {
            if (container.Contains<IAssetsManager>()) return;
            container.AddLoggerManager();
            var loggerManager = container.Get<ILoggerManager>();
            #if UNIT_ADDRESSABLES
            container.AddInterfaces(new AddressableAssetsManager(loggerManager, scope));
            #else
            container.AddInterfaces(new ResourceAssetsManager(loggerManager, scope));
            #endif
        }

        public static void AddScenesManager(this DependencyContainer container)
        {
            if (container.Contains<IScenesManager>()) return;
            container.AddLoggerManager();
            #if UNIT_ADDRESSABLES
            container.AddInterfaces<AddressableScenesManager>();
            #else
            container.AddInterfaces<ResourceScenesManager>();
            #endif
        }

        public static void AddExternalAssetsManager(this DependencyContainer container)
        {
            if (container.Contains<IExternalAssetsManager>()) return;
            container.AddLoggerManager();
            container.AddInterfaces<ExternalAssetsManager>();
        }
    }
}
#endif