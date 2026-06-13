using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MemoryMatchController : Singleton<MemoryMatchController>
{
    [Header("Settings")]
    [SerializeField] private float timeLimit = GameConstants.MemoryMatch.TimeLimit;
    [SerializeField] private float flipMatchDelay = GameConstants.MemoryMatch.MatchDelay;
    [SerializeField] private Vector2 cardSize = new Vector2(100f, 130f);
    [SerializeField] private Vector2 cardSpacing = new Vector2(12f, 12f);
    [SerializeField] private int columns = GameConstants.MemoryMatch.Columns;

    [Header("Card Sprites (12 slices dari memorigame.png)")]
    [SerializeField] private Sprite[] cardSprites;

    private static readonly int[] PairMap = { 0, 1, 0, 1, 2, 3, 3, 4, 2, 4, 5, 5 };

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.25f, 0.15f);

    private IHealthCheckListener currentListener;
    private List<MemoryMatchCard> cards;
    private MemoryMatchCard firstSelected;
    private MemoryMatchCard secondSelected;
    private int matchedCount;
    private int totalPairs;
    private float timeRemaining;
    private bool isPlaying;
    private bool isProcessing;
    private Coroutine timerCoroutine;

    private GameObject popupRoot;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI errorText;
    private Transform gridContainer;

    public bool IsPlaying => isPlaying;

    protected override void Awake()
    {
        base.Awake();
        EnsureRuntimeUi();
        HidePopup();
    }

    public bool ShowMemoryMatch(IHealthCheckListener listener, string eventTitle = "")
    {
        if (isPlaying)
            return false;

        if (listener == null)
        {
            Debug.LogWarning("MemoryMatchController: listener is null.");
            return false;
        }

        EnsureRuntimeUi();
        EnsureEventSystem();

        if (popupRoot == null || gridContainer == null)
        {
            Debug.LogWarning("MemoryMatchController: UI belum lengkap.");
            return false;
        }

        if (cardSprites == null || cardSprites.Length < 12)
        {
            Debug.LogWarning("MemoryMatchController: cardSprites (12) belum di-assign di Inspector Managers.prefab.");
            if (errorText != null)
            {
                errorText.text = "Memory Match: sprite belum di-assign!";
                errorText.gameObject.SetActive(true);
            }
            return false;
        }

        currentListener = listener;
        firstSelected = null;
        secondSelected = null;
        isProcessing = false;

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(eventTitle) ? "Memory Match" : eventTitle;

        BuildGrid();

        timeRemaining = timeLimit;
        UpdateTimerUI();

        popupRoot.SetActive(true);
        isPlaying = true;
        CoroutineHelper.StopAndStart(this, ref timerCoroutine, TimerRoutine());
        return true;
    }

    public void OnCardClicked(MemoryMatchCard card)
    {
        if (!isPlaying || isProcessing || card == null || card.IsMatched || card.IsFlipped)
            return;

        if (firstSelected == null)
        {
            firstSelected = card;
            card.FlipToFront();
            if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawPieceClick();
            return;
        }

        if (firstSelected == card)
            return;

        secondSelected = card;
        card.FlipToFront();
        isProcessing = true;

        if (firstSelected.PairId == secondSelected.PairId)
            StartCoroutine(OnMatchRoutine());
        else
            StartCoroutine(OnMismatchRoutine());
    }

    private IEnumerator OnMatchRoutine()
    {
        yield return new WaitForSecondsRealtime(flipMatchDelay);

        firstSelected.SetMatched();
        secondSelected.SetMatched();
        matchedCount++;

        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawPieceSwap();

        firstSelected = null;
        secondSelected = null;
        isProcessing = false;

        if (matchedCount >= totalPairs)
            CompleteWithSuccess();
    }

    private IEnumerator OnMismatchRoutine()
    {
        yield return new WaitForSecondsRealtime(flipMatchDelay);

        firstSelected.FlipToBack();
        secondSelected.FlipToBack();

        firstSelected = null;
        secondSelected = null;
        isProcessing = false;
    }

    private void BuildGrid()
    {
        ClearGrid();

        totalPairs = 6;
        int totalCards = 12;

        int[] indices = new int[totalCards];
        for (int i = 0; i < totalCards; i++)
            indices[i] = i;

        for (int i = totalCards - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        ConfigureGridLayout(totalCards);
        cards = new List<MemoryMatchCard>(totalCards);

        for (int i = 0; i < totalCards; i++)
        {
            int idx = indices[i];
            int pairId = PairMap[idx];
            Sprite sprite = (cardSprites != null && idx < cardSprites.Length) ? cardSprites[idx] : null;

            GameObject cardObject = CreateCardObject();
            cardObject.transform.SetParent(gridContainer, false);

            MemoryMatchCard card = cardObject.GetComponent<MemoryMatchCard>();
            if (card == null)
                card = cardObject.AddComponent<MemoryMatchCard>();

            card.Setup(pairId, sprite, OnCardClicked);
            cards.Add(card);
        }
    }

    private void ConfigureGridLayout(int totalCards)
    {
        GridLayoutGroup layout = gridContainer.GetComponent<GridLayoutGroup>();
        if (layout == null)
            layout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();

        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columns;
        layout.cellSize = cardSize;
        layout.spacing = cardSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;

        int rows = Mathf.CeilToInt((float)totalCards / columns);
        RectTransform gridRect = gridContainer as RectTransform;
        if (gridRect != null)
        {
            float width = columns * cardSize.x + (columns - 1) * cardSpacing.x;
            float height = rows * cardSize.y + (rows - 1) * cardSpacing.y;
            gridRect.sizeDelta = new Vector2(width, height);
        }
    }

    private GameObject CreateCardObject()
    {
        GameObject cardObject = new GameObject("MemoryCard", typeof(RectTransform));
        Image img = cardObject.AddComponent<Image>();
        img.raycastTarget = true;
        return cardObject;
    }

    private void ClearGrid()
    {
        if (gridContainer == null)
            return;

        for (int i = gridContainer.childCount - 1; i >= 0; i--)
            Destroy(gridContainer.GetChild(i).gameObject);
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
        if (timerText == null)
            return;

        int seconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));
        timerText.text = seconds.ToString();
        timerText.color = timeRemaining <= 10f ? warningTimerColor : normalTimerColor;
    }

    private void CompleteWithSuccess()
    {
        if (!isPlaying)
            return;

        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawComplete();
        FinishMinigame(true);
    }

    private void CompleteWithFailure()
    {
        if (!isPlaying)
            return;

        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawFail();
        FinishMinigame(false);
    }

    private void FinishMinigame(bool success)
    {
        isPlaying = false;
        isProcessing = false;
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        HidePopup();

        IHealthCheckListener listener = currentListener;
        currentListener = null;
        firstSelected = null;
        secondSelected = null;

        if (success)
        {
            GameLog.Info("MemoryMatch: Berhasil.");
            listener?.OnHealthCheckSuccess();
        }
        else
        {
            GameLog.Info("MemoryMatch: Gagal.");
            listener?.OnHealthCheckFailure();
        }
    }

    private void HidePopup()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    private void EnsureRuntimeUi()
    {
        if (popupRoot != null && timerText != null && gridContainer != null)
            return;

        GameObject canvasObject = new GameObject("MemoryMatchCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(canvasObject);
        popupRoot = canvasObject;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        CreateBackdrop(canvasObject.transform);

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(450f, 550f);
        panelRect.anchoredPosition = new Vector2(350f, 0f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.22f, 0.12f, 0.96f);
        panelImage.raycastTarget = true;

        titleText = CreateText(panelObject.transform, "TitleText", new Vector2(0f, 240f), new Vector2(400f, 40f), 26f, TextAlignmentOptions.Center);
        timerText = CreateText(panelObject.transform, "TimerText", new Vector2(0f, 195f), new Vector2(120f, 40f), 32f, TextAlignmentOptions.Center);
        errorText = CreateText(panelObject.transform, "ErrorText", new Vector2(0f, 0f), new Vector2(400f, 60f), 20f, TextAlignmentOptions.Center);
        errorText.color = new Color(1f, 0.3f, 0.3f);
        errorText.gameObject.SetActive(false);

        GameObject gridObject = new GameObject("GridContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(panelObject.transform, false);
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0f, -30f);
        gridContainer = gridObject.transform;
    }

    private void CreateBackdrop(Transform parent)
    {
        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(parent, false);

        RectTransform rect = backdrop.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

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
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        text.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");
        return text;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }
}
