using UnityEngine;
using Zenject;

// RawBehaviourEntry'den doğru IBlockBehaviour örneğini üretir; yeni davranış için sadece buraya case eklenir
public class BlockBehaviourFactory
{
    private readonly GameConfig gameConfig;

    [Inject]
    public BlockBehaviourFactory(GameConfig gameConfig)
    {
        this.gameConfig = gameConfig;
    }

    public IBlockBehaviour Create(RawBehaviourEntry entry)
    {
        switch (entry.Type)
        {
            case BlockBehaviourType.Ice:
                var data = string.IsNullOrEmpty(entry.DataJson) ? new IceBehaviourData() : JsonUtility.FromJson<IceBehaviourData>(entry.DataJson);
                return new IceBehaviour(data, gameConfig.IceBehaviourConfig);
            default:
                Debug.LogError($"[BlockBehaviourFactory] Bilinmeyen davranış türü: {entry.Type}");
                return null;
        }
    }
}
