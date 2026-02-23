using UnityEngine;

// Ice davranışına özgü asset verileri; GameConfig üzerinden erişilir
[CreateAssetMenu(fileName = "IceBehaviourConfig", menuName = "BlockFlow/Ice Behaviour Config")]
public class IceBehaviourConfig : ScriptableObject
{
    public Texture2D IceTexture;
}
