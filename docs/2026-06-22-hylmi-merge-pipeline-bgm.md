# Session 2026-06-22: Hylmi Merge, Pipeline Logic, BGM Continuity

## 1. Merge Analysis: `origin/dev/hylmi`

**2 commits baru** vs `dev/naufal`:

| Commit | Deskripsi |
|--------|-----------|
| `1db5f55` | Phase 2 fixes: Singleton CopySettingsFrom, robust canvas/timer search, wiring fix, grant tool, resetCoinOnStart=false |
| `17bbe96` | (shared with adim) restore UI elements, fix humidity toggle, save versioning, defensive guards |

### Diambil langsung (safe cherry-pick)

| File | Path | Isi |
|------|------|-----|
| GameManager | `Assets/Script/Managers/GameManager.cs` | Auto-detect level index dari scene name, fallback canvas/timer search (`StarterCanvas` → `FindFirstObjectByType<Canvas>`), auto-add `LevelTimer` component, assign `TimerText` langsung ke LevelTimer |
| LevelTimer | `Assets/Script/Managers/LevelTimer.cs` | Multi-level `EnsureTimerText()`: `GameObject.Find` → search all Canvases (incl. inactive) → search all TMP objects |
| LevelSceneInitializer | `Assets/Script/Scene/LevelSceneInitializer.cs` | Field `initializeGameManager` + panggil `GameManager.Instance.InitializeForCurrentScene()` **sebelum** load saved state |

### Manual merge

**CoinManager** (`Assets/Script/Managers/CoinManager.cs`):
- Dari hylmi: `Instance != this` guard di Awake
- Retain: `resetCoinOnStart = false` (hylmi `true`), logika Initialize() kita tetap

**SaveManager** (`Assets/Script/SaveSystem/SaveManager.cs`):
- Dari hylmi: sort-by-name di `SaveSlots()`, `SaveAll()`, `LoadAndRestoreSlots()`
- Dari hylmi: `allEmpty` guard di `SaveAll()` — skip save jika semua slot kosong
- Dari hylmi: `FindObjectsSortMode.None` (ganti dari `InstanceID`)

### Skip (fix-nya sama / sudah punya)
- `WiringMinigameController.cs`, `CoopStatusRowUI.cs`, `GameLog.cs` — sudah punya
- `GameConstants.cs` — humidity values udah sama (TimeLimit=20, TargetSuccess=3, MaxFails=1, dll.)
- `HumidityToggleController.cs` — udah versi timing-bar yang sama
- `PlayerPrefsTools.cs`, font assets, prefab (ExitButton, Notifikasi, UIAlertPanel, TimeUpPopup) — udah diambil

### Ditolak
- `StarterKandangSlot.cs` — `enableAdvancedMinigames` gate (kita mau pool penuh tanpa gate)
- `StarterGameplayUI.cs` — `hpPanelRect` jadi SerializeField (kita mau tetap private)
- Scene files (Starter/Beginner/Intermediate.unity) — 2000+ line changes, nge-override assign-an bubble sprite & useHealthMinigame kita

---

## 2. PipelinePuzzleController — Gameplay Logic

**File**: `Assets/Script/Minigame/PipelinePuzzleController.cs`

### Sebelum
- Hanya skeleton: timer, UI create, show/hide popup
- **Stub**: grid cells ada tapi ngga bisa di-interaksi

### Sesudah
- **Grid**: `PipeType?[,] grid[3x3]` + `int[,] rotations`
- **3 Pipe Types**: L (elbow, 4 rotasi), Straight (2 rotasi), T (tee, 4 rotasi)
- **Placement**: klik cell kosong → taruh pipe tipe yang dipilih
- **Rotation**: klik cell yang sudah terisi → rotasi siklus
- **Pipe Inventory**: 3 tombol (L, I, T) untuk milih tipe, highlight hijau yang aktif
- **Pathfinding**: BFS dari cell (0,0) ke (gridSize-1, gridSize-1)
  - Source (0,0) perlu `Up` atau `Left` connection
  - Target butuh `Down` atau `Right` connection
  - Adjacent cells harus punya direction yang kompatibel
