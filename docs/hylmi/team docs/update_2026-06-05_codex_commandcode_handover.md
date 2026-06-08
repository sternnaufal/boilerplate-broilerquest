# Update Handover - Codex + Command Code Collaboration

Tanggal: 5 Juni 2026
Branch kerja: `dev/Hylmi`

Dokumen ini merangkum update panjang yang terjadi selama sesi kolaborasi antara Hylmi, Command Code/DeepSeek, dan Codex. Fokus utamanya adalah stabilisasi gameplay Starter, save system, merge dari `dev/naufal`, dan pembersihan working tree agar pengembangan berikutnya lebih nyaman.

## Ringkasan Singkat

- Branch `dev/naufal` sudah digabung ke `dev/Hylmi`.
- Gameplay Starter mendapatkan beberapa fix penting terkait timer, popup, save/restore ayam, IoT active state, coin, feed, dan cleanup API.
- Save system dari `dev/naufal` sudah diintegrasikan ke scene `Starter` dengan `StarterSceneInitializer`.
- Local PlayerPrefs untuk player lokal sudah sempat direset beberapa kali untuk menghapus sisa coin/feed/save test.
- Working tree sudah dirapikan: docs yang dipindah ke `docs/hylmi/` terbaca sebagai rename, noise editor Unity dibersihkan, dan perubahan intentional sudah di-stage.

## Alur Kerja

1. Command Code digunakan sebagai implementer utama untuk beberapa code fix.
2. Codex melakukan review, verifikasi Unity MCP, dan koreksi jika ada gap.
3. Hylmi melakukan playtest langsung di Unity dan melaporkan behavior aktual.
4. Hasil playtest digunakan untuk menemukan masalah integrasi scene yang tidak terlihat dari compile-only verification.

Pola ini terbukti berguna karena beberapa bug bukan bug C# murni, tetapi scene wiring/configuration issue di Unity.

## Perubahan dari Command Code

### GameManager dan PopupKesehatan

Command Code mengerjakan fix untuk bug prioritas tinggi:

- `GameManager.OnDestroy` dan scene unload cleanup untuk mencegah dangling delegate ke `LevelTimer` yang sudah destroyed.
- `PopupKesehatan.OnStopClicked` null guard ketika `Canvas` tidak ditemukan.
- `GameManager.OnTimerUp` fallback recovery agar player tidak softlock kalau time-up popup gagal muncul.

Codex kemudian memverifikasi lewat Unity MCP:

- `GameManager.cs` valid.
- `PopupKesehatan.cs` valid.
- Unity console bersih setelah verifikasi.

### Starter Kandang Slot dan Feed

Command Code juga mengerjakan:

- Atomic chicken placement di `StarterKandangSlot.TryPlaceChicken`.
- Cleanup partial spawn agar visual ayam tidak leak jika placement gagal.
- Penambahan `FeedManager.TryConsumeFeed`.

Codex menemukan satu masalah setelah implementasi awal:

- `CreateChickenVisual()` masih menambahkan visual ke `spawnedChickens`, sementara `TryPlaceChicken()` juga melakukan `AddRange(newVisuals)`.
- Ini berpotensi membuat duplicate reference ke visual ayam.

Fix kemudian dilakukan:

- `CreateChickenVisual()` hanya membuat/aktifkan visual dan assign animator.
- Commit ke `spawnedChickens` hanya terjadi di `TryPlaceChicken()` setelah semua visual berhasil dibuat.

### FeedManager Cleanup

Awalnya `TryConsumeFeed()` berisi logic yang sama persis dengan `UseFeed()`. Behavior tidak rusak, tetapi API menjadi duplikatif.

Cleanup akhir:

```csharp
public bool TryConsumeFeed(int amount)
{
    return UseFeed(amount);
}
```

Hasil:

- Logic konsumsi feed punya satu sumber kebenaran.
- Caller tetap bisa memakai nama method yang lebih jelas.
- `FeedManager.cs` valid lewat Unity MCP.

## Merge dari dev/naufal

Branch `origin/dev/naufal` sudah digabung ke `dev/Hylmi`.

