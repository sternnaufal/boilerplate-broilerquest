using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectController : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string starterSceneName = "Starter";

    [Header("Level Buttons")]
    [SerializeField] private Button starterButton;
    [SerializeField] private Button beginnerButton;
    [SerializeField] private Button intermediateButton;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    [Header("Locked Level Feedback")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string lockedMessage = "Level ini belum tersedia.";
    [SerializeField] private string insufficientCoinMessage = "Coin tidak cukup!";
    [SerializeField] private bool disableLockedButtons = true;

    [Header("Bounce In Animation")]
    [SerializeField] private RectTransform[] bounceElements;
    [SerializeField] private float bounceDuration = 0.6f;
    [SerializeField] private float bounceStagger = 0.12f;
    [SerializeField] private float bounceOffsetY = 600f;

    [Header("Level Button Labels")]
    [SerializeField] private string starterLabel = "STARTER";
    [SerializeField] private string beginnerLabel = "BEGINNER";
    [SerializeField] private string intermediateLabel = "INTERMEDIATE";
    [SerializeField] private string lockedLabelFormat = "{0}\n({1} coin)";

    [Header("Level Button Style")]
    [SerializeField] private Color unlockedColor = new Color(0.95f, 0.72f, 0.22f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color labelUnlockedColor = new Color(0.12f, 0.15f, 0.08f, 1f);
    [SerializeField] private float labelFontSize = 24f;

    private bool listenersRegistered;
    private bool hasBounced;

    private void Start()
    {
        if (!hasBounced && bounceElements != null && bounceElements.Length > 0)
        {
            hasBounced = true;
            StartCoroutine(PlayBounceIn());
        }
    }

    private void OnEnable()
    {
        RegisterButtonListeners();
        RefreshButtonStates();
        ClearMessage();
    }

    private static float BounceOut(float t)
    {
        if (t < 1f / 2.75f)
            return 7.5625f * t * t;
        else if (t < 2f / 2.75f)
            return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
        else if (t < 2.5f / 2.75f)
            return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
        else
            return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
    }

    private System.Collections.IEnumerator PlayBounceIn()
    {
        Vector2[] targets = new Vector2[bounceElements.Length];
        for (int i = 0; i < bounceElements.Length; i++)
        {
            if (bounceElements[i] == null) continue;
            targets[i] = bounceElements[i].anchoredPosition;
            bounceElements[i].anchoredPosition = new Vector2(targets[i].x, targets[i].y + bounceOffsetY);
        }

        for (int i = 0; i < bounceElements.Length; i++)
        {
            if (bounceElements[i] == null) continue;
            int index = i;
            StartCoroutine(AnimateSingleBounce(bounceElements[index], targets[index]));
            yield return new WaitForSeconds(bounceStagger);
        }
    }

    private System.Collections.IEnumerator AnimateSingleBounce(RectTransform rt, Vector2 target)
    {
        Vector2 start = rt.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / bounceDuration);
            rt.anchoredPosition = Vector2.Lerp(start, target, BounceOut(t));
            yield return null;
        }
        rt.anchoredPosition = target;
    }

    private void RegisterButtonListeners()
    {
        if (listenersRegistered)
            return;

        ButtonHelper.AddListenerOnce(starterButton, () => { PlayClickSfx(); PlayStarter(); });
        ButtonHelper.AddListenerOnce(beginnerButton, () => { PlayClickSfx(); PlayBeginner(); });
        ButtonHelper.AddListenerOnce(intermediateButton, () => { PlayClickSfx(); PlayIntermediate(); });
        ButtonHelper.AddListenerOnce(backButton, () => { PlayClickSfx(); GoToMainMenu(); });
        listenersRegistered = true;
    }

    private void RefreshButtonStates()
    {
        if (!disableLockedButtons || beginnerButton == null || intermediateButton == null)
            return;

        bool beginnerUnlocked = IsBeginnerUnlocked();
        bool intermediateUnlocked = IsIntermediateUnlocked();
        int reserve = GameConstants.Economy.ChickenPrice + GameConstants.Economy.FeedCost;
        bool beginnerCanAfford = CoinManager.Instance != null
            && CoinManager.Instance.GetTotalCoin() >= GameConstants.LevelUnlock.BeginnerCost + reserve;
        bool intermediateCanAfford = CoinManager.Instance != null
            && CoinManager.Instance.GetTotalCoin() >= GameConstants.LevelUnlock.IntermediateCost + reserve;

        beginnerButton.interactable = beginnerUnlocked || beginnerCanAfford;
        intermediateButton.interactable = intermediateUnlocked || intermediateCanAfford;

        UpdateButtonLabel(starterButton, true, 0, starterLabel);
        UpdateButtonLabel(beginnerButton, beginnerUnlocked, GameConstants.LevelUnlock.BeginnerCost, beginnerLabel);
        UpdateButtonLabel(intermediateButton, intermediateUnlocked, GameConstants.LevelUnlock.IntermediateCost, intermediateLabel);

        StyleLevelButton(starterButton, true);
        StyleLevelButton(beginnerButton, beginnerUnlocked);
        StyleLevelButton(intermediateButton, intermediateUnlocked);
    }

    private void StyleLevelButton(Button button, bool unlocked)
    {
        if (button == null) return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = unlocked ? unlockedColor : lockedColor;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.color = unlocked ? labelUnlockedColor : Color.gray;
            label.fontStyle = FontStyles.Bold;
            if (unlocked)
                label.fontSize = Mathf.Max(label.fontSize, labelFontSize);
        }
    }

    private void UpdateButtonLabel(Button button, bool unlocked, int cost, string name)
    {
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label == null)
            return;

        if (unlocked)
        {
            label.text = name;
        }
        else
        {
            label.text = string.Format(lockedLabelFormat, name, cost);
            label.fontSize = Mathf.Max(label.fontSize - 4, 16);
        }
    }

    public void PlayStarter()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.GoToLevel(0);
            return;
        }

        if (!string.IsNullOrWhiteSpace(starterSceneName))
        {
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene(starterSceneName);
            else
                SceneManager.LoadScene(starterSceneName);
        }
    }

    public void PlayBeginner()
    {
        if (IsBeginnerUnlocked())
        {
            LoadLevelScene(1);
            return;
        }

        TryUnlockLevel(GameConstants.LevelUnlock.BeginnerCost, GameConstants.Persistence.LevelUnlockBeginnerKey, "Beginner", () => {
            LoadLevelScene(1);
        });
    }

    public void PlayIntermediate()
    {
        if (IsIntermediateUnlocked())
        {
            LoadLevelScene(2);
            return;
        }

        TryUnlockLevel(GameConstants.LevelUnlock.IntermediateCost, GameConstants.Persistence.LevelUnlockIntermediateKey, "Intermediate", () => {
            LoadLevelScene(2);
        });
    }

    private void LoadLevelScene(int levelIndex)
    {
        string[] sceneNames = GameManager.Instance != null
            ? GameManager.Instance.sceneNames
            : new string[] { "Starter", "Beginner", "Intermediate" };

        if (levelIndex < 0 || levelIndex >= sceneNames.Length)
            return;

        string sceneName = sceneNames[levelIndex];
        if (SceneController.Instance != null)
            SceneController.Instance.GoToLevel(levelIndex);
        else if (SceneTransition.Instance != null)
            SceneTransition.Instance.LoadScene(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    public void ShowLockedMessage(string levelName)
    {
        if (messageText != null)
            messageText.text = string.IsNullOrWhiteSpace(levelName) ? lockedMessage : $"{levelName}: {lockedMessage}";

        GameLog.Info($"{levelName} belum bisa dimainkan.");
    }

    private void TryUnlockLevel(int cost, string playerPrefsKey, string levelName, System.Action onSuccess)
    {
        if (CoinManager.Instance == null)
        {
            ShowLockedMessage(levelName);
            if (SFXManager.Instance != null) SFXManager.Instance.PlayUnlockFail();
            return;
        }

        Button targetButton = levelName == "Beginner" ? beginnerButton : intermediateButton;
        Vector2 buttonPos = GetButtonScreenPos(targetButton);

        int reserve = GameConstants.Economy.ChickenPrice + GameConstants.Economy.FeedCost;
        int coinsAfter = CoinManager.Instance.GetTotalCoin() - cost;
        if (coinsAfter < reserve)
        {
            FloatingFeedback.ShowText("Modal tidak cukup! Sisakan minimal " + reserve + " coin.", buttonPos, new Color(1f, 0.7f, 0.2f));
            GameLog.Info($"Coin tidak cukup: butuh {cost + reserve} coin (harga {cost} + cadangan {reserve}).");
            if (SFXManager.Instance != null) SFXManager.Instance.PlayUnlockFail();
            return;
        }

        if (CoinManager.Instance.SpendCoin(cost))
        {
            if (!DemoModeConfig.IsDemoMode)
            {
                PlayerPrefs.SetInt(playerPrefsKey, 1);
                PlayerPrefs.Save();
            }

            GameLog.Info($"{levelName} berhasil dibuka! -{cost} coin.");
            if (SFXManager.Instance != null) SFXManager.Instance.PlayUnlockSuccess();
            RefreshButtonStates();
            onSuccess?.Invoke();
        }
        else
        {
            FloatingFeedback.ShowText(insufficientCoinMessage, buttonPos, new Color(1f, 0.3f, 0.3f));
            GameLog.Info($"Coin tidak cukup untuk membuka {levelName}.");
            if (SFXManager.Instance != null) SFXManager.Instance.PlayUnlockFail();
        }
    }

    private static bool IsBeginnerUnlocked()
    {
        if (DemoModeConfig.IsDemoMode)
            return false;

        return PlayerPrefs.GetInt(GameConstants.Persistence.LevelUnlockBeginnerKey, 0) == 1;
    }

    private static bool IsIntermediateUnlocked()
    {
        if (DemoModeConfig.IsDemoMode)
            return false;

        return PlayerPrefs.GetInt(GameConstants.Persistence.LevelUnlockIntermediateKey, 0) == 1;
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = string.Empty;
    }

    private static Vector2 GetButtonScreenPos(Button button)
    {
        if (button != null)
            return RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void PlayClickSfx()
    {
        if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
    }

    private void GoToMainMenu()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.GoToMainMenu();
    }
}
