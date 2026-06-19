using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WiringMinigameController : Singleton<WiringMinigameController>
{
    [Header("Visual")]
    [SerializeField] private float nodeSize = 44f;
    [SerializeField] private float nodeSpacing = 72f;
    [SerializeField] private float lineThickness = 5f;

    [Header("Wire Colors")]
    [SerializeField] private Color[] wireColors = new Color[]
    {
        new Color(1f, 0.2f, 0.2f),
        new Color(0.2f, 0.5f, 1f),
        new Color(1f, 0.9f, 0.1f),
        new Color(0.2f, 1f, 0.2f),
        new Color(1f, 0.4f, 0.8f),
        new Color(0.4f, 1f, 0.8f)
    };

    [Header("Timer Colors")]
    [SerializeField] private Color normalTimerColor = Color.white;
    [SerializeField] private Color warningTimerColor = new Color(1f, 0.25f, 0.15f);

    private IHealthCheckListener currentListener;
    private readonly List<WireNode> leftNodes = new List<WireNode>();
    private readonly List<WireNode> rightNodes = new List<WireNode>();
    private readonly List<GameObject> activeLines = new List<GameObject>();

    private WireNode dragSource;
    private WireNode hoveredRightNode;
    private bool isDragging;
    private bool isPlaying;
    private bool isProcessing;
    private GameObject tempLine;
    private int connectedCount;
    private int currentPairCount;
    private float timeRemaining;
    private Coroutine timerCoroutine;

    private GameObject popupRoot;
    private Transform wireContainer;
    private TextMeshProUGUI timerText;
    private TextMeshProUGUI titleText;

    public bool IsPlaying => isPlaying;

    protected override void Awake()
    {
        base.Awake();
        EnsureRuntimeUi();
        HidePopup();
    }

    private void Update()
    {
        if (!isDragging || dragSource == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        UpdateTempLine(mousePos);

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            OnDragEnd();
    }

    public bool ShowWiring(IHealthCheckListener listener, int pairCount, float timeLimitSec, string eventTitle = "")
    {
        if (isPlaying)
            return false;

        if (listener == null)
        {
            Debug.LogWarning("WiringMinigameController: listener is null.");
            return false;
        }

        if (wireColors == null || wireColors.Length < pairCount)
        {
            Debug.LogWarning("WiringMinigameController: wireColors tidak cukup (" + pairCount + " warna dibutuhkan, hanya " + (wireColors?.Length ?? 0) + ").");
            return false;
        }

        EnsureRuntimeUi();
        EnsureEventSystem();

        if (popupRoot == null || wireContainer == null)
        {
            Debug.LogWarning("WiringMinigameController: UI belum lengkap.");
            return false;
        }

        currentListener = listener;
        currentPairCount = pairCount;
        timeRemaining = timeLimitSec;
        isPlaying = true;
        isProcessing = false;
        isDragging = false;
        connectedCount = 0;
        dragSource = null;
        hoveredRightNode = null;

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(eventTitle) ? "Hubungkan Kabel" : eventTitle;

        ShowPopup();
        BuildWires();
        UpdateTimerUI();

        CoroutineHelper.StopAndStart(this, ref timerCoroutine, TimerRoutine());
        return true;
    }

    public void OnLeftNodePointerDown(WireNode node)
    {
        if (!isPlaying || isProcessing || node == null || node.IsConnected)
            return;

        dragSource = node;
        isDragging = true;
        tempLine = CreateLineObject("TempLine", Color.white);
        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawPieceClick();
    }

    public void OnRightNodePointerEnter(WireNode node)
    {
        if (!isDragging || node == null || node.IsConnected)
            return;

        hoveredRightNode = node;
    }

    public void OnRightNodePointerExit(WireNode node)
    {
        if (hoveredRightNode == node)
            hoveredRightNode = null;
    }

    private void OnDragEnd()
    {
        if (!isDragging || dragSource == null)
            return;

        isDragging = false;

        if (hoveredRightNode != null && !hoveredRightNode.IsConnected)
        {
            if (dragSource.PairId == hoveredRightNode.PairId)
                StartCoroutine(OnMatchRoutine(dragSource, hoveredRightNode));
            else
                StartCoroutine(OnMismatchRoutine(dragSource, hoveredRightNode));
        }

        DestroyTempLine();
        dragSource = null;
        hoveredRightNode = null;
    }

    private void UpdateTempLine(Vector2 mousePos)
    {
        if (tempLine == null || dragSource == null || wireContainer == null)
            return;

        Vector3 leftWorld = dragSource.RectTransform.position;
        Vector3 localLeft = wireContainer.InverseTransformPoint(leftWorld);
        Vector3 localMouse = wireContainer.InverseTransformPoint(mousePos);

        Vector3 dir = localMouse - localLeft;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        RectTransform lineRect = tempLine.GetComponent<RectTransform>();
        lineRect.sizeDelta = new Vector2(Mathf.Max(distance, 2f), lineThickness);
        lineRect.localPosition = localLeft;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private IEnumerator OnMatchRoutine(WireNode left, WireNode right)
    {
        isPlaying = false;
        isProcessing = true;

        left.SetConnected();
        right.SetConnected();

        Color lineColor = left.WireColor;
        DrawConnectionLine(left, right, lineColor);
        connectedCount++;

        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawPieceSwap();

        yield return new WaitForSecondsRealtime(0.2f);

        isProcessing = false;

        if (connectedCount >= currentPairCount)
            CompleteWithSuccess();
        else
            isPlaying = true;
    }

    private IEnumerator OnMismatchRoutine(WireNode left, WireNode right)
    {
        isPlaying = false;
        isProcessing = true;

        Image leftImg = left.GetComponent<Image>();
        Image rightImg = right.GetComponent<Image>();
        if (leftImg != null) leftImg.color = Color.red;
        if (rightImg != null) rightImg.color = Color.red;
        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawPieceClick();

        yield return new WaitForSecondsRealtime(0.15f);

        if (leftImg != null) leftImg.color = left.WireColor;
        if (rightImg != null) rightImg.color = right.WireColor;

        isProcessing = false;
        isPlaying = true;
    }

    private void DrawConnectionLine(WireNode left, WireNode right, Color color)
    {
        if (wireContainer == null) return;

        GameObject lineObj = CreateLineObject("WireLine", color);
        activeLines.Add(lineObj);

        RectTransform lineRect = lineObj.GetComponent<RectTransform>();

        Vector3 leftWorld = left.RectTransform.position;
        Vector3 rightWorld = right.RectTransform.position;
        Vector3 localLeft = wireContainer.InverseTransformPoint(leftWorld);
        Vector3 localRight = wireContainer.InverseTransformPoint(rightWorld);

        Vector3 dir = localRight - localLeft;
        float distance = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        lineRect.sizeDelta = new Vector2(Mathf.Max(distance, 2f), lineThickness);
        lineRect.localPosition = localLeft;
        lineRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private GameObject CreateLineObject(string name, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(wireContainer, false);

        Image img = obj.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);

        return obj;
    }

    private void DestroyTempLine()
    {
        if (tempLine != null)
        {
            Destroy(tempLine);
            tempLine = null;
        }
    }

    private void BuildWires()
    {
        ClearWires();

        int[] leftOrder = new int[currentPairCount];
        int[] rightOrder = new int[currentPairCount];
        for (int i = 0; i < currentPairCount; i++)
        {
            leftOrder[i] = i;
            rightOrder[i] = i;
        }

        for (int i = currentPairCount - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int tmp = leftOrder[i];
            leftOrder[i] = leftOrder[j];
            leftOrder[j] = tmp;

            j = Random.Range(0, i + 1);
            tmp = rightOrder[i];
            rightOrder[i] = rightOrder[j];
            rightOrder[j] = tmp;
        }

        float totalHeight = (currentPairCount - 1) * nodeSpacing;
        float startY = totalHeight / 2f;
        float leftX = -190f;
        float rightX = 190f;

        for (int i = 0; i < currentPairCount; i++)
        {
            float y = startY - i * nodeSpacing;

            GameObject leftObj = CreateNodeObject("LeftNode_" + i, new Vector2(leftX, y));
            WireNode leftNode = leftObj.AddComponent<WireNode>();
            leftNode.Setup(leftOrder[i], true, wireColors[leftOrder[i]]);
            leftNodes.Add(leftNode);

            GameObject rightObj = CreateNodeObject("RightNode_" + i, new Vector2(rightX, y));
            WireNode rightNode = rightObj.AddComponent<WireNode>();
            rightNode.Setup(rightOrder[i], false, wireColors[rightOrder[i]]);
            rightNodes.Add(rightNode);
        }
    }

    private GameObject CreateNodeObject(string name, Vector2 anchoredPos)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(wireContainer, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(nodeSize, nodeSize);
        rt.anchoredPosition = anchoredPos;
        return obj;
    }

    private void ClearWires()
    {
        if (wireContainer != null)
            for (int i = wireContainer.childCount - 1; i >= 0; i--)
                Destroy(wireContainer.GetChild(i).gameObject);

        leftNodes.Clear();
        rightNodes.Clear();
        activeLines.Clear();
        DestroyTempLine();
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
        timerText.color = timeRemaining <= GameConstants.WiringMinigame.WarningThreshold ? warningTimerColor : normalTimerColor;
    }

    private void CompleteWithSuccess()
    {
        if (!isPlaying) return;
        isPlaying = false;

        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawComplete();
        HealthCheckResultOverlay.ShowSuccess();
        CameraShake.Trigger(0.15f, 0.05f);
        FinishMinigame(true);
    }

    private void CompleteWithFailure()
    {
        if (!isPlaying) return;
        isPlaying = false;

        CameraShake.Trigger();
        if (SFXManager.Instance != null) SFXManager.Instance.PlayJigsawFail();
        HealthCheckResultOverlay.ShowFail();
        FinishMinigame(false);
    }

    private void FinishMinigame(bool success)
    {
        isDragging = false;
        isProcessing = false;
        CoroutineHelper.StopSafe(this, ref timerCoroutine);
        HidePopup();

        IHealthCheckListener listener = currentListener;
        currentListener = null;
        dragSource = null;
        hoveredRightNode = null;
        DestroyTempLine();

        if (success)
        {
            GameLog.Info("WiringMinigame: Berhasil.");
            listener?.OnHealthCheckSuccess();
        }
        else
        {
            GameLog.Info("WiringMinigame: Gagal.");
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
        if (popupRoot != null && timerText != null && wireContainer != null)
            return;

        GameObject existingCanvas = GameObject.Find("WiringMinigameCanvas");
        if (existingCanvas != null)
            Destroy(existingCanvas);

        GameObject canvasObject = new GameObject("WiringMinigameCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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

        GameObject wireObj = new GameObject("WireContainer", typeof(RectTransform));
        wireObj.transform.SetParent(panelObject.transform, false);
        RectTransform wireRect = wireObj.GetComponent<RectTransform>();
        wireRect.anchorMin = new Vector2(0.5f, 0.5f);
        wireRect.anchorMax = new Vector2(0.5f, 0.5f);
        wireRect.pivot = new Vector2(0.5f, 0.5f);
        wireRect.anchoredPosition = new Vector2(0f, -35f);
        wireRect.sizeDelta = new Vector2(500f, 420f);
        wireContainer = wireObj.transform;
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

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
