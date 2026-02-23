using System;

// Ice davranışına ait kayıt verisi; kaç blok çıkınca kilit açılacağını tutar
[Serializable]
public class IceBehaviourData
{
    public int RequiredExitCount = 1;
}
