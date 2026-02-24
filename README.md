# BlockFlow

Blokları sürükleyerek doğru renkteki çıkışa göndermeye dayanan 3D bir bulmaca oyunu

---

## Oyun Nasıl Çalışıyor

Ekranda bir grid bulunur ve bu gridin kenarlarında renkli çıkış noktaları yer alır
Aynı renkteki bloğu sürükleyerek o renge ait çıkışa ulaştırmak gerekir
Tüm bloklar çıktığında seviye tamamlanır süre bittiğinde ise seviye kaybedilir
Bazı bloklar yalnızca yatay ya da yalnızca dikey hareket edebilirken bazıları her yöne serbeşce hareket edebilir

---

## Klasör Yapısı 

```
Assets/_Project/
├── AudioController/     ses sistemi
├── Block/               blok mantığı fabrika ve görseller
├── Camera/              kamera konumlandırma
├── Core/                event sistemi installer ve ortak yapılar
├── Data/                veri modelleri ve GameConfig
├── Editor/              level editörü penceresi
├── Grid/                grid yönetimi ve exit görünümleri
├── Level/               level yükleme ve timer
├── UI/                  panel yönetimi
└── VFXSystem/           particle efektleri
```

---

## Önemli Sınıflar 

**GameConfig**
Projedeki tek merkezi ScriptableObject
Blok renkleri level JSON dosyaları shape verileri exit prefabları duvar prefabları ses ve VFX configleri burada toplanıyor
Bir şeyi değiştirmek istediğinde ilk bakman gereken yer burası

**GridManager**
Grid mantığını tamamen yönetiyor
Hücre doluluğunu takip ediyor blokların nereye gidebileceğini hesaplıyor ve çıkış eşleşmelerini kontrol ediyor
Aynı zamanda duvar exit ve zemin objelerini object pool ile spawn ve despawn ediyor

**BlockFacade**
Her bloğun sahne tarafındaki yüzü
Sürükleme girişini alıyor hareketi hesaplıyor ve çıkış tetiklendiğinde animasyonu başlatıyor
IPoolableBlock interface'ini implement ediyor bu sayede pool'dan çıkarken ve geri dönerken temiz bir state garantisi var

**BlockFactory**
Shape bazlı object pool kullanıyor
Aynı shape tipinden bir blok gerektiğinde önce pool'a bakıyor yoksa yeni instantiate ediyor
Level bitiminde blokları destroy etmiyor sadece pool'a geri gönderiyor

**LevelManager**
JSON dosyasını okuyup LevelData'ya çeviriyor ve GridManager'a bildiriyor
Tüm blokların çıkışını sayıyor sıfıra düştüğünde kazanma eventini yayınlıyor
Aynı zamanda geri sayım timer'ını UniTask ile yönetiyor süre bitince TimerExpiredEvent yayınlıyor

**EventBus**
Sistemler arasındaki iletişimi struct tabanlı event'lerle sağlıyor
Hiçbir sistem bir diğerine direkt referans tutmuyor bu sayede bağımlılıklar minimize edilmiş

**UIManager**
EventBus'a subscribe olup LevelLoadedEvent ve LevelCompletedEvent'e göre panelleri açıp kapatıyor
Panel geçişlerinde UniTask ile bekleme yapıyor ve CancellationToken ile oyun kapandığında güvenli şekilde iptal ediyor

**CameraCalculator**
GridBuiltEvent geldiği anda grid boyutuna ve ayarladığın pitch açısına göre kamerayı otomatik konumlandırıyor
FOV tabanlı basit bir formül kullanıyor Inspector'dan padding ve z offset ayarlayabiliyorsun

**VFXSystem**
particle prefablarını generic bir Dictionary pool ile yönetiyor
BlockExitStartedEvent ve LevelCompletedEvent'e subscribe oluyor

**AudioController**
Pure C# sınıfı MonoBehaviour değil
EventBus üzerindeki olayları dinleyerek ilgili ses efektini AudioConfig üzerinden çalıyor

