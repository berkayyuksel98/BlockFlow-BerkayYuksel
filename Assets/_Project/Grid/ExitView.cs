using UnityEngine;

// Sahneye spawn edilen bir çıkış noktasının görsel MonoBehaviour bileşeni
// GridManager prefabı spawn ettikten sonra Initialize() çağırır; renk ve data atanır
public class ExitView : MonoBehaviour
{
    
    public ExitData Data { get; private set; }

    public void Initialize(ExitData data)
    {
        Data = data;
        ApplyColor(data.Color);
    }

    // BlockColor enum değerine göre materyali atar
    private void ApplyColor(BlockColor color)
    {
        
    }
}
