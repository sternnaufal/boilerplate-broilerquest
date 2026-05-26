using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalUIOverlay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI feedText;
    [SerializeField] private GameObject coinIcon;
    [SerializeField] private GameObject feedIcon;

    private Canvas canvas;
    private static GlobalUIOverlay instance;
    private bool coinBound;
    private bool feedBound;

    public static GlobalUIOverlay Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GlobalUIOverlay");
                instance = go.AddComponent<GlobalUIOverlay>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateCanvas();
        CreateUIElements();

        if (CoinManager.Instance != null)
            BindCoin();

        if (FeedManager.Instance != null)
            BindFeed();
    }

    private void Start()
    {
        if (!coinBound) BindCoin();
        if (!feedBound) BindFeed();
    }

    private void CreateCanvas()
    {
        gameObject.AddComponent<RectTransform>();
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);

        gameObject.AddComponent<GraphicRaycaster>();
    }

    private void CreateUIElements()
    {
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(transform, false);
        RectTransform barRect = topBar.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.5f, 1f);
        barRect.anchorMax = new Vector2(0.5f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.anchoredPosition = new Vector2(0f, 0f);
        barRect.sizeDelta = new Vector2(400f, 44f);
        Image barImage = topBar.GetComponent<Image>();
        barImage.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject coinObj = new GameObject("CoinDisplay", typeof(RectTransform));
        coinObj.transform.SetParent(topBar.transform, false);
        RectTransform coinRect = coinObj.GetComponent<RectTransform>();
        coinRect.anchorMin = new Vector2(0f, 0.5f);
        coinRect.anchorMax = new Vector2(0f, 0.5f);
        coinRect.pivot = new Vector2(0f, 0.5f);
        coinRect.anchoredPosition = new Vector2(12f, 0f);
        coinRect.sizeDelta = new Vector2(180f, 36f);

        GameObject coinIconObj = new GameObject("CoinIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
        coinIconObj.transform.SetParent(coinObj.transform, false);
        RectTransform coinIconRect = coinIconObj.GetComponent<RectTransform>();
        coinIconRect.anchorMin = Vector2.zero;
        coinIconRect.anchorMax = Vector2.one;
        coinIconRect.offsetMin = Vector2.zero;
        coinIconRect.offsetMax = Vector2.zero;
        TextMeshProUGUI coinIconText = coinIconObj.GetComponent<TextMeshProUGUI>();
        coinIconText.text = "💰 ";
        coinIconText.fontSize = 22f;
        coinIconText.alignment = TextAlignmentOptions.MidlineLeft;
        coinIconText.color = new Color(1f, 0.96f, 0.70f, 1f);
        coinIcon = coinIconObj;

        GameObject coinValObj = new GameObject("CoinValue", typeof(RectTransform), typeof(TextMeshProUGUI));
        coinValObj.transform.SetParent(coinObj.transform, false);
        RectTransform coinValRect = coinValObj.GetComponent<RectTransform>();
        coinValRect.anchorMin = Vector2.zero;
        coinValRect.anchorMax = Vector2.one;
        coinValRect.offsetMin = new Vector2(28f, 0f);
        coinValRect.offsetMax = Vector2.zero;
        coinText = coinValObj.GetComponent<TextMeshProUGUI>();
        coinText.text = "0";
        coinText.fontSize = 24f;
        coinText.fontStyle = FontStyles.Bold;
        coinText.alignment = TextAlignmentOptions.MidlineLeft;
        coinText.color = new Color(1f, 0.96f, 0.70f, 1f);

        GameObject feedObj = new GameObject("FeedDisplay", typeof(RectTransform));
        feedObj.transform.SetParent(topBar.transform, false);
        RectTransform feedRect = feedObj.GetComponent<RectTransform>();
        feedRect.anchorMin = new Vector2(1f, 0.5f);
        feedRect.anchorMax = new Vector2(1f, 0.5f);
        feedRect.pivot = new Vector2(1f, 0.5f);
        feedRect.anchoredPosition = new Vector2(-12f, 0f);
        feedRect.sizeDelta = new Vector2(180f, 36f);

        GameObject feedIconObj = new GameObject("FeedIcon", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedIconObj.transform.SetParent(feedObj.transform, false);
        RectTransform feedIconRect = feedIconObj.GetComponent<RectTransform>();
        feedIconRect.anchorMin = Vector2.zero;
        feedIconRect.anchorMax = Vector2.one;
        feedIconRect.offsetMin = Vector2.zero;
        feedIconRect.offsetMax = Vector2.zero;
        TextMeshProUGUI feedIconText = feedIconObj.GetComponent<TextMeshProUGUI>();
        feedIconText.text = "🌾 ";
        feedIconText.fontSize = 22f;
        feedIconText.alignment = TextAlignmentOptions.MidlineRight;
        feedIconText.color = new Color(0.7f, 1f, 0.7f, 1f);
        feedIcon = feedIconObj;

        GameObject feedValObj = new GameObject("FeedValue", typeof(RectTransform), typeof(TextMeshProUGUI));
        feedValObj.transform.SetParent(feedObj.transform, false);
        RectTransform feedValRect = feedValObj.GetComponent<RectTransform>();
        feedValRect.anchorMin = Vector2.zero;
        feedValRect.anchorMax = Vector2.one;
        feedValRect.offsetMin = Vector2.zero;
        feedValRect.offsetMax = new Vector2(-28f, 0f);
        feedText = feedValObj.GetComponent<TextMeshProUGUI>();
        feedText.text = "0";
        feedText.fontSize = 24f;
        feedText.fontStyle = FontStyles.Bold;
        feedText.alignment = TextAlignmentOptions.MidlineRight;
        feedText.color = new Color(0.7f, 1f, 0.7f, 1f);
    }

    private void BindCoin()
    {
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.CoinsChanged += UpdateCoinDisplay;
            UpdateCoinDisplay(CoinManager.Instance.GetTotalCoin());
            coinBound = true;
        }
    }

    private void BindFeed()
    {
        if (FeedManager.Instance != null)
        {
            FeedManager.Instance.FeedChanged += UpdateFeedDisplay;
            UpdateFeedDisplay(FeedManager.Instance.GetFeedCount());
            feedBound = true;
        }
    }

    private void UpdateCoinDisplay(int totalCoin)
    {
        if (coinText != null)
            coinText.text = totalCoin.ToString();
    }

    private void UpdateFeedDisplay(int totalFeed)
    {
        if (feedText != null)
            feedText.text = totalFeed.ToString();
    }
}
