using System;
using UnityEngine;

// Tek bir blogun tüm konfigurasyonunu JSON formatında tutan veri modeli
[Serializable]
public class BlockData
{
    // Blogun grid uzerindeki başlangıç koordinatı (x sütun y satır)
    public Vector2Int GridPosition;

    // Bloğun şeklini tanımlayan BlockShapeData assetinin ID'si
    public string ShapeId;

    // Blogun rengi hem görünüm hem eşleşme mantigi için kullanılır
    public BlockColor Color;
    public BlockType Type;

    // SingleAxis tipi bloklarda hangi eksende hareket edebildigi
    public MovementAxis MovementAxis;

    // IcedBlock tipi için kaç başarılı çıkış gerektiği (0 olunca blok kırılır)
    public int IceHealth;
}
