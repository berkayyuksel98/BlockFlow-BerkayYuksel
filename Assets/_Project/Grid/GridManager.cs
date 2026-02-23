using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Zenject;

// Grid'in mantıksal durumunu yöneten sınıf
public class GridManager
{
    // Grid koordinatından BlockFacade'a hızlı erişim için ana veri yapısı
    private readonly Dictionary<Vector2Int, BlockFacade> grid
        = new Dictionary<Vector2Int, BlockFacade>();

    private readonly List<ExitView> spawnedExits = new List<ExitView>();
    private readonly List<GameObject> spawnedCells = new List<GameObject>();
    private readonly List<GameObject> spawnedWalls = new List<GameObject>();

    // Prefab → pool ve instance → prefab haritaları
    private readonly Dictionary<GameObject, Queue<GameObject>> objectPool = new();
    private readonly Dictionary<GameObject, GameObject> instancePrefabMap = new();

    // SpawnBlocks async döngüsünü iptal etmek için
    private System.Threading.CancellationTokenSource spawnCts;

    // Sahne hiyerarşisi için container objeler
    private GameObject levelRoot;
    private GameObject blocksContainer;
    private GameObject cellsContainer;
    private GameObject wallsContainer;
    private GameObject cornersContainer;
    private GameObject exitsContainer;

    private int rows;
    private int columns;

    // Blok prefablarını ve exit prefablarını almak için merkezi config
    private readonly GameConfig gameConfig;
    private readonly BlockFactory blockFactory;
    private readonly DiContainer container;
    private readonly IEventBus eventBus;

    private const float CellSize = 1f;
    public float GetCellSize => CellSize;

    [Inject]
    public GridManager(GameConfig gameConfig, BlockFactory blockFactory, DiContainer container, IEventBus eventBus)
    {
        this.gameConfig = gameConfig;
        this.blockFactory = blockFactory;
        this.container = container;
        this.eventBus = eventBus;
    }


    // LevelManager tarafından level yüklendiğinde çağrılır --- m evcut sahnedeki blok/exit nesnelerini temizler ve yeni level'ı kurar
    public async UniTask BuildLevel(LevelData levelData)
    {
        ClearLevel();

        rows = levelData.Rows;
        columns = levelData.Columns;

        // Kamera hemen ayarlanabilsin diye grid boyutu hazır olunca yayınla
        eventBus.Publish(new GridBuiltEvent { Columns = columns, Rows = rows });

        CreateLevelHierarchy();
        SpawnGridCells();
        SpawnExits(levelData.Exits);
        SpawnWalls(levelData.Exits);
        spawnCts = new System.Threading.CancellationTokenSource();
        await SpawnBlocks(levelData.Blocks, spawnCts.Token);
    }

    // Tüm blok ve exit nesnelerini sahneden kaldırır; grid ve listeler temizlenir
    public void ClearLevel()
    {
        // Devam eden spawn animasyonunu iptal et
        spawnCts?.Cancel();
        spawnCts?.Dispose();
        spawnCts = null;

        blockFactory.DestroyAll();
        grid.Clear();

        // Pool'a gönderilirken levelRoot'tan koparılmalı; sonra levelRoot güvenle destroy edilebilir
        foreach (ExitView exit in spawnedExits)
            if (exit != null) ReturnToPool(exit.gameObject);
        spawnedExits.Clear();

        foreach (GameObject cell in spawnedCells)
            if (cell != null) ReturnToPool(cell);
        spawnedCells.Clear();

        foreach (GameObject wall in spawnedWalls)
            if (wall != null) ReturnToPool(wall);
        spawnedWalls.Clear();

        if (levelRoot != null)
        {
            Object.Destroy(levelRoot);
            levelRoot = null;
        }
    }

    // Prefab'dan al: pool'da varsa yeniden kullan, yoksa instantiate et
    private GameObject GetFromPool(GameObject prefab, Transform parent)
    {
        if (!objectPool.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            objectPool[prefab] = queue;
        }

        GameObject go = queue.Count > 0 ? queue.Dequeue() : CreateAndRegister(prefab);
        go.SetActive(true);
        go.transform.SetParent(parent);
        return go;
    }

    // Yeni bir instance oluşturur ve haritaya kaydeder
    private GameObject CreateAndRegister(GameObject prefab)
    {
        var go = container.InstantiatePrefab(prefab);
        instancePrefabMap[go] = prefab;
        return go;
    }

