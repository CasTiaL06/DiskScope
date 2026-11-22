# DiskScope – Tasarım Belgesi, Mimari ve Geliştirme Planı

Durum: **GENEL PLAN ONAYLANDI** – Faz 1 görev planı onayı bekleniyor (kodlama başlamadı)
Sürüm: 0.2 (kullanıcı tercihleri işlendi)

---

## 0. Onaylanan kararlar

| Konu | Karar |
|---|---|
| Teknoloji | .NET 10 + WPF + SkiaSharp (yeniden onay gerekmez) |
| MFT hızlı tarama | İlk sürümlerde **yok**; sonraki sürümlerde değerlendirilecek |
| Kapsam | Tüm özellikler uzun vadeli hedef; önce MVP (Faz 1–3), sonra fazlar. Her faz sonunda test + onay |
| Dil | **Türkçe (varsayılan) + İngilizce**; Ayarlar'dan değiştirilebilir; yerelleştirme altyapısı Faz 1'de kurulur |
| Lisans | **MIT**; açık kaynak GitHub deposu |
| Vurgu rengi | **Mavi-turkuaz**, ölçülü kullanım; sade, modern, profesyonel genel görünüm |
| Çalışma ortamı | Claude Code + Superpowers |
| İlk iş paketi | **Yalnızca Faz 1**; Faz 1 testleri geçmeden Faz 2'ye geçilmez |

---

## 1. Hedef ve ilkeler

DiskScope; diskleri algılayan, klasör/dosya kullanımını gerçek verilerle tarayan, treemap ile görselleştiren ve güvenli temizlik sunan **portable** bir Windows masaüstü uygulamasıdır.

Değişmez ilkeler:
1. **Gerçek veri**: Hiçbir ekranda sahte/statik veri yok. Veri yoksa empty state gösterilir.
2. **Güvenli silme**: Kullanıcı onayı olmadan hiçbir şey silinmez; mümkünse Geri Dönüşüm Kutusu; sistem yolları kod seviyesinde korunur.
3. **UI asla bloke olmaz**: Tüm tarama/hash/IO işleri arka planda, iptal edilebilir.
4. **İzinlere saygı**: Yetkisiz alanlara erişim denenmez; erişilemeyenler raporlanır; yönetici gerektiren işlem açıkça belirtilir.
5. **Önce çalışan MVP**, sonra fazlar. Her faz sonunda test + onay.
6. Onaysız mevcut dosya silinmez/değiştirilmez.

## 2. Teknoloji kararları

| Konu | Karar | Gerekçe |
|---|---|---|
| Runtime | .NET 10 (LTS) | Güncel, hızlı, self-contained yayın |
| UI | WPF | Olgun, sanallaştırılmış listeler, single-file portable yayın kolay |
| Çizim | SkiaSharp (`SKElement`) | Treemap/halka grafik üzerinde tam kontrol, animasyon |
| MVVM | CommunityToolkit.Mvvm | Hafif, kaynak üreticili |
| Depolama | SQLite (Microsoft.Data.Sqlite) | Geçmiş + tarama önbelleği |
| Log | Serilog (dosya, dönen) | Hata yönetimi/tanılama |
| Test | xUnit + FluentAssertions, geçici dizinlerle entegrasyon testleri | Gerçek dosya sistemi davranışı |
| Hash | xxHash3 (hızlı ön hash) + SHA-256 (doğrulama) | Duplicate Finder |
| Dağıtım | self-contained, single-file, win-x64 zip | Portable |
| Yerelleştirme | `.resx` kaynakları + özel `ILocalizationService` + XAML markup extension | Çalışma anında dil değişimi, derleme zamanı anahtar kontrolü |
| Lisans | MIT | Açık kaynak paylaşım |

WPF'in Mica/Fluent hissi hazır gelmediği için kendi **tasarım sistemimizi** (token + stil kaynakları) yazacağız. Gerekirse pencere kromu (özel title bar, DWM karanlık mod/yuvarlak köşe) Win32 DWM API'leri ile ayarlanır.

Başlangıç prensibi: SKElement (CPU raster) + önbelleğe alınmış katman. Treemap'te performans yetersiz kalırsa GPU yoluna geçiş Faz 3 ölçümleriyle değerlendirilir (varsayım yapmadan ölçerek karar verilecek).

## 3. Çözüm yapısı

