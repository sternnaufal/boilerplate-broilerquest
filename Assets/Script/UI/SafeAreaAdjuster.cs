using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdjuster : MonoBehaviour
{
    [Header("Padding")]
    [SerializeField] private float edgePaddingPercent = 0.05f;
    [SerializeField] private float androidExtraBottomPercent = 0.03f;

    [Header("Responsive")]
    [SerializeField] private bool autoAdjustMatchMode = true;
    [SerializeField][Range(0f, 1f)] private float portraitMatchValue = 0f;
    [SerializeField][Range(0f, 1f)] private float landscapeMatchValue = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool logChanges = false;

    private RectTransform rectTransform;
    private CanvasScaler canvasScaler;
    private Rect lastSafeArea;
    private ScreenOrientation lastOrientation;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasScaler = GetComponentInParent<CanvasScaler>();
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.orientation != lastOrientation)
            Apply();
    }

    private void Apply()
    {
        lastSafeArea = Screen.safeArea;
        lastOrientation = Screen.orientation;

        Rect safeRect = CalculateSafeRect();
        ApplyAnchors(safeRect);
        ApplyResponsiveMatch();

        if (logChanges)
            Debug.Log($"[SafeAreaAdjuster] Applied: safeRect={safeRect}, orientation={lastOrientation}");
    }

    private Rect CalculateSafeRect()
    {
        float screenW = Screen.width;
        float screenH = Screen.height;
        Rect safe = Screen.safeArea;

        float padX = screenW * edgePaddingPercent;
        float padY = screenH * edgePaddingPercent;

        float xMin = Mathf.Max(safe.xMin, padX);
        float yMin = Mathf.Max(safe.yMin, padY);
        float xMax = Mathf.Min(safe.xMax, screenW - padX);
        float yMax = Mathf.Min(safe.yMax, screenH - padY);

#if UNITY_ANDROID && !UNITY_EDITOR
        float extraBottom = screenH * androidExtraBottomPercent;
        yMin = Mathf.Max(yMin, extraBottom);
#endif

        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private void ApplyAnchors(Rect safeRect)
    {
        float screenW = Screen.width;
        float screenH = Screen.height;

        if (screenW <= 0 || screenH <= 0)
            return;

        Vector2 anchorMin = new Vector2(safeRect.xMin / screenW, safeRect.yMin / screenH);
        Vector2 anchorMax = new Vector2(safeRect.xMax / screenW, safeRect.yMax / screenH);

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private void ApplyResponsiveMatch()
    {
        if (!autoAdjustMatchMode || canvasScaler == null)
            return;

        if (canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return;

        float aspect = (float)Screen.width / Screen.height;
        canvasScaler.matchWidthOrHeight = aspect < 1f ? portraitMatchValue : landscapeMatchValue;
    }
}
