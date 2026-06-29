using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdjuster : MonoBehaviour
{
    [SerializeField] private float marginPercent = 0.03f;
    [SerializeField] private bool fixCanvasScalerMatchWidth = false;
    [SerializeField] private float canvasMatchValue = 1.0f; // Default to 1.0 (Height) for landscape
    [SerializeField] private bool autoAdjustMatchMode = true; // Adaptive matching based on aspect ratio
    [SerializeField] private float referenceWidth = 1920f;
    [SerializeField] private float referenceHeight = 1080f;
    [SerializeField] private bool adjustSafeArea = true; // Set to false to only use adaptive canvas scaler matching

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        UpdateMatchMode();
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateMatchMode();
            Apply();
        }
    }

    private void UpdateMatchMode()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (canvas != null)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                if (autoAdjustMatchMode)
                {
                    float screenAspect = (float)Screen.width / Screen.height;
                    float referenceAspect = referenceWidth / referenceHeight;
                    // If screen is narrower than 16:9 (e.g. 4:3, 16:10), match Width (0.0f) to prevent overlapping.
                    // If screen is wider (e.g. 19.5:9, 21:9), match Height (1.0f) to prevent top/bottom cutoff.
                    scaler.matchWidthOrHeight = screenAspect < referenceAspect ? 0f : 1.0f;
                }
                else if (fixCanvasScalerMatchWidth)
                {
                    scaler.matchWidthOrHeight = canvasMatchValue;
                }
            }
        }
    }

    private void Apply()
    {
        if (!adjustSafeArea) return;

        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        float margin = marginPercent;

        float leftRatio = Mathf.Max(safeArea.xMin / Screen.width, margin);
        float rightRatio = Mathf.Max((Screen.width - safeArea.xMax) / Screen.width, margin);
        float bottomRatio = Mathf.Max(safeArea.yMin / Screen.height, margin);
        float topRatio = Mathf.Max((Screen.height - safeArea.yMax) / Screen.height, margin);

        rectTransform.anchorMin = new Vector2(leftRatio, bottomRatio);
        rectTransform.anchorMax = new Vector2(1f - rightRatio, 1f - topRatio);
    }
}
