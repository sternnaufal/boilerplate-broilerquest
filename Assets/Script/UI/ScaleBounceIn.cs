using System.Collections;
using UnityEngine;

public class ScaleBounceIn : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float startScale = 0f;
    [SerializeField] private float targetScale = 1f;

    [Header("Settings")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private Coroutine animCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (curve == null || curve.keys.Length == 0)
        {
            curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 5f),
                new Keyframe(0.75f, 1.15f, 1.5f, -3f),
                new Keyframe(1f, 1f, 0f, 0f)
            );
        }
    }

    private void OnEnable()
    {
        if (playOnEnable)
            Play();
    }

    public void Play()
    {
        if (animCoroutine != null)
            StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        rectTransform.localScale = Vector3.one * startScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = curve.Evaluate(elapsed / duration);
            rectTransform.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, targetScale, t);
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }

        rectTransform.localScale = Vector3.one * targetScale;
        animCoroutine = null;
    }
}
