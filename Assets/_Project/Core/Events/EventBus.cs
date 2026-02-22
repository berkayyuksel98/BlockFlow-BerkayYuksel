using System;
using System.Collections.Generic;
using UnityEngine;

// Observer Pattern ile event mesajlasma sistemi
// Kullanim: eventBus.Publish(new BlockCollidedEvent())
public class EventBus : IEventBus
{

    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

    // Event yayınla (publish)
    public void Publish<T>(T eventData) where T : IGameEvent
    {
        var eventType = typeof(T);

        if (_subscribers.ContainsKey(eventType))
        {
            var subscribers = _subscribers[eventType];

            // Her subscriber'ı çağır
            foreach (var subscriber in subscribers)
            {
                try
                {
                    ((Action<T>)subscriber)?.Invoke(eventData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"EventBus: Error invoking subscriber for {eventType.Name}: {e}");
                }
            }
        }
    }

    // Event'e abone ol (subscribe)
    public void Subscribe<T>(Action<T> callback) where T : IGameEvent
    {
        var eventType = typeof(T);

        if (!_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType] = new List<Delegate>();
        }

        _subscribers[eventType].Add(callback);
    }

    // Event abonelikten çık (unsubscribe)
    public void Unsubscribe<T>(Action<T> callback) where T : IGameEvent
    {
        var eventType = typeof(T);

        if (_subscribers.ContainsKey(eventType))
        {
            _subscribers[eventType].Remove(callback);
        }
    }

    // Tüm subscriber'ları temizle
    public void ClearAll()
    {
        _subscribers.Clear();
        Debug.Log("EventBus: All subscriptions cleared");
    }
}

