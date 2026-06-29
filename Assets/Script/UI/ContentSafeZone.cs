using UnityEngine;

// Attach to a direct child of Canvas (not the Background).
// Locks this panel to 1920x1080 centered in the canvas, so content is
// never stretched or pushed into the extra space that Expand mode adds
// on non-16:9 screens (e.g. 4:3 tablets).
// On notch/punch-hole devices it falls back to safe-area anchors instead.
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class ContentSafeZone : MonoBehaviour
{
    private RectTransform rect;
    private Rect lastSafeArea;
    private int lastW, lastH;

    private void Awake() => rect = GetComponent<RectTransform>();
    private void Start() => Apply();

    private void Update()
    {
        if (Screen.width != lastW || Screen.height != lastH || Screen.safeArea != lastSafeArea)
            Apply();
    }

    private void Apply()
    {
        lastW = Screen.width;
        lastH = Screen.height;
        lastSafeArea = Screen.safeArea;

        Rect safe = Screen.safeArea;
        float l = safe.xMin / Screen.width;
        float r = (Screen.width - safe.xMax) / Screen.width;
        float b = safe.yMin / Screen.height;
        float t = (Screen.height - safe.yMax) / Screen.height;

        bool hasNotch = l > 0.001f || r > 0.001f || b > 0.001f || t > 0.001f;
        if (hasNotch)
        {
            rect.anchorMin = new Vector2(l, b);
            rect.anchorMax = new Vector2(1f - r, 1f - t);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(1920f, 1080f);
        }
    }
}
