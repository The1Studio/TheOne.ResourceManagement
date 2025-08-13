# TheOne.ResourceManagement

Resource Manager for Unity

## Installation

### Option 1: Unity Scoped Registry (Recommended)

Add the following scoped registry to your project's `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "TheOne Studio",
      "url": "https://upm.the1studio.org/",
      "scopes": [
        "com.theone"
      ]
    }
  ],
  "dependencies": {
    "com.theone.resourcemanagement": "1.1.0"
  }
}
```

### Option 2: Git URL

Add to Unity Package Manager:
```
https://github.com/The1Studio/TheOne.ResourceManagement.git
```

## Features

- Unified resource management system with pluggable backends
- Support for both Addressables and Resources folder loading
- Automatic asset caching with memory management
- Scene loading and management (Addressables/Unity SceneManager)
- External asset downloading from web URLs
- Progress reporting for all async operations
- Type-safe asset loading with component support
- Implicit key loading using type names
- Scoped resource management for namespacing
- Integration with dependency injection frameworks
- Comprehensive logging and error handling
- Both sync and async loading APIs
- Memory-efficient resource disposal

## Dependencies

- TheOne.Extensions
- TheOne.Logging

## Usage

### Basic Asset Loading

```csharp
using TheOne.ResourceManagement;
using UnityEngine;

public class AssetLoader : MonoBehaviour
{
    private IAssetsManager assetsManager;
    
    #if THEONE_UNITASK
    private async UniTaskVoid Start()
    {
        // Load single asset
        var playerPrefab = assetsManager.Load<GameObject>("Characters/Player");
        var backgroundMusic = await assetsManager.LoadAsync<AudioClip>("Music/MainTheme");
        
        // Load multiple assets with same key
        var enemyVariants = assetsManager.LoadAll<GameObject>("Enemies/Goblin");
        var levelTextures = await assetsManager.LoadAllAsync<Texture2D>("Textures/Level1");
        
        // Component-based loading (automatically gets component from GameObject)
        var playerController = assetsManager.LoadComponent<PlayerController>("Characters/Player");
        var enemyControllers = assetsManager.LoadAllComponents<EnemyController>("Enemies");
    }
    #else
    private void Start()
    {
        StartCoroutine(LoadAssetsAsync());
    }
    
    private IEnumerator LoadAssetsAsync()
    {
        // Load single asset
        var playerPrefab = assetsManager.Load<GameObject>("Characters/Player");
        
        // Load background music asynchronously
        yield return assetsManager.LoadAsync<AudioClip>("Music/MainTheme", 
            callback: music => Debug.Log("Background music loaded"));
        
        // Load multiple assets with same key
        var enemyVariants = assetsManager.LoadAll<GameObject>("Enemies/Goblin");
        
        // Load level textures asynchronously
        yield return assetsManager.LoadAllAsync<Texture2D>("Textures/Level1",
            callback: textures => Debug.Log($"Loaded {textures.Count()} textures"));
        
        // Component-based loading (automatically gets component from GameObject)
        var playerController = assetsManager.LoadComponent<PlayerController>("Characters/Player");
        var enemyControllers = assetsManager.LoadAllComponents<EnemyController>("Enemies");
    }
    #endif
}
```

### Type-Based Implicit Loading

```csharp
public class TypeBasedLoading : MonoBehaviour
{
    private IAssetsManager assetsManager;
    
    #if THEONE_UNITASK
    private async UniTaskVoid Start()
    {
        // Uses type name as key automatically
        var player = assetsManager.Load<Player>();              // Loads "Player"
        var enemies = await assetsManager.LoadAllAsync<Enemy>(); // Loads "Enemy" 
        
        // Safe loading with error handling
        if (assetsManager.TryLoad<PowerUp>(out var powerUp))
        {
            InstantiatePowerUp(powerUp);
        }
        
        // Async safe loading
        var (success, weapon) = await assetsManager.TryLoadAsync<Weapon>();
        if (success)
        {
            EquipWeapon(weapon);
        }
    }
    #else
    private void Start()
    {
        StartCoroutine(LoadTypedAssetsAsync());
    }
    
