# DiskScope

**TR:** Windows için geliştirilen, taşınabilir (portable) bir disk ve depolama alanı analiz uygulaması.
**EN:** A portable disk and storage analyzer for Windows.

> **Durum / Status:** Erken geliştirme aşaması (v0.x). Genel Bakış ve Disk Analizi (treemap) çalışıyor; yinelenen dosyalar, temizlik merkezi ve diğer modüller yol haritasında.
> Early development (v0.x). Overview and Disk Analyzer (treemap) work; duplicate finder, cleanup center and other modules are on the roadmap.

[![CI](https://github.com/CasTiaL06/DiskScope/actions/workflows/ci.yml/badge.svg)](https://github.com/CasTiaL06/DiskScope/actions/workflows/ci.yml)

## Özellikler (şu an) / Features (current)

- **Genel Bakış:** bilgisayardaki diskler; kapasite, kullanılan/boş alan, doluluk yüzdesi, dosya sistemi ve gerçek disk türü (**SSD, HDD, NVMe**; emin olunamazsa **Bilinmiyor**)
- **Disk Analizi:** disk veya klasörü arka planda tarar (iptal edilebilir, ilerleme gösterir); erişilemeyen klasörler raporlanır
- **Treemap:** klasör ve dosya boyutlarını iç içe gösterir; üzerine gelince bilgi kartı, tıklayınca seçim, çift tıklayınca içeri girme, sağ tık menüsü (Explorer'da göster, yolu kopyala)
- Klasör listesi (boyut ve yüzde çubuğu), yol çubuğu (breadcrumb), dosya türü dağılımı ve en büyük uzantılar
- Türkçe ve İngilizce arayüz, yeniden başlatmadan dil değiştirme
- Koyu temalı, tutarlı bir tasarım sistemi
- Taşınabilir: ayarlar ve günlükler uygulamanın yanındaki `Data` klasöründe tutulur
- Hata günlüğü (Serilog) ve beklenmeyen hatalarda uygulamanın çökmemesi

**Bilinen sınırlar:** boyutlar dosyaların mantıksal boyutudur (diskte kapladığı alan değil); klasör bağlantıları (junction/symlink) takip edilmez; 256 KB'den küçük dosyalar klasör başına tek blok olarak gösterilir (bellek kontrolü için).

## Yol haritası / Roadmap

Büyük dosyalar listesi · Tarama sırasında duraklatma · Yinelenen dosya bulucu · Temizlik merkezi (yalnızca kullanıcı onayıyla, Geri Dönüşüm Kutusu'na) · Geçmiş ve dışa aktarma · Açık tema

Ayrıntılar: [`docs/DiskScope-Design-and-Plan.md`](docs/DiskScope-Design-and-Plan.md)

## Kurulum (kullanıcılar) / Install (users)

1. [Releases](../../releases) sayfasından en son `DiskScope-vX.Y.Z-win-x64-portable.zip` dosyasını indirin.
2. Zip'i bir klasöre çıkarın ve `DiskScope.exe` dosyasını çalıştırın. Kurulum ve .NET gerekmez.
3. Ayarlar ve günlükler aynı klasördeki `Data` klasörüne yazılır. Klasör yazılamıyorsa `%LOCALAPPDATA%\DiskScope\Data` kullanılır.

> Uygulama henüz imzalı değildir; Windows SmartScreen uyarı gösterebilir.

## Geliştirme / Development

Gereksinimler: Windows 10/11 (64-bit), [.NET 10 SDK](https://dotnet.microsoft.com/download) (sürüm `global.json` içinde).

```powershell
dotnet build DiskScope.sln
dotnet test DiskScope.sln
dotnet run --project src\DiskScope.App
```

Taşınabilir sürümü yerelde üretmek için:

```powershell
dotnet publish src\DiskScope.App\DiskScope.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## Proje yapısı / Structure

| Proje | Görev |
|---|---|
| `src/DiskScope.Core` | Temel arayüzler, treemap yerleşim algoritması, boyut biçimlendirme |
| `src/DiskScope.Scanning` | Disk algılama, SSD/HDD tespiti, klasör tarama motoru |
| `src/DiskScope.Storage` | Ayarlar, taşınabilir yol çözümleme, günlükleme |
| `src/DiskScope.App` | WPF masaüstü uygulaması |
| `tests/` | Birim ve entegrasyon testleri |

## Sürüm yayınlama / Releasing

`v0.1.0` gibi bir etiket gönderildiğinde GitHub Actions testleri çalıştırır, taşınabilir zip'i üretir ve Releases sayfasına ekler:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

## Lisans / License

[MIT](LICENSE)
