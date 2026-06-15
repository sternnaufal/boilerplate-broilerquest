using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PanelStyleConfig
{
    public Color color = new Color(0.10f, 0.22f, 0.14f, 0.88f);
}

[System.Serializable]
public class ButtonStyleConfig
{
    public string label = "";
    public Color color = new Color(0.95f, 0.72f, 0.22f, 1f);
}

public class StarterGameplayUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject hpPanel;

    [Header("Panel Styles")]
    [SerializeField] private PanelStyleConfig hpPanelStyle = new PanelStyleConfig { color = new Color(0.10f, 0.22f, 0.14f, 0.88f) };
    [SerializeField] private PanelStyleConfig pausePanelStyle = new PanelStyleConfig { color = new Color(0.05f, 0.11f, 0.07f, 0.90f) };

    [Header("HP Panel Position")]
    [SerializeField] private Vector2 hpPanelAnchorMin = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 hpPanelAnchorMax = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 hpPanelPivot = new Vector2(1f, 0.5f);
    [SerializeField] private Vector2 hpPanelSizeDelta = new Vector2(620f, 580f);
    [SerializeField] private Vector2 hpPanelAnchoredPosition = new Vector2(-42f, -18f);

    [Header("Buttons")]
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button hpToggleButton;
    [SerializeField] private Button closeHpButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Button Styles")]
    [SerializeField] private ButtonStyleConfig pauseButtonStyle = new ButtonStyleConfig { label = "PAUSE" };
    [SerializeField] private ButtonStyleConfig resumeButtonStyle = new ButtonStyleConfig { label = "RESUME" };
    [SerializeField] private ButtonStyleConfig hpToggleButtonStyle = new ButtonStyleConfig { label = "HP" };
    [SerializeField] private ButtonStyleConfig closeHpButtonStyle = new ButtonStyleConfig { label = "TUTUP" };
    [SerializeField] private ButtonStyleConfig mainMenuButtonStyle = new ButtonStyleConfig { label = "MAIN MENU", color = new Color(0.85f, 0.35f, 0.35f, 1f) };

    [Header("Button Sprites")]
    [SerializeField] private Sprite[] buttonSprites;

    [Header("Starter References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private StarterChickenShop chickenShop;

    [Header("Coin Text Style")]
    [SerializeField] private Color coinTextColor = new Color(1f, 0.96f, 0.70f, 1f);

    [Header("IoT")]
    [Header("Startup")]
    [SerializeField] private bool showBuyPanelOnStart = true;
    [SerializeField] private bool resetFeedOnStart = false;
    [SerializeField] private int startingFeedCount = 0;
    
    [Header("HP Panel Navigation")]
    [SerializeField] private Button shopButton;        // tombol "Shop" di halaman utama HP
    [SerializeField] private Button iotButton;         // tombol "IoT" di halaman utama HP
    [SerializeField] private GameObject shopAPK;       // panel ShopAPK
    [SerializeField] private GameObject iotAPK;        // panel IoTAPK
    [SerializeField] private Button exitShopButton;    // tombol ExitBut di ShopAPK
    
    [SerializeField] private Button exitIoTButton;     // tombol ExitBut di IoTAPK
    [Header("HP Panel Animation")]
    [SerializeField] private RectTransform hpPanelRect;      // Drag BQ_HPPanel ke sini
    [SerializeField] private float animationDuration = 0.3f; // lama animasi
    private bool listenersRegistered;
    private bool hpVisible;
    private bool hpVisibleBeforePause;
    private bool iotCreated;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private Coroutine hpAnimationCoroutine;
    private void OnEnable()
    {
        RegisterButtonListeners();
    }

    private void Start()
    {
        PolishStarterUi();

        if (CoinManager.Instance != null && coinText != null)
            CoinManager.Instance.Initialize();

        if (resetFeedOnStart && FeedManager.Instance != null)
        {
            // Jangan reset progress kalau user sudah pernah main (sudah ada saved feed).
            if (!PlayerPrefs.HasKey(GameConstants.Persistence.FeedCountKey))
                FeedManager.Instance.SetFeedCount(startingFeedCount);
        }

        if (hpPanelRect != null)
        {
            Canvas.ForceUpdateCanvases();
            visiblePosition = hpPanelRect.anchoredPosition;
            hiddenPosition = new Vector2(visiblePosition.x, visiblePosition.y - 800f);
            hpPanelRect.anchoredPosition = hiddenPosition;
            hpPanelRect.gameObject.SetActive(true);
        }

        SetupHPNavigation();

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
        ButtonHelper.AddListenerOnce(exitButton, ReturnToMainMenu);

        listenersRegistered = true;
    }

    public void PauseGame()
    {
        GameStateManager.ApplyState(GameState.Paused);

        if (hudPanel != null)
            hudPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(true);

        hpVisibleBeforePause = hpVisible;
        if (hpVisible)
            CloseHpPanel();
    }

    public void ResumeGame()
    {
        GameStateManager.ApplyState(GameState.Playing);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (hudPanel != null)
            hudPanel.SetActive(true);

        if (hpVisibleBeforePause)
            ShowHpPanel(true);
    }

    public void ToggleHpPanel()
    {
        ShowHpPanel(!hpVisible);
    }

    public void CloseHpPanel()
    {
        ShowHpPanel(false);
    }

    private System.Collections.IEnumerator AnimateHPPanel(Vector2 target)
    {
        Vector2 start = hpPanelRect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            hpPanelRect.anchoredPosition = Vector2.Lerp(start, target, t);
            yield return null;
        }
        hpPanelRect.anchoredPosition = target;
        hpAnimationCoroutine = null;
    }

    public void ShowHpPanel(bool visible)
    {
        hpVisible = visible;

        if (visible)
        {
            ShowMainHPPage(); // untuk mereset ke halaman utama saat panel muncul
        }

        if (hpPanelRect != null)
        {
            if (hpAnimationCoroutine != null) StopCoroutine(hpAnimationCoroutine);
            Vector2 target = visible ? visiblePosition : hiddenPosition;
            hpAnimationCoroutine = StartCoroutine(AnimateHPPanel(target));
        }

        if (chickenShop != null)
            chickenShop.RefreshShopState();
    }

    public void ReturnToMainMenu()
    {
        UIAlertPanel.Instance?.Show(UIAlertPanel.NotificationType.MainMenuConfirm, () =>
        {
            SaveManager.SaveAll();
            GameStateManager.ApplyState(GameState.Menu);

            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMainMenu();
        });
    }

    private void PolishStarterUi()
    {
        StyleButton(pauseButton, pauseButtonStyle.label, pauseButtonStyle.color, GetSpriteSafe(0));
        StyleButton(resumeButton, resumeButtonStyle.label, resumeButtonStyle.color, GetSpriteSafe(1));
        StyleButton(hpToggleButton, hpToggleButtonStyle.label, hpToggleButtonStyle.color, GetSpriteSafe(2));
        StyleButton(closeHpButton, closeHpButtonStyle.label, closeHpButtonStyle.color, GetSpriteSafe(3));

        if (mainMenuButton != null)
            mainMenuButton.gameObject.SetActive(false);

        //StylePanel(hpPanel, hpPanelStyle.color);
        StylePanel(pausePanel, pausePanelStyle.color);
        //PositionHpPanel();
        DisableDecorativeRaycasts();

        if (coinText != null)
        {
            coinText.gameObject.SetActive(true);
            coinText.color = coinTextColor;
            coinText.fontSize = Mathf.Max(coinText.fontSize, GameConstants.UI.CoinTextFontSize);
            coinText.fontStyle = FontStyles.Bold;
            coinText.alignment = TextAlignmentOptions.MidlineLeft;
        }
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

    private void DisableDecorativeRaycasts()
    {
        DisablePanelImageRaycast(hpPanel);
        DisableNonButtonChildRaycasts(hpPanel);
        DisableNonButtonChildRaycasts(hudPanel);
        DisableNonButtonChildRaycasts(pausePanel);

        Transform canvasRoot = hudPanel != null ? hudPanel.transform.parent : null;
        if (canvasRoot == null && hpPanel != null)
            canvasRoot = hpPanel.transform.parent;

        Transform background = canvasRoot != null ? canvasRoot.Find("Background") : null;
        if (background != null && background.TryGetComponent(out Image image))
            image.raycastTarget = false;
    }

    private static void DisablePanelImageRaycast(GameObject panel)
    {
        if (panel != null && panel.TryGetComponent(out Image image))
            image.raycastTarget = false;
    }

    private static void DisableNonButtonChildRaycasts(GameObject root)
    {
        if (root == null)
            return;

        foreach (TextMeshProUGUI text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
            text.raycastTarget = false;

        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.GetComponentInParent<Button>(true) == null)
                image.raycastTarget = false;
        }
    }

    private void SetupHPNavigation()
    {
        if (shopButton != null)
            ButtonHelper.AddListenerOnce(shopButton, ShowShopAPK);
        if (iotButton != null)
            ButtonHelper.AddListenerOnce(iotButton, ShowIoTAPK);
        if (exitShopButton != null)
            ButtonHelper.AddListenerOnce(exitShopButton, ShowMainHPPage);
        if (exitIoTButton != null)
            ButtonHelper.AddListenerOnce(exitIoTButton, ShowMainHPPage);
    }

    private void ShowMainHPPage()
    {
        if (shopAPK != null) shopAPK.SetActive(false);
        if (iotAPK != null) iotAPK.SetActive(false);
        // Tampilkan tombol-tombol utama (shopButton & iotButton) – 
        // mereka biasanya berada langsung di dalam BQ_HPPanel, jadi cukup nonaktifkan panel APK.
        // Jika tombol utama ikut tersembunyi, pastikan mereka tetap aktif.
        if (shopButton != null) shopButton.gameObject.SetActive(true);
        if (iotButton != null) iotButton.gameObject.SetActive(true);
        // Opsional: sembunyikan juga panel APK yang mungkin masih terlihat
    }

    private void ShowShopAPK()
    {
        if (shopAPK != null) shopAPK.SetActive(true);
        if (iotAPK != null) iotAPK.SetActive(false);
        // Sembunyikan tombol utama agar tidak terlihat saat di dalam APK
        if (shopButton != null) shopButton.gameObject.SetActive(false);
        if (iotButton != null) iotButton.gameObject.SetActive(false);
    }

    private void ShowIoTAPK()
    {
        if (shopAPK != null) shopAPK.SetActive(false);
        if (iotAPK != null) iotAPK.SetActive(true);
        if (shopButton != null) shopButton.gameObject.SetActive(false);
        if (iotButton != null) iotButton.gameObject.SetActive(false);
    }

}
