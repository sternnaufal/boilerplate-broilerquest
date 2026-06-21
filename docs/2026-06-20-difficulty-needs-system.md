# Difficulty-Based Need System — June 20, 2026 (Updated June 21, 2026)

## Summary
Implemented dynamic difficulty-based need generation across Starter/Beginner/Intermediate levels, 4 new mini-games, runtime UI, placeholder icon assets, and scene Inspector configuration.

---

## 1. Code Changes

### GameConstants.cs
- New `ChickenNeed` enum (public): `Feed, Cooling, Heating, HumidityUp, HumidityDown, AddDryHusk, ReduceFeed`
- New `Difficulty` constants: `StarterSteps=3, BeginnerSteps=4, IntermediateSteps=5`
- 4 new mini-game config blocks: `HumidityToggle`, `PipelinePuzzle`, `DragDropSack`, `HoldSwipe`

### StarterKandangSlot.cs (main)
- Replaced fixed 6 booleans with `List<ChickenNeed> needsQueue` + `bool[] needSatisfied/needFailed`
- `GenerateNeedsQueue()` — Starter: fixed [Feed, Cooling, Heating]; Beginner/Intermediate: Feed + random subset from pool
- `GetNeedPool()` — level-appropriate pools (respects `enableAdvancedMinigames` flag)
- `GetTotalNeedsCount()` — returns steps per difficulty (capped by available pool when advanced minigames disabled)
- 8 sprite fields for bubble icons + `GetNeedSprite()`/`GetNeedText()` switch
- Bubble expiry varies by difficulty
- New `[SerializeField] private bool enableAdvancedMinigames = false;` — when false, only {Feed, Cooling, Heating, HumidityUp} are in the pool. Set true for other devs to re-enable PipelinePuzzle, DragDropSack, HoldSwipe.

### StarterKandangSlot partials (Status, Animation, BubbleUI, Health, Wander, ChickenVisual)
- Dynamic queue support across all partials
- 4 new mini-game handler blocks (HumidityToggle, PipelinePuzzle, DragDropSack, HoldSwipe)
- `IsPuzzleActive()` checks all 7 mini-games
- Health minigame flow via `TryStartHealthMinigame()`

### SaveManager.cs
- `SlotSaveData`: replaced 6 booleans with `needQueue[]`, `needSatisfied[]`, `needFailed[]`, `currentNeedIndex`
- **Cross-level save**: each level stores its own data under `BroilerQuest.GameSave_L0/1/2`. IoT states stored under `BroilerQuest.GameSave_IoT` (shared across levels).
- `ClearSave()` now clears all level keys + IoT key + legacy key.

### CoinManager.cs
- `resetCoinOnStart` default changed to `true`
- `Awake()` now checks `HasSavedCoin()` before resetting; `Initialize()` always uses `LoadSavedCoin()`

### StarterChickenShop.cs
- Removed `PolishShopButtons()` — no longer overrides Inspector-set colors/font/position
- Replaced with `EnsureIconsExistOnly()` — creates icon child only if missing
- No `StyleButtonState()` color override on interactability
- Label format: `"{price}"` (no display name prefix)
- `TryBuyFeed()` now shows `UIAlertPanel.NotificationType.CoinOut` when coin insufficient

### StarterGameplayUI.cs
- `PolishStarterUi()` line 262: `mainMenuButton.gameObject.SetActive(false)` → `ButtonHelper.AddListenerOnce(mainMenuButton, ReturnToMainMenu)`

### 4 New Mini-Game Controllers
Each has `EnsureRuntimeUi()` matching Jigsaw/Wiring/MemoryMatch layout:
- `HumidityToggleController` — toggle switch + timer
- `PipelinePuzzleController` — 3x3 grid + pipe inventory
- `DragDropSackController` — drag-drop sacks + drop zone + remaining count
- `HoldSwipeController` — hold-progress bar + feed pile scaling

All use Canvas sorting 500, 1280x720, panel 500x600 at anchored (350,0), dark green, TitleText (0,270), TimerText (0,220).

### HumidityToggleController ON/OFF Machine Toggle
`ShowToggle()` now starts with `machineOn = false`. Button shows "HIDUPKAN MESIN". Indicator stays stopped until player clicks to turn machine on. After that, button changes to "ON/OFF" and timing gameplay begins.

### LevelTimer.cs
- `StartTimer()` now calls `EnsureTimerText()` which auto-discovers a "TimerText" GameObject in the scene when the SerializeField is unassigned.

### GameManager.cs
- `InitializeForCurrentScene()` now auto-discovers "TimeUpPopup" GameObject in the scene when `timeUpPopupPrefab` is unassigned.

