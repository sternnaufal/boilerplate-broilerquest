using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaAdjuster : MonoBehaviour
{
    [SerializeField] private bool adjustSafeArea = true;

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
        Apply();
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            Apply();
        }
    }

    private void Apply()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (!adjustSafeArea) return;

        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        float leftRatio = safeArea.xMin / Screen.width;
        float rightRatio = (Screen.width - safeArea.xMax) / Screen.width;
        float bottomRatio = safeArea.yMin / Screen.height;
        float topRatio = (Screen.height - safeArea.yMax) / Screen.height;

        // Only apply if there is actually a safe area inset (e.g. notch/punch-hole).
        // On normal screens safeArea == full screen, so ratios are 0 and we skip.
        bool hasSafeAreaInset = leftRatio > 0.001f || rightRatio > 0.001f || bottomRatio > 0.001f || topRatio > 0.001f;
        if (!hasSafeAreaInset)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            return;
        }

        rectTransform.anchorMin = new Vector2(leftRatio, bottomRatio);
        rectTransform.anchorMax = new Vector2(1f - rightRatio, 1f - topRatio);
    }
}
