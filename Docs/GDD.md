# PawStorm - Oyun Tasarım Dokümanı (GDD)

## Özet
- **Tür:** Sonic tarzı, hız odaklı 3D platform
- **Karakter:** Pati, genç bir kedi
- **Motor / Dil:** Godot 4.x (.NET), C#
- **Platform:** PC
- **Görsel stil:** Cartoon (cel-shaded, kalın outline, doygun renkler)
- **Ton:** Neşeli ve renkli
- **Ana fikir:** Hızlı koş, zıpla, fırtına enerjisini topla.

## Hikâye
Storm Core adlı fırtına enerjisi şehri sarar ve sokak kedileri kaybolur. Pati, yıldırım ve rüzgâr gücüyle Storm Core parçalarını toplamaya çıkar. Kötü karakter Dr. Kennel, fırtına enerjisini çalan komik bir bilim insanı köpektir.

**Final:** Tüm Storm Core parçaları birleşir, Dr. Kennel yenilir ve kediler eve döner.

## Bölgeler
1. Çatılar Şehri
2. Liman
3. Orman
4. Fabrika
5. Gökyüzü Adası + Boss (Dr. Kennel)

### Prototip kapsamı: Bölüm 1 - Çatılar Şehri
- **Rüzgâr akımları:** Pati'yi yükselten ve hızlandıran bölgeler.
- **Uçan eşyalar:** Şemsiye, kutu, tabela gibi rüzgârla hareket eden objeler.
- **İlk Storm Core parçası:** Bölüm sonunda toplanır.

## Mekanikler

### Aşama 1 (prototip)
- Koşu ve momentum
- Zıplama
- Eğim fiziği

### Sonraki aşamalar
- Pati Sprint (Spin Dash)
- Pounce (homing attack)
- Duvar tırmanışı
- Air Twist (çift zıplama)
- Grind: korkuluk, tel ve kiriş üstünde denge
- Loop

### Toplanabilirler ve can
- **Balık:** Halka yerine toplanır.
- **Can sistemi:** "9 Lives".

## Kapsam
- Prototip: tek harita, gri kutu (placeholder) yapılarla.
- Placeholder geometri için Godot primitifleri (Box, Capsule, Sphere, CSG) kullanılır.
- Amaç: hız hissini ve kontrolü doğrulamak. Görsel varlıklar bunun sonrasına bırakılır.

## Riskler
- **His ayarı:** Hız ve kontrol dengesi tutturulmadan içerik eklenmemeli. Tüm değerler `Resources/PlayerStats.tres` içinde ayarlanır.
- **Eğim ve loop fiziği:** `CharacterBody3D` üzerinde zor, sürekli test gerektirir.
- **.NET / Godot sürüm uyumu:** Godot 4.7.2 mono sürümü kullanılıyor. Sürüm uyumsuzluğu derleme ve çalışma sorunlarına yol açabilir.
- **Kapsam şişmesi:** Beş bölgenin tamamı aynı anda yapılmaya çalışılırsa proje dağılır. Önce Bölüm 1 bitirilir.
- **Git LFS:** Büyük binary varlıklar (*.glb, *.png, *.wav, *.ogg) LFS ile tutulur. LFS kurulu değilse dosyalar bozuk gelir.

## Aşama 2 durumu
Spin Dash (Pati Sprint: Shift/X basılı tut, bırak), Pounce (havada Space, kırmızı hedefe kilitlenir), Grind (havada raya yaklaşınca yakalar, Space ile çık) prototipte çalışıyor. Hedefler `PounceTarget`, raylar `GrindRail`. Duvar tırmanışı, Air Twist ve gerçek loop sonraki aşamada.

## Durum (tüm planlanan adımlar prototip düzeyinde tamam)
- **Mekanikler:** Spin Dash, Pounce, Grind, duvar tırmanışı (`climbable` grubu), Air Twist, loop (`LoopTrack`), hasar/9 Lives, balık düşürme, düşman, checkpoint, Storm Core ile bölüm bitişi.
- **Bölgeler (gri kutu):** 1 Çatılar, 2 Liman, 3 Orman, 4 Fabrika, 5 Gökyüzü Adası, 6 Dr. Kennel boss. Sahne sırası `Scripts/Core/Levels.cs`. Bölüm 2-6 `Docs/tools/gen_levels.ps1` ile üretilir (elle düzenlenirse betik tekrar çalıştırılınca ezilir).
- **Cilalama:** toon gölgelendirme, ters-gövde outline (`Art/Materials/Outline.*`), placeholder sesler (`Docs/tools/make_sfx.ps1`), ana menü, duraklatma (Esc/Start).
- **Eksik:** gerçek modeller/animasyon, müzik, ekran-uzayı outline (küpsel zeminde outline yok), ayarlar menüsü, kaydetme, bölüm seçimi, gerçek boss çeşitliliği.
