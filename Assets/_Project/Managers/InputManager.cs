using UnityEngine;
using Zenject;

// Dokunmatik ekran girdisini algılayan sürükle bırak komutunu ilgili bloğa ileten sınıf
// Physics.RaycastNonAlloc kullanır — her frame sıfır heap allocation
// Yalnızca "Block" layer'ındaki collider'lara ray atar
public class InputManager : ITickable
{
    private readonly Camera camera;
    private readonly IEventBus eventBus;

    private readonly RaycastHit[] hitBuffer = new RaycastHit[1];

    // Yalnızca "Block" layer maskesi
    private readonly int _blockLayerMask;

    // Şu an sürüklenen blok; null ise drag yok
    private BlockFacade draggedBlock;

    // Kameranın baktığı düzlem (Y=0, XZ düzlemi) — parmak pozisyonunu world'e çevirmek için
    private readonly Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

    [Inject]
    public InputManager(Camera camera, IEventBus eventBus)
    {
        this.camera       = camera;
        this.eventBus     = eventBus;
        _blockLayerMask = LayerMask.GetMask("Block");
    }

    public void Tick()
    {
        HandleMouseInput();
    }
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            BlockFacade hit = RaycastBlock(Input.mousePosition);
            if (hit != null)
            {
                draggedBlock = hit;
                draggedBlock.OnDragBegin(GetWorldPoint(Input.mousePosition));
                eventBus.Publish(new DragStartedEvent());
            }
        }
        else if (Input.GetMouseButton(0) && draggedBlock != null)
        {
            draggedBlock.OnDrag(GetWorldPoint(Input.mousePosition));
        }
        else if (Input.GetMouseButtonUp(0) && draggedBlock != null)
        {
            draggedBlock.OnDragEnd();
            eventBus.Publish(new DragEndedEvent());
            draggedBlock = null;
        }
    }

    private BlockFacade RaycastBlock(Vector2 screenPos)
    {
        Ray ray = camera.ScreenPointToRay(screenPos);
        int hitCount = Physics.RaycastNonAlloc(ray, hitBuffer, Mathf.Infinity, _blockLayerMask);
        if (hitCount == 0) return null;
        return hitBuffer[0].collider.GetComponent<BlockFacade>();
    }

    private Vector3 GetWorldPoint(Vector2 screenPos)
    {
        Ray ray = camera.ScreenPointToRay(screenPos);
        groundPlane.Raycast(ray, out float distance);
        return ray.GetPoint(distance);
    }
}
