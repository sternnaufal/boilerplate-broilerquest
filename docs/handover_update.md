# BroilerQuest Handover Update

Date: 22 Mei 2026

## Today Completed

- Pulled the latest project update from `origin/main`.
- Confirmed Unity is opening the correct project root:
  `D:\Pertempuran Surabaya\boilerplate-broilerquest`.
- Cleaned accidental parent Unity project artifacts from:
  `D:\Pertempuran Surabaya`.
- Removed the broken local package dependency that pointed to another developer's absolute Windows path:
  `com.gladekit.mcp-bridge`.
- Added Unity MCP package dependency:
  `com.coplaydev.unity-mcp`.
- Initialized SigMap and Semble project support:
  - `gen-context.config.json`
  - `AGENTS.md`
  - `docs/broilerquest-sigmap.md`
  - `.contextignore`
  - `.opencode/agents/semble-search.md`
  - OpenCode MCP entries in `opencode.json`

## UI Work Completed

- Normalized `Assets/Scenes/MainMenu.unity` around the requested minimal UI flow:
  - Main menu
  - Options screen
  - Pause screen
  - HUD screen
- Added and wired a `UIManager` GameObject in the scene.
- Ensured required scene panels exist:
  - `MainScreenPanel`
  - `OptionScreenPanel`
  - `PauseScreenPanel`
  - `HUDPanel`
- Added and wired UI controls:
  - `StartButton`
  - `OptionButton`
  - `ExitButton`
  - `BackFromOptionButton`
  - `ResumeButton`
  - `PauseOptionButton`
  - `MainMenuButton`
  - `PauseButton`
  - `MusicVolumeSlider`
  - `SfxVolumeSlider`
- Added persistent UnityEvents in the scene so button behavior works even if runtime listener registration is skipped by editor play mode settings.
- Added Build Settings scene entries:
  - `Assets/Scenes/MainMenu.unity`
  - `Assets/Scenes/Starter.unity`

## Script Work Completed

- Reworked `Assets/Script/UIManager.cs` to use scene-assigned UI instead of runtime UI creation.
- Added public methods for UnityEvents:
  - `StartGame()`
  - `PauseGame()`
  - `ResumeGame()`
  - `ShowOptionsFromMain()`
  - `ShowOptionsFromPause()`
  - `BackFromOptions()`
  - `ReturnToMainMenuFromPause()`
  - `SetMusicVolume(float)`
  - `SetSfxVolume(float)`
  - `ExitGame()`
- Fixed the Input System runtime error by replacing legacy `UnityEngine.Input` Escape handling with:
  `UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame`.
- Kept runtime listener registration as a fallback, but skipped it when persistent inspector events already exist.

## Validation

- Unity Play Mode smoke test passed.
- Tested flow:
  Main menu -> Options -> Back -> Start -> Pause -> Options -> Back -> Resume.
- Result:
  `PASS`
- SigMap generation:
  `npx sigmap --track`
  - 7 files scanned
  - 26 symbols found
  - 7/7 source files included
  - Coverage: 100%
- SigMap report:
  `npx sigmap --report`
  - Module coverage: 100% for `Assets/Script`

## SigMap Follow-up - 23 Mei 2026

- Regenerated SigMap after the newer Starter and level-select scripts were added:
  `npx sigmap --track`
  - 11 files scanned
  - 52 symbols found
  - 11/11 source files included
  - Coverage: A / 100%
- Confirmed current coverage:
  `npx sigmap --report`
  - 11 of 11 code files included
  - Module coverage: 100% for `Assets/Script`
- Updated `docs/broilerquest-sigmap.md` so the module map covers the current 11 runtime scripts instead of the older 7-script snapshot.
- Restored the `AGENTS.md` command table to prefer `npx sigmap --query`, `npx sigmap --track`, and `npx sigmap --report`; `sigmap validate` remains unreliable as the source of truth for this project.

## Core Mechanic Follow-up - 23 Mei 2026

- Root cause for Starter core mechanic not running:
  `BQ_KandangArea` in `Assets/Scenes/Starter.unity` was inactive, so all `StarterKandangSlot` objects were inactive and could not start care-event coroutines.
- Fixed scene state:
  `BQ_KandangArea` is now active in the Starter scene.
- Hardened `StarterChickenShop`:
  it now skips inactive kandang slots when looking for an empty slot.
- Hardened `StarterKandangSlot`:
  it now auto-creates a fallback bubble UI when `bubbleVisual` / `bubbleImage` are not wired in the Inspector, showing `RAWAT` and `PANEN` labels.
