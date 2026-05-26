using TMPro;
using UnityEngine;

public class UIGlobalBinder : MonoBehaviour
{
    [Header("UI References - Drag Manual")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI feedText;

    private static UIGlobalBinder _instance;
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Cari referensi jika belum di-assign (opsional)
        if (coinText == null)
            coinText = GameObject.Find("CoinText")?.GetComponent<TextMeshProUGUI>();
        if (feedText == null)
            feedText = GameObject.Find("PakanText")?.GetComponent<TextMeshProUGUI>();

        // Daftarkan event
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged += UpdateCoinDisplay;
        if (FeedManager.Instance != null)
            FeedManager.Instance.FeedChanged += UpdateFeedDisplay;

        // Tampilkan nilai awal
        if (CoinManager.Instance != null)
            UpdateCoinDisplay(CoinManager.Instance.GetTotalCoin());
        if (FeedManager.Instance != null)
            UpdateFeedDisplay(FeedManager.Instance.GetFeedCount());
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

    private void OnDestroy()
    {
        // Lepas event saat dihancurkan
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged -= UpdateCoinDisplay;
        if (FeedManager.Instance != null)
            FeedManager.Instance.FeedChanged -= UpdateFeedDisplay;
    }

    private void OnEnable()
    {
        if (coinText == null) coinText = GameObject.Find("CoinText")?.GetComponent<TextMeshProUGUI>();
        if (feedText == null) feedText = GameObject.Find("PakanText")?.GetComponent<TextMeshProUGUI>();
        FindUIReferences();
    }

    private void FindUIReferences()
    {
        if (coinText == null)
            coinText = GameObject.Find("CoinText")?.GetComponent<TextMeshProUGUI>();
        if (feedText == null)
            feedText = GameObject.Find("PakanText")?.GetComponent<TextMeshProUGUI>();
        
        // Update tampilan setelah referensi ditemukan
        if (CoinManager.Instance != null)
            UpdateCoinDisplay(CoinManager.Instance.GetTotalCoin());
        if (FeedManager.Instance != null)
            UpdateFeedDisplay(FeedManager.Instance.GetFeedCount());
    }
}