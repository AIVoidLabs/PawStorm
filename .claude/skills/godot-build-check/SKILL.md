---
name: godot-build-check
description: PawStorm'da kod değişikliğinden sonra C# derlemesini ve Godot headless açılışını kontrol eder. Kod yazıldıktan sonra veya kullanıcı derleme kontrolü istediğinde kullan.
---

# Godot Build Check

Kod değişikliğinden sonra aşağıdaki adımları sırayla çalıştır.

## Adımlar

1. Proje kök dizininde C# derlemesini yap:
   ```
   dotnet build
   ```

2. Derleme başarılıysa Godot'yu headless modda aç ve kapat (proje yüklenebiliyor mu kontrolü):
   ```
   "C:\Projects\External\AIVoidLabs\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --path . --quit
   ```

3. Hataları kısaca özetle: dosya, satır ve hata mesajı. Uyarılar ayrıca belirtilir ama hata sayılmaz.

4. Hata varsa düzelt ve adımları tekrar çalıştır. Derlenmeyen kod bırakılmaz.

## Notlar
- Komutlar proje kök dizininde (`PawStorm/`) çalıştırılır.
- Godot yolu değişirse bu dosyadaki yol güncellenir.
