using System;
using UnityEngine;
using Zenject;

// Grid boyutuna (rows x cols), perspektif FOV'a ve pitch açısına göre
// kamerayı otomatik konumlandıran hesap sınıfı
[RequireComponent(typeof(Camera))]
public class CameraCalculator : MonoBehaviour, IInitializable, IDisposable
{
    [SerializeField][Range(50f, 89f)] private float pitchDegrees = 80f;
    [SerializeField] private float padding = 2f;    
    [SerializeField] private float zOffset = -1.5f; // pozitif = ileri, negatif = geri
    private Camera cam;
    private IEventBus eventBus;
    private ILevelManager levelManager;

    private void Awake() => cam = GetComponent<Camera>();

    [Inject]
    private void Construct(IEventBus eventBus, ILevelManager levelManager)
    {
        this.eventBus = eventBus;
        this.levelManager = levelManager;
    }

    public void Initialize()
    {
        eventBus.Subscribe<GridBuiltEvent>(OnGridBuilt);
        // LevelManager zaten yüklemiş olabilir; aninda uygula
        if (levelManager.CurrentLevel != null)
            FitToGrid(levelManager.CurrentLevel.Columns, levelManager.CurrentLevel.Rows);
    }

    public void Dispose() => eventBus?.Unsubscribe<GridBuiltEvent>(OnGridBuilt);

    private void OnGridBuilt(GridBuiltEvent e) => FitToGrid(e.Columns, e.Rows);
    public void FitToGrid(int cols, int rows)
    {
        if (cam == null) cam = GetComponent<Camera>();

        // Grid merkezi
        Vector3 gridCenter = new Vector3((cols - 1) * 0.5f, 0f, (rows - 1) * 0.5f);

        float halfV = rows * 0.5f + padding;
        float halfH = cols * 0.5f + padding;

        float tanHalfFOV = Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float distV = halfV / tanHalfFOV;
        float distH = halfH / (tanHalfFOV * cam.aspect);
        float distance = Mathf.Max(distV, distH);

        float pitch = pitchDegrees * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(0f, Mathf.Sin(pitch), -Mathf.Cos(pitch)) * distance;

        transform.SetPositionAndRotation(
            gridCenter + offset + new Vector3(0f, 0f, zOffset),
            Quaternion.Euler(pitchDegrees, 0f, 0f));
    }
}
