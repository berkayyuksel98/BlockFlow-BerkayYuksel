using System;
using System.Collections.Generic;
using UnityEngine;

// Oyun genelinde kullanılan prefab ve yapılandırma verilerini tutan merkezi ScriptableObject
// GridManager ve BlockFactory bu asset'e bağımlıdır; Zenject ile inject edilir
[CreateAssetMenu(fileName = "GameConfig", menuName = "BlockFlow/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Block Shapes")]
    // Oyundaki tüm blok şekil tanımları — BlockFactory ShapeId ile buradan prefab arar
    public List<BlockShapeData> BlockShapes = new List<BlockShapeData>();

    [Header("Grid Prefabs")]
    public GameObject WallPrefab;
    public GameObject CornerPrefab;
    public GameObject gridCellPrefab;


    [Header("Exit Prefabs")]
    // Çıkış boyutuna göre farklı prefablar — boyut (size) 1'den başlar
    // Size=1 için index 0, Size=2 için index 1 şeklinde erişilir
    public List<GameObject> ExitPrefabsBySize = new List<GameObject>();

    public BlockShapeData GetShape(string shapeId)
    {
        for (int i = 0; i < BlockShapes.Count; i++)
        {
            if (BlockShapes[i] != null && BlockShapes[i].ShapeId == shapeId)
                return BlockShapes[i];
        }

        Debug.LogError($"[GameConfig] ShapeId bulunamadı: '{shapeId}'");
        return null;
    }

    // Size değerine karşılık gelen exit prefabını döndürür; listede yoksa son elemanı döner.
    public GameObject GetExitPrefab(int size)
    {
        if (ExitPrefabsBySize == null || ExitPrefabsBySize.Count == 0)
        {
            Debug.LogError("[GameConfig] ExitPrefabsBySize listesi boş!");
            return null;
        }

        int index = Mathf.Clamp(size - 1, 0, ExitPrefabsBySize.Count - 1);
        return ExitPrefabsBySize[index];
    }

    // ExitSide değerine göre prefabın Y ekseni etrafındaki dönüş açısını döndürür
    public static Quaternion GetExitRotation(ExitSide side)
    {
        return side switch
        {
            ExitSide.Right => Quaternion.Euler(0f, 270f, 0f),
            ExitSide.Top => Quaternion.Euler(0f, -180f, 0f),
            ExitSide.Left => Quaternion.Euler(0f, 90f, 0f),
            ExitSide.Bottom => Quaternion.Euler(0f, 0f, 0f),
            _ => Quaternion.identity
        };
    }

    //Wall için y ekseni etrafındaki dönüş açısını döndürür
    public static Quaternion GetWallRotation(ExitSide side)
    {
        return side switch
        {
            ExitSide.Right => Quaternion.Euler(0f, 90f, 0f),
            ExitSide.Top => Quaternion.Euler(0f, 0f, 0f),
            ExitSide.Left => Quaternion.Euler(0f, 90f, 0f),
            ExitSide.Bottom => Quaternion.Euler(0f, 0f, 0f),
            _ => Quaternion.identity
        };
    }
}
