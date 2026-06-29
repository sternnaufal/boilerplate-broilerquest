using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdjuster : MonoBehaviour
{
    [SerializeField] private float marginPercent = 0.03f;
    [SerializeField] private bool fixCanvasScalerMatchWidth = true;

    private RectTransform rectTransform;
    private Rect lastSafeArea;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        if (fixCanvasScalerMatchWidth && canvas != null)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0f;
            }
        }

        Apply();
    }

    private void Apply()
    {
        Rect safeArea = Screen.safeArea;
        if (safeArea == lastSafeArea) return;
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