```
DiskScope.sln
src/
  DiskScope.Core        Modeller, arayüzler, sonuç/hata tipleri (bağımlılıksız)
  DiskScope.Scanning    Tarama motoru, ağaç modeli, tür sınıflandırma, ignore kuralları
  DiskScope.Analysis    Büyük dosyalar, tür istatistikleri, (ileride) duplicate, cleanup kuralları
  DiskScope.Storage     Ayarlar, SQLite önbellek/geçmiş, portable yol çözümleme, log kurulumu
  DiskScope.Visuals     Squarified treemap yerleşimi + Skia çizim bileşenleri
  DiskScope.App         WPF, ViewModel'ler, tasarım sistemi, ekranlar
tests/
  DiskScope.Core.Tests / Scanning.Tests / Analysis.Tests / Visuals.Tests
docs/                   Bu belge, ADR'ler, planlar
.github/workflows/      build + test + portable release
```

Bağımlılık yönü: `App → (Visuals, Analysis, Scanning, Storage) → Core`. Core hiçbir şeye bağlı değil.

Genişletilebilirlik: her modül (`IModule`: ad, ikon, ViewModel, sayfa) sidebar'a kaydolur. Yeni özellik (Folder Comparison, Favorites vb.) yeni modül olarak eklenir; mevcut kodu değiştirmez.

## 4. Tarama motoru tasarımı (en kritik parça)

### 4.1 Gezinti
- `FindFirstFileEx` (`FIND_FIRST_EX_LARGE_FETCH`, `FindExInfoBasic`) ile dizin listeleme; P/Invoke. `System.IO.Enumeration.FileSystemEnumerator` yedek/karşılaştırma seçeneği olarak ilk fazda benchmark edilir ve hızlısı seçilir.
- Klasör bazlı **work-stealing paralel gezinti**: `Channel<DirectoryWork>` + sınırlı sayıda worker (varsayılan: çekirdek sayısı, HDD için daha düşük; disk türüne göre ayarlanabilir).
- MFT/USN **kapsam dışı** (ilk sürümler).

