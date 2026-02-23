// Bloklarin görünen renk çeşitleri
public enum BlockColor
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
}

// Bloklarin davranış şekline göre siniflandirilmasi
public enum BlockType
{
    Normal     = 0,
    SingleAxis = 2,
}

// SingleAxis tipi bloklarin kısıtlı oldugu hareket ekseni
public enum MovementAxis
{
    Horizontal, // yalnızca sağ ve sol hareket
    Vertical,   // yalnızca yukarı ve aşağı hareket
    Free        // her yönde serbestce hareket (Normal bloklar için)
}

// Oyunun genel yaşam döngüsü boyunca bulunabilecegi durumlar
public enum GameState
{
    Idle,       // oyun henüz başlamadı
    Playing,    // oyun aktif olarak devam ediyor
    Paused,     // oyun kullanıcı tarafindan duraklatildi
    Win,        // oyuncu tüm bloklari çıkardı
    Lose        // oyuncunun süresi doldu başarısz sayılır
}

// Gridin hangi kenarında çıkış noktası bulunduğunu tanımlar
public enum ExitSide
{
    Top,    // üst kenar
    Bottom, // alt kenar
    Left,   // sol kenar
    Right   // sağ kenar
}
