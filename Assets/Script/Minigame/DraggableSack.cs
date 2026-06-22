using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Drag handler for sack items in the DragDropSack minigame.
/// Reparents to canvas root during drag so the sack renders above all panels,
/// then checks overlap with DropZone on release.
/// </summary>
public class DraggableSack : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public RectTransform dropZoneRect;
    [HideInInspector] public DragDropSackController controller;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas rootCanvas;
    private Vector2 originalAnchoredPos;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Image dropZoneImage;
    private Color dropZoneOriginalColor;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void Start()
    {
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        CacheDropZoneVisuals();
    }

    private void OnEnable()
    {
        if (rootCanvas == null)
        {
            Canvas c = GetComponentInParent<Canvas>();
            if (c != null) rootCanvas = c.rootCanvas;
        }
        CacheDropZoneVisuals();
    }

    private void CacheDropZoneVisuals()
    {
        if (dropZoneRect != null)
        {
            dropZoneImage = dropZoneRect.GetComponent<Image>();
            if (dropZoneImage != null)
                dropZoneOriginalColor = dropZoneImage.color;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (controller != null && !controller.IsPlaying) return;

        originalAnchoredPos = rectTransform.anchoredPosition;
        originalParent = rectTransform.parent;
        originalSiblingIndex = rectTransform.GetSiblingIndex();

        // Reparent to canvas root so sack renders above all panels
        rectTransform.SetParent(rootCanvas.transform, true);
        rectTransform.SetAsLastSibling();

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.85f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (controller != null && !controller.IsPlaying) return;

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }

        // Highlight drop zone when hovering over it
        if (dropZoneImage != null && dropZoneRect != null)
        {
            bool hovering = RectTransformUtility.RectangleContainsScreenPoint(
                dropZoneRect, eventData.position, eventData.pressEventCamera);
            dropZoneImage.color = hovering
                ? new Color(0.3f, 0.75f, 0.3f, 0.65f)
                : dropZoneOriginalColor;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Reset drop zone color
        if (dropZoneImage != null)
            dropZoneImage.color = dropZoneOriginalColor;

        // If game already ended during drag (e.g. timer expired), skip logic
        if (controller != null && !controller.IsPlaying)
            return;

        bool droppedOnZone = dropZoneRect != null
            && RectTransformUtility.RectangleContainsScreenPoint(
                dropZoneRect, eventData.position, eventData.pressEventCamera);

        if (droppedOnZone)
        {
            gameObject.SetActive(false);
            if (controller != null)
                controller.OnSackDropped();
        }
        else
        {
            // Snap back to original position inside kandang
            rectTransform.SetParent(originalParent, false);
            rectTransform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.anchoredPosition = originalAnchoredPos;
        }
    }
}
