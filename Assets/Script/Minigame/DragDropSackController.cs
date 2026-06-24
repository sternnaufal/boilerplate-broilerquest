using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; 

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

    [Header("Sack Asset")]
    [SerializeField] private Sprite sackSprite;

    private RectTransform dropZoneRect;
    private TextMeshProUGUI instructionText;
    private IHealthCheckListener currentListener;
    private int remainingSacks;
    private float timeRemaining;
    private bool isPlaying;
    private Coroutine timerCoroutine;

    // Scattered positions for sacks inside the storage panel
    private static readonly Vector2[] sackPositions = new Vector2[]
    {
        new Vector2(-80f,   10f),
        new Vector2( 80f,   10f),
        new Vector2(-40f,  -45f),
        new Vector2( 40f,  -45f),
        new Vector2(  0f,  -15f)
    };

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

        RecollectSacks();
        ShowPopup();
        UpdateRemainingUI();
        UpdateTimerUI();
        SetupSacks();

        if (titleText != null)
            titleText.text = "Tambah Sekam Kering";

        if (instructionText != null)
            instructionText.text = "Tarik karung ke dalam kandang";

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
            bool active = i < remainingSacks;
            sack.SetActive(active);

            if (active)
            {
                // Reset position to scattered layout
                RectTransform rt = sack.GetComponent<RectTransform>();
                if (i < sackPositions.Length)
                    rt.anchoredPosition = sackPositions[i];

                // Attach / configure drag handler
                DraggableSack draggable = sack.GetComponent<DraggableSack>();
                if (draggable == null)
                    draggable = sack.AddComponent<DraggableSack>();
                draggable.dropZoneRect = dropZoneRect;
                draggable.controller = this;
            }
        }
    }

    /// <summary>
    /// Reparent any sacks that were moved to canvas root during an interrupted drag
    /// (e.g. timer expired while player was mid-drag).
    /// </summary>
    private void RecollectSacks()
    {
        if (popupRoot == null || sackContainer == null) return;
        Transform root = popupRoot.transform;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith("Sack_"))
                child.SetParent(sackContainer, false);
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
        timerText.color = timeRemaining <= GameConstants.DragDropSack.WarningThreshold
            ? warningTimerColor
            : normalTimerColor;
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
        RecollectSacks();
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

    // ── Runtime UI Construction ─────────────────────────────────

    private void EnsureRuntimeUi()
    {
        if (popupRoot != null && timerText != null)
            return;

        // Destroy leftover from a previous session
        GameObject existingCanvas = GameObject.Find("DragDropSackCanvas");
        if (existingCanvas != null)
            Destroy(existingCanvas);

        // ── Root Canvas ──
        GameObject canvasObj = new GameObject("DragDropSackCanvas",
            typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObj);
        popupRoot = canvasObj;

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        canvas.additionalShaderChannels =
            AdditionalCanvasShaderChannels.TexCoord1 |
            AdditionalCanvasShaderChannels.Normal   |
            AdditionalCanvasShaderChannels.Tangent;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        StretchToParent(canvasObj.GetComponent<RectTransform>());

        // ── Backdrop ──
        CreateBackdrop(canvasObj.transform);

        // ── Left Panel – Kandang Area (Target Drop Zone) ──
        GameObject kandangPanel = CreatePanel(canvasObj.transform, "KandangPanel",
            new Vector2(-180f, 0f), new Vector2(520f, 440f),
            new Color(0.12f, 0.08f, 0.04f, 0.92f));

        // Inner kandang area (lighter border effect)
        GameObject kandangInner = CreatePanel(kandangPanel.transform, "KandangInner",
            new Vector2(0f, -10f), new Vector2(490f, 390f),
            new Color(0.20f, 0.15f, 0.08f, 0.80f));

        // Kandang label
        CreateText(kandangPanel.transform, "KandangLabel",
            new Vector2(0f, 195f), new Vector2(400f, 40f),
            22f, TextAlignmentOptions.Center, "KANDANG");

        // Helper label inside KandangInner to tell player to drop here
        CreateText(kandangInner.transform, "KandangDropLabel",
            Vector2.zero, new Vector2(450f, 40f),
            18f, TextAlignmentOptions.Center, "Tarik Karung Sekam ke Sini");

        // Assign dropZone and dropZoneRect to KandangInner
        dropZone = kandangInner.transform;
        dropZoneRect = kandangInner.GetComponent<RectTransform>();

        // ── Right Panel – Info & Source Storage ──
        GameObject infoPanel = CreatePanel(canvasObj.transform, "InfoPanel",
            new Vector2(280f, 0f), new Vector2(360f, 440f),
            new Color(0.06f, 0.20f, 0.10f, 0.94f));

        // Title
        titleText = CreateText(infoPanel.transform, "TitleText",
            new Vector2(0f, 185f), new Vector2(340f, 40f),
            26f, TextAlignmentOptions.Center);

        // Timer
        timerText = CreateText(infoPanel.transform, "TimerText",
            new Vector2(0f, 140f), new Vector2(120f, 50f),
            38f, TextAlignmentOptions.Center);

        // Instruction label
        instructionText = CreateText(infoPanel.transform, "InstructionText",
            new Vector2(0f, 80f), new Vector2(320f, 30f),
            16f, TextAlignmentOptions.Center, "Tarik karung ke dalam kandang");
        if (instructionText != null)
            instructionText.fontStyle = FontStyles.Italic;

        // Storage panel for sacks (Source Area)
        GameObject storagePanel = CreatePanel(infoPanel.transform, "StoragePanel",
            new Vector2(0f, -30f), new Vector2(280f, 180f),
            new Color(0.25f, 0.20f, 0.15f, 0.70f));

        // Inner border for storage panel
        CreatePanel(storagePanel.transform, "StorageBorder",
            Vector2.zero, new Vector2(268f, 168f),
            new Color(0.40f, 0.30f, 0.20f, 0.40f));

        CreateText(storagePanel.transform, "StorageLabel",
            new Vector2(0f, 70f), new Vector2(260f, 30f),
            16f, TextAlignmentOptions.Center, "Stok Sekam Baru");

        // Sack container inside the storage panel
        GameObject sackArea = new GameObject("SackContainer", typeof(RectTransform));
        sackArea.transform.SetParent(storagePanel.transform, false);
        StretchToParent(sackArea.GetComponent<RectTransform>());
        sackContainer = sackArea.transform;

        // Create sack objects (scattered inside storage panel)
        for (int i = 0; i < sackCount; i++)
        {
            GameObject sackObj = new GameObject($"Sack_{i}",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sackObj.transform.SetParent(sackContainer, false);

            RectTransform srt = sackObj.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0.5f);
            srt.anchorMax = new Vector2(0.5f, 0.5f);
            srt.pivot     = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(65f, 65f);
            if (i < sackPositions.Length)
                srt.anchoredPosition = sackPositions[i];

            Image sackImg = sackObj.GetComponent<Image>();
            if (sackSprite != null)
            {
                sackImg.sprite = sackSprite;
                sackImg.color = Color.white;   // biarkan sprite original
            }
            else
            {
                sackImg.color = new Color(0.82f, 0.62f, 0.22f, 1f); // fallback
            }

            // Inner label so sack is visually distinguishable
            TextMeshProUGUI sackLabel = CreateText(sackObj.transform, "Label",
                Vector2.zero, new Vector2(60f, 30f),
                14f, TextAlignmentOptions.Center, "Sekam");
            if (sackLabel != null)
            {
                sackLabel.color = new Color(0.25f, 0.15f, 0.05f, 1f);
                sackLabel.fontStyle = FontStyles.Normal;
            }
        }

        // Remaining count
        remainingLabel = CreateText(infoPanel.transform, "RemainingText",
            new Vector2(0f, -165f), new Vector2(340f, 36f),
            22f, TextAlignmentOptions.Center);

        EnsureEventSystem();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObj = new GameObject("EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObj);
    }

    // ── Helper Methods ──────────────────────────────────────────

    private GameObject CreatePanel(Transform parent, string panelName,
        Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(panelName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image img = panel.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true;

        return panel;
    }

    private void CreateBackdrop(Transform parent)
    {
        GameObject backdrop = new GameObject("Backdrop",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(parent, false);
        StretchToParent(backdrop.GetComponent<RectTransform>());
        Image image = backdrop.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.raycastTarget = true;
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName,
        Vector2 anchoredPosition, Vector2 size, float fontSize,
        TextAlignmentOptions alignment, string defaultText = "")
    {
        GameObject textObj = new GameObject(objectName,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObj.GetComponent<TextMeshProUGUI>();
        text.font = Resources.Load<TMP_FontAsset>(
            "Fonts & Materials/LiberationSans SDF - Fallback");
        text.alignment = alignment;
        text.fontSize  = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color     = Color.white;
        text.raycastTarget = false;
        text.text = defaultText;

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
