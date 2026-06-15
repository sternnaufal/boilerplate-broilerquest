# Dead Code Cleanup - 15 Juni 2026

## Ringkasan

Analisis dan pembersihan dead code di seluruh `Assets/Script/`. Total **~300 baris** dead code diidentifikasi, plus **2 kelas orphaned** dihapus.

## Yang Dihapus

### 1. Dua Kelas Orphaned

| File | Alasan |
|------|--------|
| `Assets/Script/UI/PanelManager.cs` | Tidak direference scene/prefab/script manapun |
| `Assets/Script/UI/SafeAreaAdjuster.cs` | Tidak pernah di-attach ke GameObject manapun |

### 2. GameStateManager.cs — 4 Dead Method

| Method | Line | Alasan |
|--------|------|--------|
| `SetMenu()` | 39-41 | Semua state change lewat `ApplyState()` / `SetGameState()` |
| `SetPlaying()` | 43-45 | Tidak pernah dipanggil |
| `SetPaused()` | 47-49 | Tidak pernah dipanggil |
| `SetGameOver()` | 51-53 | Tidak pernah dipanggil |

### 3. StarterGameplayUI.cs — Dead Field & Commented Code

| Item | Alasan |
|------|--------|
| `bool iotCreated` | Deklarasi doang, gak pernah dibaca/ditulis |
| `//StylePanel(hpPanel, hpPanelStyle.color)` | Kode stale, HP panel styling udah handled di tempat lain |
| `//PositionHpPanel()` | Kode stale, positioning sekarang dinamis di `Start()` |

### 4. StarterKandangSlot.cs + KandangSlot.prefab — Unused Field

| Item | Alasan |
|------|--------|
| `[SerializeField] private bool clearChickenOnHealthFail` | Field dideklarasikan tapi tidak pernah dibaca di kode mana pun |
| `clearChickenOnHealthFail: 0` di `KandangSlot.prefab` | Dihapus mengikuti field yang sudah dihapus |

### 5. UIManager.cs — Dead Method

| Method | Alasan |
|--------|--------|
| `ShowMainScreen()` | Hanya delegasi ke `ShowMainMenu()`, tidak pernah dipanggil |

## Yang Ditahan (Tidak Dihapus)

| Item | Alasan |
|------|--------|
| `GlobalUIOverlay.cs` | Masih direference di `KoleksiIoT.unity` via `GlobalUIOverlay.prefab`. Perlu ganti prefab ke `GlobalCoinPakanCounter` di Editor dulu sebelum hapus |
| `mainMenuButton` field di `StarterGameplayUI.cs` | Serialized field — scene masih simpan reference. Hapus field bikin Unity warning "missing field". Button sengaja di-set inactive |
| `SetSfxVolume()` di `UIManager.cs` | Di-wire ke SFX slider via `ButtonHelper.AddListenerOnce` |
| `TryConsumeFeed()` di `FeedManager.cs` | Dipanggil `StarterKandangSlot.cs:219` |
| `flipRoutine` di `MemoryMatchCard.cs` | Dipake `StopCoroutine` untuk membatalkan animasi flip |
| 6 method di `PlayerMovement.cs` (line 267-311) | Tidak ada caller C#, tapi mungkin dipanggil via Unity Animation Events. Cek manual di Editor: Animator → Animation Clip → Events tab |

## File yang Berubah

| Status | File |
|--------|------|
| Dihapus | `Assets/Script/UI/PanelManager.cs` |
| Dihapus | `Assets/Script/UI/PanelManager.cs.meta` |
| Dihapus | `Assets/Script/UI/SafeAreaAdjuster.cs` |
| Dihapus | `Assets/Script/UI/SafeAreaAdjuster.cs.meta` |
| Diubah | `Assets/Script/Managers/GameStateManager.cs` |
| Diubah | `Assets/Script/Gameplay/StarterGameplayUI.cs` |
| Diubah | `Assets/Script/Gameplay/StarterKandangSlot/StarterKandangSlot.cs` |
| Diubah | `Assets/Prefab/KandangSlot.prefab` |
| Diubah | `Assets/Script/UI/UIManager.cs` |

## Commit

- **Branch:** `dev/hylmi`
- **Commit:** `1917e5d`
- **Pesan:** `chore: hapus dead code & orphaned classes`
- **Status:** Pushed to origin

## Catatan

- `GameConstants.cs` tetap jadi single source of truth — tidak ada konstanta yang dihapus
- `GlobalUIOverlay.cs` perlu diurus next: ganti prefab di `KoleksiIoT.unity` ke `GlobalCoinPakanCounter`, baru hapus file + prefab
- `PlayerMovement.cs` 6 method perlu dicek Animation Events di Unity Editor sebelum dihapus
