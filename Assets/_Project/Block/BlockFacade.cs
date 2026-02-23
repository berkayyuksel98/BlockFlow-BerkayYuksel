using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

public class BlockFacade : MonoBehaviour, IPoolableBlock
{
    private BlockVisuals blockVisuals;
    private GridManager gridManager;
    private IEventBus eventBus;
    private IMovementStrategy movementStrategy;
    private readonly List<IBlockBehaviour> behaviours = new List<IBlockBehaviour>();

    public List<Vector2Int> ShapeCells { get; private set; }
    public Vector2Int GridPosition { get; private set; }

    private BlockColor blockColor;
    private bool isExiting;
    private Vector2Int dragLivePivot;
    private Vector3 dragOriginWorld;
    public string ShapeId { get; private set; }
    public System.Action OnExitStarted;
    public System.Action OnReturnToPool;

    [Inject]
    public void Construct(GridManager gridManager, IEventBus eventBus)
    {
        this.gridManager = gridManager;
        this.eventBus = eventBus;
    }

    public void Initialize(BlockData blockData, List<Vector2Int> shapeCells)
    {
        ShapeId = blockData.ShapeId;
        GridPosition = blockData.GridPosition;
        ShapeCells = shapeCells;
        blockColor = blockData.Color;

        movementStrategy = blockData.Type == BlockType.SingleAxis
            ? (IMovementStrategy)new SingleAxisMovementStrategy(blockData.MovementAxis)
            : new FreeMovementStrategy();

        blockVisuals = GetComponentInChildren<BlockVisuals>();
        blockVisuals.Initialize(blockData, shapeCells, this);
    }

    // Pool'dan alınırken çağrılır: state sıfırlanır, collider'lar tekrar açılır
    public void OnSpawned()
    {
        isExiting = false;
        OnExitStarted = null;
        OnReturnToPool = null;
        foreach (var b in behaviours)
            b.OnDetach();
        behaviours.Clear();
        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = true;
    }

    // Pool'a geri dönerken çağrılır: tween durdurulur, state temizlenir
    public void OnDespawned()
    {
        transform.DOKill();
        foreach (var b in behaviours)
            b.OnDetach();
        behaviours.Clear();
        OnExitStarted = null;
    }

    public void OnDragBegin(Vector3 worldHitPoint)
    {
        if (isExiting) return;
        dragOriginWorld = worldHitPoint;
        dragLivePivot = GridPosition;
    }

    public void OnDrag(Vector3 currentWorldPoint)
    {
        if (isExiting) return;

        Vector3 totalDelta = currentWorldPoint - dragOriginWorld;
        Vector3 baseWorld = gridManager.GridToWorld(GridPosition);

        float offsetX = 0f;
        Vector2Int fR = dragLivePivot, fL = dragLivePivot;
        ExitView rightExitView = null, leftExitView = null;

        if (movementStrategy.CanMove(Vector2Int.right) && BehavioursAllowMove(Vector2Int.right))
        {
            fR = gridManager.GetFarthestPosition(dragLivePivot, Vector2Int.right, ShapeCells, this);
            fL = gridManager.GetFarthestPosition(dragLivePivot, Vector2Int.left, ShapeCells, this);

            rightExitView = gridManager.FindMatchingExit(dragLivePivot, Vector2Int.right, ShapeCells, blockColor, this);
            leftExitView = gridManager.FindMatchingExit(dragLivePivot, Vector2Int.left, ShapeCells, blockColor, this);

            offsetX = Mathf.Clamp(totalDelta.x, fL.x - GridPosition.x, fR.x - GridPosition.x);
        }

        int liveX = GridPosition.x + Mathf.RoundToInt(offsetX);

        float offsetZ = 0f;
        Vector2Int fU = dragLivePivot, fD = dragLivePivot;
        ExitView upExitView = null, downExitView = null;

        var zPivot = new Vector2Int(liveX, dragLivePivot.y);
        if (movementStrategy.CanMove(Vector2Int.up) && BehavioursAllowMove(Vector2Int.up))
        {
            fU = gridManager.GetFarthestPosition(zPivot, Vector2Int.up, ShapeCells, this);
            fD = gridManager.GetFarthestPosition(zPivot, Vector2Int.down, ShapeCells, this);

            upExitView = gridManager.FindMatchingExit(zPivot, Vector2Int.up, ShapeCells, blockColor, this);
            downExitView = gridManager.FindMatchingExit(zPivot, Vector2Int.down, ShapeCells, blockColor, this);

            offsetZ = Mathf.Clamp(totalDelta.z, fD.y - GridPosition.y, fU.y - GridPosition.y);
        }

        int liveZ = GridPosition.y + Mathf.RoundToInt(offsetZ);

        // Önce bloğu konumlandır; dragLivePivot farthest'e yapıştıysa exit tetikle
        dragLivePivot = new Vector2Int(liveX, liveZ);
        transform.position = new Vector3(baseWorld.x + offsetX, baseWorld.y, baseWorld.z + offsetZ);

        // offsetX/Z'nin float değeri farthest'e fiziksel olarak ulaştıysa önce snap, sonra exit tetikle
        if (rightExitView != null && offsetX >= fR.x - GridPosition.x)
        {
            GridPosition = fR;
            transform.position = gridManager.GridToWorld(fR);
            TriggerExit(Vector2Int.right, rightExitView); return;
        }
        if (leftExitView != null && offsetX <= fL.x - GridPosition.x)
        {
            GridPosition = fL;
            transform.position = gridManager.GridToWorld(fL);
            TriggerExit(Vector2Int.left, leftExitView); return;
        }
        if (upExitView != null && offsetZ >= fU.y - GridPosition.y)
        {
            GridPosition = fU;
            transform.position = gridManager.GridToWorld(fU);
            TriggerExit(Vector2Int.up, upExitView); return;
        }
        if (downExitView != null && offsetZ <= fD.y - GridPosition.y)
        {
            GridPosition = fD;
            transform.position = gridManager.GridToWorld(fD);
            TriggerExit(Vector2Int.down, downExitView); return;
        }
    }

