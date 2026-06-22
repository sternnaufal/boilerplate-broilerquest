using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PipelinePuzzleController : Singleton<PipelinePuzzleController>, IHealthCheckListener
{
    [System.Flags]
    private enum Dir { None = 0, Up = 1, Down = 2, Left = 4, Right = 8 }

    private struct PipeDef
    {
        public PipeType Type;
        public Dir[] Rotations;
    }

    public enum PipeType { L, Straight, T }

    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform gridContainer;
    [SerializeField] private Button pipeLButton;
    [SerializeField] private Button pipeStraightButton;
    [SerializeField] private Button pipeTButton;

    [Header("Settings")]
    [SerializeField] private float timeLimit = GameConstants.PipelinePuzzle.TimeLimit;
    [SerializeField] private int gridSize = GameConstants.PipelinePuzzle.GridSize;

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.25f, 0.15f);

    private static readonly Dictionary<PipeType, PipeDef> PipeDefs = new()
    {
        [PipeType.L] = new PipeDef
        {
            Type = PipeType.L,
            Rotations = new[] { Dir.Up | Dir.Right, Dir.Right | Dir.Down, Dir.Down | Dir.Left, Dir.Left | Dir.Up }
        },
        [PipeType.Straight] = new PipeDef
        {
            Type = PipeType.Straight,
            Rotations = new[] { Dir.Up | Dir.Down, Dir.Left | Dir.Right }
        },
        [PipeType.T] = new PipeDef
        {
            Type = PipeType.T,
            Rotations = new[] { Dir.Up | Dir.Right | Dir.Down, Dir.Right | Dir.Down | Dir.Left, Dir.Down | Dir.Left | Dir.Up, Dir.Left | Dir.Up | Dir.Right }
        },
    };

    private IHealthCheckListener currentListener;
    private float timeRemaining;
    private bool isPlaying;
    private Coroutine timerCoroutine;

    private PipeType?[,] grid;
    private int[,] rotations;
    private PipeType selectedPipeType;
    private Image[] cellImages;
    private bool hasWon;

    public bool IsPlaying => isPlaying;

    protected override void Awake()
    {
        base.Awake();
        EnsureRuntimeUi();
        HidePopup();
    }

    public bool ShowPuzzle(IHealthCheckListener caller)
    {
        if (IsPlaying) return false;

        currentListener = caller;
        timeRemaining = timeLimit;
        isPlaying = true;
        hasWon = false;

        grid = new PipeType?[gridSize, gridSize];
        rotations = new int[gridSize, gridSize];
        selectedPipeType = PipeType.L;

        ClearGridVisuals();
        ShowPopup();
        UpdateTimerUI();

        if (titleText != null)
            titleText.text = "Pipeline Pipa";

        UpdateSelectedHighlight();

        SetupButton(pipeLButton, PipeType.L);
        SetupButton(pipeStraightButton, PipeType.Straight);
        SetupButton(pipeTButton, PipeType.T);

        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        timerCoroutine = StartCoroutine(TimerRoutine());

        return true;
    }

    public void OnPuzzleCompleted()
    {
        if (!isPlaying || hasWon) return;
        CompleteWithSuccess();
    }

    private void ClearGridVisuals()
    {
        if (cellImages == null || gridContainer == null) return;
        foreach (Image img in cellImages)
        {
            if (img != null)
                img.color = new Color(0.15f, 0.35f, 0.2f, 1f);
        }
    }

    private void SetupButton(Button button, PipeType type)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            selectedPipeType = type;
            UpdateSelectedHighlight();
        });
    }

    private void UpdateSelectedHighlight()
    {
        SetButtonHighlight(pipeLButton, selectedPipeType == PipeType.L);
        SetButtonHighlight(pipeStraightButton, selectedPipeType == PipeType.Straight);
        SetButtonHighlight(pipeTButton, selectedPipeType == PipeType.T);
    }

    private static void SetButtonHighlight(Button button, bool active)
    {
        if (button == null) return;
        Image img = button.GetComponent<Image>();
        if (img != null)
            img.color = active ? new Color(0.5f, 0.8f, 0.5f, 1f) : new Color(0.3f, 0.5f, 0.35f, 1f);
    }

    public void OnCellClicked(int index)
    {
        if (!isPlaying || hasWon) return;

        int row = index / gridSize;
        int col = index % gridSize;

        if (grid[row, col] == null)
        {
            grid[row, col] = selectedPipeType;
            rotations[row, col] = 0;
        }
        else
        {
            PipeDef def = PipeDefs[grid[row, col].Value];
            rotations[row, col] = (rotations[row, col] + 1) % def.Rotations.Length;
        }

        UpdateCellVisual(row, col);
        CheckForWin();
    }

    private void UpdateCellVisual(int row, int col)
    {
        int index = row * gridSize + col;
        if (cellImages == null || index >= cellImages.Length || cellImages[index] == null) return;

        if (grid[row, col] == null)
        {
            cellImages[index].color = new Color(0.15f, 0.35f, 0.2f, 1f);
            return;
        }

        PipeType type = grid[row, col].Value;
        Dir dirs = PipeDefs[type].Rotations[rotations[row, col]];

        int connectedCount = CountBits(dirs);
        cellImages[index].color = connectedCount == 4 ? new Color(0.6f, 0.8f, 0.4f, 1f) :
                                     connectedCount == 3 ? new Color(0.5f, 0.7f, 0.3f, 1f) :
                                     connectedCount == 2 ? new Color(0.4f, 0.6f, 0.25f, 1f) :
                                     new Color(0.3f, 0.5f, 0.2f, 1f);
    }

    private static int CountBits(Dir dirs)
    {
        int count = 0;
        if ((dirs & Dir.Up) != 0) count++;
        if ((dirs & Dir.Down) != 0) count++;
        if ((dirs & Dir.Left) != 0) count++;
        if ((dirs & Dir.Right) != 0) count++;
        return count;
    }

    private void CheckForWin()
    {
        if (grid == null) return;
        hasWon = BfsCheckPath();
        if (hasWon)
            CompleteWithSuccess();
    }

    private bool BfsCheckPath()
    {
        if (grid[0, 0] == null || grid[gridSize - 1, gridSize - 1] == null) return false;

        PipeDef srcDef = PipeDefs[grid[0, 0].Value];
        Dir srcDirs = srcDef.Rotations[rotations[0, 0]];
        if ((srcDirs & Dir.Up) == 0 && (srcDirs & Dir.Left) == 0) return false;

        PipeDef dstDef = PipeDefs[grid[gridSize - 1, gridSize - 1].Value];
        Dir dstDirs = dstDef.Rotations[rotations[gridSize - 1, gridSize - 1]];
        if ((dstDirs & Dir.Down) == 0 && (dstDirs & Dir.Right) == 0) return false;

        bool[,] visited = new bool[gridSize, gridSize];
        Queue<(int, int)> queue = new();
        queue.Enqueue((0, 0));
        visited[0, 0] = true;

        while (queue.Count > 0)
        {
            (int r, int c) = queue.Dequeue();

            if (r == gridSize - 1 && c == gridSize - 1)
                return true;

            Dir curDirs = PipeDefs[grid[r, c].Value].Rotations[rotations[r, c]];

            TryEnqueue(r, c, r - 1, c, Dir.Up, Dir.Down, visited, queue, curDirs);
            TryEnqueue(r, c, r + 1, c, Dir.Down, Dir.Up, visited, queue, curDirs);
            TryEnqueue(r, c, r, c - 1, Dir.Left, Dir.Right, visited, queue, curDirs);
            TryEnqueue(r, c, r, c + 1, Dir.Right, Dir.Left, visited, queue, curDirs);
        }

        return false;
    }

    private void TryEnqueue(int fromR, int fromC, int toR, int toC, Dir fromDir, Dir toDir, bool[,] visited, Queue<(int, int)> queue, Dir curDirs)
    {
        if (toR < 0 || toR >= gridSize || toC < 0 || toC >= gridSize) return;
        if (visited[toR, toC]) return;
        if (grid[toR, toC] == null) return;
        if ((curDirs & fromDir) == 0) return;

        Dir neighborDirs = PipeDefs[grid[toR, toC].Value].Rotations[rotations[toR, toC]];
        if ((neighborDirs & toDir) == 0) return;

        visited[toR, toC] = true;
        queue.Enqueue((toR, toC));
    }

    private void HighlightWinningPath()
    {
        if (cellImages == null || grid == null) return;
        bool[,] visited = new bool[gridSize, gridSize];
        Queue<(int, int)> queue = new();
        Dictionary<(int, int), (int, int)> parent = new();
        queue.Enqueue((0, 0));
        visited[0, 0] = true;

        while (queue.Count > 0)
        {
            (int r, int c) = queue.Dequeue();
            if (r == gridSize - 1 && c == gridSize - 1) break;

            Dir curDirs = PipeDefs[grid[r, c].Value].Rotations[rotations[r, c]];

            TryEnqueuePath(r, c, r - 1, c, Dir.Up, Dir.Down, visited, queue, parent, curDirs);
            TryEnqueuePath(r, c, r + 1, c, Dir.Down, Dir.Up, visited, queue, parent, curDirs);
            TryEnqueuePath(r, c, r, c - 1, Dir.Left, Dir.Right, visited, queue, parent, curDirs);
            TryEnqueuePath(r, c, r, c + 1, Dir.Right, Dir.Left, visited, queue, parent, curDirs);
        }

        (int, int) cur = (gridSize - 1, gridSize - 1);
        while (parent.ContainsKey(cur))
        {
            int idx = cur.Item1 * gridSize + cur.Item2;
            if (idx < cellImages.Length && cellImages[idx] != null)
                cellImages[idx].color = new Color(0.3f, 0.9f, 0.5f, 1f);
            cur = parent[cur];
        }
        int srcIdx = 0;
        if (srcIdx < cellImages.Length && cellImages[srcIdx] != null)
            cellImages[srcIdx].color = new Color(0.3f, 0.9f, 0.5f, 1f);
    }

    private void TryEnqueuePath(int fromR, int fromC, int toR, int toC, Dir fromDir, Dir toDir, bool[,] visited, Queue<(int, int)> queue, Dictionary<(int, int), (int, int)> parent, Dir curDirs)
    {
        if (toR < 0 || toR >= gridSize || toC < 0 || toC >= gridSize) return;
        if (visited[toR, toC]) return;
        if (grid[toR, toC] == null) return;
        if ((curDirs & fromDir) == 0) return;

        Dir neighborDirs = PipeDefs[grid[toR, toC].Value].Rotations[rotations[toR, toC]];
        if ((neighborDirs & toDir) == 0) return;

        visited[toR, toC] = true;
        parent[(toR, toC)] = (fromR, fromC);
        queue.Enqueue((toR, toC));
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
        timerText.color = timeRemaining <= GameConstants.PipelinePuzzle.WarningThreshold ? warningTimerColor : normalTimerColor;
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
        hasWon = success;
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        HidePopup();

        IHealthCheckListener listener = currentListener;
        currentListener = null;

        if (success)
            listener?.OnHealthCheckSuccess();
        else
            listener?.OnHealthCheckFailure();
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
        if (popupRoot != null && timerText != null && gridContainer != null)
            return;

        GameObject existingCanvas = GameObject.Find("PipelinePuzzleCanvas");
        if (existingCanvas != null)
            Destroy(existingCanvas);

        GameObject canvasObject = new GameObject("PipelinePuzzleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        GameObject gridObject = new GameObject("GridContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        gridObject.transform.SetParent(panelObject.transform, false);
        RectTransform gridRect = gridObject.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.anchoredPosition = new Vector2(0f, -35f);
        gridContainer = gridObject.transform;

        GridLayoutGroup gridLayout = gridObject.GetComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(100f, 100f);
        gridLayout.spacing = new Vector2(8f, 8f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = gridSize;

        int totalCells = gridSize * gridSize;
        cellImages = new Image[totalCells];

        for (int i = 0; i < totalCells; i++)
        {
            GameObject cell = new GameObject($"Cell_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            cell.transform.SetParent(gridContainer, false);
            Image cellImage = cell.GetComponent<Image>();
            cellImage.color = new Color(0.15f, 0.35f, 0.2f, 1f);
            cellImage.raycastTarget = true;

            int capturedIndex = i;
            cell.AddComponent<Button>().onClick.AddListener(() => OnCellClicked(capturedIndex));

            cellImages[i] = cellImage;
        }

        GameObject inventoryObject = new GameObject("PipeInventory", typeof(RectTransform));
        inventoryObject.transform.SetParent(panelObject.transform, false);
        RectTransform invRect = inventoryObject.GetComponent<RectTransform>();
        invRect.anchorMin = new Vector2(0.5f, 0);
        invRect.anchorMax = new Vector2(0.5f, 0);
        invRect.pivot = new Vector2(0.5f, 0);
        invRect.anchoredPosition = new Vector2(0f, 20f);
        invRect.sizeDelta = new Vector2(460f, 80f);

        pipeLButton = CreatePipeButton(inventoryObject.transform, "PipeL", new Vector2(-150f, 0f), "L");
        pipeStraightButton = CreatePipeButton(inventoryObject.transform, "PipeStraight", new Vector2(0f, 0f), "I");
        pipeTButton = CreatePipeButton(inventoryObject.transform, "PipeT", new Vector2(150f, 0f), "T");
    }

    private Button CreatePipeButton(Transform parent, string name, Vector2 position, string label)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(80f, 80f);
        rect.anchoredPosition = position;
        Image img = obj.GetComponent<Image>();
        img.color = new Color(0.3f, 0.5f, 0.35f, 1f);
        img.raycastTarget = true;

        CreateText(obj.transform, "Label", Vector2.zero, new Vector2(80f, 80f), 28f, TextAlignmentOptions.Center).text = label;
        return obj.GetComponent<Button>();
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

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public void OnHealthCheckSuccess() { }
    public void OnHealthCheckFailure() { }
}
