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

        nodeImage.color = color;
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

    public void ResetNode()
    {
        IsConnected = false;
        originalColor = WireColor;
        if (nodeImage != null)
            nodeImage.color = WireColor;
    }
}
