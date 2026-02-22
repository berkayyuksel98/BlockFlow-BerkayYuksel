using System;
using UnityEngine;
using Zenject;

// EventBus üzerinden gelen eventlere göre partikül efektlerini oynatan servis sinifi
// Object Pooling kullanarak efekt nesnelerini verimli şekilde yeniden kullanir
public class VFXSystem : IInitializable, IDisposable
{
    private readonly IEventBus _eventBus;

    public void Initialize()
    {
    }

    // Abonelikleri ve pool nesneslerini temizler
    public void Dispose()
    {
    }
}
