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
    [SerializeField] private bool disableLockedButtons = true;

    private bool listenersRegistered;

    private void OnEnable()
    {
        RegisterButtonListeners();
        ConfigureLockedButtons();
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

    private void ConfigureLockedButtons()
    {
        if (!disableLockedButtons)
            return;

        if (beginnerButton != null)
            beginnerButton.interactable = false;

        if (intermediateButton != null)
            intermediateButton.interactable = false;
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
        ShowLockedMessage("Beginner");
    }

    public void PlayIntermediate()
    {
        ShowLockedMessage("Intermediate");
    }

    public void ShowLockedMessage(string levelName)
    {
        if (messageText != null)
            messageText.text = string.IsNullOrWhiteSpace(levelName) ? lockedMessage : $"{levelName}: {lockedMessage}";

        GameLog.Info($"{levelName} belum bisa dimainkan.");
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = string.Empty;
    }
}
