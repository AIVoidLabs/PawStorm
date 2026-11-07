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
