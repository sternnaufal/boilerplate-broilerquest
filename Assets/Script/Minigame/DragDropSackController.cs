using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DragDropSackController : Singleton<DragDropSackController>, IHealthCheckListener
{
    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform dropZone;
    [SerializeField] private Transform sackContainer;
    [SerializeField] private TextMeshProUGUI remainingLabel;

    [Header("Settings")]
    [SerializeField] private float timeLimit = GameConstants.DragDropSack.TimeLimit;
    [SerializeField] private int sackCount = GameConstants.DragDropSack.SackCount;

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.25f, 0.15f);

    private IHealthCheckListener currentListener;
    private int remainingSacks;
    private float timeRemaining;
    private bool isPlaying;
    private Coroutine timerCoroutine;

    public bool IsPlaying => isPlaying;

    protected override void Awake()
    {
        base.Awake();
        EnsureRuntimeUi();
        HidePopup();
    }

    public bool ShowDragDrop(IHealthCheckListener caller)
    {
        if (IsPlaying) return false;

        currentListener = caller;
        remainingSacks = sackCount;
        timeRemaining = timeLimit;
        isPlaying = true;

        ShowPopup();
        UpdateRemainingUI();
        UpdateTimerUI();
        SetupSacks();

        if (titleText != null)
            titleText.text = "Tambah Sekam Kering";

        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());

        return true;
    }

    public void OnSackDropped()
    {
        if (!isPlaying) return;
        remainingSacks--;
        UpdateRemainingUI();

        if (remainingSacks <= 0)
            CompleteWithSuccess();
    }

    private void SetupSacks()
    {
        if (sackContainer == null) return;
        for (int i = 0; i < sackContainer.childCount; i++)
        {
            GameObject sack = sackContainer.GetChild(i).gameObject;
            sack.SetActive(i < remainingSacks);
        }
    }

    private void UpdateRemainingUI()
    {
        if (remainingLabel != null)
            remainingLabel.text = $"Sisa Karung: {remainingSacks}";
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
        timerText.color = timeRemaining <= GameConstants.DragDropSack.WarningThreshold ? warningTimerColor : normalTimerColor;
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
        HidePopup();

        IHealthCheckListener listener = currentListener;
        currentListener = null;

        if (success)
        {
            GameLog.Info("DragDropSack: Berhasil.");
            listener?.OnHealthCheckSuccess();
        }
        else
        {
            GameLog.Info("DragDropSack: Gagal.");
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

        GameObject existingCanvas = GameObject.Find("DragDropSackCanvas");
        if (existingCanvas != null)
            Destroy(existingCanvas);

        GameObject canvasObject = new GameObject("DragDropSackCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        GameObject dropZoneObject = new GameObject("DropZone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dropZoneObject.transform.SetParent(panelObject.transform, false);
        RectTransform dropRect = dropZoneObject.GetComponent<RectTransform>();
        dropRect.anchorMin = new Vector2(0.5f, 0.5f);
        dropRect.anchorMax = new Vector2(0.5f, 0.5f);
        dropRect.pivot = new Vector2(0.5f, 0.5f);
        dropRect.sizeDelta = new Vector2(200f, 160f);
        dropRect.anchoredPosition = new Vector2(0f, -10f);
        Image dropImage = dropZoneObject.GetComponent<Image>();
        dropImage.color = new Color(0.3f, 0.5f, 0.2f, 0.8f);
        dropImage.raycastTarget = true;
        dropZone = dropZoneObject.transform;

        GameObject sackArea = new GameObject("SackContainer", typeof(RectTransform));
        sackArea.transform.SetParent(panelObject.transform, false);
        RectTransform sackRect = sackArea.GetComponent<RectTransform>();
        sackRect.anchorMin = new Vector2(0.5f, 0);
        sackRect.anchorMax = new Vector2(0.5f, 0);
        sackRect.pivot = new Vector2(0.5f, 0);
        sackRect.anchoredPosition = new Vector2(0f, 20f);
        sackRect.sizeDelta = new Vector2(400f, 80f);
        sackContainer = sackArea.transform;

        for (int i = 0; i < sackCount; i++)
        {
            GameObject sackObj = new GameObject($"Sack_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sackObj.transform.SetParent(sackContainer, false);
            RectTransform sackRectT = sackObj.GetComponent<RectTransform>();
            sackRectT.anchorMin = new Vector2(0.5f, 0.5f);
            sackRectT.anchorMax = new Vector2(0.5f, 0.5f);
            sackRectT.pivot = new Vector2(0.5f, 0.5f);
            sackRectT.sizeDelta = new Vector2(60f, 60f);
            sackRectT.anchoredPosition = new Vector2((i - (sackCount - 1) * 0.5f) * 80f, 0f);
            Image sackImage = sackObj.GetComponent<Image>();
            sackImage.color = new Color(0.8f, 0.6f, 0.2f, 1f);
            sackImage.raycastTarget = true;
        }

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
