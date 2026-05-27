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

        ButtonHelper.AddListenerOnce(starterButton, PlayStarter);
        ButtonHelper.AddListenerOnce(beginnerButton, PlayBeginner);
        ButtonHelper.AddListenerOnce(intermediateButton, PlayIntermediate);
        listenersRegistered = true;
    }

    private void RefreshButtonStates()
    {
        if (!disableLockedButtons || beginnerButton == null || intermediateButton == null)
            return;

        bool beginnerUnlocked = IsBeginnerUnlocked();
        bool intermediateUnlocked = IsIntermediateUnlocked();

        beginnerButton.interactable = beginnerUnlocked;
        intermediateButton.interactable = intermediateUnlocked;

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
            SceneManager.LoadScene(starterSceneName);
    }

    public void PlayBeginner()
    {
        if (IsBeginnerUnlocked())
        {
            if (SceneController.Instance != null)
            {
                SceneController.Instance.GoToLevel(1);
                return;
            }
        }

        TryUnlockLevel(GameConstants.LevelUnlock.BeginnerCost, GameConstants.Persistence.LevelUnlockBeginnerKey, "Beginner", () => {
            if (SceneController.Instance != null)
                SceneController.Instance.GoToLevel(1);
        });
    }

    public void PlayIntermediate()
    {
        if (IsIntermediateUnlocked())
        {
            if (SceneController.Instance != null)
            {
                SceneController.Instance.GoToLevel(2);
                return;
            }
        }

        TryUnlockLevel(GameConstants.LevelUnlock.IntermediateCost, GameConstants.Persistence.LevelUnlockIntermediateKey, "Intermediate", () => {
            if (SceneController.Instance != null)
                SceneController.Instance.GoToLevel(2);
        });
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
            return;
        }

        if (CoinManager.Instance.SpendCoin(cost))
        {
            PlayerPrefs.SetInt(playerPrefsKey, 1);
            PlayerPrefs.Save();
            GameLog.Info($"{levelName} berhasil dibuka! -{cost} coin.");
            RefreshButtonStates();
            onSuccess?.Invoke();
        }
        else
        {
            if (messageText != null)
                messageText.text = $"{levelName}: {insufficientCoinMessage} ({cost} coin)";

            GameLog.Info($"Coin tidak cukup untuk membuka {levelName}.");
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
}
