using UnityEngine;
using Zenject;

// SceneContext'e eklenir; sadece oyun sahnesinde yaşayan sistemleri bağlar
// Tüm manager'lar pure C# — Zenject constructor injection ile oluşturur
public class GameplayInstaller : MonoInstaller
{
    [Header("Config")]
    // Tüm prefab ve shape tanımlarını içeren merkezi ScriptableObject
    [SerializeField] private GameConfig gameConfig;

    [Header("References")]
    [SerializeField] private Camera gameCamera;

    [Header("UI Panels")]
    [SerializeField] private GameplayPanel gameplayPanel;
    [SerializeField] private WinPanel winPanel;
    [SerializeField] private LosePanel losePanel;

    [Header("Test")]
    // Test MockLevelManager için bool
    [SerializeField] private bool useMockLevelManager = false;

    [Header("Debug")]
    [SerializeField] private GridDebugger gridDebugger;

    public override void InstallBindings()
    {
        // IEventBus — tüm sistemlerin haberleşmesini sağlar
        Container.Bind<IEventBus>().To<EventBus>().AsSingle();

        // GameConfig ScriptableObject — BlockFactory ve GridManager tarafından kullanılır
        Container.BindInstance(gameConfig).AsSingle();

        // Camera — InputManager tarafından ray cast için kullanılır
        Container.BindInstance(gameCamera).AsSingle();

        // CameraCalculator — kamerayı grid boyutuna göre konumlandırır
        Container.BindInterfacesAndSelfTo<CameraCalculator>()
            .FromComponentOn(gameCamera.gameObject)
            .AsSingle();

        // BlockBehaviourFactory — RawBehaviourEntry'den IBlockBehaviour üreten fabrika
        Container.Bind<BlockBehaviourFactory>().AsSingle();

        // BlockFactory — GameConfig üzerinden shape prefabını bulur ve spawn eder
        Container.Bind<BlockFactory>().AsSingle();

        // GridManager — Zenject new'ler, DiContainer inject eder
        Container.Bind<GridManager>().AsSingle();

        // LevelManager — test modunda MockLevelManager, üretimde gerçek LevelManager
#if UNITY_EDITOR
        if (useMockLevelManager)
            Container.BindInterfacesAndSelfTo<MockLevelManager>().AsSingle();
        else
#endif
            Container.BindInterfacesAndSelfTo<LevelManager>().AsSingle();

        // GameManager
        Container.BindInterfacesAndSelfTo<GameManager>().AsSingle();

        // InputManager
        Container.BindInterfacesAndSelfTo<InputManager>().AsSingle();

        // UIManager
        Container.BindInterfacesAndSelfTo<UIManager>().AsSingle();

        // UI Panels — sahnedeki instance'lar Zenject'e inject edilebilir hale gelir
        Container.BindInstance(gameplayPanel).AsSingle();
        Container.BindInstance(winPanel).AsSingle();
        Container.BindInstance(losePanel).AsSingle();

        // VFXSystem
        Container.BindInterfacesAndSelfTo<VFXSystem>().AsSingle();

        // AudioController
        Container.BindInstance(gameConfig.AudioConfig).AsSingle();
        Container.BindInterfacesAndSelfTo<AudioController>().AsSingle();

        // GridDebugger — opsiyonel; sahnede yoksa atlanır
        if (gridDebugger != null)
            Container.QueueForInject(gridDebugger);
    }
}
