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

    [Header("Locked Level Feedback")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string lockedMessage = "Level ini belum tersedia.";
    [SerializeField] private string insufficientCoinMessage = "Coin tidak cukup!";
    [SerializeField] private bool disableLockedButtons = true;

    private bool listenersRegistered;

    private void OnEnable()
    {
        RegisterButtonListeners();
        RefreshButtonStates();
        ClearMessage();
    }

    private void RegisterButtonListeners()
    {
        if (listenersRegistered)
            return;

        ButtonHelper.AddListenerOnce(starterButton, () => { PlayClickSfx(); PlayStarter(); });
        ButtonHelper.AddListenerOnce(beginnerButton, () => { PlayClickSfx(); PlayBeginner(); });
        ButtonHelper.AddListenerOnce(intermediateButton, () => { PlayClickSfx(); PlayIntermediate(); });
        listenersRegistered = true;
    }

    private void RefreshButtonStates()
    {
        if (!disableLockedButtons || beginnerButton == null || intermediateButton == null)
            return;

        bool beginnerUnlocked = IsBeginnerUnlocked();
        bool intermediateUnlocked = IsIntermediateUnlocked();
<<<<<<< HEAD

        beginnerButton.interactable = beginnerUnlocked;
        intermediateButton.interactable = intermediateUnlocked;
=======
        bool beginnerCanAfford = CoinManager.Instance != null && CoinManager.Instance.CanAfford(GameConstants.LevelUnlock.BeginnerCost);
        bool intermediateCanAfford = CoinManager.Instance != null && CoinManager.Instance.CanAfford(GameConstants.LevelUnlock.IntermediateCost);

        beginnerButton.interactable = beginnerUnlocked || beginnerCanAfford;
        intermediateButton.interactable = intermediateUnlocked || intermediateCanAfford;
>>>>>>> origin/dev/Hylmi

        UpdateButtonLabel(beginnerButton, beginnerUnlocked, GameConstants.LevelUnlock.BeginnerCost, "Beginner");
        UpdateButtonLabel(intermediateButton, intermediateUnlocked, GameConstants.LevelUnlock.IntermediateCost, "Intermediate");
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
            label.text = $"{name}\n({cost} coin)";
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

        if (CoinManager.Instance.SpendCoin(cost))
        {
            PlayerPrefs.SetInt(playerPrefsKey, 1);
            PlayerPrefs.Save();
            GameLog.Info($"{levelName} berhasil dibuka! -{cost} coin.");
            if (SFXManager.Instance != null) SFXManager.Instance.PlayUnlockSuccess();
            RefreshButtonStates();
            onSuccess?.Invoke();
        }
        else
        {
            if (messageText != null)
                messageText.text = $"{levelName}: {insufficientCoinMessage} ({cost} coin)";

            GameLog.Info($"Coin tidak cukup untuk membuka {levelName}.");
            if (SFXManager.Instance != null) SFXManager.Instance.PlayUnlockFail();
        }
    }

    private static bool IsBeginnerUnlocked()
    {
        return PlayerPrefs.GetInt(GameConstants.Persistence.LevelUnlockBeginnerKey, 0) == 1;
    }

    private static bool IsIntermediateUnlocked()
    {
        return PlayerPrefs.GetInt(GameConstants.Persistence.LevelUnlockIntermediateKey, 0) == 1;
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = string.Empty;
    }

    private void PlayClickSfx()
    {
        if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick();
    }
}
