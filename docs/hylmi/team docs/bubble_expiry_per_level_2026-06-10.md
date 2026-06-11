# Bubble Expiry — Per-Level Timing Rules (2026-06-10)

This document records the refactor that replaced the single global bubble expiry timer
with level-specific durations. It supersedes the "10 second flat" rule documented in
`bubble_expiry_update_2026-06-08.md`.

## Summary

Care bubble expiry is now level-aware. Each level has its own timer rule:

| Level | Expiry Behavior |
|---|---|
| **Starter** | No expiry — bubbles never expire |
| **Beginner** | Bubbles expire after **20 seconds** |
| **Intermediate** | Bubbles expire after **15 seconds** |

The old uniform 10-second timer has been removed entirely.

## Affected Files

- `Assets/Script/Core/GameConstants.cs`
- `Assets/Script/Gameplay/StarterKandangSlot/StarterKandangSlot.cs`
- `Assets/Scenes/Starter.unity`
- `Assets/Scenes/Beginner.unity`
- `Assets/Scenes/Intermediate.unity`

## Implementation Detail

### GameConstants.cs

The old constant:

```csharp
public const float BubbleExpiryDuration = 10f;
```

was replaced with three per-level constants inside `GameConstants.StarterSlot`:

```csharp
// 0 or negative = no expiry (starter level: unlimited time)
public const float BubbleExpiryDurationStarter      = 0f;   // disabled
public const float BubbleExpiryDurationBeginner     = 20f;
public const float BubbleExpiryDurationIntermediate = 15f;
```

### StarterKandangSlot.cs

Two changes:

**1. Field default updated:**

```csharp
[SerializeField] private float bubbleExpiryDuration =
    GameConstants.StarterSlot.BubbleExpiryDurationStarter;
```

**2. Guard added in `StartBubbleExpiryTimer()`:**

```csharp
private void StartBubbleExpiryTimer()
{
    StopBubbleExpiryTimer();
    if (bubbleExpiryDuration <= 0f)
        return; // no expiry for this level
    CoroutineHelper.StopAndStart(this, ref bubbleExpiryCoroutine, BubbleExpiryRoutine());
}
```

The guard uses `<= 0f` (not `== 0f`) to safely handle any small negative float values.
Without this guard, a duration of `0f` would cause `BubbleExpiryRoutine` to call
`FailCurrentNeedAndAdvance` on the first frame — an instant fail bug.

### Scene Files

Each scene's `StarterKandangSlot` prefab instances have `bubbleExpiryDuration`
overridden via the Inspector:

| Scene | Slots Changed | Value Set |
|---|---|---|
| `Starter.unity` | 4 | `0` |
| `Beginner.unity` | 6 | `20` |
| `Intermediate.unity` | 8 | `15` |

Only `propertyPath: bubbleExpiryDuration` was modified in each scene file.
No other serialized properties were changed.

## Pause and Puzzle Rules

Unchanged from previous implementation. The expiry timer does not progress while:

- the game is paused,
- the game is not active,
- `JigsawMinigameController.Instance.IsPlaying` is true.

## Scene Exit / Re-enter Rule

Unchanged from previous implementation:

- Leaving the gameplay scene stops the timer.
- Re-entering and restoring an active bubble starts a fresh timer using the level's
  configured duration.
- Remaining time is not persisted across scene/app reload.

## Save Data

No changes to `SaveManager`. `hasActiveBubble` and `activeBubbleNeed` remain the
only bubble-related save fields. The save system was not affected by this change.

## Validation Status

Reviewed by Codex (2026-06-10):

- 0 compilation errors on both changed scripts (Unity MCP `validate_script`).
- 0 Unity console errors/warnings.
- Scene diffs confirmed clean: only `bubbleExpiryDuration` property changed per scene.
- No orphaned references to the old `BubbleExpiryDuration = 10f` constant remain.
- Slot counts verified: Starter 4 slots, Beginner 6 slots, Intermediate 8 slots.

Pending (requires manual playtest by Hylmi):

1. Starter: leave a visible care bubble for 30+ seconds — it must not expire.
2. Beginner: leave a visible care bubble for 21+ seconds — it must expire and count as fail.
3. Intermediate: leave a visible care bubble for 16+ seconds — it must expire and count as fail.
4. Pause while bubble timer is running — timer must freeze and resume correctly.
5. Start a puzzle while bubble timer is running — timer must not progress during puzzle.
6. Return to main menu, re-enter the level — restored bubble must use a fresh timer.

## Notes for Future Work

- If different kandang slot types in future levels need different durations, add more
  constants to `GameConstants.StarterSlot` and set the Inspector value per scene.
- If visible countdown UI is required, build the UI element in Unity Editor first,
  then expose a serialized reference in `StarterKandangSlot` and drive it from
  `BubbleExpiryRoutine`. Do not generate UI from script.
- If remaining time must persist across scene/app reload, add a saved
  `remainingBubbleExpiry` float field to `SaveManager.SlotSaveData` and restore it
  in `RestoreFromSave`. That would change the current PM rule.
