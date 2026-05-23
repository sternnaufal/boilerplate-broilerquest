# Code Cleanup Recommendations - Boilerplate Broilerquest

## Executive Summary
This project exhibits several code organization issues including duplicate functionality, conflicting game flow logic, abandoned code patterns, and poor separation of concerns. Below are detailed findings and recommendations for refactoring.

---

## 1. **CRITICAL: Duplicate Slot/Kandang Management Systems**

### Problem
The project has **two competing systems** for managing chicken slots:
- **StarterKandangSlot** (modern, event-driven)
- **KandangController** (legacy, state-machine based)

Both implement the same functionality but with different approaches:

| Aspect | StarterKandangSlot | KandangController |
|--------|-------------------|-------------------|
| **State Management** | SlotState enum | KandangState enum |
| **Care Cycle** | Feed → Cooling/Heating → Sell | Chicken → Feed → Vitamin → Harvest |
| **Notifications** | Bubble UI (ShowBubble) | Emote System (emotAyam, emotMakan, etc) |
| **Event Pattern** | StateChanged event | Coroutine-based delays |
| **Minigame** | None | PopupKesehatan integration |

### Consequences
- **Confusing codebase**: Developers don't know which system to extend
- **Maintenance nightmare**: Bug fixes must be applied to both systems
- **Inconsistent UI/UX**: Different visual feedback for same actions
- **Resource waste**: Double implementation of same logic

### Recommendation
**Choose ONE system and delete the other:**

#### Option A: Keep StarterKandangSlot (Recommended)
- **Pros**: Cleaner architecture, uses events (better decoupling), simpler state machine, already used by the current shop/buy flow
- **Cons**: Missing minigame integration (PopupKesehatan) in the current implementation
- **Action**: Make `StarterKandangSlot` the single active kandang gameplay system, then migrate the minigame hook into it
  - Do **not** delete `KandangController.cs` immediately if the minigame design is still being used as reference.
  - Mark `KandangController` as legacy / migration-only until `PopupKesehatan` no longer depends on it.
  - Integrate the bubble-click minigame hook into `StarterKandangSlot`.
  - Replace all runtime `KandangController` scene references with `StarterKandangSlot`.
  - Delete `KandangController.cs` only after `PopupKesehatan` works through an interface or callback.

#### Option B: Keep KandangController
- **Pros**: Has minigame system
- **Cons**: More spaghetti code, harder to test, tightly coupled
- **Action**: Delete StarterKandangSlot and refactor to use events
- **Current recommendation**: Avoid this option for the active Starter scene because it would undo the newer shop/slot cleanup and reintroduce tighter coupling.

```csharp
// Proposed unified interface (if you choose to keep KandangController):
public interface IKandangSlot
{
    bool TryPlaceChicken(GameObject chickenPrefab);
    void ClearChicken();
    bool IsEmpty { get; }
    event System.Action<IKandangSlot> StateChanged;
}
```

**Timeline**: 1-2 sprints to fully consolidate

---

### Updated Direction for Bubble Minigame Flow

The intended future flow is:

```text
Bubble appears -> player clicks bubble/slot -> health minigame opens -> result returns success/fail -> slot continues state flow
```

For the current project state, the best path is:

1. Keep `StarterKandangSlot` as the source of truth for slot state.
2. Preserve the minigame concept from `KandangController`, but move the integration point into `StarterKandangSlot`.
3. Refactor `PopupKesehatan` so it does not require a concrete `KandangController`.

Recommended interface:

```csharp
public interface IHealthCheckListener
{
    void OnHealthCheckSuccess();
    void OnHealthCheckFailure();
}
```

Then:

```csharp
public class StarterKandangSlot : MonoBehaviour, IPointerClickHandler, IHealthCheckListener
{
    public void OnHealthCheckSuccess()
    {
        CompleteCurrentNeed();
    }

    public void OnHealthCheckFailure()
    {
        ClearChicken(); // or apply a future penalty/state when the game design is ready
    }
}
```

`PopupKesehatan` should store `IHealthCheckListener` or a pair of callbacks instead of `KandangController`.

Important note: because the minigame is not fully set up yet, this should be implemented behind a serialized toggle such as `useHealthMinigame`. When disabled, bubble clicks can keep using the current direct-success behavior. When enabled later, the same bubble click opens the minigame without changing the shop or slot architecture again.

