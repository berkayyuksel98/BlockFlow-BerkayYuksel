using System;
using System.Collections.Generic;
using UnityEngine;

// Bir bloğun grid üzerinde kapladığı hücre şeklini tanımlayan ScriptableObject
// BlockData içinde string ShapeId olarak saklanır; LevelEditor yüklerken bu asset'i isimiyle bulur
[CreateAssetMenu(fileName = "NewBlockShape", menuName = "BlockFlow/Block Shape")]
public class BlockShapeData : ScriptableObject
{
    // Bu şekli tüm sistemler içinde benzersiz olarak tanımlayan kimlik (örneğin "L_Shape" "Z_Shape")
    public string ShapeId;

    // Pivot hücresine (0,0) göre bloğun kapladığı hücre offset'leri
    // Örnek L şekli: (0,0) (0,1) (0,2) (1,0)
    public List<Vector2Int> Cells = new List<Vector2Int> { Vector2Int.zero };

    public GameObject Prefab;
}
