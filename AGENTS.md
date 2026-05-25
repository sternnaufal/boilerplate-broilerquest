

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

### Assets\Script\GameConstants.cs
```
class GameConstants
class Persistence
class LevelDuration
class UI
class StarterSlot
class JigsawMinigame
```

### Assets\Script\JigsawMinigameController.cs
```
class JigsawMinigameController
  GetOrCreateInstance() → JigsawMinigameController
  ShowJigsaw(IHealthCheckListener listener, Texture puzzleTexture, string eventTitle = "") → bool
  OnPieceClicked(JigsawPiece clicked) → void
```

### Assets\Script\JigsawPiece.cs
```
class JigsawPiece
  Setup(int boardIndex, int currentIndex, Texture texture, Rect uvRect) → void
  SetCurrentTile(int currentIndex, Rect uvRect) → void
  SetHighlighted(bool active) → void
  OnPointerClick(PointerEventData eventData) → void
```

### Assets\Script\KoleksiIoTController.cs
```
class KoleksiIoTController
  GoBack() → void
```

### Assets\Script\StarterKandangSlot.cs
```
class StarterKandangSlot
  TryPlaceChicken(GameObject chickenPrefab) → bool
```

### Assets\Script\ButtonHelper.cs
```
class ButtonHelper
  AddListenerOnce(Button button, UnityAction action) → void
  SetSingleListener(Button button, UnityAction action) → void
  AddListenerOnce(Slider slider, UnityAction<float> action) → void
```

### Assets\Script\CoinManager.cs
```
class CoinManager
  Initialize(TextMeshProUGUI uiText = null) → void
  AddCoin(int amount) → void
  CanAfford(int amount) → bool
  SpendCoin(int amount) → bool
  SetTotalCoin(int amount) → void
  GetTotalCoin() → int
  BindCoinText(TextMeshProUGUI text) → void
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

### Assets\Script\GameManager.cs
```
class GameManager
  InitializeForCurrentScene() → void
  GoToNextLevel() → void
  ReturnToMainMenu() → void
  IsGameActive() → bool
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

### Assets\Script\IHealthCheckListener.cs
```
interface IHealthCheckListener
```

### Assets\Script\KandangController.cs
```
class KandangController
  OnPointerClick(PointerEventData eventData) → void
```

### Assets\Script\LevelSelectController.cs
```
class LevelSelectController
  PlayStarter() → void
  PlayBeginner() → void
  PlayIntermediate() → void
  ShowLockedMessage(string levelName) → void
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

### Assets\Script\PopupKesehatan.cs
```
class PopupKesehatan
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
class StarterChickenShop
  BuyOption0() → void
  BuyOption1() → void
  BuyOption2() → void
  TryBuyChicken(int optionIndex) → bool
  RefreshShopState() → void
  SetKandangSlots(StarterKandangSlot[] slots) → void
```

### Assets\Script\StarterGameplayUI.cs
```
class StarterGameplayUI
  PauseGame() → void
  ResumeGame() → void
  ToggleHpPanel() → void
  CloseHpPanel() → void
  ShowHpPanel(bool visible) → void
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