Short-term recommendation:
- Keep direct bubble-click completion as the default behavior for now.
- Add the minigame integration as an optional path in `StarterKandangSlot`.
- Treat `KandangController` as temporary reference code, not a second active gameplay system.

---

### Implementation Started - 23 Mei 2026

Status update setelah cleanup pass pertama:

- `StarterKandangSlot` sekarang menjadi jalur utama yang siap menerima minigame melalui `IHealthCheckListener`.
- `PopupKesehatan` sudah tidak terkunci ke `KandangController`; popup sekarang bisa menerima listener/interface umum lewat `ShowHealthCheck(IHealthCheckListener listener)`.
- `KandangController` tetap ada sebagai legacy/migration reference dan sekarang mengimplementasikan interface yang sama.
- Minigame pada `StarterKandangSlot` dibuat optional melalui `useHealthMinigame`; default tetap direct-complete agar gameplay saat ini tidak rusak sebelum UI minigame benar-benar dipasang.
- Bug animasi aktif sudah ditangani: parameter script/prefab diselaraskan ke `isbakar`/`isdingin`, `idleAnimParam` dikosongkan, animator dicari otomatis, dan trigger dicek sebelum dipanggil.
- Helper awal sudah dibuat: `ButtonHelper`, `CoroutineHelper`, `GameConstants`, `GameLog`, `GameStateManager`, `PanelManager`, dan `IHealthCheckListener`.
- `StarterSceneInitializer` sudah dibuat sebagai tempat wiring scene yang lebih eksplisit untuk `CoinManager`, `StarterChickenShop`, dan `StarterKandangSlot[]`; shop tetap fallback ke discovery jika configured slots terlihat tidak lengkap.
- `GameStateManager` sekarang bisa lazy-create sendiri saat dipanggil, jadi pause/resume/menu/game-over tidak wajib menunggu object manager dipasang manual di scene.
- Build Settings sudah diubah agar `MainMenu` berada di index `0`.
- Nilai debug `startingCoin: 2147483647` di `Starter.unity` sudah diganti menjadi `100`.
- `.gitignore` dan `.contextignore` sudah diperbarui untuk mengurangi noise dari local/generated Unity files.

Catatan penting:
- `PanelManager` sudah tersedia sebagai fondasi, tetapi belum wajib dipasang di scene.
- `StarterSceneInitializer` belum ditempel ke scene melalui Inspector; current runtime discovery tetap menjadi fallback aman.
- `KandangController` belum dihapus sampai scene/prefab lama benar-benar selesai dimigrasikan.
- Perlu playtest di Unity untuk memastikan bubble, popup, dan animation controller bekerja sesuai desain runtime.

---

## 2. **Game Flow Conflicts Between UIManager & GameManager**

### Problem
Multiple managers are trying to control game state simultaneously:

```csharp
// UIManager.cs - PauseGame()
public void PauseGame()
{
    Time.timeScale = 0f;
    HideAllPanels();
    if (pausePanel != null) pausePanel.SetActive(true);
}

// StarterGameplayUI.cs - PauseGame() [Different Implementation]
public void PauseGame()
{
    Time.timeScale = 0f;
    if (GameManager.Instance != null)
        GameManager.Instance.SetGameActive(false);
    if (hudPanel != null) hudPanel.SetActive(false);
    if (pausePanel != null) pausePanel.SetActive(true);
}

// GameManager.cs - InitializeForCurrentScene()
public void InitializeForCurrentScene()
{
    isGameActive = true;  // Resets state!
    // ... rest of logic
}
```

### Issues Identified
1. **Three different PauseGame implementations** with slight variations
2. **GameManager.SetGameActive()** called from multiple places inconsistently
3. **StarterGameplayUI** has its own pause logic independent of UIManager
4. **No centralized pause/resume authority** - each manager decides independently
5. **Time.timeScale** manipulation scattered across multiple files

### Recommendation
Create a **GameStateManager** as single source of truth:

```csharp
// Pseudocode
public class GameStateManager : MonoBehaviour
{
    public enum GameState { Playing, Paused, MenuOpen, GameOver }

    private GameState currentState;
    public event System.Action<GameState> StateChanged;

    public void SetGameState(GameState newState)
    {
        if (currentState == newState) return;

        switch (newState)
        {
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
        }

        currentState = newState;
        StateChanged?.Invoke(newState);
    }
}

// Usage everywhere:
// UIManager, StarterGameplayUI, etc.
if (GameStateManager.Instance != null)
    GameStateManager.Instance.SetGameState(GameState.Paused);
```