- Smoke-tested in Unity Play Mode:
  open HP panel -> buy Ayam Kampung -> care bubble -> harvest bubble -> harvest.
  Result: `PASS`, coin changed `100 -> 85`, slot returned empty, no console errors or warnings.

## Cleanup / Modularization Follow-up - 23 Mei 2026

- Re-analyzed the latest pulled work after the Starter shop stayed disabled even when the player had enough coin.
- Root cause for disabled buy button:
  `Assets/Prefab/BQ_KandangSlot.prefab` had its prefab root inactive:
  `m_IsActive: 0`.
  Because `StarterChickenShop` checks `slot.gameObject.activeInHierarchy && slot.IsEmpty`, inactive slot prefab instances made the shop think no empty kandang slot existed.
- Fixed prefab state:
  `Assets/Prefab/BQ_KandangSlot.prefab` root is now active by default.
- Cleaned the Starter shop flow from previous-attempt conflicts:
  - `StarterChickenShop` no longer refreshes button state every frame.
  - Shop button state now reacts to coin and kandang-slot state changes.
  - The shop discovers active `StarterKandangSlot` objects at runtime, so stale or partially assigned Inspector arrays do not block buying.
- Modularized Starter economy / slot communication:
  - `CoinManager` now exposes `CoinsChanged`.
  - `StarterKandangSlot` now exposes `StateChanged`.
  - `StarterChickenShop` subscribes to those events and refreshes only when relevant game state changes.
- Hardened `CoinManager`:
  - Duplicate persistent instances can transfer scene reset coin values and UI bindings before destroying themselves.
  - Coin is saved after startup initialization.
  - `AddCoin` ignores negative input and clamps overflow at `int.MaxValue`.
  - Deprecated `FindObjectsOfType` usage was replaced with `FindObjectsByType`.
- Kept the older `KandangController` / `PopupKesehatan` flow intact because it is not wired into the current Starter scene and may still be useful for another level. It should be treated as a legacy/alternate level mechanic until the team decides to remove or migrate it.
- Validation:
  - `dotnet build Assembly-CSharp.csproj --no-restore`
  - Result: `PASS`, 0 errors, 0 warnings.
  - `npx sigmap validate`
  - Result: config valid, but reports 50% coverage. Continue treating `sigmap validate` coverage as unreliable for this project; use `npx sigmap --track` / `npx sigmap --report` for source-of-truth coverage.

## Current Code Architecture Notes

- `SceneController`: scene navigation only.
- `GameManager`: level timer, game-active state, time-up popup.
- `CoinManager`: persistent coin state, coin UI binding, `CoinsChanged` event.
- `StarterGameplayUI`: Starter-specific pause / HP panel UI.
- `StarterChickenShop`: Starter shop purchase logic and button interactability.
- `StarterKandangSlot`: one kandang slot state machine, care events, sell/harvest flow.
- `UIManager`: MainMenu scene UI only. Avoid reusing it for Starter gameplay UI to prevent menu/gameplay responsibility overlap.

## Current Console State

No active BroilerQuest script errors were found after the UI smoke test.

Remaining warning is from Unity AI Assistant/MCP tooling, not gameplay code:

```text
Account API did not become accessible within 30 seconds. This may be due to network issues or editor focus.
```

The other repeated console entries are Unity MCP bridge trace logs such as discovery, handshake, and connection messages.

## Notes For Next Session

- Treat `npx sigmap --query "<topic>"` as the first step before code exploration.
- Use `npx sigmap --track` after code changes.
- Use `npx sigmap --report` to verify coverage.
- The old `sigmap validate` command reports misleading coverage in this project and should not be used as the source of truth.
- Before future Starter shop work, check that `BQ_KandangSlot.prefab` root remains active and that scene kandang slots are active under `BQ_KandangArea`.
- Prefer event-driven updates for gameplay UI and shop state. Avoid restoring per-frame `RefreshShopState()` polling unless profiling or gameplay behavior requires it.
- If the older `KandangController` flow is no longer needed, remove it in a dedicated cleanup commit after confirming no scene/prefab references remain.
- The working tree has Unity-generated changes in scene, project settings, package lock, user settings, and layout files. Review before committing.

## Recommendation Cleanup Follow-up - 24 Mei 2026

- Started implementing the cleanup plan from `docs/recomendation.md`.
- Slot-system direction is now explicit:
  - `StarterKandangSlot` is the active/current Starter kandang system.
  - `KandangController` is kept only as legacy / migration-reference code until old scene or prefab references are confirmed safe to remove.
