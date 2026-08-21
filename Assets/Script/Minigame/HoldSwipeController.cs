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

    [Header("Feed Sprites")]
    [SerializeField] private Sprite fullFeedSprite;
    [SerializeField] private Sprite halfFeedSprite;

    [Header("Panel Sprites")]
    [SerializeField] private Sprite cagesPanelSprite;
    [SerializeField] private Sprite infoPanelSprite;
    private IHealthCheckListener currentListener;
    private int remainingSwipes;
    private float timeRemaining;
    private bool isPlaying;
    private Coroutine timerCoroutine;
    private Coroutine holdCoroutine;
    private float holdProgress;

    private TextMeshProUGUI instructionText;
    private GameObject[] feedPiles;

    public bool IsPlaying => isPlaying;

    protected override void Awake()
    {
        base.Awake();
        EnsureRuntimeUi();
        Debug.Log($"HoldSwipe feedPiles count: {(feedPiles != null ? feedPiles.Length : -1)}");
        HidePopup();

        if (fullFeedSprite == null)
        fullFeedSprite = Resources.Load<Sprite>("Gambar/tempat_makan_penuh");
        if (halfFeedSprite == null)
            halfFeedSprite = Resources.Load<Sprite>("Gambar/tempat_makan_setengah");
    }

    public bool ShowHoldSwipe(IHealthCheckListener caller)
    {
        if (IsPlaying) return false;

        currentListener = caller;
        timeRemaining = timeLimit;
        isPlaying = true;
        holdProgress = 0f;

        ShowPopup();
        UpdateTimerUI();
        SetupFeedPiles();

        // Cek apakah ada pakan yang bisa di-swipe
        if (feedPiles == null || feedPiles.Length == 0)
        {
            Debug.LogError("HoldSwipe: Tidak ada pakan! Minigame gagal.");
            FinishMinigame(false);
            return false;
        }

        if (titleText != null)
            titleText.text = "Kurangi Pakan";

        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());

        return true;
    }

    private void SetupFeedPiles()
    {
        if (feedPiles == null) return;
        remainingSwipes = feedPiles.Length;
        UpdateRemainingUI();

        for (int i = 0; i < feedPiles.Length; i++)
        {
            if (feedPiles[i] != null)
            {
                feedPiles[i].SetActive(true);
                InteractableFeedPile pile = feedPiles[i].GetComponent<InteractableFeedPile>();
                if (pile != null)
                {
                    pile.StopAllCoroutines();
                    pile.transform.localScale = Vector3.one;
                }
            }
        }
    }

    public void OnFeedPileSwiped()
    {
        if (!isPlaying) return;

        remainingSwipes--;
        UpdateRemainingUI();

        if (remainingSwipes <= 0)
            CompleteWithSuccess();
    }

    // Unused stubs to prevent compile errors if referenced dynamically
    public void OnSwipeHoldStart() {}
    public void OnSwipeHoldCancel() {}
    public void OnSwipeComplete() {}
    private IEnumerator HoldRoutine() { yield break; }
    private void UpdateProgressBar() {}
    private void UpdateFeedPileVisual() {}

    private void UpdateRemainingUI()
    {
        if (remainingLabel != null)
            remainingLabel.text = $"Sisa Pakan: {remainingSwipes}";
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
        if (SFXManager.Instance != null) SFXManager.Instance.PlayMinigameSuccess();
        FinishMinigame(true);
    }

    private void CompleteWithFailure()
    {
        if (!isPlaying) return;
        if (SFXManager.Instance != null) SFXManager.Instance.PlayMinigameFail();
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
        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    private void HidePopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void EnsureRuntimeUi()
    {
        if (popupRoot != null && timerText != null)
            return;

        GameObject existingCanvas = GameObject.Find("HoldSwipeCanvas");
        if (existingCanvas != null)
            Destroy(existingCanvas);

        // ── Root Canvas ──
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

        // ── Backdrop ──
        CreateBackdrop(canvasObject.transform);

        // ── Left Panel – Cages Grid ──
        GameObject cagesPanel = CreatePanel(canvasObject.transform, "CagesPanel",
            new Vector2(-180f, 0f), new Vector2(560f, 480f),
            new Color(0.12f, 0.08f, 0.04f, 0.92f));
            
        // Terapkan sprite jika ada
        Image cagesImg = cagesPanel.GetComponent<Image>();
        if (cagesPanelSprite != null)
        {
            cagesImg.sprite = cagesPanelSprite;
            cagesImg.color = Color.white; // agar sprite original
        }
        else
        {
            cagesImg.color = new Color(0.12f, 0.08f, 0.04f, 0.92f); // fallback
        }
        cagesImg.raycastTarget = true;

        // Cages Container Label
        /*
        CreateText(cagesPanel.transform, "CagesLabel",
            new Vector2(0f, 215f), new Vector2(400f, 40f),
            22f, TextAlignmentOptions.Center, "KANDANG AYAM");
            */

        // Quadrants setup
        Vector2[] quadPositions = new Vector2[]
        {
            new Vector2(-135f,  100f), // Top-Left
            new Vector2( 135f,  100f), // Top-Right
            new Vector2(-135f, -110f), // Bottom-Left
            new Vector2( 135f, -110f)  // Bottom-Right
        };
        string[] quadNames = new string[] { "Kandang A", "Kandang B", "Kandang C", "Kandang D" };

        feedPiles = new GameObject[4];
        
        for (int i = 0; i < 4; i++)
        {
            GameObject quad = CreatePanel(cagesPanel.transform, $"Quadrant_{i}",
                quadPositions[i], new Vector2(250f, 190f),
                new Color(0.20f, 0.15f, 0.08f, 0.45f));

            // Quadrant Label
            CreateText(quad.transform, "Label",
                new Vector2(0f, 70f), new Vector2(220f, 30f),
                16f, TextAlignmentOptions.Center, quadNames[i]);

            // Chicken representation (Emoji text)
            CreateText(quad.transform, "Chickens",
                new Vector2(0f, 25f), new Vector2(220f, 40f),
                24f, TextAlignmentOptions.Center, "🐔 🐔");

            // Feed Pile Object
            GameObject feedObj = new GameObject($"FeedPile_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            feedObj.transform.SetParent(quad.transform, false);
            RectTransform feedRect = feedObj.GetComponent<RectTransform>();
            feedRect.anchorMin = new Vector2(0.5f, 0.5f);
            feedRect.anchorMax = new Vector2(0.5f, 0.5f);
            feedRect.pivot = new Vector2(0.5f, 0.5f);
            feedRect.sizeDelta = new Vector2(70f, 55f);
            feedRect.anchoredPosition = new Vector2(0f, -25f);

            Image feedImg = feedObj.GetComponent<Image>();
            if (fullFeedSprite != null)
            {
                feedImg.sprite = fullFeedSprite;
                feedImg.color = Color.white;   // agar sprite tetap asli
            }
            else
            {
                feedImg.color = new Color(0.85f, 0.65f, 0.25f, 1f); // fallback
            }
            feedImg.raycastTarget = true;

            // Feed Label
            /*
            CreateText(feedObj.transform, "Label",
                Vector2.zero, new Vector2(60f, 25f),
                12f, TextAlignmentOptions.Center, "PAKAN");
*/
            // Progress Bar Background under the pile
            GameObject barBg = CreatePanel(quad.transform, "ProgressBarBg",
                new Vector2(0f, -65f), new Vector2(70f, 8f),
                new Color(0.1f, 0.1f, 0.1f, 0.8f));

            // Progress Bar Fill
            GameObject barFillObj = CreatePanel(barBg.transform, "ProgressBarFill",
                Vector2.zero, new Vector2(70f, 8f),
                Color.yellow);
            RectTransform barFillRect = barFillObj.GetComponent<RectTransform>();
            StretchToParent(barFillRect);

            Image barFillImg = barFillObj.GetComponent<Image>();
            barFillImg.type = Image.Type.Filled;
            barFillImg.fillMethod = Image.FillMethod.Horizontal;
            barFillImg.fillAmount = 0f;
            barFillImg.raycastTarget = false;

            // Add the InteractableFeedPile component
            InteractableFeedPile interactable = feedObj.AddComponent<InteractableFeedPile>();
            interactable.controller = this;
            interactable.progressBarFill = barFillImg;
            interactable.holdDuration = holdDuration;
            interactable.fullSprite = fullFeedSprite;
            interactable.halfSprite = halfFeedSprite;

            feedPiles[i] = feedObj;

            
        }

        // ── Right Panel – Info & Controls ──
        GameObject infoPanel = CreatePanel(canvasObject.transform, "InfoPanel",
            new Vector2(280f, 0f), new Vector2(360f, 480f),
            new Color(0.06f, 0.20f, 0.10f, 0.95f));

        // Terapkan sprite jika ada
        Image infoImg = infoPanel.GetComponent<Image>();
        if (infoPanelSprite != null)
        {
            infoImg.sprite = infoPanelSprite;
            infoImg.color = Color.white;
        }
        else
        {
            infoImg.color = new Color(0.06f, 0.20f, 0.10f, 0.95f); // fallback
        }
        infoImg.raycastTarget = true;

        // Title
        titleText = CreateText(infoPanel.transform, "TitleText",
            new Vector2(0f, 200f), new Vector2(340f, 40f),
            26f, TextAlignmentOptions.Center);

        // Timer
        timerText = CreateText(infoPanel.transform, "TimerText",
            new Vector2(0f, 140f), new Vector2(160f, 50f),
            38f, TextAlignmentOptions.Center);

        // Instruction labels
        instructionText = CreateText(infoPanel.transform, "InstructionText",
            new Vector2(0f, 75f), new Vector2(320f, 30f),
            18f, TextAlignmentOptions.Center, "Ambil Semua Pakan Ayam");
        
        TextMeshProUGUI subInstruction = CreateText(infoPanel.transform, "SubInstructionText",
            new Vector2(0f, 15f), new Vector2(320f, 60f),
            14f, TextAlignmentOptions.Center, "Tekan & tahan pakan,\nlalu geser/swipe untuk mengambil.");
        if (subInstruction != null)
        {
            subInstruction.fontStyle = FontStyles.Italic;
            subInstruction.color = new Color(0.8f, 0.9f, 0.8f, 0.9f);
        }

        // Remaining count
        remainingLabel = CreateText(infoPanel.transform, "RemainingText",
            new Vector2(0f, -165f), new Vector2(340f, 36f),
            22f, TextAlignmentOptions.Center);
    }

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
        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(parent, false);
        RectTransform rect = backdrop.GetComponent<RectTransform>();
        StretchToParent(rect);
        Image image = backdrop.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.raycastTarget = true;
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment, string defaultText = "")
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
        text.text = defaultText;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LilitaOne-Regular SDF");
    
        // Fallback ke font default TMP jika tidak ditemukan
        if (font == null)
            font = TMP_Settings.defaultFontAsset;
        
        text.font = font;
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

public class InteractableFeedPile : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public HoldSwipeController controller;
    public Image progressBarFill;
    public float holdDuration = 0.5f;
    public float swipeThreshold = 35f;

    private bool isHolding;
    private float holdTime;
    private Vector2 startPos;
    private bool isReadyToSwipe;

    public Sprite fullSprite;
    public Sprite halfSprite;
    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnDisable()
    {
        ResetState();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (controller == null || !controller.IsPlaying) return;
        isHolding = true;
        holdTime = 0f;
        startPos = eventData.position;
        isReadyToSwipe = false;
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = 0f;
            progressBarFill.color = Color.yellow;
        }
        transform.localScale = Vector3.one * 0.95f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetState();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isHolding || controller == null || !controller.IsPlaying) return;

        if (isReadyToSwipe)
        {
            float dist = Vector2.Distance(startPos, eventData.position);
            if (dist >= swipeThreshold)
            {
                isHolding = false;
                StartCoroutine(AnimateCollectAndNotify());
            }
        }
    }

    private void Update()
    {
        if (!isHolding || controller == null || !controller.IsPlaying) return;

        if (!isReadyToSwipe)
        {
            holdTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(holdTime / holdDuration);
            if (progressBarFill != null)
                progressBarFill.fillAmount = progress;

            if (progress >= 1f)
            {
                isReadyToSwipe = true;
                if (progressBarFill != null)
                    progressBarFill.color = Color.green;
                transform.localScale = Vector3.one * 1.1f;

                // Ganti sprite menjadi half
                if (image != null && halfSprite != null)
                    image.sprite = halfSprite;
            }
        }
    }

    private void ResetState()
    {
        isHolding = false;
        holdTime = 0f;
        isReadyToSwipe = false;
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = 0f;
            progressBarFill.color = Color.yellow;
        }
        transform.localScale = Vector3.one;
        if (image != null && fullSprite != null)
            image.sprite = fullSprite;
    }

    private IEnumerator AnimateCollectAndNotify()
    {
        float t = 0f;
        Vector3 startScale = transform.localScale;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 7f;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }
        gameObject.SetActive(false);
        controller.OnFeedPileSwiped();
    }
}