**Delete or refactor:**
- Remove pause logic from StarterGameplayUI (use GameStateManager listener)
- Simplify UIManager.PauseGame() to delegate to GameStateManager
- Remove Time.timeScale manipulation from individual scripts

---

## 3. **Three Different UI Panel Management Approaches**

### Problem

| File | Approach | Panels Managed |
|------|----------|-----------------|
| **UIManager** | HideAllPanels() + selective show | mainMenu, options, pause, hud |
| **StarterGameplayUI** | Direct SetActive on individual panels | hudPanel, pausePanel, hpPanel |
| **LevelSelectController** | No panel management | Only button logic |

### Code Smell
```csharp
// UIManager - verbose null checking
private void HideAllPanels()
{
    if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    if (optionsPanel != null) optionsPanel.SetActive(false);
    if (pausePanel != null) pausePanel.SetActive(false);
    if (hudPanel != null) hudPanel.SetActive(false);
}

// vs StarterGameplayUI - same thing, different names
public void PauseGame()
{
    if (hudPanel != null) hudPanel.SetActive(false);
    if (pausePanel != null) pausePanel.SetActive(true);
}
```

### Recommendation
Create a **PanelManager** utility:

```csharp
public class PanelManager : MonoBehaviour
{
    private Dictionary<string, GameObject> panels = new();

    public void RegisterPanel(string key, GameObject panel)
    {
        panels[key] = panel;
    }

    public void ShowOnly(string panelKey)
    {
        foreach (var kvp in panels)
            kvp.Value.SetActive(kvp.Key == panelKey);
    }

    public void Show(string panelKey)
    {
        if (panels.TryGetValue(panelKey, out var panel))
            panel.SetActive(true);
    }

    public void Hide(string panelKey)
    {
        if (panels.TryGetValue(panelKey, out var panel))
            panel.SetActive(false);
    }
}

// Usage:
PanelManager.Instance.ShowOnly("Pause");
```

---

## 4. **Button Registration Pattern Inconsistency**

### Problem
Different button listener registration patterns across files:

```csharp
// UIManager.cs - checks for persistent event count
private static void RegisterButton(Button button, UnityAction action)
{
    if (button == null || button.onClick.GetPersistentEventCount() > 0)
        return;
    button.onClick.AddListener(action);
}

// StarterGameplayUI.cs - removes listener first
private static void RegisterButton(Button button, UnityAction action)
{
    if (button == null) return;
    button.onClick.RemoveListener(action);
    button.onClick.AddListener(action);
}

// StarterChickenShop.cs - uses closure in loop
for (int i = 0; i < options.Length; i++)
{
    int optionIndex = i;
    RegisterButton(options[i]?.buyButton, () => TryBuyChicken(optionIndex));
}

// TimeUpPopup.cs - RemoveAllListeners()
backButton.onClick.RemoveAllListeners();
backButton.onClick.AddListener(() => { ... });
```

### Issues
- **Inconsistent**: Can't tell which approach is "correct"
- **RemoveAllListeners()**: Dangerous, might remove intended persistent listeners
- **GetPersistentEventCount()**: Doesn't account for dynamic listeners

### Recommendation
```csharp
// Create shared utility
public static class ButtonHelper
{
    public static void SetListener(Button button, UnityAction action, bool removeExisting = true)
    {
        if (button == null) return;

        if (removeExisting)
            button.onClick.RemoveAllListeners();

        button.onClick.AddListener(action);
    }
}

// Usage everywhere:
ButtonHelper.SetListener(pauseButton, PauseGame);
```

---

## 5. **CoinManager Singleton Redundancy**

### Problem
```csharp
// CoinManager - Both Awake and Start perform initialization
void Awake() { ... Instance checks, DontDestroyOnLoad ... }
void Start() { ... Loading coins from PlayerPrefs, finding UI ... }

// TryFindCoinText() - Inefficient runtime search
private void TryFindCoinText()
{
    TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(...);
    foreach (var txt in allTexts) { ... }
}

// BindCoinText() called in multiple places
// StarterGameplayUI.cs
CoinManager.Instance.BindCoinText(coinText);

// CoinManager.Awake (redundantly)
if (coinText != null)
    Instance.BindCoinText(coinText);
```

