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
    [SerializeField] private Button machineToggleButton; // ON/OFF button (above timing bar)
    [SerializeField] private Button tekanButton; // Tekan button (below timing bar)
    [SerializeField] private RectTransform timingBarBg;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private RectTransform indicator;

    [Header("Settings")]
    [SerializeField] private float timeLimit = GameConstants.HumidityToggle.TimeLimit;
    [SerializeField] private int totalAttempts = 3;
    [SerializeField] private int maxFails = 1;
    [SerializeField] private float indicatorSpeed = GameConstants.HumidityToggle.IndicatorSpeed;
    [SerializeField] private float targetZoneWidthPct = GameConstants.HumidityToggle.TargetZoneWidth;

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.25f, 0.15f);

    private IHealthCheckListener currentListener;
    private int successCount;
    private int failCount;
    private int clickCount;
    private float timeRemaining;
    private bool isPlaying;
    private bool machineOn;
    private Coroutine timerCoroutine;
    private float indicatorProgress;
    private int pingPongDirection = 1;
    private Sprite onSprite;
    private Sprite offSprite;

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
        clickCount = 0;
        timeRemaining = timeLimit;
        isPlaying = true;
        machineOn = false;
        indicatorProgress = 0f;
        pingPongDirection = 1;

        LoadSprites();
        ShowPopup();
        UpdateRemainingUI();
        UpdateTimerUI();

        if (titleText != null)
            titleText.text = "Atur Kelembaban";

        // Initial state: machine OFF, Tekan disabled
        SetMachineVisual(false);
        SetTekanButtonInteractable(false);

        if (machineToggleButton != null)
        {
            machineToggleButton.onClick.RemoveAllListeners();
            machineToggleButton.onClick.AddListener(OnMachineToggleClicked);
        }

        if (tekanButton != null)
        {
            tekanButton.onClick.RemoveAllListeners();
            tekanButton.onClick.AddListener(OnTekanClicked);
        }

        // Start overall timer coroutine
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());

        return true;
    }

    private void LoadSprites()
    {
        if (onSprite != null && offSprite != null) return;

#if UNITY_EDITOR
        if (onSprite == null)
            onSprite = Resources.Load<Sprite>("Gambar/ON_button");
        if (offSprite == null)
            offSprite = Resources.Load<Sprite>("Gambar/OFF_button");
#else
        // Fallback untuk build: coba Resources (kalo masih ada)
        if (onSprite == null)
            onSprite = Resources.Load<Sprite>("ON button");
        if (offSprite == null)
            offSprite = Resources.Load<Sprite>("OFF button");
#endif
        if (onSprite == null) Debug.LogWarning("HumidityToggle: 'ON button' sprite not found in Assets/Gambar/.");
        if (offSprite == null) Debug.LogWarning("HumidityToggle: 'OFF button' sprite not found in Assets/Gambar/.");
    }

    private void SetMachineVisual(bool on)
    {
        if (machineToggleButton == null) return;
        Image btnImage = machineToggleButton.GetComponent<Image>();
        if (btnImage != null)
        {
            if (on && onSprite != null)
                btnImage.sprite = onSprite;
            else if (!on && offSprite != null)
                btnImage.sprite = offSprite;
        }
    }

    private void SetTekanButtonInteractable(bool interactable)
    {
        if (tekanButton != null)
            tekanButton.interactable = interactable;
    }

    private void OnMachineToggleClicked()
    {
        if (!isPlaying) return;

        machineOn = !machineOn;
        SetMachineVisual(machineOn);
        SetTekanButtonInteractable(machineOn);
    }

    private void OnTekanClicked()
    {
        if (!isPlaying || !machineOn) return;

        clickCount++;

        bool hit = false;
        if (indicator != null && targetZone != null && timingBarBg != null)
        {
            float targetMin = 0.5f - (targetZoneWidthPct / 2f);
            float targetMax = 0.5f + (targetZoneWidthPct / 2f);

            if (indicatorProgress >= targetMin && indicatorProgress <= targetMax)
            {
                hit = true;
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        if (hit)
        {
            if (SFXManager.Instance != null) SFXManager.Instance.PlayTimingSuccess();
        }
        else
        {
            if (SFXManager.Instance != null) SFXManager.Instance.PlayTimingFail();
        }

        UpdateRemainingUI();

        if (clickCount >= totalAttempts)
        {
            if (failCount > maxFails)
                CompleteWithFailure();
            else
                CompleteWithSuccess();
        }
    }

    private void UpdateRemainingUI()
    {
        if (remainingLabel != null)
            remainingLabel.text = $"Percobaan: {clickCount}/{totalAttempts}";
    }

    private IEnumerator TimerRoutine()
    {
        while (timeRemaining > 0f)
        {
            timeRemaining -= Time.unscaledDeltaTime;
            UpdateTimerUI();

            if (machineOn && indicator != null && timingBarBg != null)
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

        bool listenerValid = listener != null && !(listener is UnityEngine.Object obj && obj == null);
        if (!listenerValid)
        {
            GameLog.Warn("HumidityToggle: Listener sudah di-destroy, abaikan callback.");
            return;
        }

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

    public void HidePopup()
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
        panelImage.sprite = Resources.Load<Sprite>("bgminigame_pipa1");
        panelImage.color = Color.white;
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

        // Machine Toggle Button (ON/OFF) — above timing bar, uses sprites
        GameObject machineBtnObj = new GameObject("MachineToggleButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        machineBtnObj.transform.SetParent(panelObject.transform, false);
        RectTransform machineBtnRect = machineBtnObj.GetComponent<RectTransform>();
        machineBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
        machineBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
        machineBtnRect.pivot = new Vector2(0.5f, 0.5f);
        machineBtnRect.sizeDelta = new Vector2(120f, 60f);
        machineBtnRect.anchoredPosition = new Vector2(0f, 150f);
        Image machineBtnImage = machineBtnObj.GetComponent<Image>();
        machineBtnImage.color = Color.white;
        machineBtnImage.raycastTarget = true;
        machineToggleButton = machineBtnObj.GetComponent<Button>();

        // Tekan Button — below timing bar, disabled until machine ON
        GameObject tekanBtnObj = new GameObject("TekanButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        tekanBtnObj.transform.SetParent(panelObject.transform, false);
        RectTransform tekanBtnRect = tekanBtnObj.GetComponent<RectTransform>();
        tekanBtnRect.anchorMin = new Vector2(0.5f, 0.5f);
        tekanBtnRect.anchorMax = new Vector2(0.5f, 0.5f);
        tekanBtnRect.pivot = new Vector2(0.5f, 0.5f);
        tekanBtnRect.sizeDelta = new Vector2(200f, 100f);
        tekanBtnRect.anchoredPosition = new Vector2(0f, -40f);
        Image tekanBtnImage = tekanBtnObj.GetComponent<Image>();
        tekanBtnImage.color = new Color(0.2f, 0.6f, 1f, 1f);
        tekanBtnImage.raycastTarget = true;
        tekanButton = tekanBtnObj.GetComponent<Button>();

        // Label for Tekan button
        GameObject tekanLabelObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        tekanLabelObj.transform.SetParent(tekanBtnObj.transform, false);
        RectTransform tekanLabelRect = tekanLabelObj.GetComponent<RectTransform>();
        StretchToParent(tekanLabelRect);
        TextMeshProUGUI tekanLabel = tekanLabelObj.GetComponent<TextMeshProUGUI>();
        tekanLabel.alignment = TextAlignmentOptions.Center;
        tekanLabel.fontSize = 30f;
        tekanLabel.fontStyle = FontStyles.Bold;
        tekanLabel.color = Color.white;
        tekanLabel.raycastTarget = false;
        tekanLabel.text = "Tekan";
        tekanLabel.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LilitaOne-Regular SDF");

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
        text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LilitaOne-Regular SDF");
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
