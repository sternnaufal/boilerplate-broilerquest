using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FloatingFeedback : MonoBehaviour
{
    private static FloatingFeedback instance;

    [SerializeField] private float floatSpeed = 60f;
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private float lifeTime = 1.2f;
    [SerializeField] private Color coinColor = new Color(1f, 0.96f, 0.7f);
    [SerializeField] private Color feedColor = new Color(0.7f, 0.85f, 1f);

    private Transform canvasTransform;

    public static void ShowCoin(Vector2 screenPos, int amount)
    {
        EnsureInstance();
        string sign = amount >= 0 ? "+" : "";
        instance.Spawn($"{sign}{amount} Coin", screenPos, instance.coinColor);
    }

    public static void ShowFeed(Vector2 screenPos, int amount)
    {
        EnsureInstance();
        string sign = amount >= 0 ? "+" : "";
        instance.Spawn($"{sign}{amount} Pakan", screenPos, instance.feedColor);
    }

    public static void ShowText(string text, Vector2 screenPos, Color color)
    {
        EnsureInstance();
        instance.Spawn(text, screenPos, color);
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        GameObject go = new GameObject("FloatingFeedback");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<FloatingFeedback>();
        instance.EnsureCanvas();
    }

    private void EnsureCanvas()
    {
        GameObject canvasObject = new GameObject("FloatingFeedbackCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasTransform = canvasObject.transform;
    }

    private void Spawn(string text, Vector2 screenPos, Color color)
    {
        GameObject textObject = new GameObject("FloatingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(canvasTransform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = screenPos;
        rect.sizeDelta = new Vector2(300f, 40f);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 28f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LilitaOne-Regular SDF");

        StartCoroutine(AnimateRoutine(textObject, tmp));
    }

    private IEnumerator AnimateRoutine(GameObject target, TextMeshProUGUI tmp)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        Color startColor = tmp.color;
        float elapsed = 0f;

        while (elapsed < lifeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / lifeTime;

            rect.anchoredPosition += new Vector2(0f, floatSpeed * Time.unscaledDeltaTime);

            if (t > 1f - fadeDuration / lifeTime)
            {
                float fadeT = (t - (1f - fadeDuration / lifeTime)) / (fadeDuration / lifeTime);
                tmp.color = new Color(startColor.r, startColor.g, startColor.b, 1f - fadeT);
            }

            yield return null;
        }

        Destroy(target);
    }
}