### Recommendation
- **Separate concerns**: Keep singleton pattern in Awake, move initialization to explicit Setup()
- **Cache UI references**: Instead of FindObjectsByType every time
- **Use inspector assignment**: Less runtime searching

```csharp
public class CoinManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI coinText;

    public void Initialize(TextMeshProUGUI uiText = null)
    {
        if (uiText != null)
            BindCoinText(uiText);
        else if (coinText == null)
            TryFindCoinText(); // Only search if not assigned
    }
}
```

---

## 6. **Conflicting Game Initialization Flows**

### Issue
Multiple scenes have different initialization requirements:
- **MainMenu**: Only UIManager needed
- **SelectLevel**: SceneController + UIManager
- **Starter**: GameManager + CoinManager + UIManager + StarterGameplayUI + StarterChickenShop
- **Beginner/Intermediate**: Different setup entirely?

```csharp
// GameManager.InitializeForCurrentScene() called on scene load
// But Starter scene also has StarterGameplayUI.Start()
// Both try to initialize UI, coin system, etc.

public void InitializeForCurrentScene()
{
    if (timerText == null) { TryFindUI() }
    if (currentLevelIndex >= 0 && currentLevelIndex < levelDurations.Length)
        timeRemaining = levelDurations[currentLevelIndex];
}

// StarterGameplayUI.Start()
void Start()
{
    PolishStarterUi(); // Extra UI styling
    if (CoinManager.Instance != null && coinText != null)
        CoinManager.Instance.BindCoinText(coinText);
    ResumeGame();
}
```

### Recommendation
Create **SceneInitializer** pattern:

```csharp
public interface ISceneInitializable
{
    void OnSceneLoad();
}

// Each scene has one initializer
public class StarterSceneInitializer : MonoBehaviour
{
    void Start()
    {
        GameManager.Instance.InitializeForCurrentScene();
        CoinManager.Instance.Initialize(coinText);
        UIManager.Instance.ShowGameplayUI();
    }
}
```

---

## 7. **Tightly Coupled Popup Systems**

### Problem
PopupKesehatan depends on KandangController hardcoded:

```csharp
// PopupKesehatan.cs
private KandangController currentKandang;

public void TampilkanPopup(KandangController kandang)
{
    currentKandang = kandang; // Type-locked!
}

// Later:
if (success)
    currentKandang.OnKesehatanMinigameSuccess();
```

This breaks if you switch to StarterKandangSlot!

### Recommendation
Use interface:
```csharp
public interface IHealthCheckListener
{
    void OnHealthCheckSuccess();
    void OnHealthCheckFailure();
}

public class PopupKesehatan : MonoBehaviour
{
    private IHealthCheckListener currentListener;

    public void ShowHealthCheck(IHealthCheckListener listener)
    {
        currentListener = listener;
    }
}
```

---

## 8. **Magic Numbers & Hardcoded Strings**

### Scattered Throughout:
```csharp
// CoinManager
float chickenVisualSize = new Vector2(108f, 118f);
float bubbleSize = new Vector2(130f, 56f);

// StarterGameplayUI
coinText.fontSize = Mathf.Max(coinText.fontSize, 34f);
labelText.fontSize = Mathf.Max(labelText.fontSize, 24f);

// GameManager
levelDurations = { 60f, 120f, 180f };

// KandangController
public float minDelayAyam = 3f;
public float maxDelayAyam = 8f;
```

### Recommendation
Create **GameConstants**:
```csharp
public static class GameConstants
{
    public const float STARTER_LEVEL_DURATION = 60f;
    public const float BEGINNER_LEVEL_DURATION = 120f;

    public static class UI
    {
        public const int COIN_TEXT_FONT_SIZE = 34;
        public const float BUTTON_LABEL_FONT_SIZE = 24f;
    }

    public static class Timing
    {
        public const float CHICKEN_EMOTION_MIN_DELAY = 3f;
        public const float CHICKEN_EMOTION_MAX_DELAY = 8f;
    }
}
```

---

## 9. **Unused/Abandoned Code**

### Found:
```csharp
// UIManager.cs - SetSfxVolume does nothing
public void SetSfxVolume(float volume)
{
    // Hook this to an AudioMixer group when SFX routing exists.
}

// LevelSelectController.cs - PlayBeginner/Intermediate just show locked message
public void PlayBeginner()
{
    ShowLockedMessage("Beginner");
}
```

### Recommendation
- **Remove** SFX slider and SetSfxVolume if not implemented
- **Implement** Beginner/Intermediate levels or document as future work
- **Add TODO markers** for incomplete features:

```csharp
#if DEVELOPMENT_BUILD
    Debug.LogWarning("[TODO] SFX volume control not implemented");
#endif
```

---

## 10. **Coroutine Management Issues**

### Problem
Multiple coroutines stored but not always properly cleaned up:

```csharp
// GameManager
private Coroutine timerCoroutine;
// Stopped in InitializeForCurrentScene() but not OnDestroy

// StarterKandangSlot
private Coroutine eventCoroutine;
// Stopped in StopEventTimer but duplicated in multiple methods

// KandangController
private Coroutine delayCoroutine;
// Stopped in every delay method with if (delayCoroutine != null) check
```

### Recommendation
```csharp
public class CoroutineHelper
{
    public static void StopSafe(ref Coroutine coroutine, MonoBehaviour owner)
    {
        if (coroutine == null) return;
        owner.StopCoroutine(coroutine);
        coroutine = null;
    }

    public static void StopAndStart(ref Coroutine coroutine,
        IEnumerator routine, MonoBehaviour owner)
    {
        StopSafe(ref coroutine, owner);
        coroutine = owner.StartCoroutine(routine);
    }
}
```

---

## Refactoring Priority Matrix

| Issue | Severity | Effort | Impact | Priority |
|-------|----------|--------|--------|----------|
| Duplicate Slot Systems | **CRITICAL** | High | Very High | **P0** |
| Game Flow Conflicts | **HIGH** | Medium | High | **P0** |
| UI Panel Management | HIGH | Medium | Medium | **P1** |
| Button Registration | MEDIUM | Low | Low | **P2** |
| CoinManager Refactor | MEDIUM | Low | Low | **P2** |
| Popup Coupling | MEDIUM | Medium | Medium | **P1** |
| Magic Numbers | LOW | Low | Medium | **P2** |
| Coroutine Safety | MEDIUM | Low | Medium | **P1** |
| Unused Code | LOW | Low | Low | **P3** |

---

## Implementation Roadmap

### Phase 1: Foundation (Sprint 1-2)
- [x] Choose Slot system direction (`StarterKandangSlot` active, `KandangController` legacy/migration reference)
- [x] Create GameStateManager
- [x] Integrate PopupKesehatan with new system through `IHealthCheckListener`

### Phase 2: Consolidation (Sprint 2-3)
- [x] Create PanelManager
- [x] Unify button listener pattern with `ButtonHelper`
- [ ] Refactor UIManager to use new systems

### Phase 3: Polish (Sprint 3-4)
- [x] Extract GameConstants
- [ ] Clean up unused code
- [ ] Add comprehensive comments
- [x] Document scene initialization requirements

### Phase 4: Testing & Validation (Sprint 4)
- [ ] Unit test all managers
- [ ] Gameplay testing across all scenes
- [ ] Performance profiling

---

## Quick Wins (Can Do Immediately)

1. **Delete unused code** (SetSfxVolume, PlayBeginner/Intermediate stubs)
2. **Create GameConstants** - 30 minutes
3. **Add null check helpers** - 15 minutes
4. **Document scene dependencies** - 20 minutes
5. **Create CoroutineHelper** - 20 minutes

These can be done before tackling bigger refactors.

---

## Updated Codebase Analysis - 23 Mei 2026

This section reflects the current repository state after the Starter shop cleanup and disabled-buy investigation.

### Already Partially Addressed

- `StarterChickenShop` no longer refreshes buy-button state every frame.
  It now refreshes from `CoinManager.CoinsChanged` and `StarterKandangSlot.StateChanged`.
- `StarterKandangSlot` now emits a `StateChanged` event when its state changes.
- `CoinManager` now emits `CoinsChanged`, clamps coin overflow, saves after initialization, and uses `FindObjectsByType` instead of deprecated `FindObjectsOfType`.
- The disabled-buy root cause was found in `Assets/Prefab/BQ_KandangSlot.prefab`: the prefab root was inactive. The prefab root should remain active, because shop availability depends on `slot.gameObject.activeInHierarchy && slot.IsEmpty`.
- `TimeUpPopup`, `SceneController`, and `GameManager` now guard against unavailable next-level scenes instead of blindly loading `Beginner` / `Intermediate`.

### Recommendations Still Valid

- Duplicate kandang systems remain the biggest architecture risk:
  `StarterKandangSlot` is the active Starter flow, while `KandangController` plus `PopupKesehatan` is a legacy/alternate flow.
