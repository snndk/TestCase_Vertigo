# Fortune Run — Vertigo Games Test Case

**Unity sürümü: 6000.3.23f1**

Başlamak için `Assets/_Game/Scenes/Game.unity` sahnesini açın, **Play** tuşuna basın ve başlangıç ekranına dokunun.

## Oynanış

- **Bölgeler ve çıkış:** Normal bölgeler Bronze, her 5. bölge Silver, her 30. bölge Gold'dur. Silver ve Gold bombasızdır. Ödüllerle çıkış yalnız bu güvenli bölgelerde, çark ve sonuç animasyonları tamamlanmışken mümkündür.
- **Hızlandırma:** Çark dönüşü ve ödül animasyonları sırasında ekrana dokunarak animasyonları hızlandırabilirsiniz. Kazanılan sonuç değişmez.
- **Lucky:** Bir sonraki güvenli bölgeye kadar otomatik spin başlatır. Bu spin'lerde yalnız ödül kazanılır. Hedef bölgeye ulaşınca otomatik ilerleme durur; oyuncu çıkabilir veya devam edebilir.
- **Bomba ve revive:** Bomba geldiğinde Gold ödeyerek koşunun ödüllerini kurtarıp **sonraki bölgeye** geçebilir veya vazgeçerek ödülleri kaybedebilirsiniz. Yetersiz bakiyede revive kullanılamaz. Varsayılan bedel 25 Gold'dur ve aynı koşudaki her kullanımda ikiye katlanır; yeni koşuda başlangıç bedeline döner.

## SRDebugger

Oyuna özel test kontrolleri **Editor Play Mode** ve **Development Build**'de kullanılabilir; `DISABLE_SRDEBUGGER` tanımlı olmamalıdır.

1. **Sol üst köşedeki tetikleme alanına üç kez dokunun/tıklayın.** Klavyede Game View odaktayken **Ctrl + Shift + F3**, Options sekmesini açar.
2. **Options → 01 Tester → Enter Test Mode** seçin. Test kategorileri görünür; `Mode` alanı aktif modu belirtir.
3. Test bitince **Exit Test Mode** ile normal kayda dönün.

Test modu ayrı kayıt kullanır; normal kayıt test komutlarından etkilenmez. **Moda giriş/çıkış ve zone değiştirme yeni koşu başlatır; henüz toplanmamış koşu ödülleri bırakılır.** Açılış hareketi ve kısayollar `Window → SRDebugger → Settings Window` üzerinden değiştirilebilir.

| Kategori | Kullanım |
| --- | --- |
| **01 Tester** | Mod değiştirme; `Last action` üzerinden son işlem sonucunu veya hata nedenini görme. |
| **02 Zones** | `Target zone` + `Start at target zone` ile istenen bölgede başlama. `Start before next Silver/Gold` ile geçişin bir önceki bölgesine gitme. |
| **03 Gold** | `Gold amount` girip `Set Gold` ile bakiyeyi belirleme veya `Add Gold` ile ekleme. |
| **04 Next spin** | `Next: …` ile mevcut çarktaki bir dilimi **yalnız bir sonraki spin** için seçme. Paneli kapatıp SPIN'e basın. `Cancel forced result` seçimi iptal eder. |
| **05 Save** | `Fail next save once` ile bir sonraki kayıt girişimini başarısız yapma; reload ile test kaydını yeniden yükleme. Reset için önce `Confirm test save reset`, ardından `Reset test save and restart` seçin. |
| **06 Live state** | Zone/tier, oyun durumu, Gold, revive bedeli, Lucky hedefi ve işlem kimliklerini izleme. |

Sonuç zorlama hazır manuel spin'de çalışır; çarkta olmayan bir dilimi eklemez. Gold değişiklikleri devam eden animasyon/işlem veya Lucky sırasında reddedilir. Örneğin Gold geçişini görmek için **29. bölgede başlayıp bir ödül dilimini zorlayın**, ardından spin yapın.

## Inspector ve ScriptableObject ayarları

Asset'ler `Assets/_Game/Settings` altındadır. Ayarları değiştirdikten sonra yeni bir Play oturumu başlatın.

