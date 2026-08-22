// IoTReminderController.cs
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IoTReminderController : MonoBehaviour
{
    public static IoTReminderController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float delayBeforeShow = 120f;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float autoHideDelay = 10f;
    [SerializeField] private float autoHideFadeDuration = 0.5f;
    [SerializeField] private string reminderMessage = "Sepertinya kita membutuhkan IoT";

    private static readonly string[] gameplayScenes = { "Starter", "Beginner", "Intermediate" };

    private GameObject bubblePanel;
    private CanvasGroup canvasGroup;
    private TextMeshProUGUI messageText;
    private Button bubbleButton;
    private bool isShown = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Dipanggil oleh IoTBubblePanelRegistrar dari scene
    public void RegisterBubblePanel(GameObject panel)
    {
        bubblePanel = panel;

        canvasGroup = bubblePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = bubblePanel.AddComponent<CanvasGroup>();

        messageText = bubblePanel.GetComponentInChildren<TextMeshProUGUI>();

        bubbleButton = bubblePanel.GetComponent<Button>();
        if (bubbleButton == null)
            bubbleButton = bubblePanel.AddComponent<Button>();
        bubbleButton.onClick.RemoveAllListeners();
        bubbleButton.onClick.AddListener(OnBubbleClicked);

        bubblePanel.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (messageText != null) messageText.text = reminderMessage;

        Debug.Log($"[IoTReminder] BubblePanel registered: {panel.name}");

        // Langsung mulai timer setelah panel terdaftar
        StopAllCoroutines();
        isShown = false;
        StartCoroutine(ShowAfterDelay());
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,
                               UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        StopAllCoroutines();
        isShown = false;
        bubblePanel = null;
        canvasGroup = null;
        messageText = null;
        bubbleButton = null;

        if (!IsGameplayScene(scene.name))
        {
            Debug.Log($"[IoTReminder] Scene {scene.name} bukan gameplay, skip.");
            return;
        }

        // Panel akan di-register oleh IoTBubblePanelRegistrar via RegisterBubblePanel()
        // Tidak perlu Find di sini
        Debug.Log($"[IoTReminder] Scene {scene.name} loaded, menunggu RegisterBubblePanel...");
    }

    private bool IsGameplayScene(string sceneName)
    {
        foreach (string s in gameplayScenes)
            if (s == sceneName) return true;
        return false;
    }

    private IEnumerator ShowAfterDelay()
    {
        Debug.Log($"[IoTReminder] ShowAfterDelay started, waiting {delayBeforeShow}s");
        float elapsed = 0f;
        while (elapsed < delayBeforeShow)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        Debug.Log("[IoTReminder] ShowAfterDelay selesai, memanggil ShowBubble");
        ShowBubble();
    }

    public void ShowBubble()
    {
        if (isShown || bubblePanel == null)
        {
            Debug.Log($"[IoTReminder] ShowBubble skip: isShown={isShown}, bubblePanel={bubblePanel}");
            return;
        }

        isShown = true;
        bubblePanel.SetActive(true);
        StartCoroutine(FadeIn(() =>
        {
            if (autoHideDelay > 0f)
                StartCoroutine(AutoHideAfterDelay());
        }));
    }

    public void HideBubble()
    {
        if (!isShown) return;
        isShown = false;

        if (bubblePanel != null)
            bubblePanel.SetActive(false);

        StopCoroutine(nameof(AutoHideAfterDelay));
    }

    private IEnumerator FadeIn(System.Action onComplete = null)
    {
        if (canvasGroup == null) yield break;
        canvasGroup.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        onComplete?.Invoke();
    }

    private IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSecondsRealtime(autoHideDelay);

        if (canvasGroup != null)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            while (elapsed < autoHideFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / autoHideFadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        HideBubble();
    }

    private void OnBubbleClicked()
    {
        StarterGameplayUI ui = FindObjectOfType<StarterGameplayUI>();
        if (ui != null)
        {
            ui.OpenIoTPanel();
            HideBubble();
            return;
        }

        GameObject iotAPK = GameObject.Find("IoTAPK");
        if (iotAPK != null)
        {
            GameObject hpPanel = GameObject.Find("BQ_HPPanel");
            if (hpPanel != null) hpPanel.SetActive(true);
            iotAPK.SetActive(true);
            HideBubble();
        }
        else
        {
            Debug.LogWarning("[IoTReminder] IoTAPK tidak ditemukan!");
        }
    }

    public void StartReminder()
    {
        StopAllCoroutines();
        isShown = false;
        if (bubblePanel != null)
            bubblePanel.SetActive(false);
        StartCoroutine(ShowAfterDelay());
    }
}