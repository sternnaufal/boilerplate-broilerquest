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
- The working tree has Unity-generated changes in scene, project settings, package lock, user settings, and layout files. Review before committing.
