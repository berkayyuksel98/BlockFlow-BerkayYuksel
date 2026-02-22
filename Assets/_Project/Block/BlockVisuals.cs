using UnityEngine;

// Blogun görsel katmanindan sorumlu MonoBehaviour sinifi
// Renk atama buz mesh'ini acip kapatma eksen oklarini gösterme gibi sadece görsel işler yapar
// Oyun mantigini kesinlikle bilmez BlockFacade tarafindan yönetilir
public class BlockVisuals : MonoBehaviour
{
    public void Initialize(BlockData data)
    {
        SetColor(data.Color);
        SetIceVisible(data.Type == BlockType.Iced);
        SetAxisArrows(data.MovementAxis);
    }

    // Bloğun ana rengini atar; materyali colorMaterials listesinden seçer
    public void SetColor(BlockColor color)
    {
    }

    // Buz katmanini goster veya gizle (IcedBlock durumu degistiginde çağrılır)
    public void SetIceVisible(bool visible)
    {
    }

    // Kalan buz sağlığını text olarak yansıtır
    public void UpdateIceDamageVisual(int remainingHealth, int maxHealth)
    {
        
    }

    // SingleAxis bloklarin hangi eksende hareket ettigini belirtmek için oklari gunceller
    public void SetAxisArrows(MovementAxis axis)
    {
        
    }
}
