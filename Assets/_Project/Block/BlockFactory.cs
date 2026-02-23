using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.TextCore;
using Zenject;

// BlockData'ya göre doğru shape prefabını bulan, BlockFacade'i initialize eden ve davranışları ekleyen fabrika
public class BlockFactory
{
    private readonly DiContainer container;
    private readonly GameConfig gameConfig;
    private readonly BlockBehaviourFactory behaviourFactory;
    private readonly List<BlockFacade> spawnedBlocks = new List<BlockFacade>();
    // Shape prefabına göre pool
    private readonly Dictionary<BlockShapeData, Queue<BlockFacade>> pool = new();

    [Inject]
    public BlockFactory(DiContainer container, GameConfig gameConfig, BlockBehaviourFactory behaviourFactory)
    {
        this.container = container;
        this.gameConfig = gameConfig;
        this.behaviourFactory = behaviourFactory;
    }

    // BlockData'ya göre ilgili prefabı bulur, spawn eder ve initialize eder
    // Dönen BlockFacade zaten GridPosition, Color, Type ve görselleriyle hazır durumdadır
    public BlockFacade Create(BlockData blockData, List<Vector2Int> shapeCells)
    {
        BlockShapeData shapeData = gameConfig.GetShape(blockData.ShapeId);

        if (shapeData == null)
        {
            Debug.LogError($"[BlockFactory] Shape bulunamadı: '{blockData.ShapeId}'");
            return null;
        }

        if (shapeData.Prefab == null)
        {
            Debug.LogError($"[BlockFactory] Prefab atanmamış: '{blockData.ShapeId}'");
            return null;
        }

        BlockFacade facade = GetOrCreate(shapeData);
        facade.gameObject.SetActive(true);
        facade.OnSpawned();
        facade.Initialize(blockData, shapeCells);
        // Exit tamamlanınca bu blok kendi kendini pool'a geri koyar
        facade.OnReturnToPool = () => ReturnToPool(facade, shapeData);

        if (blockData.Behaviours != null)
            foreach (var entry in blockData.Behaviours)
            {
                var behaviour = behaviourFactory.Create(entry);
                if (behaviour != null) facade.AddBehaviour(behaviour);
            }

        spawnedBlocks.Add(facade);
        return facade;
    }

    private BlockFacade GetOrCreate(BlockShapeData shapeData)
    {
        if (pool.TryGetValue(shapeData, out var queue) && queue.Count > 0)
            return queue.Dequeue();
        return container.InstantiatePrefabForComponent<BlockFacade>(shapeData.Prefab);
    }

    private void ReturnToPool(BlockFacade facade, BlockShapeData shapeData)
    {
        spawnedBlocks.Remove(facade);
        facade.OnDespawned();
        facade.transform.SetParent(null); // levelRoot destroy edilince blok yok olmasın
        facade.gameObject.SetActive(false);
        if (!pool.TryGetValue(shapeData, out var queue))
        {
            queue = new Queue<BlockFacade>();
            pool[shapeData] = queue;
        }
        queue.Enqueue(facade);
    }

    // Level bitiminde veya restart'ta aktif tüm blokları pool'a geri koyar
    public void DestroyAll()
    {
        // Kopya alıyoruz çünkü ReturnToPool spawnedBlocks'u değiştiriyor
        var toReturn = new List<BlockFacade>(spawnedBlocks);
        foreach (BlockFacade block in toReturn)
        {
            if (block == null) continue;
            var shapeData = gameConfig.GetShape(block.ShapeId);
            if (shapeData != null)
                ReturnToPool(block, shapeData);
            else
            {
                block.OnDespawned();
                block.gameObject.SetActive(false);
            }
        }
        spawnedBlocks.Clear();
    }
}
