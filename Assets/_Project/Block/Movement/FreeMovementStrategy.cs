using UnityEngine;

// Normal bloklarin dort yönde de serbestce hareket etmesini sağlayan strateji
// IMovementStrategy arayuzunu uygular herhangi bir eksen kısıtlaması yoktur
public class FreeMovementStrategy : IMovementStrategy
{
    // Blok herhangi bir yönde (horizontal,vertical) hareket edebilir
    public bool CanMove(Vector2Int currentPosition, Vector2Int direction, GridManager gridManager)
    {
        return false;
    }

    // Blogun durdurulacağı en uzak hücreyi hesaplar
    // Carpana veya sınıra ulasana kadar adim adim ilerleyerek son gecerli pozisyonu döndürür
    public Vector2Int CalculateTargetPosition(Vector2Int currentPosition, Vector2Int direction, GridManager gridManager)
    {
        return Vector2Int.zero;
    }
}
