using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JigsawMinigameController : MonoBehaviour
{
    public static JigsawMinigameController Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private Button closeButton;

    [Header("Tile Prefab")]
    [SerializeField] private GameObject tilePrefab;

    [Header("Settings")]
    [SerializeField] private float timeLimit = GameConstants.JigsawMinigame.TimeLimit;
    [SerializeField] private int gridSize = GameConstants.JigsawMinigame.GridSize;
    [SerializeField] private float tileSize = GameConstants.JigsawMinigame.TileSize;
    [SerializeField] private float tileSpacing = GameConstants.JigsawMinigame.TileSpacing;
    [SerializeField] private float swapDuration = GameConstants.JigsawMinigame.SwapDuration;

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.25f, 0.15f);

    private IHealthCheckListener currentListener;
    private JigsawPiece[] pieces;
    private JigsawPiece selectedPiece;
    private Rect[] uvRects;
    private float timeRemaining;
    private bool isPlaying;
    private bool isAnimating;
    private Coroutine timerCoroutine;
    private Coroutine swapFeedbackCoroutine;

    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureRuntimeUi();
        HidePopup();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static JigsawMinigameController GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;

        GameObject controllerObject = new GameObject("JigsawMinigameController");
        return controllerObject.AddComponent<JigsawMinigameController>();
    }

    public bool ShowJigsaw(IHealthCheckListener listener, Texture puzzleTexture, string eventTitle = "")
    {
        if (isPlaying)
            return false;

        if (listener == null || puzzleTexture == null)
        {
            Debug.LogWarning("JigsawMinigameController: listener atau puzzle texture belum tersedia.");
            return false;
        }

        EnsureRuntimeUi();
        EnsureEventSystem();

        if (popupRoot == null || gridContainer == null)
        {
            Debug.LogWarning("JigsawMinigameController: UI jigsaw belum lengkap.");
            return false;
        }

        currentListener = listener;
        selectedPiece = null;
        isAnimating = false;

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(eventTitle) ? "Susun Puzzle" : eventTitle;

        BuildGrid(puzzleTexture);

        timeRemaining = timeLimit;
        UpdateTimerUI();

        popupRoot.SetActive(true);
        isPlaying = true;
        CoroutineHelper.StopAndStart(this, ref timerCoroutine, TimerRoutine());
        return true;
    }

    public void OnPieceClicked(JigsawPiece clicked)
    {
        if (!isPlaying || isAnimating || clicked == null)
            return;

        if (selectedPiece == null)
        {
            selectedPiece = clicked;
            selectedPiece.SetHighlighted(true);
            return;
        }

        if (selectedPiece == clicked)
        {
            selectedPiece.SetHighlighted(false);
            selectedPiece = null;
            return;
        }

        selectedPiece.SetHighlighted(false);
        SwapPieces(selectedPiece, clicked);
        selectedPiece = null;

        if (IsSolved())
            CompleteWithSuccess();
    }

    private void BuildGrid(Texture texture)
    {
        ClearGrid();

        int safeGridSize = Mathf.Max(2, gridSize);
        int totalPieces = safeGridSize * safeGridSize;
        uvRects = ComputeUvRects(safeGridSize);
        int[] shuffledIndices = GenerateShuffledIndices(totalPieces);
        pieces = new JigsawPiece[totalPieces];

        ConfigureGridLayout(safeGridSize);

        for (int i = 0; i < totalPieces; i++)
        {
            GameObject tileObject = CreateTileObject();
            tileObject.transform.SetParent(gridContainer, false);

            JigsawPiece piece = tileObject.GetComponent<JigsawPiece>();
            if (piece == null)
                piece = tileObject.AddComponent<JigsawPiece>();

            piece.Setup(i, shuffledIndices[i], texture, uvRects[shuffledIndices[i]]);
            pieces[i] = piece;
        }
    }

    private void ConfigureGridLayout(int safeGridSize)
    {
        GridLayoutGroup layout = gridContainer.GetComponent<GridLayoutGroup>();
        if (layout == null)
            layout = gridContainer.gameObject.AddComponent<GridLayoutGroup>();

        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = safeGridSize;
        layout.cellSize = new Vector2(tileSize, tileSize);
        layout.spacing = new Vector2(tileSpacing, tileSpacing);
        layout.childAlignment = TextAnchor.MiddleCenter;

        RectTransform gridRect = gridContainer as RectTransform;
        if (gridRect != null)
        {
            float side = safeGridSize * tileSize + (safeGridSize - 1) * tileSpacing;
            gridRect.sizeDelta = new Vector2(side, side);
        }
    }

    private GameObject CreateTileObject()
    {
        if (tilePrefab != null)
            return Instantiate(tilePrefab);

        GameObject tileObject = new GameObject("JigsawTile", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage), typeof(JigsawPiece));
        RawImage image = tileObject.GetComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = true;
        return tileObject;
    }

    private void ClearGrid()
    {
        if (gridContainer == null)
            return;

        for (int i = gridContainer.childCount - 1; i >= 0; i--)
            Destroy(gridContainer.GetChild(i).gameObject);
    }

    private Rect[] ComputeUvRects(int safeGridSize)
    {
        float size = 1f / safeGridSize;
        Rect[] rects = new Rect[safeGridSize * safeGridSize];

        for (int row = 0; row < safeGridSize; row++)
        {
            for (int col = 0; col < safeGridSize; col++)
            {
                int index = row * safeGridSize + col;
                float u = col * size;
                float v = (safeGridSize - 1 - row) * size;
                rects[index] = new Rect(u, v, size, size);
            }
        }

        return rects;
    }

    private int[] GenerateShuffledIndices(int count)
    {
        int[] indices = new int[count];
        for (int i = 0; i < count; i++)
            indices[i] = i;

        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = indices[i];
            indices[i] = indices[j];
            indices[j] = temp;
        }

        if (IsAlreadySolved(indices))
        {
            int temp = indices[0];
            indices[0] = indices[1];
            indices[1] = temp;
        }

        return indices;
    }

    private bool IsAlreadySolved(int[] indices)
    {
        for (int i = 0; i < indices.Length; i++)
        {
            if (indices[i] != i)
                return false;
        }

        return true;
    }

    private void SwapPieces(JigsawPiece a, JigsawPiece b)
    {
        Rect uvA = a.CurrentUvRect;
        int indexA = a.CurrentIndex;

        a.SetCurrentTile(b.CurrentIndex, b.CurrentUvRect);
        b.SetCurrentTile(indexA, uvA);

        CoroutineHelper.StopAndStart(this, ref swapFeedbackCoroutine, SwapFeedbackRoutine(a.transform, b.transform));
    }

    private IEnumerator SwapFeedbackRoutine(Transform first, Transform second)
    {
        isAnimating = true;

        Vector3 startScaleA = first.localScale;
        Vector3 startScaleB = second.localScale;
        Vector3 peakScale = Vector3.one * 1.08f;
        float elapsed = 0f;

        while (elapsed < swapDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / swapDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            Vector3 scale = Vector3.Lerp(Vector3.one, peakScale, pulse);

            first.localScale = scale;
            second.localScale = scale;
            yield return null;
        }

        first.localScale = startScaleA;
        second.localScale = startScaleB;
        isAnimating = false;
    }

    private bool IsSolved()
    {
        if (pieces == null || pieces.Length == 0)
            return false;

        foreach (JigsawPiece piece in pieces)
        {
            if (piece == null || !piece.IsInCorrectPosition)
                return false;
        }

        return true;
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
        timerText.color = timeRemaining <= GameConstants.JigsawMinigame.WarningThreshold ? warningTimerColor : normalTimerColor;
    }

    private void CompleteWithSuccess()
    {
        if (!isPlaying)
            return;

        FinishMinigame(true);
    }

    private void CompleteWithFailure()
    {
        if (!isPlaying)
            return;

        FinishMinigame(false);
    }

    private void FinishMinigame(bool success)
    {
        isPlaying = false;
        isAnimating = false;
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        CoroutineHelper.StopSafe(this, ref swapFeedbackCoroutine);
        HidePopup();

        IHealthCheckListener listener = currentListener;
        currentListener = null;
        selectedPiece = null;

        if (success)
        {
            GameLog.Info("JigsawMinigame: Berhasil.");
            listener?.OnHealthCheckSuccess();
        }
        else
        {
            GameLog.Info("JigsawMinigame: Gagal.");
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

        GameObject canvasObject = new GameObject("JigsawMinigameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        popupRoot = canvasObject;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

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
        panelRect.sizeDelta = new Vector2(560f, 650f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.22f, 0.12f, 0.96f);
        panelImage.raycastTarget = true;

        titleText = CreateText(panelObject.transform, "TitleText", new Vector2(0f, 270f), new Vector2(500f, 50f), 28f, TextAlignmentOptions.Center);
        timerText = CreateText(panelObject.transform, "TimerText", new Vector2(0f, 220f), new Vector2(160f, 48f), 34f, TextAlignmentOptions.Center);

        GameObject gridObject = new GameObject("GridContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(panelObject.transform, false);
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0f, -35f);
        gridContainer = gridObject.transform;

        closeButton = CreateCloseButton(panelObject.transform);
        ButtonHelper.SetSingleListener(closeButton, CompleteWithFailure);
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
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-18f, -18f);
        rect.sizeDelta = new Vector2(44f, 44f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.95f, 0.72f, 0.20f, 1f);

        TextMeshProUGUI label = CreateText(buttonObject.transform, "Label", Vector2.zero, new Vector2(44f, 44f), 24f, TextAlignmentOptions.Center);
        label.text = "X";
        label.color = new Color(0.08f, 0.16f, 0.08f, 1f);

        return buttonObject.GetComponent<Button>();
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }
}
