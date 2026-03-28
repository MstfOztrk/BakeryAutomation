# BakeryAutomation

<p align="center">
  <img src="BakeryAutomation/Resources/Images/logo.png" alt="BakeryAutomation logo" width="180" />
</p>

Windows masaustu (WPF) ve SQLite tabanli firin otomasyonu. Urun, cari, sevkiyat, iade, tahsilat ve raporlama sureclerini tek uygulamada yonetmek icin gelistirildi.

## Ozellikler
- Urunler: ekle, sil, guncelle, fiyat gecmisi
- Subeler / cariler: kart, vade bilgisi, kredi limiti, subeye ozel fiyat
- Sevkiyat: batch bazli gonderim, ayni fis iadesi, zayi, urun ve batch iskonto
- Iadeler: sonradan gelen urunler icin ayri iade fisi, sevkiyata bagli veya bagimsiz iade
- Tahsilat: sube bazli odeme girisi, fise bagli kalan tutar kontrolu
- Raporlar: gunluk ozet, cari ekstre, devreden bakiye, CSV export
- Ayarlar: veri dosyasi konumu, yedekle, geri yukle

## Teknoloji
- .NET 8
- WPF
- SQLite
- xUnit

## Kurulum
1. Windows'ta Visual Studio 2022 ile `BakeryAutomationApp.sln` ac.
2. Build Configuration olarak `Release` sec.
3. Build / Run.

## Publish
Standart release cikisi icin hazir profil:

```powershell
dotnet publish BakeryAutomation\BakeryAutomation.csproj -c Release /p:PublishProfile=WinX64
```

Varsayilan publish klasoru:
`BakeryAutomation\bin\Release\net8.0-windows\publish\win-x64\`

Release oncesi kontrol listesi:
`docs/RELEASE_CHECKLIST.md`

## Veri
Uygulama veriyi su dosyada tutar:
`%AppData%\BakeryAutomation\bakery.db`

Ek ayar dosyasi:
`%AppData%\BakeryAutomation\settings.json`

## Notlar
- Veritabani olarak SQLite kullanilir.
- Para birimi alanlari `decimal`.
- Iskonto alanlari yuzde (`%`) olarak tutulur.
