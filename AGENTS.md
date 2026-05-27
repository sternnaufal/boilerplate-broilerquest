## ⚠️ MANDATORY — READ BEFORE EVERY TASK

Before writing any code or making any changes to the Unity project,
confirm understanding of ALL rules below.
Start your response with: **"Rules loaded. Proceeding with task."**

---

# Unity Agent Rules

## UI Creation Rules

- **NEVER create UI elements directly from script.** UI harus dibuat manual / drag-and-drop di Unity Editor.
- Jika task berkaitan dengan UI, hanya boleh membuat **logic controller / data controller** saja di script.
- Sebelum membuat UI logic, wajib mendeskripsikan **struktur UI yang dibutuhkan** terlebih dahulu:
  - Komponen apa saja (Canvas, Panel, Button, Text, Image, dll.)
  - Hierarki parent-child nya
  - Nama GameObject yang disarankan
- Tidak boleh langsung generate UI — **plan dulu, build belakangan.**

## Scene & Inspector Consistency Rules

- Pastikan semua UI yang dibuat **tampil di Inspector dan preview scene in-game** secara konsisten.
- Setiap UI wajib memiliki komponen yang lengkap: **Canvas, Main Camera** sudah terset dengan benar.
- **Jangan override komponen in-game** yang sudah ada hanya untuk keperluan UI baru.
- Inspector dan tampilan in-game harus **selalu sinkron** — tidak boleh ada perbedaan antara keduanya.

## Script Quality Rules

- **Tidak boleh ada missing properties.** Setiap field yang di-assign di Inspector harus dipastikan ter-assign sebelum dijalankan.
- **Tidak boleh ada missing script** pada GameObject manapun setelah selesai bekerja.
- Sebelum membuat fungsi baru, **cek terlebih dahulu** apakah fungsi dengan logic serupa sudah ada di codebase. Jangan duplikat fungsi.
- **Tidak boleh ada unused import / namespace.** Setiap `using` statement harus benar-benar digunakan.
- Setiap kali membuat script baru, **validasi namespace** — pastikan tidak konflik dengan struktur project.

## Workflow for UI Tasks

Saat diminta membuat fitur yang melibatkan UI:

1. Jelaskan struktur UI yang dibutuhkan (hierarchy, komponen, nama)
2. Buat script logic/controller saja — **tanpa `new GameObject()`, tanpa `AddComponent<>()` untuk UI**
3. Instruksikan user untuk membuat UI secara manual di Editor lalu drag-and-drop ke field yang tersedia

## Tools

<!-- sigmap-tools -->

```json
[
  {
    "name": "sigmap_ask",
    "description": "Rank source files by relevance to a natural-language query. Run before exploring the codebase.",
    "command": "npx sigmap --query \"$QUERY\""
  },
  {
    "name": "sigmap_generate",
    "description": "Regenerate signatures and measure context coverage. Run after changing config or source dirs.",
    "command": "npx sigmap --track"
  },
  {
    "name": "sigmap_query",
    "description": "Rank all files by relevance using TF-IDF and write a focused mini-context.",
    "command": "npx sigmap --query \"$QUERY\""
  },
  {
    "name": "sigmap_weights",
    "description": "Show learned file-ranking multipliers accumulated from past sessions.",
    "command": "npx sigmap weights"
  }
]
```

## Auto-generated signatures
<!-- Updated by gen-context.js -->
# Code signatures

## SigMap commands

| When | Command |
|------|---------|
| Before answering a question | `sigmap ask "<your question>"` |
| After code changes | `sigmap validate` |
| To query by topic | `sigmap --query "<topic>"` |

Always run `sigmap ask` or `sigmap --query` before searching for files relevant to a task.
## Assets

### Assets\Script\CoinManager.cs
```
class CoinManager
  Awake() → void
  Initialize(TextMeshProUGUI uiText = null) → void
  AddCoin(int amount) → void
  CanAfford(int amount) → bool
  SpendCoin(int amount) → bool
  SetTotalCoin(int amount) → void
  GetTotalCoin() → int
  BindCoinText(TextMeshProUGUI text) → void
```

### Assets\Script\FeedManager.cs
```
class FeedManager
  Awake() → void
  AddFeed(int amount) → void
  UseFeed(int amount) → bool
  GetFeedCount() → int
  CanUseFeed(int amount) → bool
  SetFeedCount(int amount) → void
```

### Assets\Script\GameConstants.cs
```
class GameConstants
class Persistence
class LevelDuration
class UI
class StarterSlot
class Economy
class LevelUnlock
class IoT
class JigsawMinigame
```

### Assets\Script\GameManager.cs
```
class GameManager
  InitializeForCurrentScene() → void
  GoToNextLevel() → void
  ReturnToMainMenu() → void
  IsGameActive() → bool
  SetGameActive(bool active) → void
  OnDestroy() → void
```

