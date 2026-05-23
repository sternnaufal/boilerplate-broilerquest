# BroilerQuest Sigmap

Tanggal: 23 Mei 2026
Coverage: 11/11 runtime script modules mapped (100%)

Sigmap ini adalah peta sinyal antar modul gameplay/UI yang ada sekarang. Semble dipakai sebagai layer pencarian semantik, sementara bagian "Semble Flows" di dokumen ini menjelaskan rangkaian perilaku utama agar tiap sinyal punya konteks flow, bukan hanya daftar referensi file.

Tooling aktif:

- SigMap config: `gen-context.config.json`
- SigMap generated context: `AGENTS.md`
- SigMap usage log: `.context/usage.ndjson`
- Semble OpenCode agent: `.opencode/agents/semble-search.md`
- OpenCode MCP entries: `opencode.json`

Verification commands:

- Regenerate context and usage metrics: `npx sigmap --track`
- Confirm coverage: `npx sigmap --report`

Current SigMap report: 11/11 code files included, module coverage 100%.

Latest verification:

- `npx sigmap --track`: 11 files scanned, 52 symbols found, coverage A / 100%.
- `npx sigmap --report`: 11 of 11 code files included, `Assets/Script` module coverage 100%.

## Module Coverage

| Module | Path | Responsibility | Public Signals | Depends On | Coverage |
| --- | --- | --- | --- | --- | --- |
| UI Manager | `Assets/Script/UIManager.cs` | Main menu, options, pause, HUD visibility, pause time scale | `Instance`, `ShowMainMenu()`, `ShowMainScreen()`, `StartGame()`, `PauseGame()`, `ResumeGame()`, `ShowOptionsFromMain()`, `ShowOptionsFromPause()`, `BackFromOptions()` | `GameManager`, `UnityEngine.UI`, `InputSystem`, `AudioListener` | 100% |
| Game Manager | `Assets/Script/GameManager.cs` | Level timer, time-up flow, scene progression, active-game gate | `Instance`, `InitializeForCurrentScene()`, `GoToNextLevel()`, `ReturnToMainMenu()`, `IsGameActive()`, `SetGameActive(bool)` | `UIManager`, `CoinManager`, `TimeUpPopup`, `SceneManager`, `TextMeshProUGUI` | 100% |
| Coin Manager | `Assets/Script/CoinManager.cs` | Coin total, spending guard, and coin UI binding | `Instance`, `AddCoin(int)`, `CanAfford(int)`, `SpendCoin(int)`, `SetTotalCoin(int)`, `BindCoinText(TextMeshProUGUI)`, `GetTotalCoin()` | `TextMeshProUGUI` | 100% |
| Kandang Controller | `Assets/Script/KandangController.cs` | Coop click state machine for chicken/feed/vitamin/harvest | `OnPointerClick(PointerEventData)`, `OnKesehatanMinigameSuccess()`, `OnKesehatanMinigameFail()` | `GameManager`, `PopupKesehatan`, `CoinManager` | 100% |
| Level Select Controller | `Assets/Script/LevelSelectController.cs` | Starter level launch and locked-level feedback | `PlayStarter()`, `PlayBeginner()`, `PlayIntermediate()`, `ShowLockedMessage(string)` | `GameManager`, `SceneManager`, `UnityEngine.UI`, `TextMeshProUGUI` | 100% |
| Popup Kesehatan | `Assets/Script/PopupKesehatan.cs` | Timing minigame popup and success/fail decision | `Instance`, `TampilkanPopup(KandangController)` | `KandangController`, `PopupHasilKesehatan`, `UnityEngine.UI`, `TextMeshProUGUI` | 100% |
| Popup Hasil Kesehatan | `Assets/Script/PopupHasilKesehatan.cs` | Result popup after health minigame | `Setup(bool, Action)` | `PopupKesehatan` callback, `UnityEngine.UI`, `TextMeshProUGUI` | 100% |
| Starter Chicken Shop | `Assets/Script/StarterChickenShop.cs` | Starter shop purchase flow, affordability checks, and empty-slot assignment | `BuyOption0()`, `BuyOption1()`, `BuyOption2()`, `TryBuyChicken(int)`, `RefreshShopState()` | `CoinManager`, `StarterKandangSlot`, `UnityEngine.UI`, `TextMeshProUGUI` | 100% |
| Starter Gameplay UI | `Assets/Script/StarterGameplayUI.cs` | Starter HUD, pause panel, HP/shop panel, and coin text binding | `PauseGame()`, `ResumeGame()`, `ToggleHpPanel()`, `CloseHpPanel()`, `ShowHpPanel(bool)` | `GameManager`, `CoinManager`, `StarterChickenShop`, `UnityEngine.UI`, `TextMeshProUGUI` | 100% |
| Starter Kandang Slot | `Assets/Script/StarterKandangSlot.cs` | Starter chicken placement, fallback care/harvest bubble, and harvest reward | `IsEmpty`, `TryPlaceChicken(GameObject)`, `ClearChicken()`, `OnPointerClick(PointerEventData)` | `GameManager`, `CoinManager`, `UnityEngine.EventSystems`, `UnityEngine.UI`, `TextMeshProUGUI` | 100% |
| Time Up Popup | `Assets/Script/TimeUpPopup.cs` | End-of-timer score popup actions | `Setup(int, int, string[])` | `GameManager`, `TextMeshProUGUI`, `UnityEngine.UI` | 100% |

