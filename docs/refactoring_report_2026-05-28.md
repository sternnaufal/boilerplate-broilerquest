# Refactoring Report — 28 Mei 2026

## Ringkasan

Refactor pass 1 selesai: **3 perubahan arsitektural besar** tanpa merusak gameplay (zero compile error).

---

## 1. Folder Restructure

### Sebelum
```
Assets/Script/          ← 30 file .cs flat dalam 1 folder
├── GameManager.cs
├── CoinManager.cs
├── StarterKandangSlot.cs
├── KandangController.cs
├── ... (26 file lain)
```

### Sesudah
```
Assets/Script/
├── Core/              (5)  Singleton, GameConstants, GameLog, CoroutineHelper, ButtonHelper
├── Managers/          (5)  GameManager, GameStateManager, CoinManager, FeedManager, LevelTimer
├── UI/                (6)  UIManager, PanelManager, UIGlobalBinder, GlobalUIOverlay, TimeUpPopup, PopupHasilKesehatan
├── Scene/             (3)  SceneController, LevelSelectController, StarterSceneInitializer
├── Gameplay/          (3)  StarterChickenShop, StarterGameplayUI, KandangController
│   └── StarterKandangSlot/  (6 partial files, lihat section 3)
├── IoT/               (2)  StarterIoTController, KoleksiIoTController
└── Minigame/          (4)  PopupKesehatan, JigsawMinigameController, JigsawPiece, IHealthCheckListener
```

30 file → 7 folder terorganisir. Meta file ikut dipindah, GUID tetap, semua reference scene/prefab aman.

---

## 2. `SetGameStateOrFallback()` — Consolidation + Bug Fix

### Problem
Method `SetGameStateOrFallback()` di-copy-paste ke **3 file** dengan implementasi hampir identik:

| File | Baris | Bug? |
|------|-------|------|
| `UIManager.cs` | 203-212 | - |
| `StarterGameplayUI.cs` | 374-383 | - |
| `SceneController.cs` | 58-64 | **Ya**: lupa panggil `GameManager.SetGameActive()` |

### Yang Dilakukan
- `GameStateManager.cs` mendapat method baru:
  ```csharp
  public static void ApplyState(GameState state)
  {
      if (TrySetGameState(state)) return;
      Time.timeScale = state == GameState.Paused ? 0f : 1f;
      if (GameManager.Instance != null)
          GameManager.Instance.SetGameActive(state == GameState.Playing);
  }
  ```
- Semua caller di 3 file diganti dari `SetGameStateOrFallback()` → `GameStateManager.ApplyState()`
- **-3 private methods, -45 baris duplikasi, +1 centralized method**
- Bug SceneController fixed: `GoToLevel()` sekarang correctly call `SetGameActive(true)` di fallback path

---

## 3. `StarterKandangSlot` — Partial Class Split

### Sebelum
- `StarterKandangSlot.cs` — 871 baris (state machine, chicken visual, bubble UI, animasi, health minigame, wander AI — semuanya dalam 1 file)
- `StarterKandangSlot.Wander.cs` — 184 baris

### Sesudah — 6 file dalam folder sendiri
```
Gameplay/StarterKandangSlot/
├── StarterKandangSlot.cs               (469) — Coordinator: fields, state machine, public API, need system, sell
├── StarterKandangSlot.ChickenVisual.cs  (78)  — Spawn, position, pack chicken prefabs
├── StarterKandangSlot.BubbleUI.cs       (124) — Show/hide bubble, slot label, hitbox
├── StarterKandangSlot.Animation.cs      (78)  — Animator control, animation params
├── StarterKandangSlot.Health.cs         (71)  — Jigsaw & PopupKesehatan integration
└── StarterKandangSlot.Wander.cs         (157) — Chicken wander AI (unchanged)
```

**Hasil:** 1 × 871 baris → 6 file × 977 baris (+106 baris boilerplate partial class). Partial class = no prefab/scene changes needed, semua field tetap di main file.

---

## Struktur File Akhir