### 4.2 Doğruluk kuralları
- **Reparse point/junction/symlink takip edilmez** (döngü ve çift sayım önlenir); ağaçta ayrı işaretlenir.
- **Bulut yer tutucuları** (OneDrive vb.) indirilmez; dosyalar "yerel olarak yok" diye işaretlenir, boyut politikası ayarlanır.
- İki boyut: **mantıksal boyut** ve **diskte kapladığı alan** (küme yuvarlaması). Varsayılan "diskte kapladığı alan"; Settings'ten değiştirilebilir. (Sıkıştırılmış/seyrek dosyalar için ek API ile doğruluk Faz 2'de test edilir.)
- Hard link: aynı dosya kimliği (volume serial + file index) tekrar sayılmaz (Faz 2'de ele alınır, test edilir).
- Windows yolu uzun yol (`\\?\`) desteği.

### 4.3 Bellek modeli
- Her dosya için nesne **yok**. Ağaç, yalnızca klasör düğümlerini tutar: indeks tabanlı struct dizileri (`parent`, `firstChild`, `nextSibling`, `size`, `fileCount`, `flags`, `nameId`).
- İsimler paylaşımlı bir `StringPool`'da (UTF-16 `char` blokları).
- Dosya düzeyinde veri sadece: **en büyük N dosya** (min-heap, N ayarlanabilir, vars. 1000) ve **tür/uzantı toplamları**. Treemap'te dosya seviyesi gerektiğinde klasör içeriği **talep üzerine** (lazy) okunur; tüm dosyalar RAM'de tutulmaz.
- Hedef: birkaç milyon dosya için kabul edilebilir bellek (gerçek sayı Faz 2'de ölçülüp bu belgeye işlenecek; şimdilik sayı vaadi yok).

### 4.4 Kontrol ve dayanıklılık
- `CancellationToken` + duraklatma geçidi (`PauseGate`).
- İlerleme: yoğun olay yerine **örneklemeli** rapor (≈10 Hz), UI kuyruk şişmez.
- Hatalar: `ScanIssue { Path, Kind (AccessDenied, PathTooLong, IoError, Offline...), Message }` listesi; tarama devam eder; özet ekranında raporlanır, "Yönetici olarak yeniden başlat" yalnızca kullanıcı seçerse önerilir.
- Disk bağlantısı kesilirse: ilgili alt ağaç `Incomplete` işaretlenir, tarama "kısmi sonuç" ile biter.
- Uygulama tarama sırasında kapanırsa: yarım sonuç **kaydedilmez** (veya `Incomplete` etiketiyle atomik kaydedilir); önbellek dosyası atomik yazılır (geçici dosya + rename), bozuk veri bırakmaz.

### 4.5 Önbellek
Tamamlanan tarama sonuçları SQLite'a (veya sıkıştırılmış ikili anlık görüntü dosyasına) yazılır; hangisinin daha iyi olduğu Faz 2'de ölçülerek seçilir. Geçmiş için sadece özet ölçümler (tarih, disk, toplam/kullanılan, tür dağılımı) SQLite'ta tutulur.

## 5. Treemap tasarımı (imza özellik)

- **Algoritma**: Squarified treemap (yerleşim, UI'dan bağımsız, birim test edilebilir).
- **Çizim**: Skia; ince iç boşluk (padding), yuvarlatılmış köşeler, hafif gradyan/cushion gölgeleme, uzantı kategorisine göre uyumlu renk paleti, küçük hücreler gruplanıp "diğer" olarak birleştirilir (performans + okunabilirlik).
- **Etkileşim**: hover'da vurgulama + bilgi kartı (ad, yol, boyut, yüzde, dosya sayısı); tek tık seçim; çift tık/tık ile **animasyonlu içeri zoom**; breadcrumb ile geri; sağ tık menüsü (Explorer'da göster, yolu kopyala, Geri Dönüşüm Kutusu'na gönder – onaylı); arama ile vurgulama; tür filtresi.
- **Seviye sınırı**: görünür derinlik ayarlanır (ör. 2–3 seviye iç içe), çizim süresi ölçülür.
- **Performans bütçesi** (Faz 3'te ölçülüp doğrulanacak hedefler): yerleşim hesabı arka planda; yeniden boyutlandırmada debounce; hedef akıcı etkileşim.
- **Erişilebilirlik**: klavye ile gezinti, yüksek kontrast renk seti, renk dışında ek kodlama (desen/etiket).

## 6. Tasarım sistemi

- **Dark-first**, token tabanlı: `Surface0–3` (katmanlı yüzeyler), `Border` (ince, düşük kontrast), `TextPrimary/Secondary/Disabled`, tek **vurgu rengi: mavi-turkuaz** (başlangıç önerisi `#2BB3C0` civarı; açık/koyu varyantları Faz 1'de erişilebilirlik kontrastıyla netleştirilir) + semantik renkler (başarı, uyarı, tehlike).
- **Vurgu rengi kullanım kuralı (ölçülü)**: yalnızca birincil eylem butonları, aktif sidebar göstergesi, seçim/odak halkası, ilerleme göstergeleri ve veri vurguları. Arka planlar, kartlar ve genel yüzeyler nötr koyu gri tonlarında kalır; arayüz bu renge boğulmaz.
- Treemap/grafik kategori paleti ayrıca tanımlanır (düşük doygunluklu, ayırt edilebilir); vurgu rengi kategori rengi olarak baskın kullanılmaz.
- **Tipografi**: Segoe UI Variable; ölçek: Display 28 / Title 20 / Subtitle 16 / Body 13 / Caption 11.
- **Boşluk**: 4/8 px ızgara; köşe yarıçapı 6/10/14.
- **Hareket**: 150–250 ms, ease-out; hover/seçim/sayfa geçişleri; "reduce motion" ayarına saygı.
- **Bileşenler**: Sidebar, TopBar (global arama, seçili disk, bildirim, ayarlar), StatCard, DiskCard (halka grafik), DataTable (sanal, sıralama/filtre/arama), Breadcrumb, ContextMenu, Skeleton, EmptyState, ProgressBar/Ring, Toast, ConfirmDialog.
- **Tema**: Light tema aynı token kümesinin ikinci değer seti olarak (Faz 5).
- Tüm ekranlar yalnızca tasarım sistemi bileşenlerini kullanır (tek tasarım dili kuralı).

## 7. Ekranlar

1. **Overview**: Disk kartları (halka grafik, kapasite/kullanılan/boş, %, harf, etiket, dosya sistemi, HDD/SSD/NVMe), seçili disk özeti, son tarama bilgisi, "Tara" eylemi. Disk türü `Win32_DiskDrive`/depolama sorgularıyla; belirlenemezse "Bilinmiyor" gösterilir (tahmin yok).
2. **Disk Analyzer**: klasör ağacı + boyut sütunları + treemap + ilerleme/duraklat/iptal + erişilemeyen klasör raporu.
3. **Large Files**: en büyük dosyalar, uzantı filtresi, boyut eşiği, klasöre git, özellikler.
4. **File Types**: kategori başına dosya sayısı, boyut, oran; grafik; tıklayınca treemap filtresi.
5. **Duplicate Finder** (Faz 4), **Cleanup Center** (Faz 4), **History** ve **Settings** (Faz 5).

## 8. Güvenli silme tasarımı (Faz 4, ama mimaride baştan hazır)

- `IProtectedPathPolicy`: Windows dizini, Program Files, ProgramData sistem alt klasörleri, `$Recycle.Bin`, `System Volume Information`, kullanıcı profil kökleri vb. **engelli**; politika birim testli.
- Akış: Seç → **liste + tahmini kazanç** → açık onay penceresi → Geri Dönüşüm Kutusu'na gönder (`IFileOperation`/`SHFileOperation`) → sonuç raporu.
- Geri Dönüşüm Kutusu mümkün değilse (ağ sürücüsü, çok büyük dosya) kullanıcı **ayrıca** uyarılır; kalıcı silme ayrı, açık onayla.
- Yönetici gerektiren işlem açıkça belirtilir; sessizce yükseltme/atlama yok.
- Çalışan uygulamaların/kilitli dosyaların hatası kullanıcıya raporlanır.

## 8b. Yerelleştirme (Türkçe + İngilizce)

- Varsayılan dil: **tr-TR**. İlk açılışta sistem dilinden bağımsız Türkçe; Ayarlar'dan **en-US** seçilebilir; seçim kalıcıdır.
- Altyapı Faz 1'de kurulur, sonradan eklenmez:
  - `DiskScope.Core`: `ILocalizationService` (`CurrentCulture`, `Get(key)`, `LanguageChanged` olayı).
  - `DiskScope.App`: `Strings.resx` (varsayılan/tr) ve `Strings.en.resx`; XAML'de `{loc:Tr Key}` markup extension (dil değişince **yeniden başlatmadan** güncellenir).
  - Sabit metin (hard-coded string) **yasak**: UI metinleri ve kullanıcıya gösterilen hata mesajları kaynaklardan gelir.
- **Biçimlendirme** kültüre göre: sayı, tarih, yüzde ve boyut (KB/MB/GB; birim politikası ayarlanabilir ikili/ondalık).
- **Testler**: iki dil dosyasında anahtar eşitliği testi (eksik/fazla anahtar derlemeyi/CI'ı kırar), biçimlendirme testleri, dil değişiminin ViewModel'lere yansıma testi.
- Log mesajları İngilizce kalır (tanılama için); kullanıcıya gösterilen metinler yerelleştirilir.

## 9. Portable tasarım

- Tek klasör: `DiskScope.exe` (self-contained, single-file) + `Data/` (ayarlar, SQLite, loglar).
- Veri yolu çözümü: exe yanındaki `Data/` yazılabiliyorsa orası; değilse `%LOCALAPPDATA%\DiskScope` ve kullanıcıya bildirim.
- Kayıt defteri/AppData'ya zorunlu yazım yok.
- GitHub Actions: build + test + `win-x64` portable zip artifact; sürüm etiketinde otomatik release. Depo: `README`, `LICENSE` (sen seç), `.gitignore`, `CONTRIBUTING` (opsiyonel).

## 10. Test stratejisi

- **Birim**: treemap yerleşim (alan korunumu, sıralama, kenar durumları), tür sınıflandırma, korumalı yol politikası, boyut hesapları, ignore kuralları.
- **Entegrasyon (gerçek dosya sistemi)**: geçici dizinlerde bilinen boyutlu dosya ağaçları; erişim reddi (ACL ile), uzun yol, junction/symlink döngüsü, hard link, boş klasör, Unicode adlar, iptal/duraklat, kısmi sonuç.
- **Performans ölçümleri**: sentetik büyük ağaç üretimi (test dizini) ile tarama süresi/bellek ölçümü; sonuçlar belgeye yazılır (sayı iddiası ölçümden sonra).
- **Manuel doğrulama**: gerçek diskte Explorer/PowerShell toplamlarıyla karşılaştırma.
- Bir özellik **testten geçmeden "tamamlandı" işaretlenmez**.

## 11. Yol haritası (MVP + fazlar)

**MVP = Faz 1 + 2 + 3** (Overview, Analyzer, Treemap, Large Files, File Types). Her fazın sonunda: çalışan build, test raporu, demo notları ve **senin onayın**.

### Faz 1 – Foundation
1. Çözüm iskeleti, `Directory.Build.props`, analyzer/stil kuralları, CI (build+test), **MIT `LICENSE`, `README`, `.gitignore`**
2. Portable yol çözümleme + ayarlar (JSON) + Serilog + **yerelleştirme altyapısı (tr/en, çalışma anında dil değişimi, anahtar eşitliği testi)**
3. Tasarım sistemi (renk/tipografi/boşluk token'ları, temel kontrol stilleri)
4. Ana pencere: özel title bar, Sidebar, TopBar, sayfa navigasyonu, modül kaydı
5. Disk algılama servisi (`DriveInfo` + disk türü sorgusu, yenileme) ve Overview'in gerçek verili ilk sürümü (DiskCard + halka grafik)
- **Kabul**: Tüm sabit/çıkarılabilir diskler doğru kapasite/boş alan ile görünür; Türkçe/İngilizce arasında yeniden başlatmadan geçiş çalışır ve dil tercihi kalıcıdır; tema ve navigasyon tutarlı; pencere yeniden boyutlanınca düzen bozulmaz; testler yeşil.

### Faz 2 – Core Analyzer
1. Ağaç modeli + StringPool + testler
2. Tarama motoru (paralel gezinti, iptal/duraklat, ilerleme, ScanIssue)
3. Reparse/hard link/bulut dosyası kuralları + testler
4. Büyük dosya heap'i + tür/uzantı istatistikleri + kategori sınıflandırma
5. Önbellek (atomik) + yeniden tarama
6. Analyzer ekranı (klasör ağacı, ilerleme, duraklat/iptal, erişilemeyen yollar), Large Files, File Types ekranları
- **Kabul**: Test ağaçlarında boyutlar birebir doğru; gerçek diskte Explorer toplamlarıyla uyumlu (fark nedenleri belgelenir); UI tarama sırasında akıcı; iptal anında durur; izin hataları taramayı bozmaz ve raporlanır; bellek/süre ölçümleri belgelenir.

### Faz 3 – Visualization
1. Squarified yerleşim + testler
2. Skia treemap çizimi, hover/seçim, tooltip
3. Animasyonlu zoom + breadcrumb + klasör ağacı ile senkronizasyon
4. Sağ tık menüsü, arama vurgusu, tür filtresi
5. File Types grafikleri, halka grafikler, görsel cila (Overview/Treemap ekranları)
6. Performans ölçüm/optimizasyon
- **Kabul**: Gerçek tarama sonucundan treemap; tıklayarak iç içe gezinti; büyük klasörlerde akıcı etkileşim (ölçülmüş); renk ve etiketler okunaklı.

**→ MVP onayı ve ilk portable build.**

### Faz 4 – Advanced Tools
Duplicate Finder (boyut→hızlı hash→tam hash→doğrulama, gruplar, karşılaştırma, güvenli kaldırma), Cleanup Center (geçici dosyalar, büyük/eski dosyalar, kategori, tahmini kazanç, onaylı silme, korumalı yol politikası), Folder Comparison.

### Faz 5 – History & Settings
Geçmiş grafikleri (SQLite), ayarlar (tema, boyut politikası, ignore kuralları, eşikler), dışa aktarma (CSV/JSON/HTML), bildirimler (doluluk eşiği), favoriler, tarama profilleri, açık tema.

### Faz 6 – Polish
Performans/bellek optimizasyonu, erişilebilirlik, UX cilası, uçtan uca testler, release build ve GitHub release otomasyonu, kullanıcı dokümantasyonu.

## 12. Riskler ve açık noktalar

| Risk | Önlem |
|---|---|
| Milyonlarca dosyada bellek/süre | Kompakt ağaç, ölçüm odaklı geliştirme, erken benchmark |
| "Diskte kapladığı alan" doğruluğu (sıkıştırma, seyrek dosya, hard link) | Test ağaçları + Explorer ile karşılaştırma; farklar belgelenir |
| Skia CPU çizimi yetersiz kalabilir | Faz 3'te ölçüm; gerekirse GPU yolu |
| Disk türü (SSD/NVMe) tespiti bazı sürücülerde belirsiz | "Bilinmiyor" göster, tahmin etme |
| Antivirüs/izin etkileri tarama hızını etkiler | Ayarlanabilir paralellik, raporlama |
| Single-file + native kütüphane (SkiaSharp) | Erken doğrulama; gerekirse native dosyaları self-extract ayarıyla portable tut |

Önceki açık sorular (dil, lisans, vurgu rengi) **yanıtlandı** ve Bölüm 0'a işlendi.

## 13. Sonraki adım

Genel plan onaylandı. Claude Code'da Superpowers kurulduktan sonra **yalnızca Faz 1** için görev bazlı uygulama planı (`writing-plans`) hazırlanacak ve sana sunulacak; onayından sonra Faz 1 kodlanacak. Faz 1 testleri geçmeden Faz 2'ye geçilmeyecek.