    public void OnDragEnd()
    {
        if (isExiting) return;

        Vector2Int snapped = gridManager.WorldToGrid(transform.position);
        Vector2Int safeTarget = gridManager.IsPositionValid(snapped, ShapeCells, this)
            ? snapped
            : dragLivePivot;

        // Snap pozisyonunda blok duvara dayanıyorsa ve exit varsa çıkışı tetikle
        foreach (Vector2Int dir in new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
        {
            Vector2Int farthest = gridManager.GetFarthestPosition(safeTarget, dir, ShapeCells, this);
            if (farthest != safeTarget) continue; // duvara dayanmıyor, exit kontrolü yapma

            ExitView exitView = gridManager.FindMatchingExit(safeTarget, dir, ShapeCells, blockColor, this);
            if (exitView != null)
            {
                GridPosition = safeTarget;
                transform.position = gridManager.GridToWorld(safeTarget);
                TriggerExit(dir, exitView);
                return;
            }
        }

        Vector2Int oldPivot = GridPosition;
        GridPosition = safeTarget;
        gridManager.MoveBlock(this, oldPivot, safeTarget);
        transform.position = gridManager.GridToWorld(safeTarget);
    }

    private void TriggerExit(Vector2Int direction, ExitView exitView)
    {
        isExiting = true;
        gridManager.RemoveBlock(this);

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        // Bloğun çıkış yönündeki hücre sayısı kadar hareket
        int minAxis = int.MaxValue, maxAxis = int.MinValue;
        foreach (Vector2Int o in ShapeCells)
        {
            int v = direction.x != 0 ? o.x : o.y;
            if (v < minAxis) minAxis = v;
            if (v > maxAxis) maxAxis = v;
        }
        int span = maxAxis - minAxis + 1;
        const float exitSpeed = 4.5f;
        float duration = span / exitSpeed;

        Vector3 exitDir3D = new Vector3(direction.x, 0f, direction.y);
        Vector3 target = transform.position + exitDir3D * span;

        exitView?.PlayExitAnimation(duration);
        blockVisuals.PlayExitAnimation(exitView.Data.Side, duration);

        OnExitStarted?.Invoke(); //Block visuals için eventi tetikle

        // VFX,ses vb için animasyon başlangıcında event yayınla
        eventBus.Publish(new BlockExitStartedEvent
        {
            Color = blockColor,
            ExitWorldPosition = exitView != null ? exitView.transform.position : transform.position,
            ExitDirection = exitDir3D,
            ExitSize = exitView?.Data.Size ?? 1,
            Duration = duration,
        });

        BlockColor color = blockColor;
        transform.DOMove(target, duration).SetEase(Ease.Linear).OnComplete(() =>
        {
            eventBus.Publish(new BlockExitedEvent { Color = color });
            OnReturnToPool?.Invoke();
        });
    }

    // Davranış ekler ve hemen OnAttach çağırır
    public void AddBehaviour(IBlockBehaviour behaviour)
    {
        behaviours.Add(behaviour);
        behaviour.OnAttach(this, blockVisuals, eventBus);
    }

    private bool BehavioursAllowMove(Vector2Int dir)
    {
        foreach (var b in behaviours)
            if (!b.CanMove(dir)) return false;
        return true;
    }

    private void OnDestroy()
    {
        // Pool kullanılıyorsa behaviours zaten OnDespawned'da temizlenir
        // Gerçek Destroy (editor vb.) durumu için güvenlik
        foreach (var b in behaviours)
            b.OnDetach();
        behaviours.Clear();
    }

    public void SetMovementStrategy(IMovementStrategy strategy)
    {
        movementStrategy = strategy;
    }
}