    private IEnumerator LoadTypedAssetsAsync()
    {
        // Uses type name as key automatically
        var player = assetsManager.Load<Player>();              // Loads "Player"
        
        // Load enemies asynchronously
        yield return assetsManager.LoadAllAsync<Enemy>(
            callback: enemies => Debug.Log($"Loaded {enemies.Count()} enemies"));
        
        // Safe loading with error handling
        if (assetsManager.TryLoad<PowerUp>(out var powerUp))
        {
            InstantiatePowerUp(powerUp);
        }
        
        // Async safe loading
        yield return assetsManager.TryLoadAsync<Weapon>(
            callback: result =>
            {
                if (result.IsSucceeded)
                {
                    EquipWeapon(result.Asset);
                }
            });
    }
    #endif
}
```

### Scene Management

```csharp
public class SceneLoader : MonoBehaviour
{
    private IScenesManager scenesManager;
    
    #if THEONE_UNITASK
    public async UniTask LoadGameSceneAsync()
    {
        // Synchronous scene loading
        scenesManager.LoadScene("GameLevel");
        
        // Async loading with progress
        var progress = new Progress<float>(value => 
            Debug.Log($"Loading scene: {value:P}"));
            
        await scenesManager.LoadSceneAsync("GameLevel", 
            LoadSceneMode.Single, progress);
    }
    
    public async UniTask LoadAdditiveSceneAsync()
    {
        // Load scene additively (keeps current scene)
        await scenesManager.LoadSceneAsync("UIOverlay", 
            LoadSceneMode.Additive);
    }
    #else
    public void LoadGameScene()
    {
        StartCoroutine(LoadGameSceneCoroutine());
    }
    
    public void LoadAdditiveScene()
    {
        StartCoroutine(LoadAdditiveSceneCoroutine());
    }
    
    private IEnumerator LoadGameSceneCoroutine()
    {
        // Synchronous scene loading
        scenesManager.LoadScene("GameLevel");
        
        // Async loading with progress
        var progress = new Progress<float>(value => 
            Debug.Log($"Loading scene: {value:P}"));
            
        yield return scenesManager.LoadSceneAsync("GameLevel", 
            LoadSceneMode.Single, callback: () => Debug.Log("Scene loaded"), progress);
    }
    
    private IEnumerator LoadAdditiveSceneCoroutine()
    {
        // Load scene additively (keeps current scene)
        yield return scenesManager.LoadSceneAsync("UIOverlay", 
            LoadSceneMode.Additive, callback: () => Debug.Log("Additive scene loaded"));
    }
    #endif
}
```

### External Asset Downloading

```csharp
public class ExternalAssetLoader : MonoBehaviour
{
    private IExternalAssetsManager externalAssetsManager;
    
    #if THEONE_UNITASK
    public async UniTask DownloadAssetsAsync()
    {
        var progress = new Progress<float>(p => Debug.Log($"Download: {p:P}"));
        
        // Download text data
        var jsonData = await externalAssetsManager.DownloadTextAsync(
            "https://api.example.com/gamedata.json", 
            cache: true, progress);
            
        // Download and create texture
        var profilePicture = await externalAssetsManager.DownloadTextureAsync(
            "https://example.com/profile.jpg", 
            cache: true, progress);
            
        // Download and create sprite
        var icon = await externalAssetsManager.DownloadSpriteAsync(
            "https://example.com/icon.png", 
            cache: false, progress);
            
        // Download file to disk
        await externalAssetsManager.DownloadFileAsync(
            "https://example.com/update.zip",
            Application.persistentDataPath + "/update.zip",
            cache: true, progress);
            
        // Download binary data
        var assetBundle = await externalAssetsManager.DownloadBufferAsync(
            "https://example.com/content.bundle", 
            cache: true, progress);
    }
    #else
    public void DownloadAssets()
    {
        StartCoroutine(DownloadAssetsCoroutine());
    }
    