- Pause/game state is still split across `UIManager`, `StarterGameplayUI`, `GameManager`, and `SceneController`.
- UI panel management is still duplicated between `UIManager` and `StarterGameplayUI`.
- Button registration patterns are still inconsistent across scripts.
- Popup health minigame code is no longer type-locked to `KandangController`, but Starter scene wiring still needs Unity playtesting.
- Magic numbers and UI styling values are still embedded directly in MonoBehaviours.

### New Findings Not Covered Above

#### 11. Build Settings Scene Order Is Suspicious

Current `ProjectSettings/EditorBuildSettings.asset` order starts with:

```text
Assets/Scenes/SelectLevel.unity
Assets/Scenes/Starter.unity
Assets/Scenes/MainMenu.unity
Assets/Scenes/KoleksiIoT.unity
```

If the game is built and launched normally, Unity loads scene index `0`, which is currently `SelectLevel`, not `MainMenu`.

Recommendation:
- Move `Assets/Scenes/MainMenu.unity` to build index `0`.
- Keep scene navigation by explicit scene name, but still maintain correct build order for packaged builds.

#### 12. Test / Debug Economy Values Are Leaking Into Scene Data

`Starter.unity` previously used an extreme starting coin value (`2147483647`) for the Starter scene. The first cleanup pass changed this to `100`.

Recommendation:
- Keep a realistic default before release.
- Add a separate debug/dev configuration for high starting coin instead of storing it directly in production scene data.
- Consider a `GameConfig` ScriptableObject for starting coin, level duration, rewards, prices, and debug toggles.

#### 13. Runtime UI Styling Is Mixed With Gameplay Logic

`StarterGameplayUI` and `StarterChickenShop` still style buttons, panels, icons, and text at runtime.
This makes scripts responsible for both gameplay state and visual presentation.

Recommendation:
- Move stable visual styling into prefabs, themes, or serialized UI components.
- Keep runtime code focused on state changes: interactable, visible/hidden, label text, and values.
- If runtime styling is still needed, extract it into a small `StarterUiStyler` or `UiStyleUtility`.

#### 14. Runtime Discovery Should Become Scene Initialization

`StarterChickenShop` discovers `StarterKandangSlot` objects with `FindObjectsByType`.
This is good as a safety net after merge conflicts, but it should not be the long-term primary architecture.

Recommendation:
- Add a `StarterSceneInitializer` that wires:
  - `CoinManager`
  - `StarterChickenShop`
  - `StarterKandangSlot[]`
  - `StarterGameplayUI`
  - `GameManager` timer references
- Keep runtime discovery as a fallback with a warning when serialized references are missing.

#### 15. User/Generated Unity Assets Need Commit Hygiene

The working tree has unrelated Unity-generated changes such as:

```text
UserSettings/*
Assets/TextMesh Pro/... font/material assets
Assets/_Recovery/
Assets/Adaptive Performance/
```

Recommendation:
- Do not commit `UserSettings` unless the team intentionally shares editor layout/preferences.
- Review generated TMP asset diffs carefully before committing.
- Add or update `.gitignore` / `.contextignore` rules for generated local-only Unity artifacts.
- Keep gameplay-code commits separate from Unity editor/cache churn.

#### 16. Logging Should Be Gated

Gameplay scripts use many `Debug.Log` calls inside regular gameplay paths, especially slot state transitions and coin changes.

Recommendation:
- Introduce a lightweight logging guard:

```csharp
public static class GameLog
{
    public static bool Verbose;

    public static void Info(string message)
    {
        if (Verbose)
            Debug.Log(message);
    }
}
```

- Keep warnings/errors active, but gate noisy success/progress logs behind a debug toggle.

#### 17. Persistence Has No Profile Boundary

`CoinManager` stores `TotalCoin` directly in `PlayerPrefs`.
This is fine for a prototype, but it has no save-slot/profile/reset boundary.

Recommendation:
- Define a small save service before adding more persistent values.
- Namespace keys, for example `BroilerQuest.TotalCoin`.
- Add explicit reset methods for testing and new-game flow.

### Revised Priority Additions

