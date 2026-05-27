# BroilerQuest Project Audit - 26 Mei 2026

Scope: analysis only. No gameplay/code fixes were applied during this audit.

## Validation Snapshot

- Unity Console: clean at the end of the audit, with `0` active logs/errors/warnings.
- `dotnet build Assembly-CSharp.csproj --no-restore`: failed because the fresh clone did not yet have `Temp/obj/Assembly-CSharp/project.assets.json`.
- `dotnet build Assembly-CSharp.csproj`: passed after restore with `0` errors and `1` warning.
- SigMap: `npx sigmap --track` passed with Coverage `A (90%)`, `26 of 29` source files included.
- Scene validation through Unity MCP reported the active `MainMenu` scene as clean: no missing scripts and no broken prefabs.
- Project-wide YAML scan found no merge-conflict markers and no obvious missing-script markers in `Assets/Scenes` or `Assets/Prefab`.

## Current Compile / Console Issues

### 1. Unused Field Warning

- Severity: Low
- Evidence: `dotnet build Assembly-CSharp.csproj`
- File: `Assets/Script/FeedManager.cs`
- Warning:
  - `CS0414: The field 'FeedManager.hasInitialized' is assigned but its value is never used`
- Analysis:
  - `hasInitialized` is declared and assigned, but no logic reads it.
  - This is not breaking gameplay, but it is dead state and can confuse future persistence or initialization work.

### 2. Previous Emoji Font Warnings Are Currently Reproducible By Code Path

- Severity: Medium
- Evidence:
  - `Assets/Script/GlobalUIOverlay.cs` creates `CoinIcon` and `FeedIcon` as `TextMeshProUGUI`.
  - `CoinIcon` text is `💰`.
  - `FeedIcon` text is `🌾`.
  - TMP default font has no fallback font assets configured.
  - TMP `EmojiOne.asset` exists, but does not contain Unicode `128176` money bag or `127806` rice/sheaf glyphs.
- Analysis:
  - Even though the console was clean after reload, this code can produce the missing-glyph warnings seen earlier when the overlay is rendered.
  - The warnings are not script exceptions. They are TMP glyph replacement warnings.

## Code Conflict / Workspace Conflict Signals

### 1. No Merge Conflict Markers Found

- Severity: None
- Evidence:
  - Search for `<<<<<<<`, `=======`, and `>>>>>>>` in `Assets`, `ProjectSettings`, `Packages`, and `docs` found no active merge conflict markers.

### 2. Dirty Working Tree Exists

- Severity: Medium
- Evidence: `git status --short`
- Notable dirty paths include:
  - `.context/usage.ndjson`
  - `.vscode/settings.json`
  - `AGENTS.md`
  - `ProjectSettings/EditorBuildSettings.asset`
  - `ProjectSettings/EditorSettings.asset`
  - `ProjectSettings/ShaderGraphSettings.asset`
  - `UserSettings/*`
  - `docs/broilerquest-sigmap.md`
  - `docs/handover_update.md`
  - untracked docs and screenshots
- Analysis:
  - Some changes are tool-generated or local environment data.
  - `AGENTS.md` and `.context/usage.ndjson` were touched during `npx sigmap --track`.
  - Review before committing so generated/local-only files do not get mixed into gameplay fixes.

## Unused / Possibly Dead Code Candidates

These are candidates, not deletion instructions. Unity reflection, Inspector events, prefab references, or future scene wiring can make code appear unused to text search.

### High Confidence Candidates

#### `PanelManager`

- Evidence:
  - `Assets/Script/PanelManager.cs` is only referenced by its own file.
  - Its script GUID has `0` scene/prefab references.
- Analysis:
  - Looks like a helper added during modularization but not currently wired into scenes or other code.

#### `StarterSceneInitializer`

- Evidence:
  - `Assets/Script/StarterSceneInitializer.cs` is only referenced by its own file.
  - Its script GUID has `0` scene/prefab references.
- Analysis:
  - Handover says it was created as a future Inspector-owned wiring point.
  - Currently unused by the active scene setup.

#### `KandangController`

- Evidence:
  - No scene/prefab references to its script GUID.
  - Only code reference is legacy `PopupKesehatan.TampilkanPopup(KandangController)`.
- Analysis:
  - This matches the handover: legacy/alternate flow kept for migration reference.
  - It is not currently wired into the Starter slot system.

#### `LevelTimer`

- Evidence:
  - `GameManager` has a serialized `LevelTimer levelTimer` and calls `FindFirstObjectByType<LevelTimer>()`.
  - `LevelTimer` script GUID has `0` scene/prefab references.
- Analysis:
  - Current scenes may show timer text, but no `LevelTimer` component is serialized in scenes/prefabs.
  - If no runtime object adds `LevelTimer`, `GameManager.InitializeForCurrentScene()` will not start the level timer.

### Lower Confidence Candidates

- `FeedManager.SetFeedCount(int)`: no references found outside its own declaration.
- `GameStateManager.SetMenu()`, `SetPlaying()`, `SetPaused()`, `SetGameOver()`: no direct references found; may be intended for Inspector buttons/debugging.
- `LevelTimer.BindTimerText(TextMeshProUGUI)`: no direct references found.
- `PopupKesehatan.TampilkanPopup(KandangController)`: legacy API for `KandangController`.
- `StarterIoTController.SetDeviceActive(string, bool)`: no direct references found.
- `UIManager.ShowMainScreen()`: no direct references found.
- `StarterKandangSlot.Wander.PauseWander()`: no direct references found.

