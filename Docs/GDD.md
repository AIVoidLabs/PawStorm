# PawStorm - Oyun TasarÄ±m DokÃ¼manÄ± (GDD)

## Ã–zet
- **TÃ¼r:** Sonic tarzÄ±, hÄ±z odaklÄ± 3D platform
- **Karakter:** Pati, genÃ§ bir kedi
- **Motor / Dil:** Godot 4.x (.NET), C#
- **Platform:** PC
- **GÃ¶rsel stil:** Cartoon (cel-shaded, kalÄ±n outline, doygun renkler)
- **Ton:** NeÅŸeli ve renkli
- **Ana fikir:** HÄ±zlÄ± koÅŸ, zÄ±pla, fÄ±rtÄ±na enerjisini topla.

## HikÃ¢ye
Storm Core adlÄ± fÄ±rtÄ±na enerjisi ÅŸehri sarar ve sokak kedileri kaybolur. Pati, yÄ±ldÄ±rÄ±m ve rÃ¼zgÃ¢r gÃ¼cÃ¼yle Storm Core parÃ§alarÄ±nÄ± toplamaya Ã§Ä±kar. KÃ¶tÃ¼ karakter Dr. Kennel, fÄ±rtÄ±na enerjisini Ã§alan komik bir bilim insanÄ± kÃ¶pektir.

**Final:** TÃ¼m Storm Core parÃ§alarÄ± birleÅŸir, Dr. Kennel yenilir ve kediler eve dÃ¶ner.

## BÃ¶lgeler
1. Ã‡atÄ±lar Åžehri
2. Liman
3. Orman
4. Fabrika
5. GÃ¶kyÃ¼zÃ¼ AdasÄ± + Boss (Dr. Kennel)

### Prototip kapsamÄ±: BÃ¶lÃ¼m 1 - Ã‡atÄ±lar Åžehri
- **RÃ¼zgÃ¢r akÄ±mlarÄ±:** Pati'yi yÃ¼kselten ve hÄ±zlandÄ±ran bÃ¶lgeler.
- **UÃ§an eÅŸyalar:** Åžemsiye, kutu, tabela gibi rÃ¼zgÃ¢rla hareket eden objeler.
- **Ä°lk Storm Core parÃ§asÄ±:** BÃ¶lÃ¼m sonunda toplanÄ±r.

## Mekanikler

### AÅŸama 1 (prototip)
- KoÅŸu ve momentum
- ZÄ±plama
- EÄŸim fiziÄŸi

### Sonraki aÅŸamalar
- Pati Sprint (Spin Dash)
- Pounce (homing attack)
- Duvar tÄ±rmanÄ±ÅŸÄ±
- Air Twist (Ã§ift zÄ±plama)
- Grind: korkuluk, tel ve kiriÅŸ Ã¼stÃ¼nde denge
- Loop

### Toplanabilirler ve can
- **BalÄ±k:** Halka yerine toplanÄ±r.
- **Can sistemi:** "9 Lives".

## Kapsam
- Prototip: tek harita, gri kutu (placeholder) yapÄ±larla.
- Placeholder geometri iÃ§in Godot primitifleri (Box, Capsule, Sphere, CSG) kullanÄ±lÄ±r.
- AmaÃ§: hÄ±z hissini ve kontrolÃ¼ doÄŸrulamak. GÃ¶rsel varlÄ±klar bunun sonrasÄ±na bÄ±rakÄ±lÄ±r.

## Riskler
- **His ayarÄ±:** HÄ±z ve kontrol dengesi tutturulmadan iÃ§erik eklenmemeli. TÃ¼m deÄŸerler `Resources/PlayerStats.tres` iÃ§inde ayarlanÄ±r.
- **EÄŸim ve loop fiziÄŸi:** `CharacterBody3D` Ã¼zerinde zor, sÃ¼rekli test gerektirir.
- **.NET / Godot sÃ¼rÃ¼m uyumu:** Godot 4.7.2 mono sÃ¼rÃ¼mÃ¼ kullanÄ±lÄ±yor. SÃ¼rÃ¼m uyumsuzluÄŸu derleme ve Ã§alÄ±ÅŸma sorunlarÄ±na yol aÃ§abilir.
- **Kapsam ÅŸiÅŸmesi:** BeÅŸ bÃ¶lgenin tamamÄ± aynÄ± anda yapÄ±lmaya Ã§alÄ±ÅŸÄ±lÄ±rsa proje daÄŸÄ±lÄ±r. Ã–nce BÃ¶lÃ¼m 1 bitirilir.
- **Git LFS:** BÃ¼yÃ¼k binary varlÄ±klar (*.glb, *.png, *.wav, *.ogg) LFS ile tutulur. LFS kurulu deÄŸilse dosyalar bozuk gelir.

## AÅŸama 2 durumu
Spin Dash (Pati Sprint: Shift/X basÄ±lÄ± tut, bÄ±rak), Pounce (havada Space, kÄ±rmÄ±zÄ± hedefe kilitlenir), Grind (havada raya yaklaÅŸÄ±nca yakalar, Space ile Ã§Ä±k) prototipte Ã§alÄ±ÅŸÄ±yor. Hedefler `PounceTarget`, raylar `GrindRail`. Duvar tÄ±rmanÄ±ÅŸÄ±, Air Twist ve gerÃ§ek loop sonraki aÅŸamada.