### Assets\Script\GameStateManager.cs
```
enum GameState
class GameStateManager
  SetGameState(GameState newState) → void
  SetMenu() → void
  SetPlaying() → void
  SetPaused() → void
  SetGameOver() → void
  TrySetGameState(GameState newState) → bool
```

### Assets\Script\GlobalUIOverlay.cs
```
class GlobalUIOverlay
```

### Assets\Script\JigsawMinigameController.cs
```
class JigsawMinigameController
  Awake() → void
  ShowJigsaw(IHealthCheckListener listener, Texture puzzleTexture, string eventTitle = "") → bool
  OnPieceClicked(JigsawPiece clicked) → void
```

### Assets\Script\KoleksiIoTController.cs
```
class KoleksiIoTController
class IoTProduct
```

### Assets\Script\LevelSelectController.cs
```
class LevelSelectController
  PlayStarter() → void
  PlayBeginner() → void
  PlayIntermediate() → void
  ShowLockedMessage(string levelName) → void
```

### Assets\Script\LevelTimer.cs
```
class LevelTimer
  StartTimer(float duration) → void
  StopTimer() → void
  BindTimerText(TextMeshProUGUI text) → void
```

### Assets\Script\PopupKesehatan.cs
```
class PopupKesehatan
  Awake() → void
  TampilkanPopup(KandangController kandang) → void
  ShowHealthCheck(IHealthCheckListener listener) → void
```

### Assets\Script\SceneController.cs
```
class SceneController
  GoToMainMenu() → void
  GoToSelectLevel() → void
  GoToKoleksiIoT() → void
  GoToLevel(int levelIndex) → void
```

### Assets\Script\StarterChickenShop.cs
```
class StarterChickenOption
class ShopButtonStyleConfig
class OptionIconConfig
class StarterChickenShop
  TryBuyFeed() → void
  BuyOption0() → void
  BuyOption1() → void
  BuyOption2() → void
  TryBuyChicken(int optionIndex) → bool
```

### Assets\Script\StarterGameplayUI.cs
```
class PanelStyleConfig
class ButtonStyleConfig
class StarterGameplayUI
  PauseGame() → void
  ResumeGame() → void
  ToggleHpPanel() → void
  CloseHpPanel() → void
  ShowHpPanel(bool visible) → void
```

### Assets\Script\StarterIoTController.cs
```
class StarterIoTController
  IsPurchased(string productKey) → bool
  IsActiveForNeed(string productKey) → bool
  IsActive(string productKey) → bool
  ToggleDevice(string productKey) → void
  SetDeviceActive(string productKey, bool active) → void
  PurchaseDevice(string productKey) → void
  RefreshAll() → void
class IoTDeviceDef
class IoTDeviceUI
```

### Assets\Script\UIGlobalBinder.cs
```
class UIGlobalBinder
```

### Assets\Script\UIManager.cs
```
class UIManager
  ShowMainMenu() → void
  ShowMainScreen() → void
  StartGame() → void
  PauseGame() → void
  ResumeGame() → void
  ShowOptionsFromMain() → void
  ShowOptionsFromPause() → void
  BackFromOptions() → void
```

### Assets\Script\ButtonHelper.cs
```
class ButtonHelper
  AddListenerOnce(Button button, UnityAction action) → void
  SetSingleListener(Button button, UnityAction action) → void
  AddListenerOnce(Slider slider, UnityAction<float> action) → void
```

### Assets\Script\CoroutineHelper.cs
```
class CoroutineHelper
  StopSafe(MonoBehaviour owner, ref Coroutine coroutine) → void
  StopAndStart(MonoBehaviour owner, ref Coroutine coroutine, IEnumerator routine) → void
```

### Assets\Script\GameLog.cs
```
class GameLog
  Info(string message) → void
```

### Assets\Script\IHealthCheckListener.cs
```
interface IHealthCheckListener
```

### Assets\Script\JigsawPiece.cs
```
class JigsawPiece
  Setup(int boardIndex, int currentIndex, Texture texture, Rect uvRect) → void
  SetCurrentTile(int currentIndex, Rect uvRect) → void
  SetHighlighted(bool active) → void
  OnPointerClick(PointerEventData eventData) → void
```

### Assets\Script\KandangController.cs
```
class KandangController
  OnPointerClick(PointerEventData eventData) → void
```

### Assets\Script\PanelManager.cs
```
class PanelManager
  RegisterPanel(string key, GameObject panel) → void
  ShowOnly(string panelKey) → void
  Show(string panelKey) → void
  Hide(string panelKey) → void
```

### Assets\Script\PopupHasilKesehatan.cs
```
class PopupHasilKesehatan
  Setup(bool isSuccess, System.Action onBackCallback) → void
```

### Assets\Script\StarterSceneInitializer.cs
```
class StarterSceneInitializer
```

### Assets\Script\TimeUpPopup.cs
```
class TimeUpPopup
  Setup(int finalCoin, int levelIndex, string[] scenes) → void
```
