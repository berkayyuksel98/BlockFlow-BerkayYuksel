using System.Collections.Generic;
using UnityEngine;

// Normal bloklarin dort yönde de serbestce hareket etmesini sağlayan strateji
// IMovementStrategy arayüzunu uygular herhangi bir eksen kısıtlaması yoktur
public class FreeMovementStrategy : IMovementStrategy
{
    // Blok herhangi bir yönde (horizontal, vertical) hareket edebilir
    public bool CanMove(Vector2Int direction)
    {
        return true;
    }
}
