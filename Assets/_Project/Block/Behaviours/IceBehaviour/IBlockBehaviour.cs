using UnityEngine;

// Her blok davranışının uygulaması gereken arayüz
public interface IBlockBehaviour
{
    // Davranış bloğa eklenince çağrılır; event subscription'lar burada kurulur
    void OnAttach(BlockFacade facade, BlockVisuals visuals, IEventBus eventBus);

    // Verilen yönde hareket izni var mı; false dönerse o eksen kilitlenir
    bool CanMove(Vector2Int direction);

    // Blok yok edilmeden önce temizlik için çağrılır
    void OnDetach();
}
