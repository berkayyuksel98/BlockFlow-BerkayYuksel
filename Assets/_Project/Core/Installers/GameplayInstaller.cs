using UnityEngine;
using Zenject;

// SceneContext'e eklenir; sadece oyun sahnesinde yaşayan sistemleri bağlar
// Tüm manager'lar pure C# — Zenject constructor injection ile oluşturur
public class GameplayInstaller : MonoInstaller
{
    [Header("Config")]
    // Tüm prefab ve shape tanımlarını içeren merkezi ScriptableObject
    [SerializeField] private GameConfig gameConfig;

    [Header("Test")]
    // Test MockLevelManager için bool
    [SerializeField] private bool useMockLevelManager = false;

    public override void InstallBindings()
    {
        // IEventBus — tüm sistemlerin haberleşmesini sağlar
        Container.Bind<IEventBus>().To<EventBus>().AsSingle();

        // GameConfig ScriptableObject — BlockFactory ve GridManager tarafından kullanılır
        Container.BindInstance(gameConfig).AsSingle();

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
    }
}
