using UnityEngine;

// Yeterli sayıda çıkış gerçekleşene kadar bloğun tüm hareketini engeller; buz görselini yönetir
public class IceBehaviour : IBlockBehaviour
{
    private readonly int requiredExitCount;
    private readonly IceBehaviourConfig config;
    private IEventBus eventBus;
    private int exitedCount;
    private BlockVisuals visuals;
    private bool unlocked;

    public IceBehaviour(IceBehaviourData data, IceBehaviourConfig config)
    {
        requiredExitCount = data.RequiredExitCount;
        this.config = config;
    }

    public void OnAttach(BlockFacade facade, BlockVisuals visuals, IEventBus eventBus)
    {
        this.visuals = visuals;
        this.eventBus = eventBus;
        visuals.ApplyOverlay(config.IceTexture, requiredExitCount);
        eventBus.Subscribe<BlockExitedEvent>(OnBlockExited);
    }

    // Kilit açılmadıkça tüm yönlerde hareketi engeller
    public bool CanMove(Vector2Int direction) => unlocked;

    private void OnBlockExited(BlockExitedEvent e)
    {
        if (unlocked) return;
        exitedCount++;
        int remaining = requiredExitCount - exitedCount;
        if (remaining <= 0)
        {
            unlocked = true;
            visuals.RemoveOverlay();
        }
        else
        {
            visuals.UpdateOverlayCount(remaining);
        }
    }

    public void OnDetach()
    {
        eventBus?.Unsubscribe<BlockExitedEvent>(OnBlockExited);
    }
}
