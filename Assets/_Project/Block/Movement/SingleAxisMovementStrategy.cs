using System.Collections.Generic;
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

    // Yalnızca izin verilen eksendeki yönleri kabul eder
    public bool CanMove(Vector2Int direction)
    {
        if (_allowedAxis == MovementAxis.Horizontal)
            return direction.y == 0; // sadece sol/sağ

        if (_allowedAxis == MovementAxis.Vertical)
            return direction.x == 0; // sadece yukarı/aşağı

        return true; // Free ise her yön
    }
}
