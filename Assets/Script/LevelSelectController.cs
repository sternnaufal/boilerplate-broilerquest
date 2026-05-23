using TMPro;
using UnityEngine;
using UnityEngine.Events;
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

        RegisterButton(starterButton, PlayStarter);
        RegisterButton(beginnerButton, PlayBeginner);
        RegisterButton(intermediateButton, PlayIntermediate);
        listenersRegistered = true;
    }

    private static void RegisterButton(Button button, UnityAction action)
    {
        if (button == null || button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.AddListener(action);
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
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentLevelIndex = 0;
            GameManager.Instance.SetGameActive(true);
        }

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

        Debug.Log($"{levelName} belum bisa dimainkan.");
    }

    private void ClearMessage()
    {
        if (messageText != null)
            messageText.text = string.Empty;
    }
}