## Semble Flows

### UI Shell Semble

1. Unity scene enters Play Mode.
2. `UIManager.Start()` registers button listeners.
3. `UIManager.ShowMainMenu()` disables active gameplay, enables `mainMenuPanel`, hides options/pause/HUD, and restores `Time.timeScale = 1`.
4. `StartGame()` hides menu panels, enables `hudPanel`, marks `GameManager` active, and initializes scene timer references.
5. `PauseGame()` hides HUD, shows pause panel, and sets `Time.timeScale = 0`.
6. `ResumeGame()` hides pause panel, shows HUD, and sets `Time.timeScale = 1`.
7. Options can open from main menu or pause; `BackFromOptions()` returns to the remembered source.

### Timer Semble

1. `GameManager.InitializeForCurrentScene()` resolves canvas and timer text.
2. If timer text exists, it resets `timeRemaining` from `levelDurations[currentLevelIndex]`.
3. `TimerCoroutine()` decrements once per second while active.
4. When time reaches zero, `TimeUp()` stops active gameplay and instantiates `timeUpPopupPrefab`.
5. `TimeUpPopup.Setup()` wires Back to `GameManager.ReturnToMainMenu()`.

### Level Select Semble

1. `LevelSelectController.OnEnable()` registers button listeners and disables locked buttons when configured.
2. `PlayStarter()` resets `Time.timeScale`, sets `GameManager.currentLevelIndex = 0`, marks gameplay active, and loads the Starter scene.
3. `PlayBeginner()` and `PlayIntermediate()` currently show locked-level feedback instead of loading scenes.

### Starter Shop Semble

1. `StarterGameplayUI.Start()` binds the Starter coin text to `CoinManager`.
2. Opening the HP/shop panel calls `StarterChickenShop.RefreshShopState()`.
3. `StarterChickenShop.TryBuyChicken()` finds an empty `StarterKandangSlot`, spends coins through `CoinManager`, and places the selected chicken.
4. `StarterKandangSlot` starts a care-event timer, shows a care bubble, then a harvest bubble. If no bubble object is wired, it creates a runtime fallback bubble with text labels.
5. Harvest click adds the configured reward through `CoinManager.AddCoin()` and clears the slot.

### Coop Interaction Semble

1. `KandangController.ResetKeAwal()` hides all emotes, resets indicators, then starts a random delay.
2. Chicken emote appears and the coop waits for chicken click.
3. Click advances to feed emote, then feed completion toggles the feed indicator.
4. Vitamin delay starts; vitamin click opens `PopupKesehatan`.
5. `PopupKesehatan` timing result calls success or fail callback through `PopupHasilKesehatan`.
6. Success enables health indicator and later harvest emote.
7. Harvest click adds 10 coins via `CoinManager.AddCoin(10)` and resets the coop.

## Inspector Wiring Contract

Required scene objects for the current minimal UI:

- `UIManager` GameObject with `UIManager.cs`.
- A Canvas containing `mainMenuPanel`, `optionsPanel`, `pausePanel`, and `hudPanel`.
- Main menu buttons wired to `startButton`, `optionsButton`, `exitButton`.
- Options back button wired to `backToMainButton`.
- Pause buttons wired to `resumeButton`, `pauseOptionsButton`, `pauseMainMenuButton`.
- HUD pause button wired to `pauseButton`.
- Optional volume sliders wired to `musicVolumeSlider` and `sfxVolumeSlider`.
- `LevelSelectController` buttons wired to Starter, Beginner, and Intermediate actions.
- Starter scene HUD/pause/HP panels wired to `StarterGameplayUI`.
- Starter shop options wired to `StarterChickenShop` with matching `StarterKandangSlot` references.
- Starter kandang slots wired with chicken visuals, bubble visuals, and optional bubble sprites.

## Risk Notes

- `PopupKesehatan.Awake()` assumes `popupPanel` is assigned before Play. If it is null, Unity will throw a runtime `NullReferenceException`.
- `KandangController.ResetSemuaIndikatorDanEmote()` assumes all emote and indicator references are assigned before `Start()`.
- `TimeUpPopup.Setup()` assumes `backButton` and `continueButton` are assigned.
- `StarterChickenShop.RefreshShopState()` depends on `CoinManager.Instance`; without it, buy buttons stay disabled.
- `StarterChickenShop` ignores inactive kandang slots, so the Starter scene must keep `BQ_KandangArea` active during gameplay.
- `StarterKandangSlot.TryPlaceChicken()` can still occupy a slot without a prefab if a fallback `chickenVisual` exists; otherwise it logs a warning but still starts the slot flow.
- The local `com.gladekit.mcp-bridge` package dependency was intentionally removed from Unity package files because it pointed at another developer's absolute Windows path.
