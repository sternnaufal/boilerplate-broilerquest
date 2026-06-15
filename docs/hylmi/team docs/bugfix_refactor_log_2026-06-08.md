# Bugfix and Refactor Log - 2026-06-08

This document records the BroilerQuest fixes restored after pulling `gakusahnama`, plus the cleanup/refactor work that should be preserved.

## Context

After pulling updates from `gakusahnama`, local `dev/Hylmi` moved to commit `01e2887`. Several previous gameplay fixes were overwritten or disappeared from the working tree. The fixes below were restored and validated locally.

Do not treat this as a complete release note. It is a team handover for the current restored state.

## Restored Bug Fixes

### 1. Feed price reverted

Problem:

- Feed purchase values reverted to `50 coin / 10 feed`.
- The approved balancing is `5 coin / 1 feed`.

Fix:

- `Assets/Script/Core/GameConstants.cs`
- `GameConstants.Economy.FeedCost = 5`
- `GameConstants.Economy.FeedIncrement = 1`

Why:

- Keeps feed purchase granular and easier to balance.
- Matches PM feedback that the previous bulk price was wrong.

### 2. Save/load restored slots by unstable index

Problem:

- `SaveManager.LoadAndRestoreSlots()` restored saved slots by array index.
- If hierarchy/order changes, chickens can restore into the wrong kandang slot.

Fix:

- `Assets/Script/SaveSystem/SaveManager.cs`
- `Assets/Script/Gameplay/StarterKandangSlot/StarterKandangSlot.cs`
- Added `SlotSaveData.slotId`.
- Added `StarterKandangSlot.SlotId => gameObject.name`.
- `GetSaveData()` now writes `slotId`.
- `LoadAndRestoreSlots()` builds a slot lookup by `SlotId`.
- Old saves without `slotId` still restore by index as a legacy fallback.

Risk:

- Current `slotId` is based on GameObject name. Do not rename kandang slot objects without a migration plan.

### 3. SaveAll used multiple read/write cycles

Problem:

- `SaveAll()` called `SaveSlots()` and `SaveIotStates()` separately.
- That caused multiple PlayerPrefs reads/writes per full save.
- It could also briefly write partial state between calls.

Fix:

- `SaveAll()` now loads `GameSaveData` once, populates slots and IoT states in memory, then writes once with `SaveData(data)`.

Why:

- Lower PlayerPrefs overhead.
- Less fragile read-modify-write behavior.
- Easier to reason about save state.

### 4. Inactive slots were excluded from saves

Problem:

- `SaveAll()` used `FindObjectsInactive.Exclude`.
- Inactive slot objects could be omitted from save data.

Fix:

- `SaveAll()` now uses `FindObjectsInactive.Include`.

Why:

- Save/load should preserve all configured slots, not only active hierarchy objects.

### 5. Auto Feeder did not consume feed

Problem:

- Feed bubble click checked active IoT before consuming feed.
- If Auto Feeder was ON, the feed need completed without reducing feed count.

Fix:

- `Assets/Script/Gameplay/StarterKandangSlot/StarterKandangSlot.cs`
- Feed need now calls `FeedManager.TryConsumeFeed(1)` before IoT auto-completion.

Expected behavior:

- Feed bubble always costs 1 feed.
- Auto Feeder skips the minigame/handling, but does not make feed free.

### 6. Chicken visuals could remain after selling

Problem:

- Some runtime chicken visuals were not tracked in `spawnedChickens`.
- Selling a chicken cleared game state, but untracked visual clones could remain in the kandang.

Fix:

- `Assets/Script/Gameplay/StarterKandangSlot/StarterKandangSlot.ChickenVisual.cs`
- Added:
  - `GetChickenVisualParent()`
  - `RegisterUntrackedChickenVisuals()`
  - `IsRuntimeChickenVisual(GameObject candidate)`
- `GetActiveChickenVisuals()` and `ClearChicken()` now register untracked runtime visuals before operating.

Expected behavior:

- Selling chicken clears the slot state and removes chicken visuals.

### 7. IoT purchase default OFF state was not persisted

Problem:

- Purchasing an IoT device wrote purchase state to PlayerPrefs, but did not explicitly write the initial OFF state into `SaveManager`.
- Toggle saves worked, but the initial purchased/OFF state was implicit.

Fix:

- `Assets/Script/IoT/StarterIoTController.cs`
- `PurchaseDevice()` now calls `SaveManager.SaveIotStates(activeStates)` after setting `activeStates[productKey] = false`.

Expected behavior:

- New IoT purchases are saved as purchased and OFF.

### 8. Returning to main menu from pause did not save

Problem:

- `UIManager.ReturnToMainMenuFromPause()` returned to main menu without saving current starter gameplay state.

Fix:

- `Assets/Script/UI/UIManager.cs`
- Added `SaveManager.SaveAll()` before `GameManager.Instance.ReturnToMainMenu()`.

Expected behavior:

- Progress is saved before leaving gameplay from pause.

### 9. PopupO2Panel had a broken/missing script reference

Problem:

- `Assets/Prefab/Canvas.prefab` had `PopupO2Panel` linked to an invalid/old script GUID.
- This caused missing script warnings and broke popup health result flow.

Fix:

- Relinked `PopupO2Panel` to `PopupKesehatan`.
- Correct GUID: `4426ff62de903cb458fa878df742d595`.

Validation:

- Missing script scan found no missing MonoBehaviour scripts in loaded scene or Assets prefabs.

## Cleanup and Refactor Work

### Runtime-created feed button removed

Problem:

- `StarterChickenShop.EnsureFeedButton()` created UI at runtime using `new GameObject()`.
- This violates the project UI rule: UI must be created manually in Unity and assigned through Inspector.

Fix:

- Removed runtime creation for the feed button and label.
- If `feedBuyButton` is missing, the script now logs a warning telling the team to assign:
  - `ShopAPK/Pakan`
  - `ShopAPK/Pakan/Label`

Why:

- Keeps Inspector and in-game hierarchy consistent.
- Prevents hidden runtime UI from confusing teammates.

### Bubble expiry changes preserved

Pulled branch changes added bubble expiry behavior:

- `GameConstants.StarterSlot.BubbleExpiryDuration = 10f`
- `StarterKandangSlot` starts the expiry timer only after a care bubble is visible.
- `StarterKandangSlot` saves when a care bubble appears so reload can restore it.
- Expired care bubbles are treated as failed care events.
- Expiry uses the same failure/economy path as puzzle failure:
  - mark current need failed
  - increment `completedCareCount`
  - call `RecalculateSellReward()`
  - save state
  - continue to next need or show sell bubble
- `StarterKandangSlot` stores active bubble state in save data:
  - `hasActiveBubble`
  - `activeBubbleNeed`
- Active bubble can be restored after load.
- Puzzle/minigame state is restored as the active care bubble for the same need, with a fresh 10 second timer.
- Bubble expiry pauses while gameplay is paused or puzzle is active.
- Leaving/re-entering the gameplay scene restores the active bubble with a fresh 10 second timer, per PM revision.

These changes were preserved while restoring the older bug fixes.

See `bubble_expiry_update_2026-06-08.md` for the detailed behavior and QA checklist.

## Current Validation

Unity MCP validation after restoration:

- Unity refresh/compile completed.
- Console after refresh: 0 errors, 0 warnings.
- Script validation:
  - `GameConstants.cs`: 0 errors
  - `SaveManager.cs`: 0 errors
  - `StarterKandangSlot.cs`: 0 errors
  - `StarterIoTController.cs`: 0 errors
  - `UIManager.cs`: 0 errors
  - `StarterChickenShop.cs`: 0 errors
- `StarterKandangSlot.ChickenVisual.cs` could not be validated by `validate_script` because Unity MCP rejects script names with dots, but Unity compilation succeeded.
- Missing MonoBehaviour scan: no missing scripts found in loaded scene or Assets prefabs.

## Manual QA Checklist

Run these before committing:

1. Clear BroilerQuest PlayerPrefs.
2. Enter Starter scene.
3. Buy feed and verify `5 coin -> +1 feed`.
4. Buy chicken in at least two different kandang slots.
5. Return to main menu from pause, re-enter Starter, verify the same slots restore.
6. Turn Auto Feeder ON, tap feed bubble, verify feed decreases by 1.
7. Sell chicken, verify all visuals disappear from the kandang.
8. Buy an IoT device, leave/re-enter, verify it remains purchased and OFF.
9. Let a care bubble expire, verify failure progression behaves correctly.
10. Check console for errors/warnings.

## Files Intentionally Touched

- `Assets/Prefab/Canvas.prefab`
- `Assets/Script/Core/GameConstants.cs`
- `Assets/Script/Gameplay/StarterChickenShop.cs`
- `Assets/Script/Gameplay/StarterKandangSlot/StarterKandangSlot.ChickenVisual.cs`
- `Assets/Script/Gameplay/StarterKandangSlot/StarterKandangSlot.cs`
- `Assets/Script/IoT/StarterIoTController.cs`
- `Assets/Script/SaveSystem/SaveManager.cs`
- `Assets/Script/UI/UIManager.cs`

## Known Noise

Unity Editor modified:

- `UserSettings/EditorUserSettings.asset`
- `UserSettings/Layouts/default-6000.dwlt`

These are editor-local noise and should not be committed unless the team explicitly wants layout/settings changes.