```
Assets/Script/ (30 file .cs)
├── Core/
│   ├── ButtonHelper.cs
│   ├── CoroutineHelper.cs
│   ├── GameConstants.cs
│   ├── GameLog.cs
│   └── Singleton.cs
├── Managers/
│   ├── CoinManager.cs
│   ├── FeedManager.cs
│   ├── GameManager.cs
│   ├── GameStateManager.cs
│   └── LevelTimer.cs
├── UI/
│   ├── GlobalUIOverlay.cs
│   ├── PanelManager.cs
│   ├── PopupHasilKesehatan.cs
│   ├── TimeUpPopup.cs
│   ├── UIGlobalBinder.cs
│   └── UIManager.cs
├── Scene/
│   ├── LevelSelectController.cs
│   ├── SceneController.cs
│   └── StarterSceneInitializer.cs
├── Gameplay/
│   ├── KandangController.cs
│   ├── StarterChickenShop.cs
│   ├── StarterGameplayUI.cs
│   └── StarterKandangSlot/
│       ├── StarterKandangSlot.cs
│       ├── StarterKandangSlot.Animation.cs
│       ├── StarterKandangSlot.BubbleUI.cs
│       ├── StarterKandangSlot.ChickenVisual.cs
│       ├── StarterKandangSlot.Health.cs
│       └── StarterKandangSlot.Wander.cs
├── IoT/
│   ├── KoleksiIoTController.cs
│   └── StarterIoTController.cs
└── Minigame/
    ├── IHealthCheckListener.cs
    ├── JigsawMinigameController.cs
    ├── JigsawPiece.cs
    └── PopupKesehatan.cs
```

---

## Done (Dead Code Cleanup — 28 Mei 2026)

Semua item **🔥 Hapus Dead Code** dari prioritas sebelumnya telah dieksekusi:

1. **Deleted `KandangController.cs` entirely** — 0 scene/prefab references, replaced by StarterKandangSlot
2. **Removed `BuyOption0/1/2()` from `StarterChickenShop`** — 3 dead public methods (not called from any prefab/scene/code, loop-based RegisterButtonListeners handles all wiring via `TryBuyChicken(index)`)
3. **Removed `TampilkanPopup(KandangController)` from `PopupKesehatan`** — wrapper method that only existed to accept the now-deleted `KandangController`
4. **Removed unused constants from `GameConstants.StarterSlot`** — `BaseSellReward`, `CareBonus`, `NeedInterval`
5. **Removed `GameLog.Verbose` field + dead check** — static bool never set to true, `Info()` now calls `Debug.Log` directly

---

## Pending / Next Priorities

### 🔥 Hapus Dead Code
Tidak ada lagi — lihat **Done (Dead Code Cleanup)** di bawah.

> **Catatan:** `GlobalUIOverlay` **bukan** dead code — confirmed active di 2 scenes + 1 prefab. Ia bikin UI runtime (melanggar aturan project) berdampingan dengan `UIGlobalBinder` (Inspector-based). Keduanya live.

### 🔧 Refactor UI Code → Prefab
- `JigsawMinigameController.EnsureRuntimeUi()` — buat Canvas + panel dari code
- `KoleksiIoTController.CreateFallbackCard()` — 90 baris UI code
- `StarterChickenShop.EnsureFeedButton()`, `PolishShopButtons()`, `EnsureOptionIcon()`
- `StarterGameplayUI.EnsureMainMenuButton()`, `PolishStarterUi()`

### 🔧 Consolidate IoT
- `KoleksiIoTController.IsPurchased()` duplikasi identik dengan `StarterIoTController.IsPurchased()`

---

## Done (Dead Code Cleanup — 28 Mei 2026)

| Item | Change | Lines removed |
|------|--------|---------------|
| `KandangController.cs` | Deleted file + .meta | -276 |
| `BuyOption0/1/2()` in StarterChickenShop | Removed 3 methods | -15 |
| `TampilkanPopup()` in PopupKesehatan | Removed method | -4 |
| `BaseSellReward`, `CareBonus`, `NeedInterval` in GameConstants | Removed 3 constants | -3 |
| `GameLog.Verbose` field + dead check | Removed field + conditional | -3 |
| **Total dead code eliminated** | | **-301 lines** |
