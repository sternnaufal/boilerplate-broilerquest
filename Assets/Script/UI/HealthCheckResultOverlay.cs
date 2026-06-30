using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthCheckResultOverlay : MonoBehaviour
{
    private static HealthCheckResultOverlay instance;

    [SerializeField] private float displayDuration = 1.5f;
    [SerializeField] private Sprite successSprite;
    [SerializeField] private Sprite failSprite;

    private Image resultImage;
    private RectTransform resultRect;
    private GameObject rootCanvas;

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        if (gameObject.scene.name != "DontDestroyOnLoad")
            DontDestroyOnLoad(gameObject);
        BuildCanvas();
    }

    public static void ShowSuccess()
    {
        EnsureInstance();
        if (SFXManager.Instance != null) SFXManager.Instance.PlayMinigameSuccess();
        instance.Show(true);
    }

    public static void ShowFail()
    {
        EnsureInstance();
        if (SFXManager.Instance != null) SFXManager.Instance.PlayMinigameFail();
        instance.Show(false);
    }

    // Fallback: load from prefab in Resources (created via Tools/Create HealthCheck Overlay Prefab).
    private static void EnsureInstance()
    {
        if (instance != null) return;

        var prefab = Resources.Load<GameObject>("HealthCheckResultOverlay");
        if (prefab != null)
        {
            var go = Object.Instantiate(prefab);
            go.name = "HealthCheckResultOverlay";
            // Awake on the instantiated object sets instance and calls DontDestroyOnLoad
        }
        else
        {
            // Last resort: bare instance without sprites (colored fallback will show)
            var go = new GameObject("HealthCheckResultOverlay");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HealthCheckResultOverlay>();
        }
    }

    private void BuildCanvas()
    {
        var canvasGO = new GameObject("HealthResultCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(canvasGO.transform, false);
        StretchToParent(backdrop.GetComponent<RectTransform>());
        var backdropImg = backdrop.GetComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.5f);
        backdropImg.raycastTarget = false;

        var imageGO = new GameObject("ResultImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);
        resultRect = imageGO.GetComponent<RectTransform>();
        resultRect.anchorMin = new Vector2(0.5f, 0.5f);
        resultRect.anchorMax = new Vector2(0.5f, 0.5f);
        resultRect.pivot = new Vector2(0.5f, 0.5f);
        resultRect.anchoredPosition = Vector2.zero;
        resultRect.sizeDelta = new Vector2(600f, 300f);

        resultImage = imageGO.GetComponent<Image>();
        resultImage.preserveAspect = true;
        resultImage.raycastTarget = false;

        rootCanvas = canvasGO;
        rootCanvas.SetActive(false);
    }

    private void Show(bool success)
    {
        if (rootCanvas == null) return;

        Sprite sprite = success ? successSprite : failSprite;
        if (sprite != null)
        {
            resultImage.sprite = sprite;
            resultImage.color = Color.white;
            float w = sprite.rect.width;
            float h = sprite.rect.height;
            float scale = Mathf.Min(700f / w, 400f / h, 1f);
            resultRect.sizeDelta = new Vector2(w * scale, h * scale);
        }
        else
        {
            // Fallback kalau sprite belum di-assign
            resultImage.sprite = null;
            resultImage.color = success ? new Color(0.3f, 0.9f, 0.3f, 0.9f) : new Color(0.9f, 0.3f, 0.3f, 0.9f);
            resultRect.sizeDelta = new Vector2(400f, 200f);
        }

        rootCanvas.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AutoHide());
    }

    private IEnumerator AutoHide()
    {
        yield return new WaitForSecondsRealtime(displayDuration);
        if (rootCanvas != null) rootCanvas.SetActive(false);
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
