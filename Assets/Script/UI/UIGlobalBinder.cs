using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged += UpdateCoinDisplay;
        if (FeedManager.Instance != null)
            FeedManager.Instance.FeedChanged += UpdateFeedDisplay;

        FindUIReferences();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged -= UpdateCoinDisplay;
        if (FeedManager.Instance != null)
            FeedManager.Instance.FeedChanged -= UpdateFeedDisplay;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        coinText = null;
        feedText = null;
        FindUIReferences();
    }

    private void FindUIReferences()
    {
        if (coinText == null)
        {
            var allTexts = FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTexts)
            {
                if (t.gameObject.name == "CoinText")
                {
                    coinText = t;
                    break;
                }
            }
        }

        if (feedText == null)
        {
            var allTexts = FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allTexts)
            {
                if (t.gameObject.name == "PakanText")
                {
                    feedText = t;
                    break;
                }
            }
        }

        if (CoinManager.Instance != null)
            UpdateCoinDisplay(CoinManager.Instance.GetTotalCoin());
        if (FeedManager.Instance != null)
            UpdateFeedDisplay(FeedManager.Instance.GetFeedCount());
    }
}
