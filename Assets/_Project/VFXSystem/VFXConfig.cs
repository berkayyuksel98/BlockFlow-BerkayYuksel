using UnityEngine;

// VFX sistemine ait asset referansları ve ayarları
[CreateAssetMenu(fileName = "VFXConfig", menuName = "BlockFlow/VFX Config")]
public class VFXConfig : ScriptableObject
{
    public ParticleSystem exitParticlePrefab,confettiParticlePrefab;
    public int PoolSize = 8;
}
    