# ACE OF SURVIVAL — Proje Denetimi, Next Fest Yol Haritası ve Pazarlama Planı

**Tarih:** 30 Temmuz 2026
**Kaynak:** GDD v1.3, Balance v1.7, "Benim kafamdaki şu" notları, Unity projesi (44 C# dosyası, Unity 6000.3.19f1)

---

## ÖZET: Tek Cümlelik Durum

**Kodun kendi yol haritanın 1–2 hafta ÖNÜNDE. Sanat, ses ve Steam sayfası SIFIR noktasında. Next Fest'e girip girememen kodla değil, 31 Ağustos'a kadar Steam sayfasını yayına sokup sokamamanla belirlenecek.**

Önümüzdeki 6 hafta kod yazmaya devam edersen Next Fest'i kaçırırsın. Bu doküman bunu önlemek üzerine kurulu.

---

# BÖLÜM 0 — Connector'lar (İlk sorduğun şey)

## 0.1 Neden yetkilendirme istiyor?

`engineering` plugin'i 10 connector bildiriyor: Slack, Linear, Asana, Atlassian (Jira/Confluence), Notion, GitHub, PagerDuty, Datadog, Google Calendar, Gmail. Bunlar plugin'in *desteklediği* araçlar — hepsini bağlaman gerekmiyor. Bağlamadığın sürece o araçlara özel yetenekler kapalı kalır, plugin'in skill'leri yine de çalışır.

## 0.2 Nasıl eklenir

Claude masaüstü uygulamasında: **Ayarlar → Connectors (Bağlayıcılar)** → listeden seç → **Connect** → tarayıcıda açılan OAuth ekranında hesabına giriş yap ve izin ver. Bağlandıktan sonra yeni bir görev başlatman gerekiyor ki araçlar yüklensin.

## 0.3 SENİN projen için hangileri gerçekten lazım? (Dürüst cevap)

| Connector | Senin için değeri | Karar |
|---|---|---|
| **Notion** | Tasarım dokümanı + görev panosu tek yerde. Ben okuyup güncelleyebilirim. GDD'nin Word'de yaşaması uzun vadede acı. | **Öneri: BAĞLA** |
| **Linear** | Bug/görev takibi. Notion'a alternatif, daha "issue tracker" odaklı. | Notion yerine biri yeter |
| Slack | Takım sohbeti. Sen solosun. | **Atla** |
| Asana / Jira | Kurumsal proje yönetimi. | **Atla** |
| PagerDuty / Datadog | Prod sistem izleme (nöbet, log). Oyunla ilgisi yok. | **Atla** |
| Google Calendar / Gmail | Next Fest tarih hatırlatmaları için işe yarayabilir. Düşük öncelik. | Opsiyonel |

**GitHub connector'ı listede yok — ama gerek de yok.** Ben zaten `C:\AceOfSurvival` klasörüne doğrudan erişiyorum: dosyaları okuyor, git geçmişini görüyor, kod yazıp kaydedebiliyorum. Bu, GitHub API'sinden daha güçlü. GitHub Desktop'ı sen commit atmak için kullanmaya devam et.

**Net tavsiye:** Sadece **Notion**'ı bağla. Diğerleri bu proje için gürültü.

---

# BÖLÜM 1 — PROJE DENETİMİ

## 1.1 İyi olan şeyler (bunları koru)

Beklediğimden çok daha sağlam bir temel var. Özellikle:

**Mimari kararların doğru.**
- `WeaponBase` soyut sınıf + `WeaponCategory` enum (Ranged/Melee/**Tactical**) — v1.3'teki Trap Card kategorisi kararı koda doğru geçmiş.
- `SourcePrefab` üzerinden kopya kimliği tutuluyor → aynı silahtan çoklu kopya sistemi (4x CardThrow) **gerçekten çalışıyor**. Bu, "Benim kafamdaki şu" belgesindeki en karmaşık fikirdi ve doğru uygulanmış.
- `WeaponManager.CanPurchase()` → Durum C ("slot dolu, sende olmayan silah alınamaz") kuralı kodda mevcut.
- `WeaponManager.LockCategory()` → LMSH Tactical'ı kilitlemiyor. Doğru.

**Object pooling zaten var** — GDD'nin "ZORUNLU" dediği yerde:
- `WaveManager.GetFromPool()` (düşmanlar)
- `PickupSpawner` (XP/chips)
- `DamageNumberManager` (hasar sayıları)
- `OffscreenIndicatorManager` (ok işaretleri)

**Hasar pipeline'ı tek yerde toplanmış** (`PlayerStats.RollProjectileDamage` / `RollMeleeDamage` / `ApplyCritRoll`) ve GDD'deki formüle birebir uyuyor. `Mathf.Max(1f, ...)` kuralı dahil. Bu çok önemli — hasar hesabı 5 farklı yere dağılsaydı balance imkânsız olurdu.

**Mağaza sistemi (`ShopManager.cs`, 19KB) tasarımın en zor kısmını uygulamış:** askıda/pending silah durumu, slot kilitleme, LMSH vitrin filtresi, HR max-level filtresi, sat modu, fiyat formülleri (wave ölçeği × reroll ölçeği). Bu, notlarında sayfalarca anlattığın sistemin çalışan hali.

**HUD gerçek uGUI** (`HUDController.cs`, TMP + Image fill) — can barı, XP barı, wave sayacı, dash ring hepsi doğru şekilde Canvas üzerinde.

**Input System kullanılıyor**, eski `Input.GetKey` yok.

---

## 1.2 Bulduğum somut hatalar ve ölü kod

Bunlar tahmin değil — dosyalarda doğruladım.

### 🔴 KRİTİK 1: `RunEndManager` hiç çağrılmıyor
`RunEndManager.cs` içinde altın hesabı, tematik ölüm/zafer cümleleri, istatistik özeti — hepsi yazılmış. Ama `ShowEnd()` fonksiyonunu **projede hiçbir yer çağırmıyor**.

Bunun yerine `PlayerHealth.cs:97` şunu yapıyor:
```csharp
FindFirstObjectByType<GameOverManager>().TriggerGameOver();
```
`GameOverManager` sadece bir panel açıyor — altın yok, istatistik yok, kayıt yok. Yani **run bitince kazandığın altın diske yazılmıyor.** `SaveSystem.AddGold()` de çağrılmıyor.

→ İki paralel "run sonu" sistemi var, yanlış olan bağlı. `GameOverManager`'ı sil, `PlayerHealth` → `RunEndManager.ShowEnd(false, wave)` bağla.

### 🔴 KRİTİK 2: Zafer akışı yok
`WaveManager.cs:113`:
```csharp
Debug.Log("=== STAGE TAMAMLANDI! (Boss ve run-sonu ekrani ileride) ===");
```
7 wave'i bitiren oyuncu hiçbir şey görmüyor. Konsola log basılıyor, oyun donuyor.

### 🔴 KRİTİK 3: Wave 5 harita büyümesi tetiklenmiyor
`MapController.ExpandMap()` yazılmış, animasyonu, screen shake'i, collider güncellemesi hazır. Ama `WaveManager` **hiçbir yerde çağırmıyor**. `MapController`'a tek referans `CameraFollow`'dan geliyor.

→ GDD'nin "wave 5'te masa büyür" kilitli kararı şu an oyunda yok. Tek satır fix.

### 🟠 ORTA 4: `groupSize` ölü alan
`WaveDefinition.cs:30`'da tanımlı:
```csharp
[Tooltip("YENI: tek seferde kac tane dogsun? (Kart Usagi = 4, digerleri = 1)")]
public int groupSize = 1;
```
`WaveManager.SpawnEnemy()` bu alanı **hiç okumuyor** — her zaman 1 düşman spawn ediyor. Kart Uşağı'nın "grup halinde gelir" tasarımı çalışmıyor.

### 🟠 ORTA 5: WaveDefinition, balance tablosunun gerisinde
Excel `5_Waveler` sayfasında şu sütunlar var: **Tank garanti (adet)**, **Miniboss (adet)**, **Spawn/sn**. `WaveDefinition`'da bunların karşılığı yok — sadece ağırlıklı havuz var. Yani "wave 3'te en az 1 tank garanti", "wave 5'te 1 miniboss" kuralları uygulanamaz durumda.

Ayrıca Excel'deki **Spawn/sn sütunu tamamen boş** — yoğunluk eğrisi hiç doldurulmamış. Bu, oyunun zorluk hissini belirleyen tek sayı.

### 🟡 KÜÇÜK 6: `WaveManager`'da kopyala-yapıştır artığı
```csharp
if (levelUpManager != null) yield return new WaitUntil(() => !levelUpManager.IsOpen);
// ... 5 satır sonra aynısı tekrar
if (levelUpManager != null) yield return new WaitUntil(() => !levelUpManager.IsOpen);
```
Zararsız ama temizlenmeli.

### 🟡 KÜÇÜK 7: Input binding'leri kodda gömülü
`PlayerMovement.Awake()` içinde `AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w")...` şeklinde elle yazılmış. Projede kullanılmayan bir `InputSystem_Actions.inputactions` asset'i duruyor.

→ Sonuç: tuş değiştirme (rebinding) imkânsız, gamepad desteği yamalı. GDD "klavye + gamepad baştan" diyor. Next Fest için ölümcül değil ama ayarlar menüsünde tuş değiştirme sunamazsın.

---

## 1.3 Hiç yazılmamış sistemler

| Sistem | GDD Bölüm | Durum |
|---|---|---|
| **Boss (The Dealer)** | 11 | Prefab var, script YOK, 50 sn timer YOK, mekanik TASARLANMAMIŞ |
| **Dev Eldiven** | 12 | Hiç yok. **Demonun kimlik mekaniği ve trailer açılışı bu.** |
| Miniboss spawn mantığı | 10.1 | Yok |
| Tank garanti mantığı | 10.1 | Yok |
| **Ses sistemi** | 15 | Projede tek bir `AudioSource`/`AudioClip` referansı bile yok. 0 ses dosyası. |
| **Lokalizasyon (TR/EN)** | 14 | `com.unity.localization` paketi **manifest.json'da yüklü değil**. Tüm metinler kodda gömülü Türkçe. |
| Ayarlar menüsü | 14 | Yok (ses, dil, fullscreen) |
| Ana menü işlevselliği | 14 | Sahne var, içerik belirsiz |
| Kalıcı yükseltmeler | 9 | Demoda yok — doğru karar |

---

## 1.4 En büyük iki risk (kodla ilgisi yok)

### ⚠️ RİSK 1: Sıfır sanat varlığı
Projede **hiç `.png`, `.aseprite`, `.psd` dosyası yok.** Her şey Unity'nin varsayılan kare/daire sprite'larıyla çalışıyor.

Bu sadece "oyun çirkin görünüyor" meselesi değil. **Steam sayfası açmak için capsule görselleri zorunlu:**
- Header capsule 460×215
- Main capsule 616×353
- Vertical capsule 374×448
- Small capsule 231×87
- Library görselleri
- En az 5 ekran görüntüsü (placeholder küplerle sayfa incelemesinden geçmen zor)

**Kapsül görseli, oyununun tek en önemli pazarlama varlığı.** Steam'de insanlar önce onu görüyor. Şu an elinde yok ve 31 Ağustos'a 4,5 hafta var.

### ⚠️ RİSK 2: Tek git commit
```
2026-07-20 Initial commit
commits: 1
```
10 gündür yazdığın her şey tek bir çalışma kopyasında duruyor. Unity bir sahneyi bozarsa, bir refactor patlarsa, disk gitse — geri dönüş yok.

**Bugün yap:** GitHub Desktop'ta commit at, private repo olarak push et. Sonra her akşam commit alışkanlığı edin. Bu 5 dakikalık iş, projenin tamamını kurtarabilir.

> Not: `.gitignore` doğru görünüyor (Library/, Logs/ hariç tutulmuş) ama `.vs/` klasörü repoda duruyor — onu da ignore'a ekle.

---

## 1.5 Tasarımda hâlâ çözülmemiş çelişkiler

Balance tablon bunları kendisi işaretlemiş, ama **wave 5–7'yi kodlamadan önce kilitlenmeli**:

1. **Zar seviyelemesi:** GDD bir satırda "seviye zar sayısını VE max sayıyı artırır", başka satırda "level up SADECE max sayıyı artırır" diyor. → Karar ver.
2. **Sandık içeriği:** chips mi altın mı? GDD 7.1 "run içinde altın alınabilecek tek istisna" diyor, 2.4 "altın run içinde toplanmaz" diyor. → Karar ver.
3. **Boss wave spawn oranı:** GDD 11 "%30 daha az", Excel "%75'i kadar" diyor. Bunlar aynı şey değil. → Excel'i esas al, GDD'yi düzelt.
4. **Pit Boss ilk wave:** Excel `4_Dusmanlar` 5 diyor, `5_Waveler` wave 5'te 1 miniboss diyor ama düşman tablosunda ilk wave 6 yazıyordu. → Çözülmüş görünüyor, teyit et.
5. **Bodyguards yörünge yarıçapı ve dönüş hızı** boş. → Öneri: yarıçap 1.5, 0.5 tur/sn ile başla.
6. **Nerdy Glasses melee dmg:** GDD -2, Excel -1.5. → Excel esas.

**Kural:** Excel = tek gerçek kaynak. GDD'yi Excel'e eşitle, tersini yapma.

---

# BÖLÜM 2 — GELİŞTİRME ÖNCELİKLERİ

Sıralama önem derecesine göre. Üstteki bitmeden alttakine geçme.

## Öncelik 0 — Bugün (30 dk)
- [ ] Git commit + GitHub'a private push
- [ ] `.gitignore`'a `.vs/` ekle

## Öncelik 1 — Kopan zincirleri bağla (2–3 saat)
Bunlar zaten yazılmış kodu çalışır hale getirmek. Yeni özellik değil, en yüksek getirili iş.

- [ ] `PlayerHealth.Die()` → `RunEndManager.ShowEnd(false, waveNo)` çağırsın. `GameOverManager`'ı sil.
- [ ] `RunEndManager` içinde `SaveSystem.AddGold()` çağrıldığını doğrula (altın diske yazılsın)
- [ ] `WaveManager` stage sonunda → `RunEndManager.ShowEnd(true, 7)`
- [ ] `WaveManager` wave 5 başında → `MapController.Instance.ExpandMap()`
- [ ] `WaveManager.SpawnEnemy()` → `entry.groupSize` kadar spawn etsin
- [ ] Çift `WaitUntil` bloğunu temizle
- [ ] `PlayerHealth`/`LetMeSoloHer` içindeki `FindFirstObjectByType` çağrılarını `[SerializeField]` referansa çevir

## Öncelik 2 — Wave verisi tamamla (3–4 saat)
- [ ] `WaveDefinition`'a alan ekle: `guaranteedTanks`, `minibossCount`, `minibossPrefab`
- [ ] `WaveManager`'a garanti spawn mantığı: wave başında garanti düşmanları doğur, sonra ağırlıklı havuza geç
- [ ] Excel `5_Waveler` → **Spawn/sn sütununu doldur.** Öneri başlangıç: W1 0.8/sn, W2 1.0, W3 1.2, W4 1.4, W5 1.7, W6 2.0, W7 1.5
- [ ] 7 Wave asset'ini Excel'e göre güncelle

## Öncelik 3 — Boss + Dev Eldiven (5–7 gün)
Bunlar demonun **kimliği**. Zar vitrin silahıysa, eldiven vitrin mekaniği.

**Dev Eldiven'i boss'tan ÖNCE yap.** Sebep: eldiven trailer'ın açılış sahnesi ve TikTok/Shorts klibinin tamamı. Pazarlama değeri boss'tan yüksek ve teknik olarak daha basit (tek coroutine, GDD 12.1'de adım adım yazılı).

Boss için önce **mekaniği tasarlaman** lazım — GDD'de "TBD" yazıyor. Kod yazmadan önce 1 sayfa boss tasarımı çıkar: kaç faz, hangi saldırı, telegraph nasıl.

## Öncelik 4 — OnGUI'den kurtul (4–6 gün)
Şu an bu 7 dosya `OnGUI()` kullanıyor:
`ShopManager`, `LevelUpManager`, `RunEndManager`, `WeaponManager`, `PlayerResources`, `WaveManager`, `HouseRuleLetMeSoloHer`

`OnGUI` Unity'nin editör içi debug aracı. Sorunları:
- Her frame garbage allocate eder (mobil/düşük sistem hedefin için kötü)
- Çözünürlükle ölçeklenmiyor — 1080p'de doğru görünen 1440p'de bozuluyor
- Pixel-perfect olamaz, senin 640×360 sanat yönetimine aykırı
- Stillendirilemez, ekran görüntüsünde korkunç durur

**Ekran görüntüsü Steam sayfasına gidiyor.** Mağaza ekranın IMGUI butonlarıyla görünüyorsa sayfa kalitesi düşer.

Sanat henüz yokken bile Canvas hiyerarşisini şimdi kur, düz renk `Image`'larla çalıştır, sprite'ları sonra değiştir. HUD'u zaten doğru yapmışsın — aynı yaklaşımı mağazaya uygula.

## Öncelik 5 — Ses + Lokalizasyon (sınav dönemi işi)
- [ ] `com.unity.localization` paketini kur
- [ ] Kodda gömülü Türkçe metinleri (`RunEndManager` cümleleri, `Debug.Log`lar hariç UI metinleri) tabloya taşı
- [ ] Basit bir `AudioManager` (SFX havuzu + müzik kanalı + ses seviyesi ayarı)
- [ ] Minimum SFX listesi: vuruş, düşman ölümü, pickup, level-up, satın alma, dash, eldiven uyarısı, eldiven çarpma, boss

## ✋ Bu aşamada YAPMA
- Yeni silah, yeni house rule, yeni trinket **ekleme**. Demo kapsamı zaten dolu (5 silah var, GDD 4 diyordu — zaten öndesin).
- Silah evrimi / New Deck / Reshuffle — tam sürüm işi.
- Kalıcı yükseltme ekranı — demoda yok kararı doğru, sadık kal.
- Endless mode, questler, sinerjiler — 21. bölümdeki her şey Next Fest sonrası.

---

# BÖLÜM 3 — STEAM NEXT FEST YOL HARİTASI

## 3.1 Doğrulanmış tarihler (Ekim 2026 Next Fest)

| Tarih | Ne |
|---|---|
| **31 Ağustos, 23:59 PDT** | **Kayıt kapanıyor. Bekleme listesi YOK, istisna YOK.** |
| 21 Eylül | Basın ön izlemesinde yer almak istiyorsan demo build + sayfa bu tarihe kadar incelemeye gönderilmeli |
| **5 Ekim** | **Tüm zorunlu materyallerin son inceleme tarihi** |
| 8 Ekim | Basın ön izlemesi başlar (fest'ten 11 gün önce) |
| **19 Ekim, 10:00 PDT** | Next Fest başlar |
| 26 Ekim, 10:00 PDT | Next Fest biter |

**Uygunluk şartları:** Çıkmamış oyun · daha önce Next Fest'e katılmamış · **yayında ve herkese açık "Coming Soon" sayfası** · fest başlarken oynanabilir demo · oyun fest bitmeden çıkmayacak. Bir oyun **yalnızca bir kez** Next Fest'e girebilir.

## 3.2 Geriye doğru hesap: gerçek son tarihin 31 Ağustos değil

Kayıt için sayfanın **zaten yayında** olması gerekiyor. Sayfanın yayına girmesi için:

```
31 Ağu  → kayıt kapanıyor (sayfa YAYINDA olmalı)
  ↑ 3-5 iş günü inceleme (+ reddedilirse tekrar)
~20 Ağu → sayfayı incelemeye gönder  ← GERÇEK SON TARİHİN BU
  ↑ Steam ilk gönderimde sık sık düzeltme ister, 2. tur payı bırak
~15 Ağu → güvenli hedef
  ↑ capsule + 5 ekran görüntüsü + açıklama metni hazır olmalı
```

Ayrıca **Steam Direct ücretini ödedikten sonra 30 günlük zorunlu bekleme süresi var** ve "Coming Soon" sayfası çıkıştan en az 2 hafta önce yayında olmalı. Bu süreler kısaltılamaz.

### ⏰ Bugün 30 Temmuz. Sanat için 2,5 haftan var ve sınav dönemin 15 Ağustos'ta başlıyor.

**Bu, planındaki en sıkışık nokta ve şu an farkında olmayabilirsin.** Yol haritanda Steam sayfası H6'ya (10–16 Ağustos) konmuş — bu tarih doğru ama o tarihe kadar capsule sanatının bitmiş olması gerekiyor, ki elinde hiç sprite yok.

## 3.3 Revize plan (30 Tem – 14 Ağu: sanat sprinti)

**Bu iki hafta kod yazma.** Kod zaten önde. Bu iki hafta Aseprite'ta geçmeli.

| Gün | İş |
|---|---|
| **30 Tem (bugün)** | Git commit + push. **Steam Direct 100 USD öde** — 30 günlük saat bugün başlasın. |
| 31 Tem – 2 Ağu | Öncelik 1 fixleri (2-3 saat) + karakter, 3 düşman, chip/XP sprite'ları |
| 3 – 6 Ağu | Kalan düşmanlar, silah sprite'ları, mermi/kart/zar görselleri, masa zemini |
| 7 – 9 Ağu | **Capsule görselleri** (616×353, 374×448, 460×215, 231×87). Bu 3 tam gün alır, hafife alma. |
| 10 – 12 Ağu | Sahneye sprite'ları uygula, 5–8 iyi ekran görüntüsü çek, Steam sayfa metnini yaz (TR+EN) |
| **13 – 15 Ağu** | **Sayfayı incelemeye gönder.** Reddedilirse düzeltip tekrar gönderecek payın kalsın. |

## 3.4 Sınav dönemi (15 Ağu – 10 Eyl) — hafif işler

- Sayfa incelemesinden düzeltme gelirse hallet
- **31 Ağustos'tan önce Next Fest kaydını yap** (sayfa yayına girer girmez)
- 18 Ağustos Next Fest Q&A yayınını izle
- BeepBox'ta müzik (2–3 parça: menü, masa, boss)
- BFXR/ChipTone'da SFX bankası
- 16×16 stat ikonları
- Figma'da mağaza + level-up ekranı taslakları

## 3.5 10 Eylül – 5 Ekim (son sprint)

| Dönem | İş |
|---|---|
| 10 – 16 Eyl | Dev Eldiven + Boss mekaniği |
| 17 – 22 Eyl | OnGUI → Canvas dönüşümü (mağaza, level-up, run sonu) |
| 23 – 26 Eyl | Ses sistemi + lokalizasyon + ayarlar menüsü |
| 27 Eyl – 1 Eki | **Playtest.** 10–15 arkadaşa dağıt. Farklı sistemler = bedava uyumluluk testi. |
| 2 – 4 Eki | Bugfix + balance + juice (screen shake, hit-flash, hit-stop) + **trailer** |
| **5 Eki** | **Demo build + tüm materyaller son gönderim** |

## 3.6 Trailer notu

Next Fest'te trailer, capsule'dan sonra en çok tıklanma getiren şey. 45–60 saniye yeterli. Yapı:

1. **0–3 sn:** Dev Eldiven kartı masaya çakıyor. Hiç yazı yok, direkt aksiyon. (İlk 3 saniyede izleyiciyi kaybediyorsun ya da kazanıyorsun.)
2. **3–15 sn:** Zar silahı sekiyor, AoE patlıyor, ekran doluyor
3. **15–30 sn:** Mağaza ekranı, build kurma, house rule seçimi
4. **30–45 sn:** Boss + kalabalık wave 7 kaosu
5. **45–50 sn:** Logo + "Wishlist on Steam"

DaVinci Resolve ücretsiz sürümü fazlasıyla yeter.

---

# BÖLÜM 4 — ~0 EURO PAZARLAMA PLANI

## 4.1 Bütçe gerçeği

| Kalem | Tutar | Zorunlu mu |
|---|---|---|
| Steam Direct | 100 USD (~92 EUR) | ✅ Zorunlu, kaçış yok |
| Aseprite | ~10 EUR | ✅ (aldıysan bitti) |
| **Kalan** | **~100–200 EUR** | Serbest |

**Kalan parayı reklama harcama.** 150 EUR'luk reklam bütçesi indie oyunda ölçülebilir hiçbir şey yapmaz — Steam'de wishlist başına maliyet reklamla 1-3 EUR bandındadır, yani ~75 wishlist alırsın. Aynı para tek bir profesyonel capsule'a gitse 10 katı getirir çünkü **capsule ömür boyu her gösterimde çalışır.**

**Öneri:** Kendi capsule'ünü yap, arkadaşlarına ve r/ImaginaryTechnology gibi yerlere göster, dürüst geri bildirim al. Capsule zayıf kalırsa kalan bütçeyi **capsule'a** harcayacak birini bul (Fiverr/ArtStation'da 80–150 EUR bandında iyi pixel art capsule yapan çok kişi var). Trailer kurgusu ikinci öncelik.

## 4.2 Temel matematik: neden şimdi başlamalısın

Next Fest'e girmen wishlist getirir ama **fest'e girerken zaten bir kitlen varsa 5–10 kat daha fazla getirir.** Steam algoritması ilk günün performansına göre görünürlük dağıtıyor. Fest'e 0 wishlist'le girersen listenin dibinde başlarsın.

Yani: **içerik paylaşımına bugünden başla, Ekim'de değil.** Sayfa henüz yokken bile klip atabilirsin.

## 4.3 Ücretsiz kanallar — etki sırasına göre

### 1. Dikey video (TikTok / YouTube Shorts / Instagram Reels) — EN YÜKSEK GETİRİ
Solo indie dev için bedava erişimin tek gerçek kaynağı. OBS ile kaydet, dikey kırp, 15–25 sn.

**Senin oyununun doğal viral materyali var:**
- **Dev Eldiven** — "masadaki gerçek oyuncu üstüne kart yapıştırıyor" konsepti tek başına bir video. Yorum kutusunu doldurur.
- **Zar mekaniği** — 6 gelince ekranın patlaması tatmin edici, izlenebilir.
- **4x aynı silah build'i** — "ne olur 4 tane zar takarsan?" formatı survivor-like izleyicisinin bayıldığı şey.
- **House Always Wins** (1 canla x3 hasar) — riskli build içeriği.

Haftada 3 video hedefle. İlk 20'si tutmayabilir, normal. Format: hook (0-2 sn) → mekanik → sonuç.

### 2. Reddit
Kurallara dikkat — çoğu sub self-promo'yu sınırlar, önce oku.
- `r/IndieDev`, `r/indiegames`, `r/gamedev` — geliştirme süreci paylaşımı
- `r/Unity2D` — teknik post'lar
- **`r/roguelites`** — asıl hedef kitlen burada
- `r/brotato`, `r/VampireSurvivors` — **çok dikkatli ol**, direkt reklam yasak. Önce aylarca gerçekten katıl, sonra "ilham aldığım oyun" bağlamında paylaş.
- `r/PixelArt`, `r/aseprite` — sanat paylaşımı, dolaylı tanıtım

### 3. Twitter/X + Bluesky
- `#ScreenshotSaturday` (Cumartesi), `#WishlistWednesday`, `#indiedev`, `#pixelart`, `#gamedev`
- Erişim düşük ama sıfır maliyet ve basın/streamer'lar buradan buluyor

### 4. Discord toplulukları
Survivor-like ve roguelite Discord'larına katıl. **Reklam yapmadan** aylarca gerçekten sohbet et. İnsanlar sonra kendiliğinden soruyor. Bu, en yavaş ama en sadık kitleyi getiren yol.

### 5. Küçük YouTuber/Streamer'lar — bedava ve etkili
500–20.000 abonesi olan, Brotato/VS/Halls of Torment gibi oyunlar oynayan kanallar. Bunlar:
- Cevap veriyor (büyük kanallar vermiyor)
- Demo key'i bedava
- Bir video 500–3000 wishlist getirebiliyor

**Yaklaşım:** Kısa mail. "Merhaba, [X oyunu] videonuzu izledim. Brotato ve Balatro karışımı bir survivor yapıyorum, demo Next Fest'te çıkıyor. İlgilenirseniz erken erişim verebilirim. 30 sn'lik klip: [link]" — 2 paragrafı geçme.

Hedef: 50 kanala mail at, 5-10 cevap gelir, 2-3 video çıkar.

### 6. itch.io
Next Fest exclusivity istemiyor. itch'te erken bir build yayınlamak bedava playtest + geri bildirim demek. Ama **Steam demo'sunu fest'ten önce çok yayma** — fest'teki "yeni demo" etkisini korumak istiyorsun.

### 7. presskit
Ücretsiz araçlar var (presskit() aracı, ya da basit bir Notion/itch sayfası). İçinde: 5 ekran görüntüsü, logo, capsule, 1 paragraf açıklama, trailer linki, iletişim. Streamer'lar bunu ister.

### 8. Türkiye avantajı — bunu değerlendir
Oyunun TR lokalizasyonlu doğuyor. Türk indie/roguelite YouTuber ve Twitch yayıncıları:
- Yabancı indie devlerden çok daha az mail alıyorlar → cevap oranın yüksek
- TR dil desteği gerçek bir satış argümanı
- Türk oyuncu kitlesi survivor-like'lara çok yatkın

Bu, çoğu solo devin sahip olmadığı bedava bir kanal. Kullan.

## 4.4 Steam sayfası optimizasyonu (bedava ama kritik)

**Etiketler (tags) trafiğin yarısını belirler.** Steam benzer oyunları etiketlerden eşleştiriyor. Öncelik sırasıyla:
`Bullet Heaven` · `Roguelite` · `Auto Battler` · `Pixel Graphics` · `Action Roguelike` · `2D` · `Survival` · `Card Game` · `Singleplayer` · `Indie` · `Arcade`

**Kısa açıklama (short description)** — arama sonuçlarında görünen 2 cümle. Kancayı ilk cümleye koy:
> "Blackjack oynamıyorsun — blackjack masasında hayatta kalıyorsun."

Bu cümle zaten GDD'nde var ve çok iyi. Kullan.

**"Neden bu oyun?" farklılaştırıcıları** (rakiplerinden ayrılan yönler):
- Casino teması (Balatro sonrası bu tema sıcak — bilinçli avantaj)
- Aynı silahtan 4 kopya takabilme (build derinliği, çoğu survivor-like'ta yok)
- Dev Eldiven gibi çevresel tehlike (masanın "dışarıdan" müdahale edilmesi konsepti özgün)

## 4.5 Zaman çizelgesi

| Dönem | Pazarlama işi |
|---|---|
| **Ağustos** | İlk sprite'lar çıkar çıkmaz #ScreenshotSaturday'e başla. Twitter/Bluesky hesabı aç. |
| **Eylül** | Sayfa yayında → wishlist toplamaya başla. Haftada 3 dikey video. Streamer listesi hazırla (50 kanal). |
| **1–15 Ekim** | Streamer mailleri git. Reddit'te "demo geliyor" postları. Trailer yayınla. |
| **19–26 Ekim** | **Fest haftası.** Her gün 1 klip at. Steam sayfası yorumlarına ve Discord'a cevap ver — fest sırasında aktif olmak algoritmik olarak da işine yarıyor. |
| **27 Ekim+** | Demo geri bildirimlerini topla, "Next Fest'te ne öğrendim" devlog'u yaz (bu tür postlar r/gamedev'de iyi gidiyor) |

## 4.6 Gerçekçi beklenti

Solo dev, ilk oyun, sıfır kitle, Next Fest ile: **1.000–5.000 wishlist** normal aralık. İyi bir capsule + tutan bir TikTok klibi bunu 10.000+'a çıkarabilir. 10.000 wishlist genelde "çıkışta anlamlı satış" eşiği olarak konuşulur.

**Wishlist 500'de kalırsa oyun kötü demek değil** — görünürlük demek. O yüzden capsule ve dikey video bu kadar önemli.

---

# BÖLÜM 5 — BU HAFTA YAPILACAKLAR (kısa liste)

1. ☐ Git commit + GitHub private push *(bugün, 15 dk)*
2. ☐ **Steam Direct 100 USD öde** *(bugün — 30 günlük saat başlasın)*
3. ☐ Öncelik 1 fixleri: RunEnd bağla, ExpandMap çağır, groupSize kullan *(2-3 saat)*
4. ☐ Excel `5_Waveler` Spawn/sn sütununu doldur *(30 dk)*
5. ☐ Zar seviyelemesi + sandık parası çelişkilerini karara bağla *(30 dk)*
6. ☐ Aseprite'ı aç, karakter sprite'ıyla başla *(kalan tüm zaman)*
7. ☐ Twitter/Bluesky hesabı aç, ilk placeholder ekran görüntüsünü at

---

## Kaynaklar

- [Steam Next Fest: October 2026 — Steamworks](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest/2026october)
- [Steam Next Fest — Steamworks](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest)
- [Steam Next Fest Tips — Steamworks](https://partner.steamgames.com/doc/marketing/upcoming_events/nextfest/tips)
- [Release Process — Steamworks](https://partner.steamgames.com/doc/store/releasing)
- [Coming Soon Pages — Steamworks](https://partner.steamgames.com/doc/store/coming_soon)
- [Store Review Process — Steamworks](https://partner.steamgames.com/doc/store/review_process)
