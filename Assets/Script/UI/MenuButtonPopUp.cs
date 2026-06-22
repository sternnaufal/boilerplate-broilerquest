using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuButtonPopUp : MonoBehaviour
{
    [Header("Pop Animation")]
    [SerializeField] private AnimationCurve curve;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float startScale = 0f;
    [SerializeField] private float targetScale = 1f;
    [SerializeField] private float staggerDelay = 0.08f;

    private List<RectTransform> buttons;

    private void Awake()
    {
        CacheButtons();
    }

    private void OnEnable()
    {
        Play();
    }

    private void CacheButtons()
    {
        buttons = new List<RectTransform>();
        foreach (Button btn in GetComponentsInChildren<Button>(true))
            buttons.Add(btn.GetComponent<RectTransform>());
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].localScale = Vector3.one * startScale;

        for (int i = 0; i < buttons.Count; i++)
        {
            StartCoroutine(AnimateOne(i));
            yield return new WaitForSecondsRealtime(staggerDelay);
        }
    }

    private IEnumerator AnimateOne(int index)
    {
        RectTransform rt = buttons[index];
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = curve.Evaluate(elapsed / duration);
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(startScale, targetScale, t);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rt.localScale = Vector3.one * targetScale;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        if (curve == null || curve.keys.Length == 0)
        {
            curve = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 4f),
                new Keyframe(0.8f, 1.12f, 1f, -2f),
                new Keyframe(1f, 1f, 0f, 0f)
            );
        }
    }
#endif
}