| Asset / bileşen | Ayarlar |
| --- | --- |
| `GameSettings.asset` | Başlangıç bakiyesi, para birimi, revive aç/kapat, ilk bedel ve artış yüzdesi. Başlangıç bakiyesi mevcut kaydı değiştirmez. |
| `WheelConfiguration.asset` | Dilim sayısı ve Bronze/Silver/Gold referansları. Dilim sayısı değiştirilirse UI hücre kapasitesi de uyarlanmalıdır. |
| `Tiers/` | Dilim havuzları, ağırlıklar, başlık, çark/indicator görselleri ve animasyon davranışı. |
| `Rewards/` | Ödül kimliği, isim, ikon ve bölge başına artış. |
| `Slices/` | Dilim kimliği, ödül referansı ve başlangıç miktarı gibi dilime özgü veriler. |
| `WheelAnimations/` | Standard ve Gold giriş/kazanma efektleri. |
| `WheelView`, `RewardOutcomeView`, `WalletFeedbackView` | Inspector üzerinden dönüş ve hızlandırma, ödül uçuşu, cüzdan sayacı ve görsel vurgu ayarları. |

### Havuz ağırlığı ve kazanma ağırlığı

| Alan | Anlamı |
| --- | --- |
| `Weight = 0` | Aday çarka alınmaz. |
| `Weight = 100` | Çarkta yer alması garantidir; kazanması garanti değildir. |
| `Weight = 1–99` | Garanti dilimlerden sonra kalan yerler için göreli seçim ağırlığıdır; yüzde değildir. |
| `Spin Weight` | Çarka alınan dilimin kazanma ağırlığıdır. En az 1 olmalıdır; eşit ağırlıklar eşit şans verir. |

Bronze havuzunda `Weight = 100` olan tam bir bomba bulunmalıdır; Silver/Gold havuzlarında aktif bomba bulunamaz. Havuzda çarkı dolduracak kadar aktif aday olmalı ve garanti aday sayısı kapasiteyi aşmamalıdır. Aynı ödül farklı dilimlerde bulunabilir; kazanılan miktarlar listede birleştirilir.

### Ödül büyümesi ve yeni ödül ekleme

```text
Miktar = Base Amount + (Zone - 1) × Increase Per Zone
```

`Base Amount` dilimde, `Increase Per Zone` ödül asset'inde tutulur. Aynı ödül kimliği için bütün tier'larda aynı `RewardAsset` kullanılmalıdır. Farklı başlangıç miktarlı varyantlarda artışın bölgeler arasında miktarı düşürmemesi doğrulanır.

1. **Create → Vertigo → Reward:** Benzersiz `Reward Id`, isim, ikon ve pozitif `Increase Per Zone` girin.
2. **Create → Vertigo → Slices → Reward:** `Slice Id`, ödül referansı ve pozitif `Base Amount` belirleyin.
3. Dilimi ilgili tier'ın `Pool` listesine ekleyin; `Weight` ve `Spin Weight` ayarlarını yapın.
4. İkonu uygun Sprite Atlas'a dahil edin; yeni Play oturumunda SRDebugger ile ilgili bölgeyi ve ödülü test edin.

## Testleri çalıştırma

**Window → General → Test Runner** üzerinden **EditMode → Run All**, ardından **PlayMode → Run All** çalıştırın. Önce açık sahnedeki değişikliklerinizi kaydedin. Debug testleri için SRDebugger etkin olmalıdır.

- **Runtime:** Zone kuralları, seçim, ödül büyümesi, Lucky, revive ve ekonomi.
- **Presentation:** Görsel işlem sırası, iptal, timeout ve tekrarlanan/eski callback'ler.
- **Unity:** Asset/prefab bağlantıları, dosya kaydı ve gerçek UI/animasyon akışları.
- **Debug:** SRDebugger kontrolleri ve test kaydının normal kayıttan ayrılması.

## Klasör yapısı

```text
Assets/_Game/
  Runtime/         Oyun kuralları ve sözleşmeler
  Presentation/    Presenter, ekran verisi ve görsel işlem sırası
  Unity/           Authoring, bootstrap, input, view ve kayıt
  Debug/           SRDebugger test kontrolleri
  Settings/        Oyun, tier, ödül, dilim ve animasyon SO'ları
  Prefabs/         Oyun ekranı ve UI parçaları
  Scenes/          Game.unity
  Art/             Görseller ve atlaslar
  Tests/           Runtime, Presentation, Unity ve Debug testleri
```
