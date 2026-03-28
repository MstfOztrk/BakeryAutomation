# Kullanim ve Devir Notlari

Bu not, uygulamayi ilk kez kullanacak kisiye hizli egitim vermek ve canli kullanima guvenli gecis yapmak icin hazirlandi.

## 1. Uygulamayi nasil kullanmalarini oneririm

En saglikli kullanim sirasi su:

1. `Ayarlar` ekranina girip veri klasorunu goster.
2. `Yedek Al` ile ilk manuel yedegi al.
3. `Urunler` ekraninda aktif urunleri ve liste fiyatlarini kontrol et.
4. `Subeler / Cariler` ekraninda cari kartlarini, vade gununu, kredi limitini ve gerekirse ozel fiyatlari kontrol et.
5. Gunluk operasyonu agirlikli olarak `Sevkiyat`, `Tahsilat`, `Iadeler`, `Raporlar` sirasi ile yonet.

Sebep:

- Sevkiyat yeni borc olusturur.
- Iade bu borcu azaltir.
- Tahsilat kalan bakiyeyi kapatir.
- Raporlar en son kontrol ekrani gibi kullanilmalidir.

## 2. Kullaniciya anlatilacak gunluk akis

### Sabah acilis

- Uygulama acildiginda otomatik backup aliyor; yine de ilk gunlerde manuel yedek almayi aliskanlik yapin.
- `Dashboard` ekranindan bugunku sevkiyat, net ciro ve toplam alacagi kontrol edin.

### Sevkiyat girisi

- Her cari icin bir fis acin.
- Urun eklerken fiyat otomatik gelir; cariye ozel fiyat varsa onu kullanir.
- Ayni fis icindeki anlik iade ve zayiyi `Sevkiyat` ekranindaki satirlardan girin.
- Sonradan gelen iade icin `Iadeler` ekranini kullanin, sevkiyat satirinda degil.
- Fis numarasini bos birakirsaniz sistem otomatik uretir.
- Kredi limiti asiliyorsa uyari gelir; yine de devam etmek kullanicinin kararidir.

### Tahsilat girisi

- Once soldan cari secin.
- Mumkunse `Acik Fisler` listesinden satira tiklayip fise bagli tahsilat girin.
- `Serbest Tahsilat` sadece para geldi ama hangi fis kapandi net degilse kullanilsin.
- `Kalani Yaz` dugmesi acik fisin kalanini hizli doldurur.

### Iade girisi

- `Fise Bagli Iade`: belirli bir sevkiyat satirindan donen urun varsa.
- `Serbest Iade`: iade var ama belirli bir sevkiyata baglamayacaksaniz.
- Fise bagli iadede sistem musait miktardan fazla iade yazdirmaz.

### Gun sonu kontrolu

- `Raporlar` ekraninda once `Gunluk Muhasebe Ozeti`, sonra gerekiyorsa `Cari Ekstre` alin.
- CSV disa aktarimi muhasebe paylasimi icin yeterli.

## 3. Kullaniciya mutlaka soylenecek kurallar

- Hareket gormus urun ve cari kayitlari tam silinmeyebilir; sistem bunlari pasife ceker.
- Tahsilat, bagli oldugu fisin kalan tutarini gecemez.
- Bagli iade veya tahsilat varken bazi sevkiyat degisiklikleri engellenir.
- Ayni sevkiyat no veya ayni iade no ikinci kez kullanilamaz.
- `Sevkiyat` ekranindaki iade alani sadece ayni fis icindeki durumlar icindir.
- Sonradan gelen iadeler her zaman `Iadeler` ekranindan girilmelidir.

## 4. Teslimden once senin icin pratik kontrol listesi

1. Test kullanicisi ile bir urun, bir cari, bir sevkiyat, bir iade, bir tahsilat gir.
2. `Raporlar` ekraninda bu hareketlerin gorundugunu dogrula.
3. `Ayarlar > Yedek Al` ile manuel backup alip dosyanin olustugunu kontrol et.
4. Kullaniciya veri klasorunu gostermeden teslim etme.
5. Geri yuklemenin uygulamayi kapatacagini mutlaka soyle.

## 5. Kullaniciya soyleyebilecegin kisa egitim metni

"Bu programda once kartlar tanimlanir: urunler ve cariler. Sonra gunluk isler sevkiyat ile baslar. Sonradan gelen iadeler ayri ekrana girilir. Tahsilatta mumkun oldugunca acik fise bagli calisin. Gun sonunda rapordan kontrol alip gerekiyorsa CSV disa aktarabilirsiniz. Her kritik islemden once veya gun sonunda yedek alin."

## 6. Veri ve destek bilgisi

- Veritabani: `%AppData%\\BakeryAutomation\\bakery.db`
- Ayarlar: `%AppData%\\BakeryAutomation\\settings.json`
- Loglar: `%AppData%\\BakeryAutomation\\Logs`
- Otomatik backup klasoru: `%AppData%\\BakeryAutomation\\Backups`

## 7. Benim teslim tavsiyem

Yarin devredeceksen kullaniciya tum menuleri tek tek gostermek yerine su mini senaryoyu canli yap:

1. Yeni cari ac.
2. Cariye ozel fiyat ver.
3. Sevkiyat fisini acip urun ekle.
4. O fisin bir kismina bagli tahsilat gir.
5. Sonradan gelen iade gir.
6. Rapor ekranindan ekstreyi ac.
7. En son yedek aldir.

Bu 10-15 dakikalik akis, kullanicinin programin mantigini menulerden daha hizli kavramasini saglar.