Update utama dari `dev/naufal`:

- Save system baru melalui `SaveManager`.
- IoT product collection dan active-state persistence.
- Global UI binder untuk coin/pakan.
- Scene `Beginner` dan `Intermediate` mendapatkan isi baru.
- Player animation/assets baru.
- UI tambahan seperti `SafeAreaAdjuster` dan `UIAlertPanel`.

Saat merge, beberapa konflik diselesaikan:

- `CoinManager.cs`: mengikuti arsitektur baru event-based dari `dev/naufal`, karena UI binding sekarang ditangani `UIGlobalBinder`.
- `StarterKandangSlot.cs`: menggabungkan atomic placement dari local fix dengan save system dari `dev/naufal`.
- `UserSettings/*`: dipertahankan sebagai local/editor preference dan tidak dianggap bagian gameplay.
- Dangling `.meta` scene initializer lama dibersihkan karena file `.cs` terkait sudah tidak ada.

Commit penting yang dibuat:

- `2ea9903 WIP: preserve local gameplay fixes before naufal merge`
- `5fac621 Merge branch 'dev/naufal' into dev/Hylmi`

Catatan: branch belum dipush dari sesi ini.

## Save System: Temuan dan Fix

### Masalah yang Ditemukan

Hylmi melakukan playtest dan menemukan:

- Ayam tidak tersimpan/restore.
- Pakan tersimpan, bahkan membawa sisa sesi sebelumnya.
- IoT toggle kembali OFF.

Analisis Codex:

- Pakan tersimpan karena `FeedManager` langsung memakai `PlayerPrefs`.
- Ayam dan IoT active state memakai `SaveManager`, tetapi restore path ada di `StarterSceneInitializer`.
- `Starter.unity` tidak memiliki komponen `StarterSceneInitializer`, sehingga restore tidak pernah dipanggil.

### Fix yang Dilakukan

Di `Assets/Scenes/Starter.unity`:

- Menambahkan root GameObject `StarterSceneInitializer`.
- Menambahkan komponen `StarterSceneInitializer`.
- Menghubungkan:
  - `chickenShop` ke `BQ_HPPanel` yang memiliki `StarterChickenShop`.
  - `kandangSlots` ke 4 slot kandang di scene.
  - `initializeGameManager = true`.
  - `initializeCoinManager = true`.
  - `loadSavedState = true`.

Di `Assets/Script/IoT/StarterIoTController.cs`:

- `SetActiveStates(...)` sekarang memanggil `RefreshAll()` setelah load state.
- Tujuannya agar UI IoT langsung menampilkan ON/OFF sesuai saved active state.

Di scene gameplay:

- `resetCoinOnStart` diubah menjadi `false` pada:
  - `Assets/Scenes/Starter.unity`
  - `Assets/Scenes/Beginner.unity`
  - `Assets/Scenes/Intermediate.unity`

Alasan:

- `CoinManager.LoadSavedCoin()` sudah fallback ke `GameConstants.Economy.StartingCoin` jika belum ada save.
- Dengan `resetCoinOnStart = true`, coin tersimpan bisa tertimpa setiap scene dimulai.

### SaveManager Cleanup

Sebelumnya:

```csharp
private const string SaveKey = "BroilerQuest.GameSave";
```

Padahal key yang sama sudah ada di:

```csharp
GameConstants.Persistence.GameSaveKey
```

Cleanup:

```csharp
private const string SaveKey = GameConstants.Persistence.GameSaveKey;
```

Hasil:

- Tidak ada duplikasi string key save.
- Save key sekarang terpusat di `GameConstants.Persistence`.

## Local Player Reset

Untuk menghindari data test lama mengganggu playtest, PlayerPrefs lokal BroilerQuest sudah direset.

Key yang sempat dibersihkan:

- `BroilerQuest.GameSave`
- `BroilerQuest.TotalCoin`
- `TotalCoin`
- `BroilerQuest.FeedCount`
- `Level.Beginner.Unlocked`
- `Level.Intermediate.Unlocked`
- `KoleksiIoT.Purchased.AutoFeeder`
- `KoleksiIoT.Purchased.AutoFan`
- `KoleksiIoT.Purchased.AutoHeater`

