# UI Restore & Defensive Fixes — 2026-06-21

## Summary

Cross-referenced `refrensidebugging` (stable `dev/hylmi` snapshot) against `boilerplate-broilerquest` (`dev/naufal`). Fixed missing UI elements, dead code, CoinManager coin reset bug, and added defensive bounds guards.

---

## 1. SyncStarterScene Neutered

`Assets/Editor/SyncStarterScene.cs`

The `Sync()` tool was destroying **Notifikasi**, **UIAlertPanel**, **EXIT button**, **StarterSceneInitializer**, and **duplicate StarterGameplayUI** every time it ran. All 5 destructive lines have been removed. The tool now only adds missing structural elements (Hp background, RowContainer, HPPanel position fix) without stripping UI.

**New tool added:** `Tools/Restore UI Elements in All Levels`

Iterates Starter/Beginner/Intermediate scenes and:
- Creates Notifikasi under StarterCanvas if missing
- Creates UIAlertPanel at root if missing
- Creates EXIT button under PausePanel if missing
- Wires UIAlertPanel serialized references to Notifikasi children (PakanHabis, DuidHabis, WaktuHabis, MainMenu)
- Wires StarterGameplayUI.exitButton → EXIT button
- Wires StarterGameplayUI.hpPanelRect → HPPanel RectTransform

Run this once in the Unity Editor before playtesting.

---

## 2. Defensive Code Fixes

### StarterKandangSlot.cs — completedCareCount bounds guard

The `completedCareCount` getter iterates `needSatisfied[]` and `needFailed[]`. If the arrays have different lengths (corrupted save), it would throw `IndexOutOfRangeException`. Now uses `Mathf.Min()` to clamp iteration, plus null check for `needFailed`.

### StarterKandangSlot.cs — SetBubbleExpiryByLevel null guard

Added early return + `Debug.LogWarning` if `GameManager.Instance` is null. Bubble defaults to Starter (no expiry) as safe fallback.

### SaveManager.cs — Save format versioning

`GameSaveData` now has:
```csharp
public int version = 2;
```

On save load, if `version < 2`, logs a warning about legacy format. Existing fallback (`ResetChickenProgress()` when `needQueue == null`) still handles migration silently.

---

## 3. Dead Code Cleanup

Both files were confirmed unused (cleaned in `dev/hylmi` but not `dev/naufal`):

| File | Reason |
|---|---|
| `Assets/Script/UI/PanelManager.cs` | Unused keyed panel show/hide system |
| `Assets/Script/UI/SafeAreaAdjuster.cs` | Unused safe-area responsive layout |

Both `.cs` and `.meta` files deleted.

---

## 4. hpPanelRect Serialized

`Assets/Script/Gameplay/StarterGameplayUI.cs`

Changed `private RectTransform hpPanelRect` → `[SerializeField] private RectTransform hpPanelRect` so it can be wired via Inspector or by the Restore UI tool. `SetupReferences()` fallback kept for backward compat (auto-finds from hpPanel if not set).

---

## 5. CoinManager Reset Bugfix

`Assets/Script/Managers/CoinManager.cs` + `Assets/Script/Editor/PlayerPrefsTools.cs`

**Root cause:** `CoinManager.Awake()` always called `SetTotalCoin(StartingCoin=400)` when `resetCoinOnStart=true`, overwriting any PlayerPrefs value. Even if you set 50000 via Editor, the moment a scene loaded with CoinManager, it reset to 400.

**Fix:**
- `Awake()` now checks `HasSavedCoin()` first — skips reset if saved data exists
- `Initialize()` always reads `LoadSavedCoin()` when `usePlayerPrefs=true` (no longer ignores existing data)
- Grant tool also updates live `CoinManager.Instance` during PlayMode

**New menu item:** `Tools/BroilerQuest/Grant 50000 Test Coins`

Usage flow:
1. Before PlayMode: `Tools → BroilerQuest → Grant 50000 Test Coins` (sets PlayerPrefs)
2. Enter PlayMode: CoinManager reads the saved value instead of resetting to 400
3. During PlayMode: re-run Grant tool to instantly update live coin display

---

## 6. Affected Files

| File | Change |
|---|---|
| `Assets/Editor/SyncStarterScene.cs` | Neuted 5 destructive lines; added `RestoreUIInAllLevels()` |
| `Assets/Script/Gameplay/StarterKandangSlot.cs` | Bounds-safe `completedCareCount`; null guard in `SetBubbleExpiryByLevel()` |
| `Assets/Script/SaveSystem/SaveManager.cs` | Added `version=2` + legacy format warning |
| `Assets/Script/UI/PanelManager.cs` | **Deleted** (dead code) |
| `Assets/Script/UI/SafeAreaAdjuster.cs` | **Deleted** (dead code) |
| `Assets/Script/Managers/CoinManager.cs` | `Awake()` respects existing saves; `Initialize()` always reads PlayerPrefs |
| `Assets/Script/Editor/PlayerPrefsTools.cs` | Added `Grant 50000 Test Coins` + PlayMode live update |
| `Assets/Script/Gameplay/StarterGameplayUI.cs` | `hpPanelRect` → `[SerializeField]` |

## 7. Decisions Made

- All 7 minigames and dynamic need queue are **kept** as intentional features (confirmed by Hylmi as "sudah sesuai dengan plan")
- SyncStarterScene.Sync() kept for future structure syncs — only the destructive lines removed
- UIAlertPanel.Awake() `DontDestroyOnLoad` behavior unchanged; Restore tool only assigns null references

## 8. Validation Status

- All `.cs` files have zero compilation errors (verified by Unity MCP validate_script)
- Dead code deletion confirmed: no missing script warnings expected
- Pending (manual in Unity Editor):
  - [ ] Run `Tools/Restore UI Elements in All Levels`
  - [ ] Play each level → Pause → verify EXIT button visible and clickable
  - [ ] Click EXIT → verify MainMenuConfirm popup appears
  - [ ] Buy chicken → wait for care bubble → verify expiry behavior
  - [ ] Run `Tools/BroilerQuest/Grant 50000 Test Coins` → verify coin display updates

## 9. Related Documents

- `difficulty-needs-system.md` — Dynamic need queue and minigame design
- `bubble_expiry_per_level_2026-06-10.md` — Bubble expiry timing rules
- `bugfix_refactor_log_2026-06-08.md` — Previous save/feed/IoT bugfix history
