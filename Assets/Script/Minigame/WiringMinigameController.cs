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

    [Header("Node Layout")]
    [SerializeField] private float cascadeOffset = 2f;

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

    [Header("UI References")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private Transform wireContainer;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI titleText;

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

        Vector2 pointerPos = Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
        UpdateTempLine(pointerPos);

        bool shouldEndDrag = false;
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            shouldEndDrag = true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            shouldEndDrag = true;

        if (shouldEndDrag)
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
            if (i > 0) y -= cascadeOffset;

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

        GameObject prefab = Resources.Load<GameObject>("WiringMinigameCanvas");
        if (prefab == null)
        {
            Debug.LogError("WiringMinigameController: WiringMinigameCanvas.prefab tidak ditemukan di Resources!");
            return;
        }

        GameObject canvasObj = Instantiate(prefab);
        canvasObj.name = "WiringMinigameCanvas";
        DontDestroyOnLoad(canvasObj);
        popupRoot = canvasObj;

        Transform panelTransform = canvasObj.transform.Find("Panel");
        if (panelTransform != null)
        {
            titleText = panelTransform.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
            timerText = panelTransform.Find("TimerText")?.GetComponent<TextMeshProUGUI>();
            wireContainer = panelTransform.Find("WireContainer");
        }
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
            return;

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }
}