Setelah reset terakhir, verifikasi menunjukkan tidak ada key gameplay BroilerQuest yang tersisa di `PlayerPrefs`.

Expected fresh play:

- Coin mulai dari `GameConstants.Economy.StartingCoin`, yaitu `400`.
- Feed mulai dari `0`.
- Tidak ada ayam tersimpan.
- IoT belum purchased/active.

## Verifikasi Unity MCP

Verifikasi yang sudah dilakukan:

- `SaveManager.cs`: valid, 0 diagnostics.
- `StarterSceneInitializer.cs`: valid, 0 diagnostics.
- `StarterIoTController.cs`: valid, 0 diagnostics.
- `FeedManager.cs`: valid, 0 diagnostics.
- Runtime start di scene `Starter`: tidak ada console error/warning.
- `StarterSceneInitializer` terdeteksi 1 instance di Play Mode.
- `StarterKandangSlot` terdeteksi 4 instance di Play Mode.
- Unity console terakhir bersih: 0 error/warning.

## Working Tree Cleanup

Docs tidak dihapus. File lama di root `docs/` dipindahkan ke `docs/hylmi/`.

Git sekarang membaca perpindahan tersebut sebagai rename `R100`, bukan delete + untracked:

- `docs/BUG-BUG-FIX.md -> docs/hylmi/BUG-BUG-FIX.md`
- `docs/Copilot_handover.md -> docs/hylmi/Copilot_handover.md`
- `docs/handover_update.md -> docs/hylmi/handover_update.md`
- `docs/implementation_plan.md -> docs/hylmi/implementation_plan.md`
- `docs/patch_2026-06-05.md -> docs/hylmi/patch_2026-06-05.md`
- `docs/refactoring_report_2026-05-28.md -> docs/hylmi/refactoring_report_2026-05-28.md`
- `docs/refactoring_report_verification_2026-05-28.md -> docs/hylmi/refactoring_report_verification_2026-05-28.md`

Noise editor yang dibersihkan:

- `UserSettings/EditorUserSettings.asset`
- `Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`

Perubahan intentional yang sekarang di-stage:

- `.gitignore`
- `Assets/Scenes/Starter.unity`
- `Assets/Scenes/Beginner.unity`
- `Assets/Scenes/Intermediate.unity`
- `Assets/Script/IoT/StarterIoTController.cs`
- `Assets/Script/Managers/FeedManager.cs`
- `Assets/Script/SaveSystem/SaveManager.cs`
- Rename docs ke `docs/hylmi/`

Belum ada commit baru setelah cleanup ini.

## Status Saat Ini

Gameplay/code status:

- Compile/validation bersih.
- Save restore ayam sudah tersambung di scene `Starter`.
- IoT active state seharusnya restore dan refresh UI.
- Feed API sudah dirapikan.
- Coin tidak lagi reset setiap scene start.

Update tambahan 8 Juni 2026:

- `gakusahnama` sudah ditarik ke `dev/Hylmi`; local HEAD berada di `01e2887`.
- Setelah update, beberapa bugfix gameplay/save/UI lama direstore dan dicatat di `bugfix_refactor_log_2026-06-08.md`.
- Fitur care bubble expiry untuk `StarterKandangSlot` sudah diimplementasikan:
  - default expiry 10 detik melalui `GameConstants.StarterSlot.BubbleExpiryDuration`.
  - timer dimulai saat care bubble terlihat.
  - pause dan puzzle menghentikan countdown tanpa reset dalam sesi scene yang sama.
  - keluar lalu masuk lagi ke scene gameplay me-refresh active bubble ke 10 detik baru.
  - bubble expired digolongkan sebagai gagal dan memengaruhi harga jual lewat flow failure yang sama dengan puzzle failure.
  - save data menyimpan `hasActiveBubble` dan `activeBubbleNeed` agar active bubble bisa direstore.
- Detail behavior dan QA checklist fitur ini ada di `bubble_expiry_update_2026-06-08.md`.

Git status:

