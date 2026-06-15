using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JigsawPiece : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RawImage tileImage;
    [SerializeField] private Image highlightBorder;

    public int BoardIndex { get; private set; }
    public int CurrentIndex { get; private set; }
    public Rect CurrentUvRect => tileImage != null ? tileImage.uvRect : default;
    public bool IsInCorrectPosition => CurrentIndex == BoardIndex;

    public void Setup(int boardIndex, int currentIndex, Texture texture, Rect uvRect)
    {
        EnsureReferences();

        BoardIndex = boardIndex;
        SetCurrentTile(currentIndex, uvRect);

        if (tileImage != null)
        {
            tileImage.texture = texture;
            tileImage.raycastTarget = true;
        }

        SetHighlighted(false);
    }

    public void SetCurrentTile(int currentIndex, Rect uvRect)
    {
        CurrentIndex = currentIndex;

        if (tileImage != null)
            tileImage.uvRect = uvRect;
    }

    public void SetHighlighted(bool active)
    {
        if (highlightBorder != null)
            highlightBorder.enabled = active;

        transform.localScale = active ? Vector3.one * 1.05f : Vector3.one;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        JigsawMinigameController.Instance?.OnPieceClicked(this);
    }

    private void EnsureReferences()
    {
        if (tileImage == null)
            tileImage = GetComponent<RawImage>();

        if (tileImage == null)
            tileImage = gameObject.AddComponent<RawImage>();

        if (highlightBorder == null)
        {
            Transform existingHighlight = transform.Find("HighlightBorder");
            if (existingHighlight != null)
                highlightBorder = existingHighlight.GetComponent<Image>();
        }

        if (highlightBorder == null)
            highlightBorder = CreateHighlightBorder();
    }

    private Image CreateHighlightBorder()
    {
        GameObject borderObject = new GameObject("HighlightBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        borderObject.transform.SetParent(transform, false);

        RectTransform rect = borderObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = borderObject.GetComponent<Image>();
        image.color = new Color(1f, 0.78f, 0.16f, 0.35f);
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }
}
