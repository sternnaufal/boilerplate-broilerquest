

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
| Before answering a question | `npx sigmap --query "<your question>"` |
| After code changes | `npx sigmap --track` |
| To verify coverage | `npx sigmap --report` |
| To query by topic | `npx sigmap --query "<topic>"` |

Always run `npx sigmap --query` before searching for files relevant to a task.
## Assets

### Assets\Script\CoinManager.cs
```
class CoinManager
  AddCoin(int amount) → void
  CanAfford(int amount) → bool
  SpendCoin(int amount) → bool
  SetTotalCoin(int amount) → void
  BindCoinText(TextMeshProUGUI text) → void
  GetTotalCoin() → int
```

### Assets\Script\LevelSelectController.cs
```
class LevelSelectController
  PlayStarter() → void
  PlayBeginner() → void
  PlayIntermediate() → void
  ShowLockedMessage(string levelName) → void
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

### Assets\Script\StarterKandangSlot.cs
```
class StarterKandangSlot
  TryPlaceChicken(GameObject chickenPrefab) → bool
  ClearChicken() → void
  OnPointerClick(PointerEventData eventData) → void
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

### Assets\Script\GameManager.cs
```
class GameManager
  InitializeForCurrentScene() → void
  GoToNextLevel() → void
  ReturnToMainMenu() → void
  IsGameActive() → bool
  SetGameActive(bool active) → void
```

### Assets\Script\KandangController.cs
```
class KandangController
  OnPointerClick(PointerEventData eventData) → void
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
```

### Assets\Script\TimeUpPopup.cs
```
class TimeUpPopup
  Setup(int finalCoin, int levelIndex, string[] scenes) → void
```
