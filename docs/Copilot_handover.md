# Copilot / OpenCode Handover

**Last updated:** 2026-06-05  
**Project:** BroilerQuest  
**Engine:** Unity 6000.3.15f1  
**Status:** Ready as agent handover reference

---

## 1. Source of Truth

Read these files before changing the project:

1. `AGENTS.md` — primary rules for all agent work.
2. `.agent/workflows/unity-ui.md` — UI task workflow.
3. `docs/opencode setup/README.md` — OpenCode setup notes.
4. `opencode.json` — MCP configuration.
5. `.context/query-context.md` — latest SigMap focused context.

`AGENTS.md` is the primary rule file. `.antigravity/rules.md` currently mirrors the core Unity rules, but do not treat it as a replacement for `AGENTS.md`.

Every agent response must start with:

```text
Rules loaded. Proceeding with task.
```

---

## 2. Project Overview

BroilerQuest is a Unity farming/management game. Current systems support:

- Chicken/farm gameplay in `Starter`.
- Coin and feed resource management.
- IoT device purchase/activation flow.
- Level navigation: `MainMenu`, `SelectLevel`, `Starter`, `Beginner`, `Intermediate`, `KoleksiIoT`.
- Health-check and jigsaw minigame flow.

---

## 3. Project Structure

```text
Assets/
├── Animation/       Animation clips and controllers
├── Gambar/          Game images/graphics
├── Prefab/          Reusable prefab assets
├── Scenes/          Unity scenes
├── Script/          C# gameplay and UI logic
│   ├── Core/        Helpers, constants, singleton base
│   ├── Gameplay/    Player, shop, Starter gameplay controllers
│   ├── IoT/         IoT collection and Starter IoT controllers
│   ├── Managers/    Game, state, economy, timer, audio managers
│   ├── Minigame/    Health popup and jigsaw minigame
│   ├── Scene/       Scene navigation and scene initialization
│   └── UI/          Menu, panel, global UI, result/time-up popups
├── Settings/        URP settings
└── TextMesh Pro/    TextMeshPro resources
```

---

## 4. Technical Stack

| Area | Current state |
| --- | --- |
| Unity | `6000.3.15f1` |
| Render pipeline | URP `17.3.0` |
| Input | Unity Input System `1.19.0` |
| UI | uGUI (`UnityEngine.UI`) + TextMeshPro |
| Tests | Unity Test Framework `1.6.0` |
| Language | C# |

Do not describe the project as UI Toolkit-based unless actual `UIDocument`/UI Toolkit assets are introduced later.

---

## 5. OpenCode / MCP Setup

Project config: `opencode.json`.

| MCP | Configured purpose | Notes |
| --- | --- | --- |
| `unityMCP` | Unity scene/script/prefab/console/test tooling | Remote endpoint `http://127.0.0.1:8080/mcp`; requires Unity Editor MCP running |
| `sigmap` | File relevance and context generation | Must be used before code search |
| `semble` | Extra semantic tooling | Configured via `semble.mcp.serve(...)` with this project root as the default source |

Do not claim a configured MCP is unavailable unless it has been tested in the current session.

---

## 6. Mandatory Workflow

Before searching or editing:

```powershell
npx sigmap ask "describe the task"
```

Preferred discovery order:

1. SigMap query/ask.
2. `rg` / `rg --files`.
3. Read relevant files.
4. Unity MCP inspection/validation when Unity Editor is open.

After source or setup docs change:

```powershell
npx sigmap --track
```

For exact search on Windows/PowerShell, prefer:

```powershell
rg "string to find" Assets/Script
```

---

## 7. Unity UI Rules

Do not create UI objects directly from scripts.

For UI tasks:

1. First describe required hierarchy:
   - Canvas
   - panels
   - buttons
   - text/TMP fields
   - images/raw images
   - suggested GameObject names
2. Create only logic/data/controller scripts.
3. Use serialized fields for references.
4. Tell the user exactly what to create manually in Unity Editor.
5. Tell the user which references to drag into the Inspector.

