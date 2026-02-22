using System;

// Gridin bir kenarındaki çıkış noktasını tanımlayan veri sınıfı
// Hangi renk bloğun hangi kenardan hangi hücreden başlayarak ne kadar genişlikte çıkacağını belirtir
[Serializable]
public class ExitData
{
    // Bu çıkışı kullanabilecek bloğun rengi
    public BlockColor Color;

    // Çıkışın bulunduğu kenar (üst alt sol sağ)
    public ExitSide Side;

    // Kenar boyunca 0 tabanlı başlangıç hücre indeksi
    // Üst/alt kenarda sütun indeksi sol/sağ kenarda satır indeksine karşılık gelir
    public int StartIndex;

    // Çıkışın kapladığı hücre sayısı
    // Üst/alt kenarda sütun genişliği sol/sağ kenarda satır yüksekliğidir
    public int Size = 1;
}