Catatan: poin di bawah ini adalah status historis dari sesi 5 Juni, bukan status working tree saat ini.
Untuk status setelah update 8 Juni, lihat `bugfix_refactor_log_2026-06-08.md` dan `bubble_expiry_update_2026-06-08.md`.

- Perubahan intentional sudah staged.
- Tidak ada unstaged diff setelah cleanup terakhir.
- Belum commit.

## Rekomendasi Playtest Berikutnya

Jalankan test manual ini sebelum commit final:

1. Play `Starter`.
2. Pastikan fresh state:
   - coin `400`
   - pakan `0`
   - tidak ada ayam
   - IoT belum aktif
3. Beli ayam.
4. Beli pakan.
5. Jika coin cukup, beli dan toggle salah satu IoT.
6. Kembali ke Main Menu lewat tombol game, bukan langsung stop Play Mode.
7. Masuk lagi ke `Starter`.
8. Pastikan:
   - ayam muncul lagi di kandang
   - pakan sesuai sisa terakhir
   - coin tidak reset ke nilai salah
   - IoT active state sesuai toggle terakhir

Jika test ini lolos, perubahan save system bisa dianggap siap commit.

## Sisa Catatan Clean Code

Masih ada beberapa debt kecil yang bisa dibahas nanti:

- `SaveManager.SaveAll()` masih mencari slot dengan `FindObjectsByType`; ini oke untuk skala kecil, tetapi bisa diganti dependency eksplisit jika gameplay membesar.
- `UIGlobalBinder` masih mencari `CoinText` dan `PakanText` berdasarkan nama GameObject. Ini fragile, tetapi untuk sementara sudah bekerja.
- `GlobalUIOverlay` diberi status obsolete, tetapi masih ada code lama yang membuat UI lewat script. Perlu diputuskan apakah akan dihapus total atau dipertahankan sebagai fallback.

## Brainstorming Minigame Berikutnya

Konteks permintaan:

- Target audience: anak SD, SMP, dan SMA.
- Jangan terlalu sulit sampai membuat player berhenti main.
- Tetap engaging dan punya potensi dimainkan berulang.
- Jangan menyulitkan artist game; utamakan ide yang bisa memakai sprite, UI, dan aset ayam/kandang yang sudah ada.

### 1. Sortir Pakan

Player drag pakan bagus ke karung hijau dan pakan jelek ke tong sampah.

- Gameplay: drag and drop cepat.
- Edukasi: kualitas pakan ayam.
- Beban art: ringan, cukup 3-5 sprite pakan dan 2 container.
- Cocok untuk: SD-SMP.

### 2. Isi Takaran Pakan

Player menahan tombol untuk mengisi wadah sampai garis target, jangan kurang atau lebih.

- Gameplay: timing sederhana.
- Edukasi: pakan harus sesuai takaran.
- Beban art: ringan, cukup wadah, bar meter, dan visual pakan.
- Cocok untuk: SD-SMA karena bisa dibuat makin cepat di level tinggi.

### 3. Bersihkan Kandang

Player tap atau swipe noda/kotoran di kandang sebelum waktu habis.

- Gameplay: tap/swipe objek kotor.
- Edukasi: kebersihan kandang memengaruhi kesehatan ayam.
- Beban art: ringan, cukup overlay noda dan ikon alat bersih-bersih.
- Cocok untuk: SD-SMP.

### 4. Atur Suhu Kandang

Player menggeser slider suhu ke zona aman.

- Gameplay: slider dengan zona hijau.
- Edukasi: ayam butuh suhu stabil.
- Beban art: ringan, cukup termometer UI dan indikator warna.
- Cocok untuk: SD-SMA.

### 5. Tangkap Ayam Kabur

Ayam bergerak pelan di area kecil, player tap untuk mengembalikan ayam ke kandang.

- Gameplay: tap target bergerak.
- Edukasi: kandang perlu dijaga aman.
- Beban art: rendah, bisa memakai sprite ayam yang sudah ada.
- Cocok untuk: SD-SMP.

### 6. Pasang IoT Cepat

Player mencocokkan ikon alat IoT ke slot yang benar: feeder, fan, heater.

