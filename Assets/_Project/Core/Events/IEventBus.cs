using System;


// Zenject ile IEventBus bağlanır gercek sınıf disa kapalı kalir
public interface IEventBus
{
    // Belirtilen event tipine abone ol
    void Subscribe<T>(Action<T> callback) where T : IGameEvent;

    // Belirtilen event tipinden aboneligi kaldır
    void Unsubscribe<T>(Action<T> callback) where T : IGameEvent;

    // Belirtilen event'i tüm abonelere yayınla
    void Publish<T>(T eventData) where T : IGameEvent;
}
