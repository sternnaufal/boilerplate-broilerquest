# Bubble Expiry Update - 2026-06-08

This document records the current implementation of the Starter Kandang Slot care bubble expiry feature.

## Summary

Care bubbles now expire after a configurable timer. The default expiry duration is 10 seconds.

If a care bubble expires, the event is treated as a failed care event. This means it affects the final sell reward through the same failure path used by puzzle failure.

## Affected Files

- `Assets/Script/Core/GameConstants.cs`
- `Assets/Script/Gameplay/StarterKandangSlot/StarterKandangSlot.cs`
- `Assets/Script/SaveSystem/SaveManager.cs`

## Implemented Behavior

- `GameConstants.StarterSlot.BubbleExpiryDuration = 10f`
- `StarterKandangSlot` has a serialized `bubbleExpiryDuration` field.
- The expiry timer starts only after a care bubble is visible.
- The timer counts down while the slot is in `WaitingForCareClick`.
- The timer stops when the slot leaves the active care bubble state.
- The slot state is saved when a care bubble appears, so app close/reload can restore the active bubble.
- `OnDisable()` stops the expiry coroutine so it does not continue outside the gameplay scene.

## Pause and Puzzle Rules

The expiry timer does not progress while:

- the game is paused,
- the game is not active,
- `JigsawMinigameController.Instance.IsPlaying` is true.

Pause and puzzle behavior preserves the remaining time during the same scene session.

## Scene Exit / Re-enter Rule

Per PM revision, leaving the gameplay scene is different from pausing:

- When the player leaves the Starter gameplay scene, the timer does not continue outside the scene.
- When the player enters Starter again and the same active care bubble is restored, the timer refreshes to a fresh 10 seconds.
- The exact remaining seconds are not persisted across scene/app reload.

## Save Data

`SaveManager.SlotSaveData` now stores:

- `hasActiveBubble`
- `activeBubbleNeed`

This is enough to restore the visible care bubble and start a fresh 10 second expiry timer after load.

`activeBubbleNeed` stores the current `ChickenNeed` as an integer. A value of `-1` means no active care bubble.

The active bubble save flag is true for both:

- `WaitingForCareClick`
- `WaitingForHealthMinigame`

This means a saved puzzle/minigame state restores as the care bubble for the same need, not as the puzzle UI.

## Economy Impact

Bubble expiry uses the same failure path as puzzle failure:

- stop the active expiry timer,
- reset the chicken animation to normal,
- mark the current need failed,
- increment `completedCareCount`,
- recalculate the sell reward with `RecalculateSellReward()`,
- save the slot state,
- continue to the next need timer or show the sell bubble if all needs are done.

There is no separate expiry penalty system.

## Current Validation Status

Reported by Command Code:

- Implementation completed across 3 files.
- Zero compilation errors.

Codex follow-up before push:

- Active bubble state is now saved immediately when the bubble appears.
- `WaitingForHealthMinigame` is saved as active bubble state, so reload restores the care bubble with a fresh 10 second timer.

Still recommended before commit:

1. Let a visible care bubble expire after 10 seconds.
2. Verify the expired need is marked failed.
3. Verify final sell reward decreases like puzzle failure.
4. Pause while a bubble timer is running and confirm the timer resumes from the same remaining time.
5. Start a puzzle while a bubble timer is running and confirm the timer does not progress during the puzzle.
6. Return to main menu, re-enter Starter, and confirm the restored active bubble gets a fresh 10 second timer.
7. Stop/start Play Mode or close/reopen the app after saving an active bubble and confirm the bubble restores with a fresh 10 second timer.

## Notes for Future Work

- If the team later wants exact remaining time to persist across scene reloads, add a saved remaining-time field. That would change the current PM rule.
- If the team wants visible countdown UI for the bubble, build that UI manually in Unity Editor first, then expose serialized references in code. Do not generate UI from script.
