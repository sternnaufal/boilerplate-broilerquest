using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class StarterGameplayUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject hpPanel;

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button hpToggleButton;
    [SerializeField] private Button closeHpButton;

    [Header("Starter References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private StarterChickenShop chickenShop;

    private bool listenersRegistered;
    private bool hpVisible;

    private void OnEnable()
    {
        RegisterButtonListeners();
    }

    private void Start()
    {
        if (CoinManager.Instance != null && coinText != null)
            CoinManager.Instance.BindCoinText(coinText);

        ResumeGame();
        ShowHpPanel(false);
    }

    private void RegisterButtonListeners()
    {
        if (listenersRegistered)
            return;

        RegisterButton(pauseButton, PauseGame);
        RegisterButton(resumeButton, ResumeGame);
        RegisterButton(hpToggleButton, ToggleHpPanel);
        RegisterButton(closeHpButton, CloseHpPanel);

        listenersRegistered = true;
    }

    private static void RegisterButton(Button button, UnityAction action)
    {
        if (button == null || button.onClick.GetPersistentEventCount() > 0)
            return;

        button.onClick.AddListener(action);
    }

    public void PauseGame()
    {
        Time.timeScale = 0f;

        if (hudPanel != null)
            hudPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameActive(true);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (hudPanel != null)
            hudPanel.SetActive(true);
    }

    public void ToggleHpPanel()
    {
        ShowHpPanel(!hpVisible);
    }

    public void CloseHpPanel()
    {
        ShowHpPanel(false);
    }

    public void ShowHpPanel(bool visible)
    {
        hpVisible = visible;

        if (hpPanel != null)
            hpPanel.SetActive(visible);

        if (chickenShop != null)
            chickenShop.RefreshShopState();
    }
}
