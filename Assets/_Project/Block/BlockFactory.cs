using System.Collections.Generic;
using UnityEngine;
using Zenject;

// BlockData'ya göre doğru shape prefabını bulan ve BlockFacade'ı initialize eden fabrika sınıfı
// Her shape'in kendi prefabı vardır; GameConfig.BlockShapes listesinden ShapeId ile aranır
// Zenject DiContainer kullanarak prefabı instantiate eder ve [Inject] bağımlılıklarını çözer
public class BlockFactory
{
    private readonly DiContainer container;

    // Tüm shape tanımları ve prefab referanslarını içeren merkezi config
    private readonly GameConfig gameConfig;

    // Sahne içinde spawn edilen tüm blokları takip eder; DestroyAll için kullanılır
    private readonly List<BlockFacade> spawnedBlocks = new List<BlockFacade>();

    [Inject]
    public BlockFactory(DiContainer container, GameConfig gameConfig)
    {
        this.container  = container;
        this.gameConfig = gameConfig;
    }

    // BlockData'ya göre ilgili prefabı bulur, spawn eder ve initialize eder
    // Dönen BlockFacade zaten GridPosition, Color, Type ve görselleriyle hazır durumdadır
    public BlockFacade Create(BlockData blockData)
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

        // Zenject ile instantiate: [Inject] alanları otomatik doldurulur
        BlockFacade facade = container.InstantiatePrefabForComponent<BlockFacade>(shapeData.Prefab);
        facade.Initialize(blockData);

        spawnedBlocks.Add(facade);
        return facade;
    }

    // Level bitiminde veya restart'ta tüm blok nesnelerini sahneden kaldırır
    public void DestroyAll()
    {
        foreach (BlockFacade block in spawnedBlocks)
        {
            if (block != null)
                Object.Destroy(block.gameObject);
        }

        spawnedBlocks.Clear();
    }
}