### Editor Tools
- `Tools/Restore UI Elements in All Levels` — now creates Notifikasi, UIAlertPanel, EXIT button, wires all references, and assigns hpPanelRect across all 3 level scenes.

---

## 2. Scene Inspector Changes (Starter, Beginner, Intermediate)

### StarterKandangSlot (×4 per scene)
- `useHealthMinigame` = true
- New bubble sprites: humidityUp, humidityDown, addDryHusk, reduceFeed
- `enableAdvancedMinigames` = false (PipelinePuzzle, DragDropSack, HoldSwipe disabled by default)

### CoopStatusManager (CoopStatusPanelController)
- 14 placeholder icon sprites assigned (7 needs × sukses/gagal)

---

## 3. Prefab Changes

### CoopStatusRow.prefab
- New children: LembabIcon, KeringIcon, SekamIcon, KurangiIcon, SellIcon (Image)
- `needIcons` list references all 8 icon Image components

---

## 4. New Assets Created

### Assets/Gambar/Icons/ (14 placeholder sprites, 64×64 checkerboard)
| File | Color |
|------|-------|
| iconFeedSukses | Green |
| iconFeedGagal | Gray |
| iconCoolingSukses | Blue |
| iconCoolingGagal | Gray |
| iconHeatingSukses | Red |
| iconHeatingGagal | Gray |
| iconHumidityUpSukses | Purple |
| iconHumidityUpGagal | Gray |
| iconHumidityDownSukses | Brown |
| iconHumidityDownGagal | Gray |
| iconAddDryHuskSukses | Yellow |
| iconAddDryHuskGagal | Gray |
| iconReduceFeedSukses | Orange |
| iconReduceFeedGagal | Gray |

---

### StarterKandangSlot.cs — Additional Serialized Fields
Two `[SerializeField]` fields not covered above:
- `clearChickenOnHealthFail` (bool) — defined but currently unused (dead field)
- `wiringPairCount` (int, default 4) + `wiringTimeLimit` (float, default 30f) — config for Wiring minigame (Heating need)

### New Methods Not Documented Above
- `GetCurrentNeedIndex()` — scans `needQueue` for the first unsatisfied/not-failed need. Replaces the old `GetNextNeed()` approach.
- `GetNeedAt(index)` — safe getter into `needsQueue` with bounds check.

## 5. Post-Release Notes (June 21, 2026)

### CoinManager Reset Behavior Fixed
`resetCoinOnStart=true` previously overwrote any saved coin value (always reset to `StartingCoin=400`). Now `Awake()` checks `HasSavedCoin()` first — if saved data exists, the reset is skipped. `Initialize()` always reads `LoadSavedCoin()` when `usePlayerPrefs=true`.

### StyleButtonState Dead Code
`StarterChickenShop.StyleButtonState()` still exists as a defined method at line 258 but is never called. Its interactability override behavior was intentionally removed from the `RefreshShopState()` flow. Consider deleting if confirmed unused.

### Advanced Minigames Disabled
`enableAdvancedMinigames = false` by default. Only Feed (Jigsaw), Cooling (Memory Match), Heating (Wiring), HumidityUp (Humidity Toggle) are active. Set to `true` in Inspector to re-enable PipelinePuzzle, DragDropSack, HoldSwipe for other devs.

### Cross-Level Save
Save key changed to `BroilerQuest.GameSave_L{levelIndex}` (0=Starter, 1=Beginner, 2=Intermediate). Each level preserves its own chicken/need/sell state independently. IoT purchases share a global key `BroilerQuest.GameSave_IoT`.

### Humidity Toggle ON/OFF
Machine starts OFF. Player must click "HIDUPKAN MESIN" first. After that, timing bar and ON/OFF interaction begins.

## 6. Difficulty Rules

| Level | Total Steps | Need Pool | Random? |
|-------|------------|-----------|---------|
| Starter (0) | 3 | Feed, Cooling, Heating | Fixed order |
| Beginner (1) | 4 (3 without advanced) | Feed + random 3 of {Cooling,Heating,HumidityUp,[HumidityDown]} | Yes |
| Intermediate (2) | 5 (4 without advanced) | Feed + random 4 of {Cooling,Heating,HumidityUp,[HumidityDown,AddDryHusk,ReduceFeed]} | Yes |

Items in `[]` are only included when `enableAdvancedMinigames = true`. Without them, Beginner = 1+3, Intermediate = 1+3 = 4 steps.

Each failure reduces sell price by `FailPenalty`.