| Issue | Severity | Effort | Impact | Priority |
|-------|----------|--------|--------|----------|
| Build Settings scene order | HIGH | Low | High | P0 |
| Debug economy value in scene | HIGH | Low | High | P0 |
| Commit hygiene for generated Unity files | HIGH | Low | High | P0 |
| Runtime discovery as primary wiring | MEDIUM | Medium | Medium | P1 |
| Runtime UI styling mixed with logic | MEDIUM | Medium | Medium | P1 |
| Ungated gameplay logging | LOW | Low | Medium | P2 |
| PlayerPrefs without save boundary | MEDIUM | Low | Medium | P2 |

### Recommended Next Safe Steps

1. Done: Fix Build Settings order so `MainMenu` is index `0`.
2. Done: Replace the extreme Starter starting coin with a realistic value (`100`). Remaining: add a debug-only override path.
3. Done: Add commit hygiene rules for `UserSettings`, `_Recovery`, and generated local assets.
4. Done: Decide `KandangController` is legacy/migration reference and adapt popup/minigame behind shared interface. Remaining: remove it after scene migration.
5. Done: Extract `GameStateManager`. It now lazy-creates itself when needed.
6. Done: Add `StarterSceneInitializer`. Remaining: attach it in the Starter scene and assign references through Inspector.

---

## Updated Codebase Analysis - 23 Mei 2026 (Branch dev/Hylmi)

Bagian ini merupakan hasil analisis langsung dari branch `dev/Hylmi` commit `dd2cb12` ("Clean up starter gameplay flow").

### Temuan Baru: Sistem Animasi Ayam

Branch ini menambahkan sistem animasi ayam (file `Assets/Animation/Ayam.controller`, plus animasi `ayambiasa`, `ayamdingin`, `ayampanas`), dan `StarterKandangSlot` mendapat field `chickenAnimator`, `idleAnimParam`, `heatAnimParam`, `coldAnimParam`, serta method `UpdateAnimationByNeed()` dan `ResetAnimationToNormal()`. Sebelumnya ada beberapa bug kritis pada implementasi ini; cleanup pass pertama sudah menangani bagian aktifnya, tetapi tetap perlu playtest di Unity.

#### 18. Parameter Nama Animasi Tidak Cocok Antara Controller dan Script (RESOLVED IN CLEANUP PASS 1)

Animator Controller `Ayam.controller` mendefinisikan parameter trigger bernama `isbakar` dan `isdingin`. Namun kode `StarterKandangSlot` memanggil:

```csharp
chickenAnimator.SetTrigger(heatAnimParam);   // default: "Panas"
chickenAnimator.SetTrigger(coldAnimParam);   // default: "Dingin"
chickenAnimator.SetTrigger(idleAnimParam);   // default: "Normal"
```

Nama default di script (`"Panas"`, `"Dingin"`, `"Normal"`) **tidak cocok** dengan nama parameter di Controller (`"isbakar"`, `"isdingin"`). Akibatnya animasi tidak pernah terpicu sama sekali, dan Unity akan membuang warning `Animator.SetTrigger` parameter not found di console.

**Rekomendasi:** Pilih salah satu:
- Ubah default value di Inspector setiap slot menjadi `isbakar` dan `isdingin` (dan tambahkan parameter `Normal` atau gunakan `isbakar = false` untuk idle).
- Atau ubah nama parameter di Animator Controller menjadi `"Panas"`, `"Dingin"`, dan `"Normal"` agar konsisten dengan kode.

#### 19. `FindAnimator()` Adalah Method Mati — Tidak Pernah Dipanggil (RESOLVED IN CLEANUP PASS 1)

Method berikut didefinisikan di `StarterKandangSlot` namun **tidak pernah dipanggil** di mana pun:

```csharp
private void FindAnimator()
{
    if (chickenAnimator == null && chickenVisual != null)
    {
        Transform ayam = chickenVisual.transform.Find("Ayam");
        if (ayam != null)
            chickenAnimator = ayam.GetComponent<Animator>();
    }
}
```

Akibatnya, jika `chickenAnimator` tidak di-assign manual di Inspector, animasi **tidak akan pernah bekerja** meski prefab-nya punya Animator.

**Rekomendasi:** Panggil `FindAnimator()` di dua tempat:
```csharp
// Di Awake(), setelah chickenVisual diinisialisasi
FindAnimator();

// Di TryPlaceChicken(), setelah spawnedChicken di-Instantiate
spawnedChicken = Instantiate(chickenPrefab, parent);
PositionChickenVisual(spawnedChicken.transform);
// Cari Animator di chicken yang baru di-spawn
chickenAnimator = spawnedChicken.GetComponentInChildren<Animator>();
```