    // Pool'a geri gönderir: levelRoot destroy edilmeden önce parent'tan koparılır
    private void ReturnToPool(GameObject instance)
    {
        if (!instancePrefabMap.TryGetValue(instance, out var prefab)) return;
        instance.SetActive(false);
        instance.transform.SetParent(null);
        if (!objectPool.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            objectPool[prefab] = queue;
        }
        queue.Enqueue(instance);
    }


    private async UniTask SpawnBlocks(List<BlockData> blocks, System.Threading.CancellationToken ct)
    {
        if (blocks == null) return;

        foreach (BlockData data in blocks)
        {
            if (ct.IsCancellationRequested) return;

            BlockShapeData shapeData = gameConfig.GetShape(data.ShapeId);
            if (shapeData == null) continue;

            BlockFacade facade = blockFactory.Create(data, shapeData.Cells);
            if (facade == null) continue;

            facade.transform.position = GridToWorld(data.GridPosition);
            facade.transform.SetParent(blocksContainer.transform);

            // Tüm hücreleri grid'e kaydet (pivot + offset hücreleri)
            foreach (Vector2Int offset in shapeData.Cells)
                grid[data.GridPosition + offset] = facade;

            // Scale animasyonu: 0 → 1
            facade.transform.localScale = Vector3.zero;
            facade.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack, 3f);

            await UniTask.WaitForSeconds(0.1f, cancellationToken: ct);
        }
    }

    private void SpawnExits(List<ExitData> exits)
    {
        if (exits == null) return;

        foreach (ExitData data in exits)
        {
            GameObject prefab = gameConfig.GetExitPrefab(data.Size);
            if (prefab == null) continue;

            GameObject go = GetFromPool(prefab, exitsContainer.transform);
            go.transform.rotation = GameConfig.GetExitRotation(data.Side);
            go.transform.position = GetExitWorldPosition(data);
            ExitView view = go.GetComponent<ExitView>();
            if (view != null)
            {
                view.Initialize(data);
                spawnedExits.Add(view);
            }
            else
            {
                Debug.LogWarning("[GridManager] Exit prefab üzerinde ExitView bileşeni bulunamadı");
            }
        }
    }

    // Her grid hücresi için zemin prefabını spawn eder
    // gridCellPrefab pivot'u sol-altta olmalıdır (GridToWorld ile hizalanır)
    private void SpawnGridCells()
    {
        if (gameConfig.gridCellPrefab == null) return;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                GameObject cell = GetFromPool(gameConfig.gridCellPrefab, cellsContainer.transform);
                cell.transform.position = GridToWorld(new Vector2Int(x, y));
                cell.transform.rotation = Quaternion.identity;
                spawnedCells.Add(cell);
            }
        }
    }


    // Grid kenarlarına exit olmayan hücrelere duvar, 4 köşeye köşe prefabı spawn eder
    private void SpawnWalls(List<ExitData> exits)
    {
        if (gameConfig.WallPrefab == null) return;


        // Her kenar için exitlerin kapladığı hücre indexlerini topla
        var covered = new Dictionary<ExitSide, HashSet<int>>
        {
            { ExitSide.Bottom, new HashSet<int>() },
            { ExitSide.Top,    new HashSet<int>() },
            { ExitSide.Left,   new HashSet<int>() },
            { ExitSide.Right,  new HashSet<int>() }
        };

        if (exits != null)
        {
            foreach (ExitData exit in exits)
                for (int i = exit.StartIndex; i < exit.StartIndex + exit.Size; i++)
                    covered[exit.Side].Add(i);
        }

        // Alt kenar Duvarlar
        for (int i = 0; i < columns; i++)
        {
            if (!covered[ExitSide.Bottom].Contains(i))
            {
                var pos = new Vector3(i * CellSize, 0f, 0f) + new Vector3(0.5f, 0f, -0.8f);//global pos + offset bottom duvarlar için + duvar genişliği 0.05f
                SpawnWallAt(pos, ExitSide.Bottom);
            }
        }

        //Üst kenar duvarlar
        for (int i = 0; i < columns; i++)
        {
            if (!covered[ExitSide.Top].Contains(i))
            {
                var pos = new Vector3(i * CellSize, 0f, rows * CellSize) + new Vector3(0.5f, 0f, -0.5f);
                SpawnWallAt(pos, ExitSide.Top);
            }
        }


        // Sol kenar duvarlar
        for (int j = 0; j < rows; j++)
        {
            if (!covered[ExitSide.Left].Contains(j))
            {
                var pos = new Vector3(0f, 0f, j * CellSize) + new Vector3(-0.8f, 0f, -0.5f);
                SpawnWallAt(pos, ExitSide.Left);
            }
        }


        // Sağ kenar duvarlar
        for (int j = 0; j < rows; j++)
        {
            if (!covered[ExitSide.Right].Contains(j))
            {
                var pos = new Vector3(columns * CellSize, 0f, j * CellSize) + new Vector3(-0.5f, 0f, -0.5f);
                SpawnWallAt(pos, ExitSide.Right);
            }
        }


        // 4 köşe — saat yönünde 90° artışlarla döndürülür
        SpawnCornerAt(new Vector3(-CellSize / 2f, 0f, -CellSize / 2f), Vector3.back);   // Alt-Sol
        SpawnCornerAt(new Vector3((columns - 1) * CellSize + CellSize / 2f, 0f, -CellSize / 2f), Vector3.right);   // Alt-Sağ
        SpawnCornerAt(new Vector3((columns - 1) * CellSize + CellSize / 2f, 0f, (rows - 1) * CellSize + CellSize / 2f), Vector3.forward);   // Üst-Sağ
        SpawnCornerAt(new Vector3(-CellSize / 2f, 0f, (rows - 1) * CellSize + CellSize / 2f), Vector3.left);   // Üst-Sol
    }

    // Belirtilen dünya pozisyonuna, ExitSide rotasyonuyla bir duvar spawn eder
    private void SpawnWallAt(Vector3 worldPos, ExitSide side)
    {
        GameObject wall = GetFromPool(gameConfig.WallPrefab, wallsContainer.transform);
        wall.transform.position = worldPos;
        wall.transform.rotation = GameConfig.GetWallRotation(side);
        spawnedWalls.Add(wall);
    }

    // Belirtilen dünya pozisyonuna yRotation ile bir köşe parçası spawn eder
    private void SpawnCornerAt(Vector3 worldPos, Vector3 lookRotation)
    {
        if (gameConfig.CornerPrefab == null) return;
        GameObject corner = GetFromPool(gameConfig.CornerPrefab, cornersContainer.transform);
        corner.transform.position = worldPos;
        corner.transform.forward = lookRotation;
        spawnedWalls.Add(corner);
    }


    // Level için sahne hiyerarşisini oluşturur
    private void CreateLevelHierarchy()
    {
        levelRoot        = new GameObject("Level");
        blocksContainer  = CreateContainerChild(levelRoot, "Blocks");
        cellsContainer   = CreateContainerChild(levelRoot, "Cells");
        wallsContainer   = CreateContainerChild(levelRoot, "Walls");
        cornersContainer = CreateContainerChild(levelRoot, "Corners");
        exitsContainer   = CreateContainerChild(levelRoot, "Exits");
    }

    private static GameObject CreateContainerChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    // Bloğu grid'de eski konumdan yeni konuma taşır
    // Eski hücre kayıtları silinir yeni hücreler yazılır
    public void MoveBlock(BlockFacade facade, Vector2Int oldPivot, Vector2Int newPivot)
    {
        List<Vector2Int> cells = facade.ShapeCells;

        // Eski hücreleri temizle
        foreach (Vector2Int offset in cells)
        {
            Vector2Int old = oldPivot + offset;
            if (grid.TryGetValue(old, out BlockFacade occupant) && occupant == facade)
                grid.Remove(old);
        }

        // Yeni hücreleri yaz
        foreach (Vector2Int offset in cells)
            grid[newPivot + offset] = facade;
    }

    // Bloğun belirtilen pivottan verilen yönde gidebileceği en uzak pivotu döndürür
    // Her adımda bloğun tüm hücrelerini kontrol eder
    public Vector2Int GetFarthestPosition(Vector2Int pivot, Vector2Int direction, List<Vector2Int> shapeCells, BlockFacade self)
    {
        Vector2Int current = pivot;

        while (true)
        {
            Vector2Int next = current + direction;

            foreach (Vector2Int offset in shapeCells)
            {
                Vector2Int cell = next + offset;

                if (!IsInsideGrid(cell))
                    return current;

                if (grid.TryGetValue(cell, out BlockFacade occupant) && occupant != self)
                    return current;
            }

            current = next;
        }
    }

    #region Sorgular 
    // Belirtilen koordinatta bir blok var mı?
    public bool IsCellOccupied(Vector2Int position) => grid.ContainsKey(position);

    // Belirtilen koordinat grid sınırları içinde mi?
    public bool IsInsideGrid(Vector2Int position)
        => position.x >= 0 && position.x < columns
        && position.y >= 0 && position.y < rows;

    // Bloğun verilen pivot'ta tüm hücreleri geçerli mi? (sınır dışı yok, başka blok yok)
    public bool IsPositionValid(Vector2Int pivot, List<Vector2Int> shapeCells, BlockFacade self)
    {
        foreach (Vector2Int offset in shapeCells)
        {
            Vector2Int cell = pivot + offset;
            if (!IsInsideGrid(cell)) return false;
            if (grid.TryGetValue(cell, out BlockFacade occupant) && occupant != self) return false;
        }
        return true;
    }

    // Belirtilen koordinattaki bloğu döndürür; yoksa null
    public BlockFacade GetBlockAt(Vector2Int position)
        => grid.TryGetValue(position, out BlockFacade block) ? block : null;

    public int Rows => rows;
    public int Columns => columns;
    #endregion

    public ExitView FindMatchingExit(Vector2Int pivot, Vector2Int direction,
        List<Vector2Int> shapeCells, BlockColor color, BlockFacade self)
    {
        Vector2Int farthest = GetFarthestPosition(pivot, direction, shapeCells, self);
        Vector2Int oneStep  = farthest + direction;

        bool hitsEdge = false;
        foreach (Vector2Int offset in shapeCells)
        {
            Vector2Int cell = oneStep + offset;
            if (!IsInsideGrid(cell))                                                        { hitsEdge = true; }
            else if (grid.TryGetValue(cell, out BlockFacade occ) && occ != self) return null;
        }
        if (!hitsEdge) return null;

        ExitSide side = direction == Vector2Int.right ? ExitSide.Right
                      : direction == Vector2Int.left  ? ExitSide.Left
                      : direction == Vector2Int.up    ? ExitSide.Top
                                                      : ExitSide.Bottom;
        bool isHorizontal = direction.x != 0;

        foreach (ExitView exitView in spawnedExits)
        {
            ExitData exit = exitView.Data;
            if (exit.Side != side || exit.Color != color) continue;

            bool allFit = true;
            foreach (Vector2Int offset in shapeCells)
            {
                int idx = isHorizontal
                    ? farthest.y + offset.y
                    : farthest.x + offset.x;
                if (idx < exit.StartIndex || idx >= exit.StartIndex + exit.Size)
                    { allFit = false; break; }
            }
            if (allFit) return exitView;
        }
        return null;
    }

    public void RemoveBlock(BlockFacade facade)
    {
        var toRemove = new List<Vector2Int>();
        foreach (var kvp in grid)
            if (kvp.Value == facade) toRemove.Add(kvp.Key);
        foreach (var key in toRemove)
            grid.Remove(key);
    }

    #region Koordinat Dönüşümleri

    // Grid koordinatını dünya pozisyonuna çevirir (sol-alt = 0,0)
    public Vector3 GridToWorld(Vector2Int gridPos)
        => new Vector3(gridPos.x * CellSize, 0f, gridPos.y * CellSize);

    // Dünya pozisyonunu en yakın grid koordinatına çevirir (snap için kullanılır)
    public Vector2Int WorldToGrid(Vector3 worldPos)
        => new Vector2Int(Mathf.RoundToInt(worldPos.x / CellSize), Mathf.RoundToInt(worldPos.z / CellSize));

    private Vector3 GetExitWorldPosition(ExitData data) // Çıkışın kenarına, başlangıç indexine ve boyutuna göre dünya pozisyonunu hesap
    {
        return data.Side switch
        {
            ExitSide.Bottom => new Vector3(data.StartIndex * CellSize - CellSize / 2f, 0f, -CellSize / 2f) + new Vector3(0, 0f, -0.3f), //global pos + offset
            ExitSide.Top => new Vector3((data.StartIndex + data.Size) * CellSize - CellSize / 2f, 0f, rows * CellSize - 0.2f),
            ExitSide.Left => new Vector3(-CellSize / 2f, 0f, (data.StartIndex + data.Size) * CellSize - CellSize / 2f) + new Vector3(-0.3f, 0f, 0f),
            ExitSide.Right => new Vector3(columns * CellSize - 0.2f, 0f, (data.StartIndex * CellSize) - CellSize / 2f),
            _ => Vector3.zero
        };
    }
    #endregion

}
