# PawStorm

Sonic tarzı, hız odaklı 3D platform oyunu. Karakter: genç bir kedi (Pati).
Motor: Godot 4.x (.NET sürümü), dil: C#. Platform: PC.
Görsel stil: cartoon (cel-shaded, kalın outline, doygun renkler). Ton: neşeli ve renkli.

## Hikâye (kısa)
Storm Core adlı fırtına enerjisi şehri sarar, sokak kedileri kaybolur. Pati yıldırım/rüzgâr
gücüyle Storm Core parçalarını toplar. Kötü karakter: Dr. Kennel (fırtına enerjisini çalan
komik bilim insanı köpek). 5 bölge: Çatılar, Liman, Orman, Fabrika, Gökyüzü Adası + boss.

## Mekanikler (Sonic'ten kediye uyarlama)
- Pati Sprint (spin dash), Pounce (homing attack), duvar tırmanışı, Air Twist (çift zıplama),
  korkuluk/tel/kiriş üstünde denge (grind), eğim fiziği, loop.
- Toplanabilir: Balık (halka yerine). Can sistemi: "9 Lives".

## Kurallar
- Tüm fizik sabitleri `Resources/PlayerStats.tres` (C# Resource) içinde, koda gömülmez.
- Karakter: `CharacterBody3D` + State Machine (Idle, Run, Jump, Spin, Grind, Hurt...).
- İsimlendirme: sınıflar PascalCase, `partial class`, `[Export]` alanlar, sinyaller `EventHandler`.
- Klasörler: `Scripts/{Player,Camera,Enemies,UI,Core}`, `Scenes/{Levels,Player,Props,UI}`,
  `Art/{Models,Materials,Textures}`, `Audio`, `Resources`, `Docs`.
- Her değişiklikten sonra `dotnet build` çalıştır, hata varsa düzelt. Derlenmeyen kod bırakma.
- Büyük dosyayı baştan yazma, `Edit` ile küçük değişiklik yap.
- Yeni paket/eklenti ekleme, önce bana sor.
- Placeholder varlıklar için Godot primitifleri (Box/Capsule/Sphere mesh, CSG) kullan.

## Token ekonomisi
- Varsayılan olarak subagent kullanma. Sadece birbirinin dosyasına dokunmayan, paralel
  işlerde en fazla 2 subagent aç (ucuz model: sonnet/haiku). Fizik ve mimari kararlar ana oturumda.
- Subagent görevleri dar olsun: dosya yolu, beklenen çıktı, kısa rapor (max 10 satır).
- Tüm projeyi taramak yerine hedefli Grep/Glob kullan.