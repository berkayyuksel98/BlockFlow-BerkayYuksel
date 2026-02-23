// Zenject yerine manuel pool kullanılan bloklar için spawn/despawn hook'ları
public interface IPoolableBlock
{
    void OnSpawned();
    void OnDespawned();
}
