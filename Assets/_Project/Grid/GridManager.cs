using System.Collections.Generic;
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

    private int rows;
    private int columns;

    // Blok prefablarını ve exit prefablarını almak için merkezi config
    private readonly GameConfig gameConfig;

    private readonly BlockFactory blockFactory;

    private readonly DiContainer container;

    private const float CellSize = 1f;


    [Inject]
    public GridManager(GameConfig gameConfig, BlockFactory blockFactory, DiContainer container)
    {
        this.gameConfig = gameConfig;
        this.blockFactory = blockFactory;
        this.container = container;
    }


    // LevelManager tarafından level yüklendiğinde çağrılır --- m evcut sahnedeki blok/exit nesnelerini temizler ve yeni level'ı kurar
    public void BuildLevel(LevelData levelData)
    {
        ClearLevel();

        rows = levelData.Rows;
        columns = levelData.Columns;

        SpawnGridCells();
        SpawnBlocks(levelData.Blocks);
        SpawnExits(levelData.Exits);
        SpawnWalls(levelData.Exits);
    }

    // Tüm blok ve exit nesnelerini sahneden kaldırır grid ve listeler temizlenir
    public void ClearLevel()
    {
        blockFactory.DestroyAll();
        grid.Clear();

        foreach (ExitView exit in spawnedExits)
        {
            if (exit != null)
                Object.Destroy(exit.gameObject);
        }
        spawnedExits.Clear();

        foreach (GameObject cell in spawnedCells)
            if (cell != null) Object.Destroy(cell);
        spawnedCells.Clear();

        foreach (GameObject wall in spawnedWalls)
            if (wall != null) Object.Destroy(wall);
        spawnedWalls.Clear();
    }


    private void SpawnBlocks(List<BlockData> blocks)
    {
        if (blocks == null) return;

        foreach (BlockData data in blocks)
        {
            BlockFacade facade = blockFactory.Create(data);
            if (facade == null) continue;
            facade.transform.position = GridToWorld(data.GridPosition);
            grid[data.GridPosition] = facade;
        }
    }

    private void SpawnExits(List<ExitData> exits)
    {
        if (exits == null) return;

        foreach (ExitData data in exits)
        {
            GameObject prefab = gameConfig.GetExitPrefab(data.Size);
            if (prefab == null) continue;

            GameObject go = container.InstantiatePrefab(prefab);

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
                GameObject cell = container.InstantiatePrefab(gameConfig.gridCellPrefab);
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
        GameObject wall = container.InstantiatePrefab(gameConfig.WallPrefab);
        wall.transform.position = worldPos;
        wall.transform.rotation = GameConfig.GetWallRotation(side);
        spawnedWalls.Add(wall);
    }

    // Belirtilen dünya pozisyonuna yRotation ile bir köşe parçası spawn eder
    private void SpawnCornerAt(Vector3 worldPos, Vector3 lookRotation)
    {
        if (gameConfig.CornerPrefab == null) return;
        GameObject corner = container.InstantiatePrefab(gameConfig.CornerPrefab);
        corner.transform.position = worldPos;
        corner.transform.forward = lookRotation;
        spawnedWalls.Add(corner);
    }


    #region Sorgular 
    // Belirtilen koordinatta bir blok var mı?
    public bool IsCellOccupied(Vector2Int position) => grid.ContainsKey(position);

    // Belirtilen koordinat grid sınırları içinde mi?
    public bool IsInsideGrid(Vector2Int position)
        => position.x >= 0 && position.x < columns
        && position.y >= 0 && position.y < rows;

    // Belirtilen koordinattaki bloğu döndürür; yoksa null
    public BlockFacade GetBlockAt(Vector2Int position)
        => grid.TryGetValue(position, out BlockFacade block) ? block : null;

    public int Rows => rows;
    public int Columns => columns;
    #endregion

    #region Koordinat Dönüşümleri

    // Grid koordinatını dünya pozisyonuna çevirir (sol-alt = 0,0)
    public Vector3 GridToWorld(Vector2Int gridPos)
        => new Vector3(gridPos.x * CellSize, 0f, gridPos.y * CellSize);

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
