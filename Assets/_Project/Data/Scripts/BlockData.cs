using System;
using System.Collections.Generic;
using UnityEngine;

// Tek bir bloğun tüm konfigürasyonunu JSON formatında tutan veri modeli
[Serializable]
public class BlockData
{
    public Vector2Int GridPosition;
    public string ShapeId;
    public BlockColor Color;
    public BlockType Type;
    public MovementAxis MovementAxis;
    public List<RawBehaviourEntry> Behaviours = new List<RawBehaviourEntry>();
}
