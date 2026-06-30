using System.Collections;
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
    [SerializeField] private Vector2 hpPanelAnchorMin = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 hpPanelAnchorMax = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 hpPanelPivot = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 hpPanelSizeDelta = new Vector2(556f, 960f);
    [SerializeField] private Vector2 hpPanelAnchoredPosition = new Vector2(513f, -86.884f);

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
    private RectTransform hpPanelRect;
    [SerializeField] private float animationDuration = 0.3f; // lama animasi
    private bool listenersRegistered;
    private bool hpVisible;
    private bool hpVisibleBeforePause;
    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;
    private Coroutine hpAnimationCoroutine;

    private void SetupReferences()
    {
        if (hpPanel != null && hpPanelRect == null)
            hpPanelRect = hpPanel.GetComponent<RectTransform>();
    }

    private void SetupHpPanelPosition()
    {
        SetupReferences();
        if (hpPanelRect == null) return;

        hpPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        hpPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        hpPanelRect.pivot = new Vector2(0.5f, 0.5f);
        hpPanelRect.sizeDelta = new Vector2(556f, 960f);
        hpPanelRect.anchoredPosition = new Vector2(513f, -86.884f);

        Canvas.ForceUpdateCanvases();

        visiblePosition = new Vector2(513f, -86.884f);

        Transform parent = hpPanelRect.parent;
        float parentHeight = Screen.height;
        if (parent is RectTransform parentRt && parentRt.rect.height > 0f)
            parentHeight = parentRt.rect.height;

        float panelHeight = hpPanelRect.sizeDelta.y > 0f
            ? hpPanelRect.sizeDelta.y
            : hpPanelRect.rect.height;

        hiddenPosition = new Vector2(
            visiblePosition.x,
            -parentHeight * hpPanelAnchorMin.y
            - panelHeight * (1f - hpPanelPivot.y)
            - GameConstants.UI.HPPanelSafetyMargin
        );

        hpPanelRect.anchoredPosition = hiddenPosition;
    }

    private void Awake()
    {
        SetupReferences();
    }

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

        SetupHpPanelPosition();

        SetupHPNavigation();

        ResumeGame();
        ShowHpPanel(showBuyPanelOnStart);
    }

    private void RegisterButtonListeners()
    {
        if (listenersRegistered)
            return;

        ButtonHelper.AddListenerOnce(pauseButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); PauseGame(); });
        ButtonHelper.AddListenerOnce(resumeButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); ResumeGame(); });
        ButtonHelper.AddListenerOnce(hpToggleButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); ToggleHpPanel(); });
        ButtonHelper.AddListenerOnce(closeHpButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); CloseHpPanel(); });
        ButtonHelper.AddListenerOnce(exitButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); ReturnToMainMenu(); });

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

    private IEnumerator AnimateHPPanel(Vector2 target)
    {
        if (hpPanelRect == null) yield break;
        Vector2 start = hpPanelRect.anchoredPosition;
        float elapsed = 0f;

        bool isShowing = target == visiblePosition;
        Vector3 originalScale = hpPanelRect.localScale;

        if (isShowing)
            hpPanelRect.localScale = originalScale * 0.85f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            float eased = EaseOutBack(t);
            hpPanelRect.anchoredPosition = Vector2.LerpUnclamped(start, target, eased);

            if (isShowing)
            {
                float scaleT = Mathf.Clamp01(elapsed / (animationDuration * 0.6f));
                hpPanelRect.localScale = Vector3.Lerp(originalScale * 0.85f, originalScale * 1.04f, scaleT);
            }

            yield return null;
        }

        hpPanelRect.anchoredPosition = target;
        if (isShowing)
        {
            hpPanelRect.localScale = originalScale;
            StartCoroutine(PunchScale(hpPanelRect, 1.04f, 0.15f));
            StartCoroutine(StaggerChildren());
        }

        hpAnimationCoroutine = null;
    }

    private IEnumerator PunchScale(RectTransform target, float punch, float duration)
    {
        Vector3 baseScale = target.localScale;
        float half = duration * 0.5f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(baseScale, baseScale * punch, t);
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(baseScale * punch, baseScale, t);
            yield return null;
        }
        target.localScale = baseScale;
    }

    private IEnumerator StaggerChildren()
    {
        if (hpPanel == null) yield break;
        int count = hpPanel.transform.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = hpPanel.transform.GetChild(i);
            StartCoroutine(ScaleBounceChild(child, 0.25f));
            yield return new WaitForSecondsRealtime(0.04f);
        }
    }

    private IEnumerator ScaleBounceChild(Transform target, float totalDuration)
    {
        Vector3 original = target.localScale;
        target.localScale = Vector3.zero;

        float half = totalDuration * 0.5f;
        float elapsed = 0f;

        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(Vector3.zero, original * 1.08f, t * t * (3f - 2f * t));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(original * 1.08f, original, t * t * (3f - 2f * t));
            yield return null;
        }

        target.localScale = original;
    }

    private static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    public void ShowHpPanel(bool visible)
    {
        hpVisible = visible;

        if (visible)
            ShowMainHPPage();

        if (hpAnimationCoroutine != null)
            StopCoroutine(hpAnimationCoroutine);

        Vector2 target = visible ? visiblePosition : hiddenPosition;
        hpAnimationCoroutine = StartCoroutine(AnimateHPPanel(target));

        if (chickenShop != null)
            chickenShop.RefreshShopState();
    }

    public void ReturnToMainMenu()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        var alert = UIAlertPanel.Instance;
        if (alert == null)
            alert = FindFirstObjectByType<UIAlertPanel>();

        if (alert == null)
        {
            Debug.LogWarning("UIAlertPanel not found in scene! Can't show MainMenu confirmation.");
            return;
        }

        alert.Show(UIAlertPanel.NotificationType.MainMenuConfirm,
            onConfirm: () =>
            {
                SaveManager.SaveAll();
                GameStateManager.ApplyState(GameState.Menu);

                if (GameManager.Instance != null)
                    GameManager.Instance.ReturnToMainMenu();
            },
            onBack: () =>
            {
                if (pausePanel != null)
                    pausePanel.SetActive(true);
            });
    }

    private void PolishStarterUi()
    {
        StyleButton(pauseButton, pauseButtonStyle.label, pauseButtonStyle.color, GetSpriteSafe(0));
        StyleButton(resumeButton, resumeButtonStyle.label, resumeButtonStyle.color, GetSpriteSafe(1));
        //StyleButton(hpToggleButton, hpToggleButtonStyle.label, hpToggleButtonStyle.color, GetSpriteSafe(2));
        StyleButton(closeHpButton, closeHpButtonStyle.label, closeHpButtonStyle.color, GetSpriteSafe(3));

        if (mainMenuButton != null)
            ButtonHelper.AddListenerOnce(mainMenuButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); ReturnToMainMenu(); });

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
            ButtonHelper.AddListenerOnce(shopButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); ShowShopAPK(); });
        if (iotButton != null)
            ButtonHelper.AddListenerOnce(iotButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); ShowIoTAPK(); });
        if (exitShopButton != null)
            ButtonHelper.AddListenerOnce(exitShopButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); ShowMainHPPage(); });
        if (exitIoTButton != null)
            ButtonHelper.AddListenerOnce(exitIoTButton, () => { if (SFXManager.Instance != null) SFXManager.Instance.PlayButtonClick(); ShowMainHPPage(); });

        ConfigureButtonHover(shopButton);
        ConfigureButtonHover(iotButton);
    }

    private void ConfigureButtonHover(Button button)
    {
        if (button == null) return;
        ColorBlock cb = button.colors;
        cb.highlightedColor = new Color(1f, 0.85f, 0.4f);
        cb.pressedColor = new Color(0.8f, 0.65f, 0.2f);
        button.colors = cb;
    }

    private void ShowMainHPPage()
    {
        StartCoroutine(AnimateSubPageOut(shopAPK));
        StartCoroutine(AnimateSubPageOut(iotAPK));
        if (shopButton != null) shopButton.gameObject.SetActive(true);
        if (iotButton != null) iotButton.gameObject.SetActive(true);
    }

    private void ShowShopAPK()
    {
        if (iotAPK != null && iotAPK.activeSelf)
            StartCoroutine(AnimateSubPageOut(iotAPK));
        if (shopButton != null) shopButton.gameObject.SetActive(false);
        if (iotButton != null) iotButton.gameObject.SetActive(false);
        StartCoroutine(AnimateSubPageIn(shopAPK));
    }

    private void ShowIoTAPK()
    {
        if (shopAPK != null && shopAPK.activeSelf)
            StartCoroutine(AnimateSubPageOut(shopAPK));
        if (shopButton != null) shopButton.gameObject.SetActive(false);
        if (iotButton != null) iotButton.gameObject.SetActive(false);
        StartCoroutine(AnimateSubPageIn(iotAPK));
    }

    private IEnumerator AnimateSubPageIn(GameObject page)
    {
        if (page == null) yield break;
        page.SetActive(true);
        RectTransform rt = page.GetComponent<RectTransform>();
        if (rt == null) yield break;

        rt.localScale = Vector3.zero;
        float elapsed = 0f;
        float duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rt.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t * t * (3f - 2f * t));
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private IEnumerator AnimateSubPageOut(GameObject page)
    {
        if (page == null || !page.activeSelf) yield break;
        RectTransform rt = page.GetComponent<RectTransform>();
        if (rt == null)
        {
            page.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        float duration = 0.12f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t * t);
            yield return null;
        }
        rt.localScale = Vector3.zero;
        page.SetActive(false);
    }

}
