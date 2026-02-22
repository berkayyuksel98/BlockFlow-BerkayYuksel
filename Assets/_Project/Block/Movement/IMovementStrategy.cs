using UnityEngine;

// Blok hareket kurali stratejilerinin tanımlayan interface
// Strategy tasarim kalibini kullanarak farkli hareket davranislari çalışma zamaninda değiştirilebilir
public interface IMovementStrategy
{
    // Blogun belirtilen yönde hareket edip edemeyecegini kontrol eder
    bool CanMove(Vector2Int currentPosition, Vector2Int direction, GridManager gridManager);

    // Blogun ilerleme halinde duracagi nihai hedef pozisyonu hesaplar
    Vector2Int CalculateTargetPosition(Vector2Int currentPosition, Vector2Int direction, GridManager gridManager);
}
