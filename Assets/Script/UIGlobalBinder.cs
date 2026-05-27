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
        FindUIReferences();
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

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Daftarkan event
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged += UpdateCoinDisplay;
        if (FeedManager.Instance != null)
            FeedManager.Instance.FeedChanged += UpdateFeedDisplay;

        FindUIReferences();
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Lepas event
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged -= UpdateCoinDisplay;
        if (FeedManager.Instance != null)
            FeedManager.Instance.FeedChanged -= UpdateFeedDisplay;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        coinText = null;
        feedText = null;
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