- Added shared architecture helpers:
  - `Assets/Script/IHealthCheckListener.cs`
  - `Assets/Script/ButtonHelper.cs`
  - `Assets/Script/CoroutineHelper.cs`
  - `Assets/Script/GameConstants.cs`
  - `Assets/Script/GameLog.cs`
  - `Assets/Script/GameStateManager.cs`
  - `Assets/Script/PanelManager.cs`
  - `Assets/Script/StarterSceneInitializer.cs`
- Decoupled the health popup flow:
  - `PopupKesehatan` now accepts `IHealthCheckListener` through `ShowHealthCheck(...)`.
  - `KandangController` implements the interface for backward compatibility.
  - `StarterKandangSlot` implements the interface and can optionally open the health minigame through `useHealthMinigame`.
  - Default Starter behavior remains direct bubble-click completion so current gameplay is not blocked by unfinished minigame UI setup.
- Fixed active animation integration issues in `StarterKandangSlot`:
  - Animator is discovered from the scene visual and newly spawned shop chicken.
  - Prefab animation parameters were aligned to the existing controller triggers:
    `isbakar` and `isdingin`.
  - `idleAnimParam` is intentionally empty because the current `Ayam.controller` has no normal/idle trigger.
  - Trigger calls now check that the animator parameter exists before calling `SetTrigger`.
- Added safer wiring / modularization:
  - `StarterChickenShop.SetKandangSlots(...)` allows future explicit scene initialization.
  - Runtime slot discovery remains as a fallback when configured slots are incomplete, preserving the previous disabled-buy fix.
  - `StarterSceneInitializer` can wire `CoinManager`, `StarterChickenShop`, and `StarterKandangSlot[]` later through the Inspector.
- Centralized game-state foundation:
  - `GameStateManager` handles `Menu`, `Playing`, `Paused`, and `GameOver`.
  - It lazy-creates itself when used, so the scene does not immediately need a manually placed manager object.
  - `UIManager`, `StarterGameplayUI`, `SceneController`, and `GameManager` now delegate or fallback through this state path.
- Button listener registration was normalized with `ButtonHelper`.
- Coroutine cleanup was normalized with `CoroutineHelper`.
- Noisy normal gameplay logs were moved behind `GameLog.Info(...)`; warnings and errors remain visible.
- Persistence was cleaned up:
  - `CoinManager` now supports explicit `Initialize(...)`.
  - Coin saves under `BroilerQuest.TotalCoin`.
  - Legacy `TotalCoin` is still read as fallback.
- Quick project-level fixes:
  - `ProjectSettings/EditorBuildSettings.asset` now starts with `Assets/Scenes/MainMenu.unity` at build index `0`.
  - Starter scene debug coin was reduced from `2147483647` to `100`.
  - `.gitignore` and `.contextignore` now ignore local/generated Unity folders:
    `UserSettings/`, `Assets/_Recovery/`, and `Assets/Adaptive Performance/`.
- Updated `docs/recomendation.md` with completed items, remaining playtest needs, and the new helper files.

## Validation - 24 Mei 2026

- Compile check:
  `dotnet build Assembly-CSharp.csproj --no-restore`
  - Result: `PASS`, 0 errors, 0 warnings.
- SigMap check:
  `npx sigmap validate`
  - Result: config valid.
  - Coverage still reports `50%`; this warning is still treated as misleading for this project unless SigMap config is tuned.
- Scoped whitespace check:
  `git diff --check -- .contextignore .gitignore Assets/Prefab/BQ_KandangSlot.prefab Assets/Scenes/Starter.unity Assets/Script ProjectSettings/EditorBuildSettings.asset docs/recomendation.md`
  - Result: no actual whitespace errors; only Git line-ending warnings.

## Remaining Work After 24 Mei Cleanup

- Unity Play Mode test is still needed for:
  - buy chicken -> bubble -> care -> sell;
  - animation changes for heating/cooling;
  - optional `useHealthMinigame` flow on at least one slot.
- Attach `StarterSceneInitializer` in the Starter scene only after deciding which references should be Inspector-owned.
- Keep `KandangController` until old scene/prefab references are audited.
- `docs/recomendation.md` is still untracked and should be explicitly staged if the team wants it committed.
- New helper scripts and `.meta` files are also untracked until staged.
- `Assembly-CSharp.csproj` was locally updated for `dotnet build` verification, but it is generated/ignored and Unity may regenerate it.

