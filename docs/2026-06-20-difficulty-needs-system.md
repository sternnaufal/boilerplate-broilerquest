# Difficulty-Based Need System — June 20, 2026

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
- `GetNeedPool()` — level-appropriate pools
- `GetTotalNeedsCount()` — returns steps per difficulty
- 8 sprite fields for bubble icons + `GetNeedSprite()`/`GetNeedText()` switch
- Bubble expiry varies by difficulty

### StarterKandangSlot partials (Status, Animation, BubbleUI, Health, Wander, ChickenVisual)
- Dynamic queue support across all partials
- 4 new mini-game handler blocks (HumidityToggle, PipelinePuzzle, DragDropSack, HoldSwipe)
- `IsPuzzleActive()` checks all 7 mini-games
- Health minigame flow via `TryStartHealthMinigame()`

### SaveManager.cs
- `SlotSaveData`: replaced 6 booleans with `needQueue[]`, `needSatisfied[]`, `needFailed[]`, `currentNeedIndex`

### CoinManager.cs
- `resetCoinOnStart` default changed to `true`

### StarterChickenShop.cs
- Removed `PolishShopButtons()` — no longer overrides Inspector-set colors/font/position
- Replaced with `EnsureIconsExistOnly()` — creates icon child only if missing
- No `StyleButtonState()` color override on interactability
- Label format: `"{price}"` (no display name prefix)

### StarterGameplayUI.cs
- `PolishStarterUi()` line 262: `mainMenuButton.gameObject.SetActive(false)` → `ButtonHelper.AddListenerOnce(mainMenuButton, ReturnToMainMenu)`

### 4 New Mini-Game Controllers
Each has `EnsureRuntimeUi()` matching Jigsaw/Wiring/MemoryMatch layout:
- `HumidityToggleController` — toggle switch + timer
- `PipelinePuzzleController` — 3x3 grid + pipe inventory
- `DragDropSackController` — drag-drop sacks + drop zone + remaining count
- `HoldSwipeController` — hold-progress bar + feed pile scaling

All use Canvas sorting 500, 1280x720, panel 500x600 at anchored (350,0), dark green, TitleText (0,270), TimerText (0,220).

---

## 2. Scene Inspector Changes (Starter, Beginner, Intermediate)

### StarterKandangSlot (×4 per scene)
- `useHealthMinigame` = true
- New bubble sprites: humidityUp, humidityDown, addDryHusk, reduceFeed

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

## 5. Difficulty Rules

| Level | Total Steps | Need Pool | Random? |
|-------|------------|-----------|---------|
| Starter (0) | 3 | Feed, Cooling, Heating | Fixed order |
| Beginner (1) | 4 | Feed + random 3 of {Cooling,Heating,HumidityUp,HumidityDown} | Yes |
| Intermediate (2) | 5 | Feed + random 4 of {Cooling,Heating,HumidityUp,HumidityDown,AddDryHusk,ReduceFeed} | Yes |

Each failure reduces sell price by `FailPenalty`.