---

## Design Pattern'lar ve Teknolojiler

**Zenject**
Tüm sistemler constructor injection ile oluşturulur
MonoBehaviour olmayan pure C# sınıfları Zenject tarafından new'lenir ve bağımlılıkları otomatik enjekte edilir
GameplayInstaller sahneye bağlı tek kurulum noktasıdır

**EventBus (Observer Pattern)**
IGameEvent interface'ini implement eden struct'lar yayınlanıp dinlenebilir
Sistemler birbirini tanımak zorunda değildir yalnızca event tipini bilmesi yeterlidir

**Object Pooling**
BlockFactory bloklar için shape bazlı pool kullanır
GridManager exit duvar köşe ve zemin objeleri için prefab bazlı generic Dictionary pool kullanır
VFXSystem partikül prefabları için aynı yaklaşımı uygular

**Strategy Pattern**
IMovementStrategy interface'i ile blok hareket davranışı runtime'da değiştirilebilir
SingleAxisMovementStrategy yatay ya da dikey ekseni kısıtlar
FreeMovementStrategy her yönde serbest bırakır

**Facade Pattern**
BlockFacade dışarıya yalnızca temiz bir API sunar
İçinde hareket stratejisi görseller behaviour'lar ve event yayını kapsüllenir

**Factory Pattern**
BlockFactory ve BlockBehaviourFactory veri modelinden somut objeyi üretir

**Decorator Pattern (Behaviour Sistemi)**
IBlockBehaviour interface'ini implement eden sınıflar blok üzerine eklenerek davranış katmanları oluşturulur
Örneğin IceBehaviour bloğa buz kırılma mekaniği ekler buz kalkmadan blok hareket edemez

**UniTask**
Coroutine yerine tercih edilir
Spawn animasyonları arasındaki bekleme timer ve panel geçiş gecikmelerinde kullanılır

**DOTween**
Blok spawn scale animasyonu çıkış animasyonu ve UI panel animasyonlarında kullanılır

---

## Yeni Level Nasıl Oluşturulur

Unity editöründe menü çubuğunda BlockFlow > Level Editor penceresi açılır

Açılan pencerede grid boyutu belirlenir kaç satır kaç sütun olacağı girilir

Blok ekle bölümünden ShapeId renk tip ve başlangıç pozisyonu seçilerek blok eklenir
ShapeId GameConfig üzerindeki BlockShapeDatas listesindeki id ile eşleşmelidir

Exit ekle bölümünden hangi kenara hangi renk için hangi hücreden başlayan ve kaç hücre genişliğinde çıkış istendiği belirtilir

TimeLimit alanına saniye cinsinden süre girilebilir 0 bırakılırsa süre limiti olmadan oynanır

Kaydet butonuna basıldığında bir JSON dosyası oluşturulur

Bu JSON dosyası GameConfig üzerindeki Levels listesine TextAsset olarak eklenir

Sıralama önemlidir LevelManager listedeki index sırasına göre level yükler ve liste bittiğinde başa döner

---

## JSON Level Formatı 

```json
{
  "Rows": 4,
  "Columns": 4,
  "TimeLimit": 60,
  "Blocks": [
    {
      "GridPosition": { "x": 0, "y": 1 },
      "ShapeId": "1x1",
      "Color": 0,
      "Type": 0,
      "MovementAxis": 2,
      "Behaviours": []
    }
  ],
  "Exits": [
    {
      "Color": 0,
      "Side": 3,
      "StartIndex": 1,
      "Size": 1
    }
  ]
}
```

Color değerleri sırasıyla Red 0 Blue 1 Green 2 Yellow 3 Purple 4
Side değerleri Top 0 Bottom 1 Left 2 Right 3
Type değerleri Normal 0 SingleAxis 2
MovementAxis değerleri Horizontal 0 Vertical 1 Free 2