## Duplicate / Overlapping Systems

### 1. Starter Slot System vs Legacy Kandang System

- Current system:
  - `StarterKandangSlot`
  - `StarterChickenShop`
  - `FeedManager`
  - optional `JigsawMinigameController`
- Legacy system:
  - `KandangController`
  - `PopupKesehatan.TampilkanPopup(KandangController)`
- Analysis:
  - The two systems overlap conceptually around chicken care, health checks, and harvest flow.
  - Current evidence says only the Starter slot system is wired.
  - Legacy code should stay until old scene/prefab references are intentionally audited, but it should be treated as inactive.

### 2. Scene UI Coin Text vs Global Runtime Overlay

- Evidence:
  - Starter scene has `BQ_CoinIcon`.
  - `KoleksiIoT` scene has a static `CoinIcon`.
  - `GlobalUIOverlay` dynamically creates its own `CoinIcon`, `CoinValue`, `FeedIcon`, and `FeedValue`.
- Analysis:
  - This can produce duplicated HUD elements depending on scene and manager lifecycle.
  - It also creates two visual strategies: scene-authored UI and runtime-created overlay UI.

## Missing Variable / Missing Reference Findings

### 1. No C# Missing Variable Errors

- Evidence:
  - `dotnet build Assembly-CSharp.csproj` passed with `0` errors.
- Analysis:
  - No missing variable, missing method, missing type, or syntax error exists at compile time.

### 2. Possible Runtime Null / Missing Component Paths

These are not compile errors, but they can cause missing behavior if expected scene objects are absent.

- `GameManager.levelTimer`
  - No `LevelTimer` component found in scene/prefab references.
  - Timer behavior depends on a runtime or future scene object.
- `GlobalUIOverlay` emoji icons
  - Uses TMP glyphs rather than sprite assets, causing missing glyph warnings when rendered.
- `PopupKesehatan` / `JigsawMinigameController`
  - Several defensive warnings exist when UI or textures are not assigned.
  - Current prefab has jigsaw textures assigned, so the Starter prefab path looks prepared.

## Asset Verification: Text / Emoji / Event Indicators

### 1. Care Event Bubbles Use Image Sprites

- Evidence:
  - `Assets/Prefab/BQ_KandangSlot.prefab` has assigned sprites:
    - `feedBubbleSprite`
    - `coolingBubbleSprite`
    - `heatingBubbleSprite`
    - `sellBubbleSprite`
  - All four point to sub-assets from `Assets/Gambar/Ayam.png`.
- Analysis:
  - The active care-event bubble system is image-based, not emoji-based.

### 2. Care Bubble Text Is Still Serialized But Hidden For Care Events

- Evidence:
  - `feedBubbleText: MAKAN`
  - `coolingBubbleText: KIPAS`
  - `heatingBubbleText: HEATER`
  - `sellBubbleText: JUAL`
  - `StarterKandangSlot.ShowBubble(...)` disables `bubbleLabel` when the label is feed/cooling/heating.
- Analysis:
  - The previous overlapping text issue is handled in code.
  - `MAKAN`, `KIPAS`, and `HEATER` remain as serialized labels and logic keys, but are not displayed on care bubbles.
  - `JUAL` remains visible by design.

### 3. Jigsaw Puzzle Textures Exist And Are Assigned

- Evidence:
  - `jigsawFeedTexture` points to `Assets/Gambar/pakan puzzle.png`.
  - `jigsawCoolingTexture` points to `Assets/Gambar/dingin puzzle.png`.
  - `jigsawHeatingTexture` points to `Assets/Gambar/panas puzzle.png`.
- Analysis:
  - The previous event-description assets for jigsaw/care events are present and assigned on `BQ_KandangSlot.prefab`.

### 4. Coin / Feed Overlay Assets Exist But Are Not Used By Current Overlay Code

- Evidence:
  - `Assets/Gambar/coin-stat.png` exists and is imported as a Sprite.
  - `Assets/Gambar/pakan puzzle.png` exists and is imported as a Sprite.
  - `GlobalUIOverlay` currently uses text emoji instead of these image assets.
- Analysis:
  - If the goal is to avoid TMP emoji warnings, these assets are available, but the current overlay code does not reference them.

## Recommended Fix Order

1. Replace `GlobalUIOverlay` emoji TMP icons with image sprites or TMP sprite tags that actually exist in the configured TMP sprite asset.
2. Decide whether `LevelTimer` should be attached to the Starter scene or replaced by the current timer UI path.
3. Remove or explicitly mark `FeedManager.hasInitialized` as unnecessary.
4. Decide whether `PanelManager` and `StarterSceneInitializer` are future scaffolding or should be removed.
5. Keep `KandangController` only if the team still needs legacy migration reference; otherwise remove it in a dedicated cleanup pass.
6. Review dirty generated/local files before any commit.

## Files Most Worth Reviewing Next

- `Assets/Script/GlobalUIOverlay.cs`
- `Assets/Script/FeedManager.cs`
- `Assets/Script/GameManager.cs`
- `Assets/Script/LevelTimer.cs`
- `Assets/Script/PanelManager.cs`
- `Assets/Script/StarterSceneInitializer.cs`
- `Assets/Script/KandangController.cs`
- `Assets/Prefab/BQ_KandangSlot.prefab`