## Durum (tÃ¼m planlanan adÄ±mlar prototip dÃ¼zeyinde tamam)
- **Mekanikler:** Spin Dash, Pounce, Grind, duvar tÄ±rmanÄ±ÅŸÄ± (`climbable` grubu), Air Twist, loop (`LoopTrack`), hasar/9 Lives, balÄ±k dÃ¼ÅŸÃ¼rme, dÃ¼ÅŸman, checkpoint, Storm Core ile bÃ¶lÃ¼m bitiÅŸi.
- **BÃ¶lgeler (gri kutu):** 1 Ã‡atÄ±lar, 2 Liman, 3 Orman, 4 Fabrika, 5 GÃ¶kyÃ¼zÃ¼ AdasÄ±, 6 Dr. Kennel boss. Sahne sÄ±rasÄ± `Scripts/Core/Levels.cs`. BÃ¶lÃ¼m 2-6 `Docs/tools/gen_levels.ps1` ile Ã¼retilir (elle dÃ¼zenlenirse betik tekrar Ã§alÄ±ÅŸtÄ±rÄ±lÄ±nca ezilir).
- **Cilalama:** toon gÃ¶lgelendirme, ters-gÃ¶vde outline (`Art/Materials/Outline.*`), placeholder sesler (`Docs/tools/make_sfx.ps1`), ana menÃ¼, duraklatma (Esc/Start).
- **Eksik:** gerÃ§ek modeller/animasyon, mÃ¼zik, ekran-uzayÄ± outline (kÃ¼psel zeminde outline yok), ayarlar menÃ¼sÃ¼, kaydetme, bÃ¶lÃ¼m seÃ§imi, gerÃ§ek boss Ã§eÅŸitliliÄŸi.

## Modeller
Pati, Pup (düþman), Dr. Kennel, Balýk, Storm Core, Checkpoint, Sandýk ve Aðaç Blender ile betikle üretilir: `Docs/tools/blender/make_models.py` -> `Art/Models/*.glb`. Oyunda `ToonModel` betiði toon + outline uygular. Animasyon yok (statik poz), kemik kurulumu sonraki iþ.

## Animasyon
Pati (Idle, Run, Jump, Fall, Ball, Charge, Hurt, Grind, Climb), Pup (Walk), Dr. Kennel (Hover, Charge, Stunned) ve Balýk (Swim) Blender'da kemiklendirilip klipleri GLB'ye gömülür (`make_models.py`). Oyunda `ModelAnimator` durumlara göre klip seçer. Katý parçalar tek kemiðe aðýrlýklýdýr (yumuþak deri yok).

## Ortam kiti
`Docs/tools/blender/make_environment.py` -> `Art/Models/Env/*.glb`: zemin ve rampa karolarý, kenar korkuluklarý, dekor (bina, vinç, fener, kaya, boru, gaz bacasý, bulut...). `LevelDresser` CSG zeminlerini (metadata `skin`) karo MultiMesh'iyle kaplar, `BarrierLine` iki kenara korkuluk + görünmez çarpýþma duvarý koyar (layer 2; oyuncu maskesi 3, kamera kolu çarpmaz). Bölüm 2-6 `gen_levels.ps1` ile üretilir.

## Ses ve menü
Ses: `Docs/tools/audio/make_audio.py` (Blender'ýn Python'u + numpy ile sentez) -> `Audio/Music` (menü, 5 bölge, boss; 16 ölçülük döngüler), `Audio/Ambient` (rüzgâr, dalga, fabrika uðultusu, orman kuþlarý...), ek efektler. `AudioSettings` Music/SFX/Ambient bus'larýný açar ve ayarý `user://settings.cfg`'ye kaydeder; `Music` bölümler arasý çapraz geçiþ yapar. Menü: `Art/Textures/Menu/*.png` (`Docs/tools/capture_scene.gd` ile oyundan çekilir), `MainMenu` + `UiStyle`.

## Bölüm 7-9 (geniþleme)
- **7 Kristal Maðaralar:** dikey bölüm. Düþme -> týrmanma -> düþme -> iki aþamalý týrmanma. Neon kristaller, sis, karanlýk maðara.
- **8 Antik Tapýnak:** týrman, düþ, týrman, ray ile iniþ (rayý kaçýrýrsan alt zeminden týrmanarak devam).
- **9 Fýrtýna Kalesi - General Growl:** menzilli boss. Lazer toplarý atar (tek, üçlü yay, halka). Topu Pounce ile ya da saldýrýrken dokunarak geri çevir (boss'a hasar), ya da bir salvo sonrasý ýsýnýp sersemlediðinde ona vur (6 can). Hikâye: Dr. Kennel yenilince asýl fýrtýna sahibi General Growl ortaya çýkar.
- Bölüm 6 artýk final deðil; oyun 9. bölümde biter.
