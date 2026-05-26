using TMPro;
using UnityEngine;
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
    [SerializeField] private Button mainMenuButton;

    [Header("Button Sprites")]
    [SerializeField] private Sprite[] buttonSprites;

    [Header("Starter References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private StarterChickenShop chickenShop;

    [Header("IoT")]
    [SerializeField] private StarterIoTController iotController;

    [Header("Startup")]
    [SerializeField] private bool showBuyPanelOnStart = true;

    private bool listenersRegistered;
    private bool hpVisible;
    private bool iotCreated;

    private void OnEnable()
    {
        RegisterButtonListeners();
    }

    private void Start()
    {
        PolishStarterUi();

        if (CoinManager.Instance != null && coinText != null)
            CoinManager.Instance.Initialize(coinText);

        ResumeGame();
        ShowHpPanel(showBuyPanelOnStart);
    }

    private void RegisterButtonListeners()
    {
        if (listenersRegistered)
            return;

        ButtonHelper.AddListenerOnce(pauseButton, PauseGame);
        ButtonHelper.AddListenerOnce(resumeButton, ResumeGame);
        ButtonHelper.AddListenerOnce(hpToggleButton, ToggleHpPanel);
        ButtonHelper.AddListenerOnce(closeHpButton, CloseHpPanel);
        ButtonHelper.AddListenerOnce(mainMenuButton, ReturnToMainMenu);

        listenersRegistered = true;
    }

    public void PauseGame()
    {
        SetGameStateOrFallback(GameState.Paused);

        if (hudPanel != null)
            hudPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        SetGameStateOrFallback(GameState.Playing);

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
        {
            hpPanel.SetActive(visible);
            if (visible)
                EnsureIotController();
        }

        if (chickenShop != null)
            chickenShop.RefreshShopState();
    }

    private void EnsureIotController()
    {
        if (iotCreated)
            return;

        if (iotController == null)
            iotController = FindFirstObjectByType<StarterIoTController>();

        if (iotController == null && hpPanel != null)
        {
            GameObject iotObj = new GameObject("StarterIoTController", typeof(StarterIoTController));
            iotObj.transform.SetParent(hpPanel.transform, false);

            RectTransform rect = iotObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -360f);
            rect.sizeDelta = new Vector2(0f, 180f);

            iotController = iotObj.GetComponent<StarterIoTController>();

            StarterIoTController.IoTDeviceDef[] defs = new StarterIoTController.IoTDeviceDef[]
            {
                new StarterIoTController.IoTDeviceDef
                {
                    productKey = GameConstants.IoT.ProductKeyFeeder,
                    displayName = GameConstants.IoT.ProductNameFeeder
                },
                new StarterIoTController.IoTDeviceDef
                {
                    productKey = GameConstants.IoT.ProductKeyFan,
                    displayName = GameConstants.IoT.ProductNameFan
                },
                new StarterIoTController.IoTDeviceDef
                {
                    productKey = GameConstants.IoT.ProductKeyHeater,
                    displayName = GameConstants.IoT.ProductNameHeater
                }
            };
            iotController.devices = defs;
        }

        iotCreated = true;
    }

    public void ReturnToMainMenu()
    {
        SetGameStateOrFallback(GameState.Menu);

        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMainMenu();
    }

    private void PolishStarterUi()
    {
        StyleButton(pauseButton, "PAUSE", new Color(0.95f, 0.72f, 0.22f, 1f), GetSpriteSafe(0));
        StyleButton(resumeButton, "RESUME", new Color(0.95f, 0.72f, 0.22f, 1f), GetSpriteSafe(1));
        StyleButton(hpToggleButton, "HP", new Color(0.95f, 0.72f, 0.22f, 1f), GetSpriteSafe(2));
        StyleButton(closeHpButton, "TUTUP", new Color(0.95f, 0.72f, 0.22f, 1f), GetSpriteSafe(3));
        EnsureMainMenuButton();
        StyleButton(mainMenuButton, "MAIN MENU", new Color(0.85f, 0.35f, 0.35f, 1f), null);

        StylePanel(hpPanel, new Color(0.10f, 0.22f, 0.14f, 0.88f));
        StylePanel(pausePanel, new Color(0.05f, 0.11f, 0.07f, 0.90f));
        PositionHpPanel();

        if (coinText != null)
        {
            coinText.gameObject.SetActive(true);
            coinText.color = new Color(1f, 0.96f, 0.70f, 1f);
            coinText.fontSize = Mathf.Max(coinText.fontSize, GameConstants.UI.CoinTextFontSize);
            coinText.fontStyle = FontStyles.Bold;
            coinText.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    private void EnsureMainMenuButton()
    {
        if (mainMenuButton != null)
            return;

        if (pausePanel == null)
            return;

        GameObject btnObj = new GameObject("MainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(pausePanel.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 80f);
        btnRect.sizeDelta = new Vector2(220f, 52f);

        mainMenuButton = btnObj.GetComponent<Button>();
        ButtonHelper.AddListenerOnce(mainMenuButton, ReturnToMainMenu);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(btnObj.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private Sprite GetSpriteSafe(int index)
    {
        return buttonSprites != null && index < buttonSprites.Length ? buttonSprites[index] : null;
    }

    private static void StyleButton(Button button, string label, Color color, Sprite sprite = null)
    {
        if (button == null)
            return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            if (sprite != null)
            {
                buttonImage.sprite = sprite;
                buttonImage.color = Color.white;
            }
            else
            {
                buttonImage.color = color;
            }
        }

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText == null)
            labelText = CreateButtonLabel(button.transform);

        labelText.gameObject.SetActive(true);
        labelText.text = label;
        labelText.color = new Color(0.12f, 0.15f, 0.08f, 1f);
        labelText.fontSize = Mathf.Max(labelText.fontSize, GameConstants.UI.ButtonLabelFontSize);
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

    private void PositionHpPanel()
    {
        if (hpPanel == null)
            return;

        RectTransform rect = hpPanel.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(620f, 580f);
        rect.anchoredPosition = new Vector2(-42f, -18f);
    }

    private static void SetGameStateOrFallback(GameState state)
    {
        if (GameStateManager.TrySetGameState(state))
            return;

        Time.timeScale = state == GameState.Paused ? 0f : 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.SetGameActive(state == GameState.Playing);
    }
}
