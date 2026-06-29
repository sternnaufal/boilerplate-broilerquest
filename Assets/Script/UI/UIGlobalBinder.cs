using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIGlobalBinder : MonoBehaviour
{
    [Header("UI References - Drag Manual")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private TextMeshProUGUI feedText;

    [Header("Animated Counter")]
    [SerializeField] private float counterAnimDuration = 0.3f;

    private const int UNINITIALIZED = int.MinValue;

    private static UIGlobalBinder _instance;
    private int lastCoinAmount = UNINITIALIZED;
    private int lastFeedAmount = UNINITIALIZED;

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
        if (lastCoinAmount == UNINITIALIZED)
        {
            lastCoinAmount = totalCoin;
            if (coinText != null)
            {
                StopCoroutine(nameof(AnimateCoinText));
                StartCoroutine(AnimateCoinText(totalCoin));
            }
            return;
        }

        int delta = totalCoin - lastCoinAmount;
        lastCoinAmount = totalCoin;

        if (coinText != null)
        {
            StopCoroutine(nameof(AnimateCoinText));
            StartCoroutine(AnimateCoinText(totalCoin));
        }

        if (delta != 0)
        {
            Vector2 pos = GetTextScreenPos(coinText, new Vector2(80f, 0f));
            FloatingFeedback.ShowCoin(pos, delta);
        }
    }

    private void UpdateFeedDisplay(int totalFeed)
    {
        if (lastFeedAmount == UNINITIALIZED)
        {
            lastFeedAmount = totalFeed;
            if (feedText != null)
            {
                StopCoroutine(nameof(AnimateFeedText));
                StartCoroutine(AnimateFeedText(totalFeed));
            }
            return;
        }

        int delta = totalFeed - lastFeedAmount;
        lastFeedAmount = totalFeed;

        if (feedText != null)
        {
            StopCoroutine(nameof(AnimateFeedText));
            StartCoroutine(AnimateFeedText(totalFeed));
        }

        if (delta != 0)
        {
            Vector2 pos = GetTextScreenPos(feedText, new Vector2(80f, 0f));
            FloatingFeedback.ShowFeed(pos, delta);
        }
    }

    private IEnumerator AnimateCoinText(int target)
    {
        if (coinText == null) yield break;

        int start = int.TryParse(coinText.text.Replace(",", ""), out int parsed) ? parsed : target;
        float elapsed = 0f;

        while (elapsed < counterAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / counterAnimDuration);
            int current = Mathf.RoundToInt(Mathf.Lerp(start, target, t));
            coinText.text = current.ToString();
            AdjustTextPosition(coinText, current);
            yield return null;
        }

        coinText.text = target.ToString();
        AdjustTextPosition(coinText, target);
    }

    private IEnumerator AnimateFeedText(int target)
    {
        if (feedText == null) yield break;

        int start = int.TryParse(feedText.text.Replace(",", ""), out int parsed) ? parsed : target;
        float elapsed = 0f;

        while (elapsed < counterAnimDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / counterAnimDuration);
            int current = Mathf.RoundToInt(Mathf.Lerp(start, target, t));
            feedText.text = current.ToString();
            AdjustTextPosition(feedText, current);
            yield return null;
        }

        feedText.text = target.ToString();
        AdjustTextPosition(feedText, target);
    }

    private static void AdjustTextPosition(TextMeshProUGUI text, int value)
    {
        if (text == null) return;
        RectTransform rt = text.rectTransform;
        Vector2 pos = rt.anchoredPosition;
        rt.anchoredPosition = new Vector2(value >= 100 ? 50.71f : 70.71f, pos.y);
    }

    private static Vector2 GetTextScreenPos(TextMeshProUGUI text, Vector2 offset)
    {
        if (text == null) return new Vector2(Screen.width / 2f, Screen.height / 2f);

        RectTransform rt = text.rectTransform;
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) / 2f;

        var rootCanvas = rt.root.GetComponent<Canvas>();
        if (Camera.main != null && rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            center = Camera.main.WorldToScreenPoint(center);

        return new Vector2(center.x, center.y) + offset;
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
        lastCoinAmount = UNINITIALIZED;
        lastFeedAmount = UNINITIALIZED;
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

        if (coinText != null && CoinManager.Instance != null)
        {
            int coin = CoinManager.Instance.GetTotalCoin();
            coinText.text = coin.ToString();
            AdjustTextPosition(coinText, coin);
        }

        if (feedText != null && FeedManager.Instance != null)
        {
            int feed = FeedManager.Instance.GetFeedCount();
            feedText.text = feed.ToString();
            AdjustTextPosition(feedText, feed);
        }
    }
}
