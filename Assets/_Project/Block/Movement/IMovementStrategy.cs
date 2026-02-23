using System.Collections.Generic;
using UnityEngine;

// Blok hareket kuralı stratejilerinin tanımlayan interface
// Strategy tasarım kalıbını kullanarak farklı hareket davranışları çalışma zamanında değiştirilebilir
public interface IMovementStrategy
{
    // Bloğun belirtilen yönde hareket edip edemeyeceğini kontrol eder (eksen kısıtlaması)
    bool CanMove(Vector2Int direction);
}