Forbidden for UI construction:

```csharp
new GameObject()
AddComponent<Canvas>()
AddComponent<Button>()
AddComponent<TextMeshProUGUI>()
```

Existing components may still be referenced, validated, enabled/disabled, or styled when they already exist in the scene/prefab.

---

## 8. Script Quality Rules

- Check for similar logic before adding new functions.
- Reuse existing helpers/managers where appropriate.
- Keep `using` statements clean.
- Validate serialized references before runtime use.
- Avoid missing scripts and missing Inspector assignments.
- Do not override existing in-game components just to support new UI.
- Keep Inspector state and in-game behavior aligned.

---

## 9. Current Code Reference

### Core

- `ButtonHelper` — safe Button/Slider listener helpers.
- `CoroutineHelper` — safe coroutine stop/restart helpers.
- `GameConstants` — central constants.
- `GameLog` — lightweight logging helper.
- `Singleton<T>` / `SingletonQuittingDetector` — singleton infrastructure.

### Managers

- `CoinManager` — coin state, add/spend/check, bind coin text.
- `FeedManager` — feed state, add/use/check.
- `GameManager` — scene initialization, level progression, game active state.
- `GameStateManager` — `SetMenu`, `SetPlaying`, `SetPaused`, `SetGameOver`, `TrySetGameState`, `ApplyState`.
- `LevelTimer` — timer start/stop and timer text binding.
- `SFXManager` — SFX playback and volume.

### Scene

- `SceneController` — main menu, level select, IoT collection, level loading.
- `LevelSelectController` — starter/beginner/intermediate selection and locked messages.
- `StarterSceneInitializer` — Starter scene binding/initialization.

### UI

- `UIManager` — main menu, options, pause, HUD flow.
- `PanelManager` — keyed panel registration and show/hide/show-only behavior.
- `GlobalUIOverlay` — global UI overlay marker/controller.
- `UIGlobalBinder` — global UI binding helper.
- `PopupHasilKesehatan` — health result popup setup.
- `TimeUpPopup` — time-up popup setup.

### Gameplay

- `PlayerMovement` — player movement.
- `StarterChickenShop` — feed/chicken purchase flow and Starter slot integration.
- `StarterGameplayUI` — Starter HUD/pause/HP panel behavior.
- `StarterKandangSlot` partials — chicken slot state, visuals, bubble UI, animation, health, wander.

### IoT

- `StarterIoTController` — purchase/check/toggle/active state logic for Starter devices.
- `KoleksiIoTController` — IoT collection screen/product binding.

### Minigame

- `PopupKesehatan` — health-check popup.
- `PopupHasilKesehatan` — health result UI.
- `JigsawMinigameController` — jigsaw minigame flow.
- `JigsawPiece` — tile setup/click/highlight behavior.
- `IHealthCheckListener` — callback interface for health-check results.

There is no current `PopupSystem` class. Refer to the concrete popup controllers above.

---

## 10. Validation

Use the most relevant validation available:

- Unity Console check via Unity Editor/MCP.
- Unity MCP script validation for changed C# files.
- Unity Test Framework for edit/play mode tests when available.
- `npx sigmap --track` after source or setup context changes.

Do not use `npm test`; this is not a Node test project.

---

## 11. Setup Quick Start

1. Open the project in Unity `6000.3.15f1`.
2. Start/confirm MCP for Unity in the Editor.
3. Confirm `opencode.json` has the expected MCPs.
4. Run OpenCode from project root:

```powershell
cd "D:\Bandung Lautan Api\boilerplate-broilerquest"
opencode
```

5. For UI tasks, follow `.agent/workflows/unity-ui.md`.

---

## 12. Maintenance Notes

Update this handover when:

- `AGENTS.md` rules change.
- `opencode.json` MCP entries change.
- Major folder/script organization changes.
- UI workflow rules change.
- A new validation/test command becomes canonical.

Keep this file factual. If a tool or feature was not tested in the current session, describe it as configured/expected, not guaranteed.
