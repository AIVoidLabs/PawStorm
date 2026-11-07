---
name: tune-feel
description: PawStorm karakterinin hız ve kontrol hissini Resources/PlayerStats.tres üzerinden ayarlar. Hareket hissi, zıplama, eğim, kamera FOV veya "daha Sonic hissi" istendiğinde kullan.
---

# Tune Feel

Karakterin tüm fizik sabitleri `Resources/PlayerStats.tres` (C# Resource) içindedir. Koda sabit değer yazılmaz. His ayarı yalnızca bu dosyadan yapılır.

## Yöntem
1. `Resources/PlayerStats.tres` dosyasını aç ve ilgili alanı bul.
2. Değeri **küçük adımlarla** değiştir (genelde %10-20).
3. Godot'da sahneyi çalıştırıp hissi test et (gerekirse `godot-build-check` skill'i ile derlemeyi doğrula).
4. Bir değişiklik birden fazla alanı etkileyebilir. Tek seferde en fazla 2-3 alanı değiştir.

## Alan tablosu

| Alan | Ne yapar | "Daha Sonic" için yön |
|---|---|---|
| `MaxSpeed` | Maksimum yatay hız. | Artır. Hız hissi bu alanla gelir. |
| `Acceleration` | Hıza ulaşma hızı. | Artır. Daha çabuk hızlanır. |
| `Deceleration` | Yavaşlama ve fren hızı. | Azalt. Sonic gibi frenlemek yerine kaymak hissi verir. |
| `TurnDrag` | Dönüşte hız kaybı (sürtünme). | Azalt. Dönüşlerde daha az yavaşlar, akış korunur. |
| `JumpVelocity` | Zıplamanın başlangıç hızı. | Artır. Zıplama daha yüksek olur. |
| `JumpCutMultiplier` | Zıplama tuşu erken bırakılınca yükselişin kesilme oranı. | Azalt. Tuş bırakılınca zıplama daha sert kesilir. Çok düşük yapma, kontrol zorlaşır. |
| `Gravity` | Yerçekimi kuvveti. | Artır. Düşüş hızlanır, havada uzun kalma biter. |
| `SlopeForce` | Eğimde hıza eklenen kuvvet. | Artır. İnişte hızlanır, yokuşta yavaşlar. |
| `AirControl` | Havadayken yön kontrolü. | Artır. Havada daha çevik olur. |
| `WindForce` | Rüzgâr akımlarının itme gücü. | Artır. Rüzgâr bölgelerinde daha güçlü yükselir. |
| `FovMin` | Kamera FOV alt sınırı (yavaşken). | Ayarlama gerekmez. Taban görüş açısıdır. |
| `FovMax` | Kamera FOV üst sınırı (hızlıyken). | Artır. Hız arttıkça geniş açı hissi güçlenir. |

## Sık hatalar
- `Gravity` yüksekken `JumpVelocity` da artırılmazsa zıplama kısa kalır.
- `MaxSpeed` artırıldıkça `TurnDrag` ve `AirControl` ile birlikte dengelenmeli. Yoksa karakter kontrolden çıkar.
- `FovMax` çok yüksek olursa görüntü bozulur ve mide bulandırabilir. Makul aralık için küçük adımlarla ilerle.
