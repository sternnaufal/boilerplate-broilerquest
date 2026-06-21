using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HumidityToggleController : Singleton<HumidityToggleController>, IHealthCheckListener
{
    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI remainingLabel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private RectTransform timingBarBg;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private RectTransform indicator;

    [Header("Settings")]
    [SerializeField] private float timeLimit = GameConstants.HumidityToggle.TimeLimit;
    [SerializeField] private int targetSuccess = GameConstants.HumidityToggle.TargetSuccess;
    [SerializeField] private int maxFails = GameConstants.HumidityToggle.MaxFails;
    [SerializeField] private float indicatorSpeed = GameConstants.HumidityToggle.IndicatorSpeed;
    [SerializeField] private float targetZoneWidthPct = GameConstants.HumidityToggle.TargetZoneWidth;

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.25f, 0.15f);

    private IHealthCheckListener currentListener;
    private int successCount;
    private int failCount;
    private float timeRemaining;
    private bool isPlaying;
    private bool machineOn;
    private Coroutine timerCoroutine;
    private float indicatorProgress;
    private int pingPongDirection = 1;

    public bool IsPlaying => isPlaying;

    protected override void Awake()
    {
        base.Awake();
        EnsureRuntimeUi();
        HidePopup();
    }

    public bool ShowToggle(IHealthCheckListener caller)
    {
        if (IsPlaying) return false;

        currentListener = caller;
        successCount = 0;
        failCount = 0;
        timeRemaining = timeLimit;
        isPlaying = true;
        machineOn = false;
        indicatorProgress = 0f;
        pingPongDirection = 1;

        ShowPopup();
        UpdateRemainingUI();
        UpdateTimerUI();

        if (titleText != null)
            titleText.text = "Atur Kelembaban";

        // Initial state: machine OFF, show "HIDUPKAN" button, indicator stopped
        UpdateToggleButtonLabel("HIDUPKAN MESIN");
        StopIndicator();

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(OnToggleClicked);
        }

        return true;
    }

    private void StopIndicator()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
    }

    private void UpdateToggleButtonLabel(string label)
    {
        TextMeshProUGUI btnLabel = toggleButton?.GetComponentInChildren<TextMeshProUGUI>(true);
        if (btnLabel != null)
            btnLabel.text = label;
    }

    private void OnToggleClicked()
    {
        if (!isPlaying) return;

        // First click: turn machine ON, start indicator
        if (!machineOn)
        {
            machineOn = true;
            UpdateToggleButtonLabel("ON/OFF");
            CoroutineHelper.StopSafe(this, ref timerCoroutine);
            timerCoroutine = StartCoroutine(TimerRoutine());
            return;
        }

        bool hit = false;
        if (indicator != null && targetZone != null && timingBarBg != null)
        {
            float targetMin = 0.5f - (targetZoneWidthPct / 2f);
            float targetMax = 0.5f + (targetZoneWidthPct / 2f);

            if (indicatorProgress >= targetMin && indicatorProgress <= targetMax)
            {
                hit = true;
            }
        }

        if (hit)
        {
            successCount++;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayTimingSuccess();
        }
        else
        {
            failCount++;
            if (SFXManager.Instance != null) SFXManager.Instance.PlayTimingFail();
        }

        UpdateRemainingUI();

        if (failCount > maxFails)
        {
            CompleteWithFailure();
        }
        else if (successCount >= targetSuccess)
        {
            CompleteWithSuccess();
        }
    }

    private void UpdateRemainingUI()
    {
        if (remainingLabel != null)
            remainingLabel.text = $"Berhasil: {successCount}/{targetSuccess} | Gagal: {failCount}/{maxFails}";
    }

    private IEnumerator TimerRoutine()
    {
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.unscaledDeltaTime;
            UpdateTimerUI();

            if (indicator != null && timingBarBg != null)
            {
                indicatorProgress += pingPongDirection * indicatorSpeed * Time.unscaledDeltaTime;
                if (indicatorProgress >= 1f)
                {
                    indicatorProgress = 1f;
                    pingPongDirection = -1;
                }
                else if (indicatorProgress <= 0f)
                {
                    indicatorProgress = 0f;
                    pingPongDirection = 1;
                }

                float barWidth = timingBarBg.rect.width;
                float xPos = Mathf.Lerp(-barWidth / 2f, barWidth / 2f, indicatorProgress);
                indicator.anchoredPosition = new Vector2(xPos, 0f);
            }

            yield return null;
        }

        CompleteWithFailure();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int seconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));
        timerText.text = seconds.ToString();
        timerText.color = timeRemaining <= GameConstants.HumidityToggle.WarningThreshold ? warningTimerColor : normalTimerColor;
    }

    private void CompleteWithSuccess()
    {
        if (!isPlaying) return;
        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawComplete();
        HealthCheckResultOverlay.ShowSuccess();
        FinishMinigame(true);
    }

    private void CompleteWithFailure()
    {
        if (!isPlaying) return;
        CameraShake.Trigger();
        UIAlertPanel.Instance?.Show(UIAlertPanel.NotificationType.TimeOut);
        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawFail();
        HealthCheckResultOverlay.ShowFail();
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
            GameLog.Info("HumidityToggle: Berhasil.");
            listener?.OnHealthCheckSuccess();
        }
        else
        {
            GameLog.Info("HumidityToggle: Gagal.");
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
        if (popupRoot != null && timerText != null && indicator != null)
            return;

        GameObject existingCanvas = GameObject.Find("HumidityToggleCanvas");
        if (existingCanvas != null)
            Destroy(existingCanvas);

        GameObject canvasObject = new GameObject("HumidityToggleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        GameObject timingBgObj = new GameObject("TimingBarBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        timingBgObj.transform.SetParent(panelObject.transform, false);
        timingBarBg = timingBgObj.GetComponent<RectTransform>();
        timingBarBg.anchorMin = new Vector2(0.5f, 0.5f);
        timingBarBg.anchorMax = new Vector2(0.5f, 0.5f);
        timingBarBg.pivot = new Vector2(0.5f, 0.5f);
        timingBarBg.sizeDelta = new Vector2(400f, 40f);
        timingBarBg.anchoredPosition = new Vector2(0f, 80f);
        Image bgImg = timingBgObj.GetComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        GameObject targetObj = new GameObject("TargetZone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        targetObj.transform.SetParent(timingBgObj.transform, false);
        targetZone = targetObj.GetComponent<RectTransform>();
        targetZone.anchorMin = new Vector2(0.5f, 0.5f);
        targetZone.anchorMax = new Vector2(0.5f, 0.5f);
        targetZone.pivot = new Vector2(0.5f, 0.5f);
        targetZone.sizeDelta = new Vector2(400f * targetZoneWidthPct, 40f);
        targetZone.anchoredPosition = Vector2.zero;
        Image targetImg = targetObj.GetComponent<Image>();
        targetImg.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);

        GameObject indObj = new GameObject("Indicator", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        indObj.transform.SetParent(timingBgObj.transform, false);
        indicator = indObj.GetComponent<RectTransform>();
        indicator.anchorMin = new Vector2(0.5f, 0.5f);
        indicator.anchorMax = new Vector2(0.5f, 0.5f);
        indicator.pivot = new Vector2(0.5f, 0.5f);
        indicator.sizeDelta = new Vector2(10f, 60f);
        indicator.anchoredPosition = new Vector2(-200f, 0f);
        Image indImg = indObj.GetComponent<Image>();
        indImg.color = new Color(1f, 1f, 0.2f, 1f);

        GameObject buttonObject = new GameObject("ToggleButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panelObject.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(200f, 100f);
        buttonRect.anchoredPosition = new Vector2(0f, -40f);

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        buttonImage.raycastTarget = true;
        toggleButton = buttonObject.GetComponent<Button>();

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        StretchToParent(labelRect);
        TextMeshProUGUI btnLabel = labelObject.GetComponent<TextMeshProUGUI>();
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.fontSize = 30f;
        btnLabel.fontStyle = FontStyles.Bold;
        btnLabel.color = Color.white;
        btnLabel.raycastTarget = false;
        btnLabel.text = "ON/OFF";

        remainingLabel = CreateText(panelObject.transform, "RemainingText", new Vector2(0f, -140f), new Vector2(500f, 40f), 24f, TextAlignmentOptions.Center);
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
