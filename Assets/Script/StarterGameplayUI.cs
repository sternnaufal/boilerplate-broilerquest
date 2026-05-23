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

    [Header("Startup")]
    [SerializeField] private bool showBuyPanelOnStart = true;

    private bool listenersRegistered;
    private bool hpVisible;

    private void OnEnable()
    {
        RegisterButtonListeners();
    }

    private void Start()
    {
        PolishStarterUi();

        if (CoinManager.Instance != null && coinText != null)
            CoinManager.Instance.BindCoinText(coinText);

        ResumeGame();
        ShowHpPanel(showBuyPanelOnStart);
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

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameActive(false);

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

    private void PolishStarterUi()
    {
        StyleButton(pauseButton, "PAUSE", new Color(0.95f, 0.72f, 0.22f, 1f));
        StyleButton(resumeButton, "RESUME", new Color(0.95f, 0.72f, 0.22f, 1f));
        StyleButton(hpToggleButton, "HP", new Color(0.95f, 0.72f, 0.22f, 1f));
        StyleButton(closeHpButton, "TUTUP", new Color(0.95f, 0.72f, 0.22f, 1f));

        StylePanel(hpPanel, new Color(0.10f, 0.22f, 0.14f, 0.96f));
        StylePanel(pausePanel, new Color(0.05f, 0.11f, 0.07f, 0.90f));

        if (coinText != null)
        {
            coinText.color = new Color(1f, 0.96f, 0.70f, 1f);
            coinText.fontSize = Mathf.Max(coinText.fontSize, 34f);
            coinText.fontStyle = FontStyles.Bold;
            coinText.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    private static void StyleButton(Button button, string label, Color color)
    {
        if (button == null)
            return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
            buttonImage.color = color;

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText == null)
            labelText = CreateButtonLabel(button.transform);

        labelText.gameObject.SetActive(true);
        labelText.text = label;
        labelText.color = new Color(0.12f, 0.15f, 0.08f, 1f);
        labelText.fontSize = Mathf.Max(labelText.fontSize, 24f);
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.raycastTarget = false;
    }

    private static TextMeshProUGUI CreateButtonLabel(Transform parent)
    {
        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 6f);
        rect.offsetMax = new Vector2(-8f, -6f);

        return labelObject.GetComponent<TextMeshProUGUI>();
    }

    private static void StylePanel(GameObject panel, Color color)
    {
        if (panel == null)
            return;

        Image image = panel.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }
}