## UI Invisibility & Font Migration Fixes - 25 Mei 2026

- **Solved Jigsaw Minigame Invisibility**:
  - Root cause: Dynamic canvas was a child of the non-UI `JigsawMinigameController` transform inside `DontDestroyOnLoad`. In URP, nested canvases under non-UI transforms undergo coordinate and projection clipping, rendering them invisible.
  - Fix: Configured the canvas as a **root GameObject** and added persistent lifecycle management with `DontDestroyOnLoad(canvasObject)`.
  - Text mesh generation fix: Dynamic canvases default to `additionalShaderChannels = None`. TextMeshProUGUI requires `TexCoord1 | Normal | Tangent` channels to construct/render distance-field geometry. Explicitly assigned these URP shader channels on the canvas component in `EnsureRuntimeUi()`.

- **Solved Broken Default Font Asset**:
  - Root cause: The project's default `LiberationSans SDF` font asset was corrupt or incompatible in this build, causing all TMPro texts utilizing it (such as the minigame texts and the level timer HUD) to render as completely transparent. Only the `LiberationSans SDF - Fallback` asset worked correctly (e.g., `CoinText`).
  - Fix: Statically migrated all 14 `TextMeshProUGUI` components in the `Starter.unity` scene to use `LiberationSans SDF - Fallback`, and dynamically loaded/assigned this working font inside `JigsawMinigameController.CreateText()`.
  - Follow-up verification found the fallback asset itself had been left in a corrupted merge-conflict state. The fallback font asset was restored to a valid YAML asset; Unity now loads it correctly and all 14 Starter scene TMP objects have assigned fonts.

- **Solved Screen-Scaling Layout Issues for HUD Timer**:
  - Root cause: The level `TimerText` in `Starter.unity` was anchored to the center `(0.5, 0.5)` with a large fixed height offset of `470`. On varying aspect ratios or resolutions, the timer was pushed entirely off-screen.
  - Fix: Anchored and pivoted `TimerText` to **Top-Center** `(0.5, 1.0)` at position `(0, -70)` so that it scales seamlessly and remains visible on all screen sizes.

- **Fast Git Integration**:
  - Pulled and merged updates from `origin/dev/Hylmi` cleanly.
  - Successfully committed and pushed the UI, script, and font asset changes directly to `dev/Hylmi` branch.

### Verification
- Tested in Unity Play Mode:
  - Countdown level timer now renders beautifully at the top-center.
  - Jigsaw Minigame triggers successfully, rendering its backdrop, white grid tiles, title text, countdown, and close button label flawlessly.
  - Result: `PASS`

## Docs Cleanup - 25 Mei 2026

- Removed obsolete jigsaw planning/research documents after the jigsaw minigame was implemented and its current status moved into this handover:
  - `docs/jigsaw-minigame-implementation-new-plan.md`
  - `docs/jigsaw-minigame-new-plan-analysis.md`
  - `docs/jigsaw-minigame-implementation-plan.md`
  - `docs/jigsaw-minigame-research-and-recommendations.md`
- Kept the durable project docs:
  - `docs/handover_update.md`
  - `docs/recomendation.md`
  - `docs/broilerquest-sigmap.md`
  - `docs/broilerquest-ui-handover-codex.md`
  - `docs/GDD_CoopQuest.docx`

## Care Event Speech Bubble Text Cleanup - 25 Mei 2026

- **Removed Overlapping Text on Event Speech Bubbles**:
  - Root cause: In the chicken care events (Feed/memberikan makan, Heating/ayam kedinginan, Cooling/ayam kepanasan), the speech bubbles display beautiful icons (seeds, fire, fan) but also rendered text overlays ("MAKAN", "HEATER", "KIPAS") directly on top of them. This overlapped with the icons and looked visually redundant and cluttered.
  - Fix: Modified `StarterKandangSlot.ShowBubble()` to detect if the incoming label matches any of the care event texts (`feedBubbleText`, `coolingBubbleText`, or `heatingBubbleText`). If it does, the text mesh label (`bubbleLabel`) is disabled (`enabled = false`) and set to empty (`text = ""`), leaving only the clean event icon bubble visible.
  - Retained "JUAL" (Sell) Label: The "JUAL" label still renders its text overlay perfectly as it does not overlap with any complex graphic.
  - Verification: Ran Unity in Play Mode, triggered care events on slots, and confirmed that only the beautiful event icons render inside the speech bubbles with zero overlapping text.

