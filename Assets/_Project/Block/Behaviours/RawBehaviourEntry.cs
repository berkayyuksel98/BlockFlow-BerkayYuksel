using System;

// JSON polimorfizmi için tür bilgisi + seri hale getirilmiş data tutan kapsayıcı
[Serializable]
public class RawBehaviourEntry
{
    public BlockBehaviourType Type;
    public string DataJson;
}
