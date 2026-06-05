using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIAlertPanel : MonoBehaviour
{
    [Header("UI References (manual drag)")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    [Header("Behavior")]
    [SerializeField] private bool dontDestroyOnLoad = true;
    [SerializeField] private float autoHideSeconds = 0f;

    private static UIAlertPanel instance;
    private float hideAtTime;

    public static UIAlertPanel Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);

        Hide();
    }

    private void Update()
    {
        if (autoHideSeconds <= 0f)
            return;

        if (hideAtTime > 0f && Time.unscaledTime >= hideAtTime)
        {
            hideAtTime = 0f;
            Hide();
        }
    }

    public void Show(string message)
    {
        if (messageText != null)
            messageText.text = message ?? string.Empty;

        if (panelRoot != null)
            panelRoot.SetActive(true);
        else
            gameObject.SetActive(true);

        if (autoHideSeconds > 0f)
            hideAtTime = Time.unscaledTime + autoHideSeconds;
    }

    public void Hide()
    {
        hideAtTime = 0f;

        if (panelRoot != null)
            panelRoot.SetActive(false);
        else
            gameObject.SetActive(false);
    }
}