- **Auto-win check**: setiap placement/rotation, cek BFS → kalau path ketemu, auto-success
- **Visual feedback**: warna cell beda berdasarkan jumlah koneksi (2/3/4 way)
- **Source**: masuk dari atas/kanan atas, **Target**: keluar dari bawah/kiri bawah

### Belum dites
- Runtime UI fallback (EnsureRuntimeUi) mungkin perlu debug di Unity
- Belum ada animasi atau particle effect untuk koneksi
- Belum ada highlight winning path (method `HighlightWinningPath()` ada tapi belum dipanggil)

---

## 3. BGM Continuity (Main Menu ↔ Level Select)

**Problem**: BGM restart/putus saat ganti scene MainMenu → LevelSelect karena `SceneController` panggil `PlayMainMenuBGM()` / `PlayLevelSelectBGM()` secara terpisah, padahal clip-nya sama.

**Fix**:
- Tambah method `PlayMenuBGM()` di `BGMManager` (`Assets/Script/Managers/BGMManager.cs`):
  - Guard: kalau clip menu sudah playing (`bgmSource.clip == menuClip && bgmSource.isPlaying`), return tanpa restart
  - Hanya crossfade kalau clip berbeda (misal lagi gameplay → balik ke menu)
- Hapus `PlayLevelSelectBGM()` dan `PlayKoleksiIoTBGM()` dari public API
- Semua 3 method menu di `SceneController` (`GoToMainMenu`, `GoToSelectLevel`, `GoToKoleksiIoT`) panggil `PlayMenuBGM()`

**Behavior**:
- MainMenu → LevelSelect → KoleksiIoT: nyambung terus ✅
- Gameplay → menu manapun: crossfade dari gameplay BGM ke menu BGM ✅
- First launch: BGM jalan normal via `UIManager.Start()` ✅

---

## 4. Status Minigame (Final)

| Minigame | Logic | UI | Status |
|----------|-------|----|--------|
| Jigsaw | ✅ Lengkap | ✅ Prefab | Siap |
| Wiring | ✅ Lengkap (timer bug fix + serialized fields) | ✅ Runtime fallback | Siap |
| HumidityToggle | ✅ Timing-bar (hylmi version) | ✅ Runtime fallback | Siap |
| MemoryMatch | ✅ cardBackSprite + cardSprites dari Gambar/ | ✅ Prefab | Siap |
| DragDropSack | ✅ Isi penuh (adim) | ✅ Runtime fallback | Siap |
| HoldSwipe | ✅ Isi penuh (adim) | ✅ Runtime fallback | Siap |
| PipelinePuzzle | ✅ Grid 3x3, placement, rotate, BFS | ✅ Runtime fallback | **Belum dites** |

---

## 5. File Changed Summary

### New
- `Assets/Script/Minigame/PipelinePuzzleController.cs` — gameplay logic (276→447 lines)

### Modified
- `Assets/Script/Managers/GameManager.cs` — hylmi cherry-pick (170→192 lines)
- `Assets/Script/Managers/LevelTimer.cs` — hylmi cherry-pick (85→123 lines)
- `Assets/Script/Scene/LevelSceneInitializer.cs` — hylmi cherry-pick (44→48 lines)
- `Assets/Script/Managers/CoinManager.cs` — Instance guard (119→120 lines)
- `Assets/Script/SaveSystem/SaveManager.cs` — sort + allEmpty guard (280→306 lines)
- `Assets/Script/Managers/BGMManager.cs` — PlayMenuBGM (211→214 lines)
- `Assets/Script/Scene/SceneController.cs` — PlayMenuBGM (83→83 lines)

### Deleted
- `Assets/Script/DevMenu.cs` — dibuat lalu dihapus (tidak diminta user)

---

## 6. Key Decisions

- Tidak menggunakan `git merge` langsung — selective cherry-pick + manual merge
- `resetCoinOnStart = false` tetap dipertahankan
- `enableAdvancedMinigames` gate ditolak — pool penuh tanpa batasan
- `hpPanelRect SerializeField` ditolak — tetap private
- Scene files hylmi ditolak — retain assign-an bubble sprite & useHealthMinigame kita
