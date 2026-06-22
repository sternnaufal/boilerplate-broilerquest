using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MemoryMatchCard : MonoBehaviour, IPointerClickHandler
{
    [Header("Colors")]
    [SerializeField] private Color backColor = new Color(0.15f, 0.35f, 0.7f);
    [SerializeField] private Color frontColor = Color.white;
    [SerializeField] private Color matchedColor = new Color(0.3f, 0.85f, 0.4f);

    public int PairId { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsFlipped { get; private set; }

    private Image cardImage;
    private Sprite faceSprite;
    private Sprite cardBackSprite;
    private System.Action<MemoryMatchCard> onClickCallback;
    private bool isAnimating;
    private Coroutine flipRoutine;

    public void Setup(int pairId, Sprite faceSprite, Sprite cardBackSprite, System.Action<MemoryMatchCard> onClickCallback)
    {
        if (cardImage == null)
            cardImage = GetComponent<Image>();

        PairId = pairId;
        this.faceSprite = faceSprite;
        this.cardBackSprite = cardBackSprite;
        this.onClickCallback = onClickCallback;
        IsMatched = false;
        IsFlipped = false;
        cardImage.sprite = cardBackSprite;
        cardImage.color = backColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isAnimating || IsMatched || IsFlipped || onClickCallback == null)
            return;
        onClickCallback.Invoke(this);
    }

    public void FlipToFront()
    {
        if (IsFlipped || isAnimating)
            return;
        IsFlipped = true;
        if (flipRoutine != null)
            StopCoroutine(flipRoutine);
        flipRoutine = StartCoroutine(FlipRoutine(true));
    }

    public void FlipToBack()
    {
        if (!IsFlipped || isAnimating)
            return;
        IsFlipped = false;
        if (flipRoutine != null)
            StopCoroutine(flipRoutine);
        flipRoutine = StartCoroutine(FlipRoutine(false));
    }

    public void SetMatched()
    {
        IsMatched = true;
        cardImage.color = matchedColor;
    }

    private System.Collections.IEnumerator FlipRoutine(bool toFront)
    {
        isAnimating = true;
        float duration = 0.25f;
        float half = duration / 2f;

        RectTransform rt = (RectTransform)transform;

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / half;
            float scaleX = Mathf.Lerp(1f, 0f, t);
            rt.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }

        rt.localScale = new Vector3(0f, 1f, 1f);

        if (toFront)
        {
            cardImage.sprite = faceSprite;
            cardImage.color = frontColor;
        }
        else
        {
            cardImage.sprite = cardBackSprite;
            cardImage.color = backColor;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / half;
            float scaleX = Mathf.Lerp(0f, 1f, t);
            rt.localScale = new Vector3(scaleX, 1f, 1f);
            yield return null;
        }

        rt.localScale = Vector3.one;
        isAnimating = false;
    }
}