- Gameplay: matching ikon.
- Edukasi: fungsi perangkat IoT di kandang.
- Beban art: ringan, cukup ikon alat dan slot.
- Cocok untuk: SMP-SMA.

### 7. Cek Kesehatan Cepat

Muncul 3 gejala sederhana, player memilih tindakan yang sesuai: pakan, kipas, heater, atau cek kesehatan.

- Gameplay: pilihan cepat berbasis kondisi.
- Edukasi: mengenali kebutuhan ayam.
- Beban art: ringan, cukup ikon gejala dan tombol tindakan.
- Cocok untuk: SMP-SMA.

### 8. Ritme Pemberian Pakan

Player tap saat indikator masuk area hijau, seperti rhythm minigame tetapi lambat dan ramah anak.

- Gameplay: timing tap.
- Edukasi: jadwal pakan perlu teratur.
- Beban art: ringan, cukup bar ritme dan ikon pakan.
- Cocok untuk: SD-SMA.

### 9. Susun Jalur Air

Puzzle kecil menyambungkan pipa dari tangki ke tempat minum ayam.

- Gameplay: rotate tile sederhana.
- Edukasi: air minum penting untuk ayam.
- Beban art: sedang-ringan, cukup 3-4 bentuk tile pipa.
- Cocok untuk: SMP-SMA.

### 10. Packing Hasil Panen

Player memasukkan hasil panen ke kotak sesuai warna, ukuran, atau label.

- Gameplay: sorting cepat.
- Edukasi: panen dan distribusi.
- Beban art: ringan, cukup kotak, produk, dan label.
- Cocok untuk: SD-SMP.

### Opsi Tambahan dari Command Code

Daftar berikut berasal dari brainstorming Command Code. Beberapa sangat cocok langsung dipakai, beberapa lain perlu adaptasi agar tetap sesuai tema BroilerQuest yang lebih dekat ke ayam broiler, kandang, pakan, suhu, kesehatan, dan IoT.

### 11. Egg Drop Catch

Telur bergulir atau jatuh dari perch/conveyor, player menangkapnya dengan basket sebelum pecah.

- Gameplay: tap/drag menangkap objek jatuh.
- Edukasi: ketelitian saat panen.
- Beban art: ringan, cukup telur, basket, dan perch/conveyor.
- Catatan: lebih cocok untuk ayam petelur; perlu adaptasi jika BroilerQuest tetap fokus broiler.

### 12. Feed Flinger

Player memakai power bar untuk melempar pakan ke trough pada jarak berbeda.

- Gameplay: aiming sederhana dengan power meter.
- Edukasi: distribusi pakan ke tempat yang tepat.
- Beban art: ringan-sedang, cukup pellet pakan, trough, dan alat lempar.
- Catatan: seru, tetapi physics aiming perlu tuning agar tidak menyulitkan anak SD.

### 13. Scratch & Peck

Bug seperti cacing atau serangga muncul dari patch tanah, player tap agar ayam mematuknya.

- Gameplay: whack-a-mole sederhana.
- Edukasi: bisa diadaptasi menjadi kontrol hama/kebersihan kandang.
- Beban art: ringan, cukup 3 tipe bug, patch tanah, dan efek kecil.
- Catatan: kandidat kuat jika narasinya diubah menjadi "bersihkan hama di kandang".

### 14. Egg Sorter

Conveyor membawa telur dengan warna/ukuran berbeda, player drag ke basket yang sesuai.

- Gameplay: sorting cepat.
- Edukasi: klasifikasi hasil panen.
- Beban art: ringan, cukup telur varian, basket, dan belt.
- Catatan: gameplay bagus, tetapi tema telur lebih cocok untuk ayam petelur daripada broiler.

### 15. Chicken Herd

Player menggiring ayam yang tersebar agar kembali ke pintu kandang sebelum malam.

- Gameplay: tap/arah sederhana untuk mengarahkan ayam.
- Edukasi: menjaga ayam tetap aman di area kandang.
- Beban art: sedang, karena butuh movement ayam dan beberapa obstacle.
- Catatan: cocok, tetapi lebih mahal dari minigame berbasis UI/tap statis.

