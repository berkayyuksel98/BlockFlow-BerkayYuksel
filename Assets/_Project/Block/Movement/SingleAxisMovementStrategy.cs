using UnityEngine;

// Blogun yalnızca belirlenmis eksende hareket etmesini kısıtlayan strateji
// Yatay blok sadece horizontal dikey blok sadece vertical hareket edebilir
public class SingleAxisMovementStrategy : IMovementStrategy
{
    // Bu blogun izin verilen tek hareket ekseni
    private readonly MovementAxis _allowedAxis;

    // Izin verilen eksen dışarıdan atanır (BlockData.MovementAxis kaynaklidir)
    public SingleAxisMovementStrategy(MovementAxis allowedAxis)
    {
        _allowedAxis = allowedAxis;
    }

    // Yalnizca izin verilen eksende verilen yon gecerliyse true döndürür
    // Diger yone yapılan sürükle hareketi sessizce reddedilir
    public bool CanMove(Vector2Int currentPosition, Vector2Int direction, GridManager gridManager)
    {
        return false;
    }

    // Izin verilen eksende çarpma veya sınıra kadar en uzak pozisyonu hesaplar
    public Vector2Int CalculateTargetPosition(Vector2Int currentPosition, Vector2Int direction, GridManager gridManager)
    {
        return Vector2Int.zero;
    }
}
