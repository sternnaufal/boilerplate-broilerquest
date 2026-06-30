using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WireNode : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
    public int PairId { get; private set; }
    public bool IsLeft { get; private set; }
    public bool IsConnected { get; private set; }
    public Color WireColor { get; private set; }

    private Image nodeImage;
    private RectTransform rectTransform;
    private Color originalColor;

    public RectTransform RectTransform => rectTransform;

    public void Setup(int pairId, bool isLeft, Color color)
    {
        PairId = pairId;
        IsLeft = isLeft;
        WireColor = color;
        IsConnected = false;
        originalColor = color;

        nodeImage = GetComponent<Image>();
        if (nodeImage == null)
            nodeImage = gameObject.AddComponent<Image>();

        nodeImage.sprite = GetCircleSprite();
        nodeImage.type = Image.Type.Simple;
        nodeImage.color = new Color(color.r, color.g, color.b, 0.8f);
        nodeImage.raycastTarget = true;

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();
    }

    public void SetConnected()
    {
        IsConnected = true;
        originalColor = new Color(0.3f, 0.3f, 0.3f);
        if (nodeImage != null)
            nodeImage.color = originalColor;
    }

    public bool ContainsScreenPoint(Vector2 screenPoint, Camera camera = null)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPoint, camera);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsConnected) return;
        var controller = WiringMinigameController.Instance;
        if (controller != null && IsLeft)
            controller.OnLeftNodePointerDown(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsConnected || IsLeft) return;
        var controller = WiringMinigameController.Instance;
        if (controller != null)
            controller.OnRightNodePointerEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsConnected || IsLeft) return;
        var controller = WiringMinigameController.Instance;
        if (controller != null)
            controller.OnRightNodePointerExit(this);
    }

    private static Sprite _circleSprite;
    private static Sprite GetCircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = center - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                tex.SetPixel(x, y, (dx * dx + dy * dy) <= radius * radius ? Color.white : Color.clear);
            }
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        return _circleSprite;
    }

    public void ResetNode()
    {
        IsConnected = false;
        originalColor = WireColor;
        if (nodeImage != null)
            nodeImage.color = new Color(WireColor.r, WireColor.g, WireColor.b, 0.8f);
    }
}
