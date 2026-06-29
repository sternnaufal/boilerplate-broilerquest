using System.Collections;
using UnityEngine;

public class SlideInFromTop : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float extraOffset = 20f;

    private RectTransform rectTransform;
    private Vector2 targetPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (curve == null || curve.keys.Length == 0)
        {
            curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 4f),
                new Keyframe(0.78f, 1.08f, 1f, -2f),
                new Keyframe(1f, 1f, 0f, 0f)
            );
        }
    }

    private void Start()
    {
        targetPosition = rectTransform.anchoredPosition;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        float panelHeight = rectTransform.rect.height > 0f
            ? rectTransform.rect.height
            : 74f;

        float startYOffset = panelHeight + extraOffset;
        Vector2 startPos = new Vector2(targetPosition.x, targetPosition.y + startYOffset);
        rectTransform.anchoredPosition = startPos;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = curve.Evaluate(elapsed / duration);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPosition, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
    }
}
