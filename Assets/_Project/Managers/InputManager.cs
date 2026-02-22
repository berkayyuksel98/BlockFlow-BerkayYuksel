using UnityEngine;
using Zenject;

// Dokunmatik ekran girdisini algılayan sürükle bırak komutunu ilgili bloğa ileten sınıf
public class InputManager : ITickable
{
    private readonly GridManager _gridManager;
    private readonly IEventBus _eventBus;

    [Inject]
    public InputManager(GridManager gridManager, IEventBus eventBus)
    {
        _gridManager = gridManager;
        _eventBus = eventBus;
    }

    public void Tick()
    {
    }
}
