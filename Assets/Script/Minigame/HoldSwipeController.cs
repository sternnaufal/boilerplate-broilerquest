using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoldSwipeController : Singleton<HoldSwipeController>, IHealthCheckListener
{
    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image feedPileImage;
    [SerializeField] private Image swipeProgressBar;
    [SerializeField] private TextMeshProUGUI remainingLabel;

    [Header("Settings")]
    [SerializeField] private float timeLimit = GameConstants.HoldSwipe.TimeLimit;
    [SerializeField] private int swipeCount = GameConstants.HoldSwipe.SwipeCount;
    [SerializeField] private float holdDuration = 0.8f;

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.25f, 0.15f);

    private IHealthCheckListener currentListener;
    private int remainingSwipes;
    private float timeRemaining;
    private bool isPlaying;
    private Coroutine timerCoroutine;
    private Coroutine holdCoroutine;
    private float holdProgress;

    public bool IsPlaying => isPlaying;

    protected override void Awake()
    {
        base.Awake();
        EnsureRuntimeUi();
        HidePopup();
    }

    public bool ShowHoldSwipe(IHealthCheckListener caller)
    {
        if (IsPlaying) return false;

        currentListener = caller;
        remainingSwipes = swipeCount;
        timeRemaining = timeLimit;
        isPlaying = true;
        holdProgress = 0f;

        ShowPopup();
        UpdateRemainingUI();
        UpdateTimerUI();
        UpdateProgressBar();

        if (titleText != null)
            titleText.text = "Kurangi Pakan";

        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());

        return true;
    }

    public void OnSwipeHoldStart()
    {
        if (!isPlaying) return;
        CoroutineHelper.StopSafe(this, ref holdCoroutine);
        holdCoroutine = StartCoroutine(HoldRoutine());
    }

    public void OnSwipeHoldCancel()
    {
        CoroutineHelper.StopSafe(this, ref holdCoroutine);
        holdProgress = 0f;
        UpdateProgressBar();
    }

    public void OnSwipeComplete()
    {
        if (!isPlaying) return;

        holdProgress = 0f;
        remainingSwipes--;
        UpdateRemainingUI();
        UpdateProgressBar();
        UpdateFeedPileVisual();

        if (remainingSwipes <= 0)
            CompleteWithSuccess();
    }

    private IEnumerator HoldRoutine()
    {
        holdProgress = 0f;

        while (holdProgress < 1f)
        {
            holdProgress += Time.unscaledDeltaTime / holdDuration;
            UpdateProgressBar();
            yield return null;
        }

        holdProgress = 1f;
        UpdateProgressBar();
        OnSwipeComplete();
    }

    private void UpdateProgressBar()
    {
        if (swipeProgressBar != null)
            swipeProgressBar.fillAmount = holdProgress;
    }

    private void UpdateFeedPileVisual()
    {
        if (feedPileImage != null)
        {
            float scale = (float)remainingSwipes / swipeCount;
            feedPileImage.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void UpdateRemainingUI()
    {
        if (remainingLabel != null)
            remainingLabel.text = $"Sisa Tarikan: {remainingSwipes}";
    }

    private IEnumerator TimerRoutine()
    {
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.unscaledDeltaTime;
            UpdateTimerUI();
            yield return null;
        }

        CompleteWithFailure();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int seconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));
        timerText.text = seconds.ToString();
        timerText.color = timeRemaining <= GameConstants.HoldSwipe.WarningThreshold ? warningTimerColor : normalTimerColor;
    }

    private void CompleteWithSuccess()
    {
        if (!isPlaying) return;
        FinishMinigame(true);
    }

    private void CompleteWithFailure()
    {
        if (!isPlaying) return;
        FinishMinigame(false);
    }

    private void FinishMinigame(bool success)
    {
        isPlaying = false;
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        CoroutineHelper.StopSafe(this, ref holdCoroutine);
        HidePopup();

        IHealthCheckListener listener = currentListener;
        currentListener = null;

        if (success)
        {
            GameLog.Info("HoldSwipe: Berhasil.");
            listener?.OnHealthCheckSuccess();
        }
        else
        {
            GameLog.Info("HoldSwipe: Gagal.");
            listener?.OnHealthCheckFailure();
        }
    }

    private void ShowPopup()
    {
        if (popupRoot == null) return;
        foreach (Transform child in popupRoot.transform)
            child.gameObject.SetActive(true);
    }

    private void HidePopup()
    {
        if (popupRoot == null) return;
        foreach (Transform child in popupRoot.transform)
            child.gameObject.SetActive(false);
    }

    private void EnsureRuntimeUi()
    {
        if (popupRoot != null && timerText != null)
            return;

        GameObject existingCanvas = GameObject.Find("HoldSwipeCanvas");
        if (existingCanvas != null)
            Destroy(existingCanvas);

        GameObject canvasObject = new GameObject("HoldSwipeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);
        popupRoot = canvasObject;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.Normal | AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchToParent(canvasRect);

        CreateBackdrop(canvasObject.transform);

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(500f, 600f);
        panelRect.anchoredPosition = new Vector2(350f, 0f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.22f, 0.12f, 0.96f);
        panelImage.raycastTarget = true;

        titleText = CreateText(panelObject.transform, "TitleText", new Vector2(0f, 270f), new Vector2(500f, 50f), 28f, TextAlignmentOptions.Center);
        timerText = CreateText(panelObject.transform, "TimerText", new Vector2(0f, 220f), new Vector2(160f, 48f), 34f, TextAlignmentOptions.Center);

        GameObject feedObj = new GameObject("FeedPile", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        feedObj.transform.SetParent(panelObject.transform, false);
        RectTransform feedRect = feedObj.GetComponent<RectTransform>();
        feedRect.anchorMin = new Vector2(0.5f, 0.5f);
        feedRect.anchorMax = new Vector2(0.5f, 0.5f);
        feedRect.pivot = new Vector2(0.5f, 0.5f);
        feedRect.sizeDelta = new Vector2(120f, 120f);
        feedRect.anchoredPosition = new Vector2(0f, 60f);
        feedPileImage = feedObj.GetComponent<Image>();
        feedPileImage.color = new Color(1f, 0.85f, 0.4f, 1f);
        feedPileImage.raycastTarget = true;
        feedPileImage.type = Image.Type.Filled;
        feedPileImage.fillMethod = Image.FillMethod.Vertical;
        feedPileImage.fillAmount = 1f;

        GameObject swipeObj = new GameObject("SwipeArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        swipeObj.transform.SetParent(panelObject.transform, false);
        RectTransform swipeRect = swipeObj.GetComponent<RectTransform>();
        swipeRect.anchorMin = new Vector2(0.5f, 0.5f);
        swipeRect.anchorMax = new Vector2(0.5f, 0.5f);
        swipeRect.pivot = new Vector2(0.5f, 0.5f);
        swipeRect.sizeDelta = new Vector2(160f, 40f);
        swipeRect.anchoredPosition = new Vector2(0f, -20f);
        Image swipeBg = swipeObj.GetComponent<Image>();
        swipeBg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        swipeBg.raycastTarget = true;

        GameObject progressObj = new GameObject("ProgressBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        progressObj.transform.SetParent(swipeObj.transform, false);
        RectTransform progRect = progressObj.GetComponent<RectTransform>();
        progRect.anchorMin = Vector2.zero;
        progRect.anchorMax = Vector2.one;
        progRect.offsetMin = Vector2.zero;
        progRect.offsetMax = Vector2.zero;
        swipeProgressBar = progressObj.GetComponent<Image>();
        swipeProgressBar.color = new Color(0.2f, 1f, 0.4f, 1f);
        swipeProgressBar.raycastTarget = false;
        swipeProgressBar.type = Image.Type.Filled;
        swipeProgressBar.fillMethod = Image.FillMethod.Horizontal;
        swipeProgressBar.fillAmount = 0f;

        remainingLabel = CreateText(panelObject.transform, "RemainingText", new Vector2(0f, -120f), new Vector2(500f, 40f), 24f, TextAlignmentOptions.Center);
    }

    private void CreateBackdrop(Transform parent)
    {
        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(parent, false);
        RectTransform rect = backdrop.GetComponent<RectTransform>();
        StretchToParent(rect);
        Image image = backdrop.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.raycastTarget = true;
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void OnHealthCheckSuccess() { }
    public void OnHealthCheckFailure() { }
}
