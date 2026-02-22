using UnityEngine;
using Zenject;

// Blogun ana MonoBehaviour bileseni - Facade tasarim kalibini uygular
// Alt sistemleri (hareket stratejisi durum görünüsel) bir arada tutar
// Dis dunya (InputManager GameManager) sadece bu sınıfla konuşur
public class BlockFacade : MonoBehaviour
{

    // Görsel güncellemelerden sorumlu yardımcı bileşen referansı
    private BlockVisuals blockVisuals;

    private GridManager gridManager;
    private IEventBus eventBus;

    private IMovementStrategy movementStrategy;

    // Zenject method injection ile bağımlılıklar atanır
    [Inject]
    public void Construct(GridManager gridManager, IEventBus eventBus)
    {
        this.gridManager = gridManager;
        this.eventBus = eventBus;
    }

    // BlockFactory tarafından spawn sonrası çağrılır; veriyi ve görseli kurar
    public void Initialize(BlockData blockData)
    {
        
    }

    // InputManager'dan gelecek yön komutunu strateji ve duruma ileterek işleme alır

    public void TryMove(Vector2Int direction)
    {
       
    }

    public void SetMovementStrategy(IMovementStrategy strategy)
    {
        movementStrategy = strategy;
    }
}
