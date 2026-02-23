using UnityEngine;
using Zenject;

// Scene View'da hücreleri renkli wire cube olarak gösterir:
//   Yeşil  = boş hücre
//   Kırmızı = dolu hücre (blok var)
public class GridDebugger : MonoBehaviour
{
    [Header("Görsel Ayarlar")]
    [SerializeField] private Color emptyColor  = new Color(0f, 1f, 0f, 0.4f);
    [SerializeField] private Color filledColor = new Color(1f, 0f, 0f, 0.6f);
    [SerializeField] private float cubeHeight  = 0.05f;

    private GridManager gridManager;

    [Inject]
    public void Construct(GridManager gridManager)
    {
        this.gridManager = gridManager;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (gridManager == null) return;

        int rows    = gridManager.Rows;
        int columns = gridManager.Columns;
        float size  = gridManager.GetCellSize;

        if (rows == 0 || columns == 0) return;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                var cell     = new Vector2Int(x, y);
                bool occupied = gridManager.IsCellOccupied(cell);
                Vector3 center = gridManager.GridToWorld(cell) + new Vector3(size / 2f, 0f, size / 2f);

                Gizmos.color = occupied ? filledColor : emptyColor;
                Gizmos.DrawWireCube(center, new Vector3(size * 0.95f, cubeHeight, size * 0.95f));

                // Koordinat etiketi
                UnityEditor.Handles.color = occupied ? Color.red : Color.green;
                UnityEditor.Handles.Label(center + Vector3.up * 0.15f, $"{x},{y}");
            }
        }
    }
#endif
}
