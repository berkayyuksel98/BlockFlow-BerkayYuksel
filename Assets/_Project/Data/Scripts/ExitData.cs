using System;

// Gridin bir kenarındaki çıkış noktasını tanımlayan veri sınıfı
// Hangi renk bloğun hangi kenardan hangi hücreden başlayarak ne kadar genişlikte çıkacağını belirtir
[Serializable]
public class ExitData
{
    public BlockColor Color;

    public ExitSide Side;

    // Kenar başlangıç cell indeksi
    public int StartIndex;

    // Çıkış genişliği (kaç cell kapladığı)
    public int Size = 1;
}
