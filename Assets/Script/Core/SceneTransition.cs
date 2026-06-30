using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransition : Singleton<SceneTransition>
{
    [Header("Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private AnimationCurve fadeCurve;

    [Header("Prefab (optional)")]
    [SerializeField] private GameObject transitionCanvasPrefab;

    private CanvasGroup overlay;
    private bool isTransitioning;
    private float transitionElapsed;

    protected override bool PersistAcrossScenes => true;

    protected override void Awake()
    {
        base.Awake();
        if (fadeCurve == null || fadeCurve.keys.Length == 0)
            fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        CreateOverlay();
    }

    private void CreateOverlay()
    {
        if (transitionCanvasPrefab != null)
        {
            GameObject inst = Instantiate(transitionCanvasPrefab, transform);
            overlay = inst.GetComponentInChildren<CanvasGroup>();
            if (overlay == null)
            {
                Image img = inst.GetComponentInChildren<Image>();
                if (img != null)
                    overlay = img.gameObject.AddComponent<CanvasGroup>();
            }
            if (overlay != null)
            {
                overlay.alpha = 0f;
                overlay.blocksRaycasts = false;
            }
            return;
        }

        GameObject go = new GameObject("SceneTransitionCanvas");
        go.transform.SetParent(transform, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        GameObject panel = new GameObject("TransitionOverlay");
        panel.transform.SetParent(go.transform, false);

        Image image = panel.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        overlay = panel.AddComponent<CanvasGroup>();
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
    }

    private void Update()
    {
        if (!isTransitioning) { transitionElapsed = 0f; return; }
        transitionElapsed += Time.unscaledDeltaTime;
        if (transitionElapsed > 5f)
            ForceComplete();
    }

    public void ForceComplete()
    {
        StopAllCoroutines();
        if (overlay != null)
        {
            overlay.alpha = 0f;
            overlay.blocksRaycasts = false;
        }
        isTransitioning = false;
        transitionElapsed = 0f;
    }

    public void LoadScene(string sceneName, Action onComplete = null)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionRoutine(sceneName, onComplete));
    }

    private IEnumerator TransitionRoutine(string sceneName, Action onComplete)
    {
        isTransitioning = true;
        overlay.blocksRaycasts = true;

        yield return Fade(1f);

        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        while (!async.isDone)
            yield return null;

        yield return null;

        yield return Fade(0f);

        overlay.blocksRaycasts = false;
        isTransitioning = false;
        onComplete?.Invoke();
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = overlay.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            overlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, fadeCurve.Evaluate(t));
            yield return null;
        }

        overlay.alpha = targetAlpha;
    }
}
