using System;
using System.Collections.Generic;

// Bir seviyeyi tanımlayan tüm yapılandirma verisini JSON formatında tutan sınıf
// LevelManager bu sinifi Resources klasorundeki JSON dosyalarindan okur
[Serializable]
public class LevelData
{
    // Grid kac satirdan olusacak (dikey hücre sayısı)
    public int Rows;

    // Grid kac sutundan olusacak (yatay hücre sayısı)
    public int Columns;

    // Bu seviye için saniye cinsinden verilen sure sınırı
    public float TimeLimit;

    // Seviyedeki tüm bloklarin konfigurasyonunu içerir
    public List<BlockData> Blocks = new List<BlockData>();

    // Gridin kenarlarındaki çıkış noktalarını tanımlar
    public List<ExitData> Exits = new List<ExitData>();
}
