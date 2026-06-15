using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthCheckResultOverlay : MonoBehaviour
{
    private static HealthCheckResultOverlay instance;

    [SerializeField] private float displayDuration = 1.5f;
    [SerializeField] private Color successColor = new Color(0.3f, 0.9f, 0.3f);
    [SerializeField] private Color failColor = new Color(0.9f, 0.3f, 0.3f);
    [SerializeField] private string successText = "Berhasil!";
    [SerializeField] private string failText = "Gagal!";

    private TextMeshProUGUI resultText;
    private GameObject rootCanvas;

    public static void ShowSuccess()
    {
        EnsureInstance();
        instance.Show(instance.successText, instance.successColor);
    }

    public static void ShowFail()
    {
        EnsureInstance();
        instance.Show(instance.failText, instance.failColor);
    }

    private static void EnsureInstance()
    {
        if (instance != null) return;
        GameObject go = new GameObject("HealthCheckResultOverlay");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<HealthCheckResultOverlay>();
        instance.BuildCanvas();
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("HealthResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchToParent(canvasRect);

        GameObject backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(canvasObject.transform, false);
        RectTransform backdropRect = backdrop.GetComponent<RectTransform>();
        StretchToParent(backdropRect);
        Image backdropImage = backdrop.GetComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.4f);
        backdropImage.raycastTarget = true;

        GameObject panelObject = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400f, 200f);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.22f, 0.12f, 0.96f);
        panelImage.raycastTarget = true;

        GameObject textObject = new GameObject("ResultText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        resultText = textObject.GetComponent<TextMeshProUGUI>();
        resultText.fontSize = 48f;
        resultText.fontStyle = FontStyles.Bold;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.raycastTarget = false;
        resultText.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF - Fallback");

        rootCanvas = canvasObject;
        rootCanvas.SetActive(false);
    }

    private void Show(string text, Color color)
    {
        if (rootCanvas == null) return;

        if (resultText != null)
        {
            resultText.text = text;
            resultText.color = color;
        }

        rootCanvas.SetActive(true);

        if (gameObject.activeInHierarchy)
        {
            StopAllCoroutines();
            StartCoroutine(AutoHideRoutine());
        }
    }

    private IEnumerator AutoHideRoutine()
    {
        yield return new WaitForSecondsRealtime(displayDuration);
        if (rootCanvas != null)
            rootCanvas.SetActive(false);
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
