# Refactoring Report Verification — 28 Mei 2026

## Metadata Verifikasi

- Branch lokal: `dev/Hylmi`
- Commit sumber refactor: `f603f88`
- Tanggal verifikasi: 28 Mei 2026
- Scope: verifikasi dokumentasi dan kondisi codebase setelah refactor; tidak mengubah script, scene, prefab, atau asset Unity.

## Ringkasan

Dokumen `docs/refactoring_report_2026-05-28.md` tetap berguna sebagai catatan historis refactor, tetapi beberapa bagian sudah tidak sinkron dengan kondisi repo saat ini. Verifikasi ini mencatat klaim yang masih benar, bagian yang perlu dikoreksi jika laporan asli nanti diedit, dan nilai aktual yang terukur dari codebase.

## Verified Accurate

- Build `Assembly-CSharp.csproj` berhasil dengan `0 Warning(s)` dan `0 Error(s)`.
- Struktur folder refactor sudah aktif: `Core`, `Managers`, `UI`, `Scene`, `Gameplay`, `IoT`, dan `Minigame`.
- `SetGameStateOrFallback()` sudah tidak ada di script aktif; pemanggil sekarang memakai `GameStateManager.ApplyState(...)`.
- Dead-code cleanup utama terverifikasi: `KandangController.cs`, `BuyOption0/1/2`, `TampilkanPopup(...)`, `GameLog.Verbose`, `CreateFallbackCard()`, `EnsureProductContainer()`, `cardInstances`, dan `productCardPrefab` tidak muncul di script/scene/prefab aktif.
- `KoleksiIoTController` sudah memakai referensi scene untuk `productContainer` dan `backButton`, lalu mencari kartu berdasarkan nama produk.
- `GlobalUIOverlay` masih aktif dan masih direferensikan di scene `Starter`, scene `KoleksiIoT`, serta prefab `GlobalUIOverlay`.
- Catatan pending runtime UI masih relevan: `JigsawMinigameController`, `StarterChickenShop`, dan `StarterGameplayUI` masih punya jalur pembuatan UI/runtime styling yang belum sepenuhnya prefab-first.

## Report Issues Found

- Laporan asli masih menulis `Assets/Script/ (30 file .cs)`, sedangkan jumlah aktual sekarang adalah `33` file `.cs`.
- Pohon folder laporan asli masih mencantumkan `Gameplay/KandangController.cs`, padahal file itu sudah dihapus.
- Ringkasan folder `Gameplay/ (3)` sudah stale. Kondisi aktual adalah 2 script langsung plus 6 partial file di subfolder `StarterKandangSlot`.
- Line count partial `StarterKandangSlot` di laporan asli sudah stale.
- Pending item `FeedManager.hasInitialized` sudah stale. `FeedManager` tidak memiliki field tersebut dan build saat ini tidak menghasilkan warning `CS0414`.
- Section `Done (Dead Code Cleanup — 28 Mei 2026)` muncul dua kali di laporan asli.

## Comparison With `origin/dev/naufal`

- `HEAD` lokal berada di commit yang sama dengan `origin/dev/naufal`: `f603f88`.
- Script, scene, dan prefab lokal sama dengan `origin/dev/naufal` saat verifikasi.
- Laporan asli `docs/refactoring_report_2026-05-28.md` sama dengan versi di `origin/dev/naufal`.
- File verifikasi ini belum ada di `origin/dev/naufal`; ini adalah tambahan lokal untuk mencatat hasil pengecekan setelah pull.
- Perbedaan lokal lain yang tidak berasal dari `origin/dev/naufal`: update `AGENTS.md` dari refresh SigMap sebelumnya dan folder screenshot yang belum tracked.

## Correct Current Values

Struktur script aktual:

```text
Assets/Script/ (33 file .cs)
├── Core/              (5)  ButtonHelper, CoroutineHelper, GameConstants, GameLog, Singleton
├── Managers/          (5)  CoinManager, FeedManager, GameManager, GameStateManager, LevelTimer
├── UI/                (6)  GlobalUIOverlay, PanelManager, PopupHasilKesehatan, TimeUpPopup, UIGlobalBinder, UIManager
├── Scene/             (3)  LevelSelectController, SceneController, StarterSceneInitializer
├── Gameplay/          (2)  StarterChickenShop, StarterGameplayUI
│   └── StarterKandangSlot/  (6 partial files)
├── IoT/               (2)  KoleksiIoTController, StarterIoTController
└── Minigame/          (4)  IHealthCheckListener, JigsawMinigameController, JigsawPiece, PopupKesehatan
```

Line count aktual `StarterKandangSlot`:

| File | Lines |
|------|------:|
| `StarterKandangSlot.cs` | 469 |
| `StarterKandangSlot.Animation.cs` | 94 |
| `StarterKandangSlot.BubbleUI.cs` | 154 |
| `StarterKandangSlot.ChickenVisual.cs` | 94 |
| `StarterKandangSlot.Health.cs` | 83 |
| `StarterKandangSlot.Wander.cs` | 184 |
| **Total** | **1078** |

Status build aktual:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

Status Unity console saat verifikasi:

- Tidak ada warning/error project dari script BroilerQuest.
- Ada satu warning dari tooling MCP: `[WebSocket] Unexpected receive error: WebSocket is not initialised`.

## Recommended Fix If Editing Original Report Later

- Hapus semua referensi `KandangController.cs` dari pohon folder final.
- Ubah jumlah file dari `30 file .cs` menjadi `33 file .cs`.
- Update ringkasan `Gameplay` menjadi 2 script langsung plus 6 partial file `StarterKandangSlot`.
- Update line count dan total `StarterKandangSlot` sesuai tabel di atas.
- Hapus pending item `FeedManager.hasInitialized` karena sudah tidak berlaku.
- Deduplicate section `Done (Dead Code Cleanup — 28 Mei 2026)`.
- Pertahankan catatan bahwa `GlobalUIOverlay` masih aktif dan bahwa beberapa runtime UI path masih menjadi kandidat refactor berikutnya.

## Verification Commands

Command yang dipakai untuk verifikasi:

```powershell
Get-ChildItem Assets/Script -Recurse -Filter *.cs
Get-ChildItem Assets/Script/Gameplay/StarterKandangSlot -Filter *.cs
rg -n "KandangController|BuyOption0|BuyOption1|BuyOption2|TampilkanPopup|FeedManager\.hasInitialized|GameLog\.Verbose|CreateFallbackCard|EnsureProductContainer|cardInstances|productCardPrefab|SetGameStateOrFallback" Assets/Script Assets/Scenes Assets/Prefab docs/refactoring_report_2026-05-28.md
dotnet build .\Assembly-CSharp.csproj --nologo
git diff --quiet origin/dev/naufal -- Assets/Script Assets/Scenes Assets/Prefab
git diff --quiet origin/dev/naufal -- docs/refactoring_report_2026-05-28.md
```
