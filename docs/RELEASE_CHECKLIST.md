# Release Checklist

## Yayin Oncesi
- `dotnet build BakeryAutomationApp.sln -c Release`
- `dotnet test BakeryAutomationApp.sln -c Release`
- Test makinesinde mevcut `%AppData%\BakeryAutomation\bakery.db` icin manuel yedek al
- Ilk acilista migration ve otomatik backup akisinin calistigini kontrol et

## Smoke Test
- Yeni sevkiyat kaydi ac, urun ekle, kaydet
- Kredi limitli bir cari icin limit asimi uyarisini kontrol et
- Ayni sevkiyat numarasi ile ikinci fis kaydinin engellendigini kontrol et
- Iade ekraninda ayni iade no ile tekrar kayit acilamadigini kontrol et
- Upgrade edilen eski veritabaninda duplicate fis/iade no varsa ilk acilista otomatik yedek + yeniden numaralandirma logunu kontrol et
- Fise bagli tahsilatta kalan tutar sinirinin korundugunu kontrol et
- Backup al, restore et, uygulamanin kontrollu sekilde kapandigini dogrula

## Paketleme
- `dotnet publish BakeryAutomation\BakeryAutomation.csproj -c Release /p:PublishProfile=WinX64`
- Cikti klasorunu temiz bir makinede ac ve uygulama acilisini dogrula
- `%AppData%\BakeryAutomation\Logs` altina hata logu dusmeden temel akislari gec

## Teslim
- Publish klasorunu versiyon etiketi ile arsivle
- Veritabani yedegi ve release notlarini ayni teslim paketine ekle