#### 20. Ayam yang Dibeli dari Shop Tidak Pernah Dapat Animator (RESOLVED IN CLEANUP PASS 1)

Saat pemain membeli ayam melalui `StarterChickenShop`, `TryPlaceChicken(chickenPrefab)` dipanggil dan meng-`Instantiate` prefab ayam ke `spawnedChicken`. Namun `chickenAnimator` tidak pernah di-set ulang untuk menunjuk ke Animator di `spawnedChicken`. Hanya ayam yang sudah ada di scene sejak awal (via `startsOccupied = true`) yang bisa punya Animator jika di-assign manual di Inspector.

**Rekomendasi:**
```csharp
// Dalam TryPlaceChicken(), setelah instantiate:
spawnedChicken = Instantiate(chickenPrefab, parent);
PositionChickenVisual(spawnedChicken.transform);
chickenAnimator = spawnedChicken.GetComponentInChildren<Animator>(true);
```

#### 21. `ResetAnimationToNormal()` Dipanggil di `Awake()` Sebelum Ayam Ada (RESOLVED IN CLEANUP PASS 1)

`ResetChickenProgress()` dipanggil di `Awake()`, yang di dalamnya memanggil `ResetAnimationToNormal()` → `SetTrigger("Normal")`. Jika `chickenAnimator` belum null (misalnya di-assign di Inspector tapi ayam belum di-spawn), ini bisa menyebabkan state Animator yang tidak diharapkan saat scene pertama kali load.

**Rekomendasi:** Guard sederhana:
```csharp
private void ResetAnimationToNormal()
{
    if (chickenAnimator != null && occupied) // hanya reset jika ayam ada
        chickenAnimator.SetTrigger(idleAnimParam);
}
```

### Konfirmasi Temuan Lama yang Masih Ada

- **Build Settings order** — fixed in cleanup pass 1: `MainMenu` sekarang index `0`.
- **`UserSettings/` di-commit** — masih ada di branch ini; `.gitignore` sudah ditambah, tetapi file yang sudah tracked perlu keputusan commit hygiene terpisah.
- **`KandangController` vs `StarterKandangSlot`** — dua file masih hidup berdampingan, tetapi `KandangController` sekarang legacy/migration reference dan popup sudah lewat interface.
- **Pause logic terpecah** — sudah mulai dipusatkan lewat `GameStateManager`, tetapi masih perlu scene wiring/playtest sebelum dianggap selesai total.

### Tambahan Prioritas Revisi (dari Branch dev/Hylmi)

| Issue | Severity | Effort | Impact | Priority |
|-------|----------|--------|--------|----------|
| Nama parameter animasi tidak cocok (Controller vs Script) | **CRITICAL** | Low | High | **P0** |
| `FindAnimator()` tidak pernah dipanggil | **HIGH** | Low | High | **P0** |
| Ayam dari shop tidak dapat Animator | **HIGH** | Low | High | **P0** |
| `ResetAnimationToNormal` dipanggil sebelum ayam ada | MEDIUM | Low | Medium | **P2** |

### Langkah Aman Berikutnya (dari Branch dev/Hylmi)

1. Done: Selaraskan nama parameter Animator ke `isbakar`/`isdingin`.
2. Done: Panggil animator discovery di `Awake()` dan update `chickenAnimator` di `TryPlaceChicken()` setelah `Instantiate`.
3. Done: Guard `ResetAnimationToNormal()` agar tidak trigger saat slot masih kosong.
4. Remaining: Playtest animasi di Unity dan pertimbangkan `Animator.StringToHash` jika animasi sudah stabil.

---

## Files to Create (Architecture Improvements)

```
Assets/Script/
├── GameStateManager.cs (done)
├── PanelManager.cs (done)
├── StarterSceneInitializer.cs (done)
├── GameConstants.cs (done)
├── GameLog.cs (done)
├── ButtonHelper.cs (done)
├── CoroutineHelper.cs (done)
└── IHealthCheckListener.cs (done)
```

---

## Conclusion

The codebase works but has grown organically without a clear architecture. The **highest priority is resolving the duplicate Slot systems** - this single issue creates cascading confusion throughout the project.

By implementing the GameStateManager and PanelManager early, you'll reduce friction for future features and make the codebase significantly more maintainable.

**Estimated total refactoring time: 3-4 sprints** (can be done incrementally without breaking gameplay)
