using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

// EventBus üzerinden BlockExitStartedEvent alanında exit particle oynatır; object pool kullanır
public class VFXSystem : IInitializable, IDisposable
{
    private readonly IEventBus eventBus;
    private readonly GameConfig gameConfig;
    private readonly GridManager gridManager;
    // Her prefab için ayrı pool
    private readonly Dictionary<ParticleSystem, Queue<ParticleSystem>> pools = new();

    [Inject]
    public VFXSystem(IEventBus eventBus, GameConfig gameConfig, GridManager gridManager)
    {
        this.eventBus = eventBus;
        this.gameConfig = gameConfig;
        this.gridManager = gridManager;
    }

    public void Initialize()
    {
        // Tüm kullanılacak prefablar burada eklenebilir
        TryCreatePool(gameConfig.VFXConfig?.exitParticlePrefab);
        TryCreatePool(gameConfig.VFXConfig?.confettiParticlePrefab);
        eventBus.Subscribe<BlockExitStartedEvent>(OnBlockExitStarted);
        eventBus.Subscribe<LevelCompletedEvent>(OnLevelCompleted);
    }

    private void TryCreatePool(ParticleSystem prefab)
    {
        if (prefab == null) return;
        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<ParticleSystem>();
        for (int i = 0; i < gameConfig.VFXConfig.PoolSize; i++)
            pools[prefab].Enqueue(CreatePooled(prefab));
    }

    public void Dispose()
    {
        eventBus.Unsubscribe<BlockExitStartedEvent>(OnBlockExitStarted);
        eventBus.Unsubscribe<LevelCompletedEvent>(OnLevelCompleted);
    }

    private void OnBlockExitStarted(BlockExitStartedEvent e)
    {
        var prefab = gameConfig.VFXConfig?.exitParticlePrefab;
        if (prefab == null) return;
        ParticleSystem ps = GetFromPool(prefab);
        ConfigureExitParticle(ps, e);
        ps.gameObject.SetActive(true);
        ps.Play();
        DOVirtual.DelayedCall(e.Duration + 0.3f, () => ReturnToPool(prefab, ps));
    }

    private void ConfigureExitParticle(ParticleSystem ps, BlockExitStartedEvent e)
    {
        // Y her zaman 0.5 (blok yüzeyi hizası)
        Vector3 pos = e.ExitWorldPosition;
        pos.y = 0.5f;
        bool isHorizontal = Mathf.Abs(e.ExitDirection.x) > 0.5f;
        if (isHorizontal)
            pos.z += e.ExitDirection.x > 0f ? e.ExitSize / 2f : -e.ExitSize / 2f;
        else
            pos.x += e.ExitDirection.z < 0f ? e.ExitSize / 2f : -e.ExitSize / 2f;

        // Çıkış yönünün tersine 0.25 birim offset öteleyerek exit yüzeyine yaklaştır
        pos -= e.ExitDirection * 0.25f;

        ps.transform.SetPositionAndRotation(
            pos,
            Quaternion.LookRotation(e.ExitDirection, Vector3.up));

        // süre ve renk
        var main = ps.main;
        main.duration = e.Duration;
        main.startLifetime = e.Duration;
        main.startColor = GetColor(e.Color);

        //genişlik = exit size (cellSize = 1 olduğundan doğrudan birim)
        var shape = ps.shape;
        shape.scale = new Vector3(e.ExitSize, shape.scale.y, shape.scale.z);
    }

    private Color GetColor(BlockColor blockColor)
    {
        var list = gameConfig.BlockColors;
        int idx = (int)blockColor;
        return list != null && idx >= 0 && idx < list.Count ? list[idx] : Color.white;
    }

    private void OnLevelCompleted(LevelCompletedEvent levelCompletedEvent)
    {
        var prefab = gameConfig.VFXConfig?.confettiParticlePrefab;
        if (prefab == null) return;

        float cellSize = gridManager.GetCellSize;
        float columns = gridManager.Columns;
        float rows = gridManager.Rows;

        // Sağ spawn pozisyonunun X değeri
        float rightSideX = columns * cellSize + 2f;

        Vector3[] spawnPositions = {
        new Vector3(-2f, 1f, -2f),       // Sol Taraf
        new Vector3(rightSideX, 1f, -2f) // Sağ Taraf
    };

        float centerX = (columns * cellSize) / 2f;
        float centerZ = (rows * cellSize) / 2f;

        Vector3 gridCenter = new Vector3(centerX, 0f, centerZ);

        //Bakış Hedefini Belirle (Merkez + 5 birim yukarı)
        Vector3 targetLookPosition = gridCenter + (Vector3.up * 5f);

        foreach (Vector3 pos in spawnPositions)
        {
            ParticleSystem ps = GetFromPool(prefab);
            ps.transform.position = pos;

            ps.transform.LookAt(targetLookPosition);

            ps.gameObject.SetActive(true);
            ps.Play();

            DOVirtual.DelayedCall(2f, () => ReturnToPool(prefab, ps));
        }
    }
    private ParticleSystem CreatePooled(ParticleSystem prefab)
    {
        var ps = UnityEngine.Object.Instantiate(prefab);
        ps.gameObject.SetActive(false);
        return ps;
    }

    private ParticleSystem GetFromPool(ParticleSystem prefab)
    {
        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<ParticleSystem>();
        var pool = pools[prefab];
        return pool.Count > 0 ? pool.Dequeue() : CreatePooled(prefab);
    }

    private void ReturnToPool(ParticleSystem prefab, ParticleSystem ps)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
        if (!pools.ContainsKey(prefab))
            pools[prefab] = new Queue<ParticleSystem>();
        pools[prefab].Enqueue(ps);
    }
}