    private IEnumerator DownloadAssetsCoroutine()
    {
        var progress = new Progress<float>(p => Debug.Log($"Download: {p:P}"));
        
        // Download text data
        yield return externalAssetsManager.DownloadTextAsync(
            "https://api.example.com/gamedata.json",
            callback: jsonData => Debug.Log("JSON data downloaded"),
            cache: true, progress);
            
        // Download and create texture
        yield return externalAssetsManager.DownloadTextureAsync(
            "https://example.com/profile.jpg",
            callback: texture => Debug.Log("Profile picture downloaded"),
            cache: true, progress);
            
        // Download and create sprite
        yield return externalAssetsManager.DownloadSpriteAsync(
            "https://example.com/icon.png",
            callback: sprite => Debug.Log("Icon downloaded"),
            cache: false, progress);
            
        // Download file to disk
        yield return externalAssetsManager.DownloadFileAsync(
            "https://example.com/update.zip",
            Application.persistentDataPath + "/update.zip",
            callback: () => Debug.Log("Update file downloaded"),
            cache: true, progress);
            
        // Download binary data
        yield return externalAssetsManager.DownloadBufferAsync(
            "https://example.com/content.bundle",
            callback: buffer => Debug.Log("Asset bundle downloaded"),
            cache: true, progress);
    }
    #endif
    
    public void ManageCache()
    {
        // Delete specific cached item
        externalAssetsManager.DeleteCache("https://example.com/old-content.json");
    }
}
```

### Advanced Asset Management

```csharp
public class AdvancedAssetManager : MonoBehaviour
{
    private IAssetsManager assetsManager;
    
    #if THEONE_UNITASK
    public async UniTask PreloadAssetsAsync()
    {
        // Preload assets for better performance
        assetsManager.Download("Level1Assets");  // Sync download
        await assetsManager.DownloadAsync("Level2Assets"); // Async download
        
        // Download all addressable content
        await assetsManager.DownloadAllAsync(
            progress: new Progress<float>(p => Debug.Log($"Downloading all: {p:P}")));
    }
    #else
    public void PreloadAssets()
    {
        StartCoroutine(PreloadAssetsCoroutine());
    }
    
    private IEnumerator PreloadAssetsCoroutine()
    {
        // Preload assets for better performance
        assetsManager.Download("Level1Assets");  // Sync download
        yield return assetsManager.DownloadAsync("Level2Assets", 
            callback: () => Debug.Log("Level 2 assets downloaded")); // Async download
        
        // Download all addressable content
        yield return assetsManager.DownloadAllAsync(
            callback: () => Debug.Log("All assets downloaded"),
            progress: new Progress<float>(p => Debug.Log($"Downloading all: {p:P}")));
    }
    #endif
    
    public void UnloadUnusedAssets()
    {
        // Unload specific asset from cache
        assetsManager.Unload("HeavyTexture");
        assetsManager.Unload<AudioClip>("LongMusicTrack");
        
        // Dispose entire asset manager (unloads everything)
        assetsManager.Dispose();
    }
    
    #if THEONE_UNITASK
    public async UniTask LoadWithErrorHandlingAsync()
    {
        try
        {
            var asset = await assetsManager.LoadAsync<GameObject>("MightNotExist");
            Debug.Log("Asset loaded successfully");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load asset: {ex.Message}");
        }
        
        // Or use safe loading
        var (success, asset) = await assetsManager.TryLoadAsync<GameObject>("MightNotExist");
        if (!success)
        {
            Debug.Log("Asset not found, using fallback");
            asset = defaultPrefab;
        }
    }
    #else
    public void LoadWithErrorHandling()
    {
        StartCoroutine(LoadWithErrorHandlingCoroutine());
    }
    
    private IEnumerator LoadWithErrorHandlingCoroutine()
    {
        bool loadSucceeded = false;
        GameObject loadedAsset = null;
        
        // Try loading with error handling
        yield return assetsManager.LoadAsync<GameObject>("MightNotExist", 
            callback: asset => { loadedAsset = asset; loadSucceeded = true; })
            .Catch(() => Debug.LogError("Failed to load asset"));
        
        if (loadSucceeded)
        {
            Debug.Log("Asset loaded successfully");
        }
        
        // Or use safe loading
        yield return assetsManager.TryLoadAsync<GameObject>("MightNotExist",
            callback: result =>
            {
                if (!result.IsSucceeded)
                {
                    Debug.Log("Asset not found, using fallback");
                    // Use default prefab
                }
            });
    }
    #endif
}
```

### Scoped Asset Management

```csharp
public class ScopedAssetManager : MonoBehaviour
{
    // Scoped managers for different content areas
    private IAssetsManager mainAssetsManager;
    private IAssetsManager dlcAssetsManager;
    
    private void Start()
    {
        // Main assets manager - no scope
        mainAssetsManager = new AddressableAssetsManager(loggerManager);
        
        // DLC assets manager - scoped to "DLC" folder
        dlcAssetsManager = new AddressableAssetsManager(loggerManager, "DLC");
        
        LoadScopedAssets();
    }
    