### 16. Nest Memory Match

Card matching dengan pasangan item farm seperti ayam, pakan, biji, dan anak ayam.

- Gameplay: flip card memory.
- Edukasi: pengenalan item peternakan.
- Beban art: ringan, cukup card back dan beberapa icon.
- Catatan: mudah dibuat, tetapi terasa generic dan kurang terhubung langsung ke loop kandang.

### 17. Morning Chores Dash

Player menyelesaikan urutan tugas cepat: isi pakan, refill air, bersihkan kandang, cek suhu, dan cek ayam.

- Gameplay: sequence tap cepat.
- Edukasi: rutinitas perawatan kandang.
- Beban art: ringan, cukup 4-5 icon task dan timer.
- Catatan: salah satu kandidat terbaik; bisa diadaptasi menjadi "Perawatan Kandang Cepat".

### 18. Egg Decorator

Player menghias telur dengan warna, garis, stiker, dan pola tanpa fail state.

- Gameplay: creative activity.
- Edukasi: lebih ke reward/ekspresi kreatif daripada manajemen kandang.
- Beban art: ringan-sedang, butuh template telur dan beberapa decal.
- Catatan: aman untuk anak-anak, tetapi kurang nyambung ke core gameplay broiler.

### 19. Storm Prep

Awan gelap datang, player memindahkan ayam ke shelter dan menutup pakan dengan tarp sebelum hujan.

- Gameplay: timed tap/drag.
- Edukasi: persiapan kandang saat cuaca buruk.
- Beban art: sedang-ringan, butuh overlay awan, tarp, shelter, dan efek hujan sederhana.
- Catatan: kandidat kuat untuk variasi event di level Beginner/Intermediate.

### 20. Chick Chase

Baby chick auto-run, player tap untuk lompat melewati genangan, pagar, dan mengambil biji.

- Gameplay: side-scrolling runner sederhana.
- Edukasi: lebih ke refleks daripada edukasi peternakan.
- Beban art: sedang, butuh animasi chick run/jump dan obstacle.
- Catatan: engaging, tetapi mulai terasa seperti genre terpisah dan lebih mahal dibuat.

### Rekomendasi Prioritas

Prioritas paling aman untuk implementasi pertama:

1. `Bersihkan Kandang`
2. `Atur Suhu Kandang`
3. `Isi Takaran Pakan`
4. `Morning Chores Dash` / `Perawatan Kandang Cepat`

Alasan:

- Paling mudah dipahami oleh anak SD sampai SMA.
- Tidak butuh asset kompleks.
- Bisa dibuat dari UI sederhana.
- Sangat nyambung dengan loop utama BroilerQuest: merawat ayam, menjaga kandang, dan menyeimbangkan kebutuhan ayam.

Ide yang paling cocok untuk variasi berikutnya:

- `Pasang IoT Cepat`, karena sudah terhubung dengan fitur IoT yang ada.
- `Cek Kesehatan Cepat`, karena bisa memperkaya minigame kesehatan tanpa membuat puzzle terlalu sulit.
- `Susun Jalur Air`, jika tim ingin minigame yang sedikit lebih puzzle-oriented untuk anak SMP/SMA.
- `Scratch & Peck`, jika diadaptasi menjadi minigame kontrol hama/kebersihan kandang.
- `Storm Prep`, jika tim ingin event cuaca yang terasa berbeda dari aktivitas harian.
- `Chicken Herd`, jika tim siap membuat sedikit behavior gerak ayam.

## Pesan Untuk PM / Teammate

Versi singkat:

> Update dari `dev/naufal` sudah masuk ke `dev/Hylmi`. Save system sempat tidak restore ayam karena `StarterSceneInitializer` belum terpasang di scene `Starter`, sudah dibenerin dan diverifikasi lewat Unity MCP. Feed/coin/IoT juga sudah dirapikan. Working tree sudah dipilah: docs pindah ke `docs/hylmi/`, noise editor dibersihkan, perubahan intentional sudah staged. Tinggal playtest ulang save ayam + IoT sebelum commit final.