    #if THEONE_UNITASK
    private async UniTaskVoid LoadScopedAssets()
    {
        // Loads from main addressable catalog
        var player = mainAssetsManager.Load<GameObject>("Player");
        
        // Loads from "DLC/Player" in addressable catalog
        var dlcPlayer = dlcAssetsManager.Load<GameObject>("Player");
        
        // Both managers can load the same key but from different scopes
        var mainEnemy = await mainAssetsManager.LoadAsync<GameObject>("Enemy");
        var dlcEnemy = await dlcAssetsManager.LoadAsync<GameObject>("Enemy");
    }
    #else
    private void LoadScopedAssets()
    {
        StartCoroutine(LoadScopedAssetsCoroutine());
    }
    
    private IEnumerator LoadScopedAssetsCoroutine()
    {
        // Loads from main addressable catalog
        var player = mainAssetsManager.Load<GameObject>("Player");
        
        // Loads from "DLC/Player" in addressable catalog
        var dlcPlayer = dlcAssetsManager.Load<GameObject>("Player");
        
        // Both managers can load the same key but from different scopes
        yield return mainAssetsManager.LoadAsync<GameObject>("Enemy",
            callback: enemy => Debug.Log("Main enemy loaded"));
        yield return dlcAssetsManager.LoadAsync<GameObject>("Enemy",
            callback: enemy => Debug.Log("DLC enemy loaded"));
    }
    #endif
}
```

## Architecture

### Folder Structure

```
TheOne.ResourceManagement/
├── Scripts/
│   ├── Assets/                           # Asset loading system
│   │   ├── IAssetsManager.cs            # Main asset management interface
│   │   ├── AssetsManager.cs             # Abstract base with caching
│   │   ├── AddressableAssetsManager.cs  # Addressables implementation
│   │   └── ResourceAssetsManager.cs     # Resources folder implementation
│   ├── Scenes/                          # Scene loading system
│   │   ├── IScenesManager.cs            # Scene management interface
│   │   ├── AddressableScenesManager.cs  # Addressables scene loading
│   │   └── ResourceScenesManager.cs     # Unity SceneManager wrapper
│   ├── ExternalAssets/                  # External asset downloading
│   │   ├── IExternalAssetsManager.cs    # External assets interface
│   │   └── ExternalAssetsManager.cs     # Web downloading implementation
│   └── DI/                              # Dependency injection extensions
│       ├── ResourcesManagerDI.cs
│       ├── ResourcesManagerVContainer.cs
│       └── ResourcesManagerZenject.cs
```

### Core Classes

#### `IAssetsManager`
Central interface for all asset loading operations:

**Synchronous Loading:**
- `Load<T>(string key)` - Load single asset
- `LoadAll<T>(string key)` - Load multiple assets with same key
- `LoadComponent<T>(string key)` - Load GameObject and get component
- `TryLoad<T>(string key, out T asset)` - Safe loading with error handling

**Asynchronous Loading:**
```csharp
#if THEONE_UNITASK
UniTask<T> LoadAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;
UniTask<IEnumerable<T>> LoadAllAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;
UniTask<(bool IsSucceeded, T Asset)> TryLoadAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default) where T : Object;
UniTask<T> LoadComponentAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
UniTask<IEnumerable<T>> LoadAllComponentsAsync<T>(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
UniTask DownloadAsync(string key, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
UniTask DownloadAllAsync(IProgress<float>? progress = null, CancellationToken cancellationToken = default);
#else
IEnumerator LoadAsync<T>(string key, Action<T> callback, IProgress<float>? progress = null) where T : Object;
IEnumerator LoadAllAsync<T>(string key, Action<IEnumerable<T>> callback, IProgress<float>? progress = null) where T : Object;
IEnumerator TryLoadAsync<T>(string key, Action<(bool IsSucceeded, T Asset)> callback, IProgress<float>? progress = null) where T : Object;
IEnumerator LoadComponentAsync<T>(string key, Action<T> callback, IProgress<float>? progress = null);
IEnumerator LoadAllComponentsAsync<T>(string key, Action<IEnumerable<T>> callback, IProgress<float>? progress = null);
IEnumerator DownloadAsync(string key, Action? callback = null, IProgress<float>? progress = null);
IEnumerator DownloadAllAsync(Action? callback = null, IProgress<float>? progress = null);
#endif
```

**Asset Management:**
- `Download(string key)` - Pre-download assets
- `DownloadAll()` - Download all available content
- `Unload(string key)` - Remove asset from cache

**Implicit Key Support:**
- `Load<T>()` - Uses type name as key
- All methods support type-based implicit keys

#### `AssetsManager` (Abstract Base)
Provides common functionality for all implementations:
- Dictionary-based asset caching for performance
- Automatic memory management and disposal
- Logging integration for debugging
- Thread-safe caching operations
- Template method pattern for concrete implementations

#### `AddressableAssetsManager`
Addressables-based implementation:
- Full Addressables API integration
- Support for remote content catalogs
- Asset downloading and dependency management
- Scoped asset loading with namespace support
- Automatic handle management and cleanup

#### `ResourceAssetsManager`
Unity Resources folder implementation:
- Traditional Resources.Load integration
- Limited to Resources folder content
- Simpler setup but less flexible
- Scoped loading within Resources structure

#### `IScenesManager`
Scene management interface:
- `LoadScene(string sceneName, LoadSceneMode mode)` - Sync loading
- `LoadSceneAsync(string sceneName, LoadSceneMode mode)` - Async with progress
- Support for both Single and Additive loading modes

#### `IExternalAssetsManager`
External asset downloading interface:
- `DownloadTextAsync(string url)` - Download text/JSON data
- `DownloadTextureAsync(string url)` - Download and create Texture2D
- `DownloadSpriteAsync(string url)` - Download and create Sprite
- `DownloadBufferAsync(string url)` - Download binary data
- `DownloadFileAsync(string url, string path)` - Download to disk
- Built-in caching system with cache management

### Resource Loading Strategies

#### Addressables (Recommended)
```csharp
// Best for: Production builds, remote content, memory optimization
var assetsManager = new AddressableAssetsManager(loggerManager);

// Benefits:
// - Remote content support
// - Memory-efficient loading
// - Dependency management
// - Asset streaming
// - Analytics integration
```

#### Resources Folder
```csharp
// Best for: Prototyping, simple projects, guaranteed inclusion
var assetsManager = new ResourceAssetsManager(loggerManager);

// Benefits:
// - Simple setup
// - Always included in build
// - No additional configuration
// - Direct access to assets
```

### Design Patterns

- **Strategy Pattern**: Pluggable asset loading implementations (Addressables vs Resources)
- **Template Method**: AssetsManager provides caching, concrete classes handle loading
- **Factory Pattern**: Asset creation through type-safe generic methods  
- **Caching Pattern**: Automatic asset caching with memory management
- **Async/Await Pattern**: Modern async programming with progress and cancellation
- **Interface Segregation**: Separate interfaces for different resource types

### Code Style & Conventions

- **Namespace**: All code under `TheOne.ResourceManagement` namespace
- **Null Safety**: Uses `#nullable enable` directive
- **Interfaces**: Prefixed with `I` (e.g., `IAssetsManager`)
- **Generic Constraints**: `where T : Object` for Unity object types
- **Async Support**: Conditional compilation for UniTask vs Coroutines
- **Error Handling**: Both throwing and non-throwing variants of methods
- **Scope Support**: Optional namespace scoping for asset organization

### Performance Optimizations

```csharp
public class OptimizedResourceLoading
{
    private IAssetsManager assetsManager;
    
    #if THEONE_UNITASK
    public async UniTask OptimalLoadingPatternsAsync()
    {
        // Good - Cache references to avoid repeated lookups
        var playerPrefab = assetsManager.Load<GameObject>("Player");
        for (int i = 0; i < 10; i++)
        {
            var player = Instantiate(playerPrefab); // Uses cached reference
        }
        
        // Good - Preload assets before needed
        assetsManager.Download("Level1Assets");
        
        // Good - Batch async operations
        var tasks = new[]
        {
            assetsManager.LoadAsync<GameObject>("Enemy1"),
            assetsManager.LoadAsync<GameObject>("Enemy2"),
            assetsManager.LoadAsync<GameObject>("Enemy3")
        };
        var enemies = await UniTask.WhenAll(tasks);
        
        // Good - Unload unused assets
        assetsManager.Unload("HeavyAsset");
    }
    #else
    public void OptimalLoadingPatterns()
    {
        StartCoroutine(OptimalLoadingPatternsCoroutine());
    }
    
    private IEnumerator OptimalLoadingPatternsCoroutine()
    {
        // Good - Cache references to avoid repeated lookups
        var playerPrefab = assetsManager.Load<GameObject>("Player");
        for (int i = 0; i < 10; i++)
        {
            var player = Instantiate(playerPrefab); // Uses cached reference
        }
        
        // Good - Preload assets before needed
        assetsManager.Download("Level1Assets");
        
        // Good - Batch async operations (coroutine approach)
        yield return assetsManager.LoadAsync<GameObject>("Enemy1",
            callback: enemy1 => Debug.Log("Enemy1 loaded"));
        yield return assetsManager.LoadAsync<GameObject>("Enemy2",
            callback: enemy2 => Debug.Log("Enemy2 loaded"));
        yield return assetsManager.LoadAsync<GameObject>("Enemy3",
            callback: enemy3 => Debug.Log("Enemy3 loaded"));
        
        // Good - Unload unused assets
        assetsManager.Unload("HeavyAsset");
    }
    #endif
    
    public void AvoidThesePatterns()
    {
        // Bad - Repeated loading without caching
        for (int i = 0; i < 10; i++)
        {
            var prefab = assetsManager.Load<GameObject>("Bullet"); // Cached, but lookup overhead
            Instantiate(prefab);
        }
        
        // Bad - Not handling loading failures
        var asset = assetsManager.Load<GameObject>("MightNotExist"); // May throw
        
        // Better - Safe loading
        if (assetsManager.TryLoad<GameObject>("MightNotExist", out var safeAsset))
        {
            Instantiate(safeAsset);
        }
    }
}
```

### Integration with DI Frameworks

#### VContainer
```csharp
using TheOne.ResourceManagement.DI;

public class GameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // Register resource managers
        builder.RegisterResourcesManager();
        
        // Or register specific implementation
        builder.Register<IAssetsManager, AddressableAssetsManager>(Lifetime.Singleton);
        builder.Register<IScenesManager, AddressableScenesManager>(Lifetime.Singleton);
        
        // Services can now inject resource managers
        builder.Register<AssetLoader>(Lifetime.Singleton);
    }
}
```

#### Zenject
```csharp
using TheOne.ResourceManagement.DI;

public class ResourceInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Register all resource managers
        Container.BindResourcesManager();
        
        // Or individual registration
        Container.BindInterfacesTo<AddressableAssetsManager>().AsSingle();
        Container.BindInterfacesTo<ExternalAssetsManager>().AsSingle();
        
        // Services automatically get resource management
        Container.BindInterfacesTo<GameAssetLoader>().AsSingle();
    }
}
```

#### Custom DI
```csharp
using TheOne.ResourceManagement.DI;

// Register with your DI container
container.RegisterResourcesManager();

// Or manual registration
container.Register<IAssetsManager>(() => new AddressableAssetsManager(loggerManager));
container.Register<IScenesManager>(() => new AddressableScenesManager(loggerManager));
container.Register<IExternalAssetsManager>(() => new ExternalAssetsManager(loggerManager));
```

### Advanced Usage Patterns

#### Progressive Asset Loading
```csharp
public class ProgressiveLoader : MonoBehaviour
{
    private IAssetsManager assetsManager;
    
    #if THEONE_UNITASK
    public async UniTask LoadGameContentAsync()
    {
        // Phase 1: Essential assets
        var progress1 = new Progress<float>(p => UpdateLoadingBar("Essential", p));
        await assetsManager.DownloadAsync("Core", progress1);
        
        // Phase 2: Level content
        var progress2 = new Progress<float>(p => UpdateLoadingBar("Level", p));
        await assetsManager.DownloadAsync("Level1", progress2);
        
        // Phase 3: Optional content (background loading)
        _ = LoadOptionalContentAsync().Forget();
    }
    #else
    public void LoadGameContent()
    {
        StartCoroutine(LoadGameContentCoroutine());
    }
    
    private IEnumerator LoadGameContentCoroutine()
    {
        // Phase 1: Essential assets
        var progress1 = new Progress<float>(p => UpdateLoadingBar("Essential", p));
        yield return assetsManager.DownloadAsync("Core", 
            callback: () => Debug.Log("Core assets loaded"), progress1);
        
        // Phase 2: Level content
        var progress2 = new Progress<float>(p => UpdateLoadingBar("Level", p));
        yield return assetsManager.DownloadAsync("Level1",
            callback: () => Debug.Log("Level 1 assets loaded"), progress2);
        
        // Phase 3: Optional content (background loading)
        StartCoroutine(LoadOptionalContent());
    }
    #endif
    
    private IEnumerator LoadOptionalContent()
    {
        yield return assetsManager.DownloadAsync("Audio/Music");
        yield return assetsManager.DownloadAsync("Effects/Particles");
        Debug.Log("Optional content loaded");
    }
}
```

#### Dynamic Content Management
```csharp
public class DynamicContentManager : MonoBehaviour
{
    private IAssetsManager assetsManager;
    private IExternalAssetsManager externalAssetsManager;
    
    #if THEONE_UNITASK
    public async UniTask LoadDynamicContentAsync(string contentUrl)
    {
        try
        {
            // Download content manifest
            var manifestJson = await externalAssetsManager.DownloadTextAsync(
                $"{contentUrl}/manifest.json", cache: true);
                
            var manifest = JsonUtility.FromJson<ContentManifest>(manifestJson);
            
            // Load each asset in manifest
            foreach (var assetInfo in manifest.assets)
            {
                var progress = new Progress<float>(p => 
                    Debug.Log($"Loading {assetInfo.name}: {p:P}"));
                    
                switch (assetInfo.type)
                {
                    case "texture":
                        var texture = await externalAssetsManager.DownloadTextureAsync(
                            assetInfo.url, cache: true, progress);
                        RegisterAsset(assetInfo.name, texture);
                        break;
                        
                    case "audio":
                        // Custom audio loading logic
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load dynamic content: {ex.Message}");
        }
    }
    #else
    public void LoadDynamicContent(string contentUrl)
    {
        StartCoroutine(LoadDynamicContentCoroutine(contentUrl));
    }
    
    private IEnumerator LoadDynamicContentCoroutine(string contentUrl)
    {
        bool manifestLoaded = false;
        string manifestJson = "";
        
        // Download content manifest
        yield return externalAssetsManager.DownloadTextAsync(
            $"{contentUrl}/manifest.json",
            callback: json => { manifestJson = json; manifestLoaded = true; },
            cache: true);
        
        if (!manifestLoaded)
        {
            Debug.LogError("Failed to load content manifest");
            yield break;
        }
        
        try
        {
            var manifest = JsonUtility.FromJson<ContentManifest>(manifestJson);
            
            // Load each asset in manifest
            foreach (var assetInfo in manifest.assets)
            {
                var progress = new Progress<float>(p => 
                    Debug.Log($"Loading {assetInfo.name}: {p:P}"));
                    
                switch (assetInfo.type)
                {
                    case "texture":
                        yield return externalAssetsManager.DownloadTextureAsync(
                            assetInfo.url,
                            callback: texture => RegisterAsset(assetInfo.name, texture),
                            cache: true, progress);
                        break;
                        
                    case "audio":
                        // Custom audio loading logic
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load dynamic content: {ex.Message}");
        }
    }
    #endif
}
```

## Performance Considerations

- **Caching**: All assets are cached automatically to prevent redundant loading
- **Memory Management**: Proper disposal patterns prevent memory leaks
- **Async Operations**: Non-blocking loading prevents frame drops
- **Progress Reporting**: Minimal overhead progress callbacks
- **Batch Loading**: Efficient batch operations for multiple assets
- **Scoped Loading**: Namespace isolation prevents key conflicts
- **Handle Management**: Automatic Addressables handle cleanup

## Best Practices

1. **Preloading**: Download assets during loading screens or app startup
2. **Error Handling**: Always use Try* methods for assets that might not exist
3. **Memory Management**: Unload heavy assets when no longer needed
4. **Progress Feedback**: Provide loading progress for better user experience
5. **Batch Operations**: Load related assets together for efficiency
6. **Scoping**: Use scoped managers for different content areas (base game, DLC, etc.)
7. **Caching Strategy**: Consider cache settings for external assets
8. **Resource Strategy**: Choose Addressables for production, Resources for prototyping
9. **Testing**: Mock resource managers for unit tests
10. **Disposal**: Always dispose asset managers to prevent memory leaks