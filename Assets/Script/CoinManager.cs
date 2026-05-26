using UnityEngine;
using TMPro;
using System;

public class CoinManager : Singleton<CoinManager>
{
    public event Action<int> CoinsChanged;

    [Header("UI Reference")]
    public TextMeshProUGUI coinText;

    [Header("Settings")]
    [SerializeField] private bool resetCoinOnStart = false;
    [SerializeField] private bool usePlayerPrefs = true;

    private int totalCoin = 0;
    private bool hasInitialized;
    private bool coinTextSearched;

    protected override void Awake()
    {
        base.Awake();
        if (resetCoinOnStart)
            SetTotalCoin(GameConstants.Economy.StartingCoin);
    }

    void Start()
    {
        Initialize(coinText);
    }

    public void Initialize(TextMeshProUGUI uiText = null)
    {
        if (uiText != null)
            coinText = uiText;

        if (!hasInitialized)
        {
            if (usePlayerPrefs && !resetCoinOnStart)
                totalCoin = LoadSavedCoin();
            else if (resetCoinOnStart)
                totalCoin = Mathf.Max(0, GameConstants.Economy.StartingCoin);
            else
                totalCoin = 0;

            hasInitialized = true;
            SaveCoin();
        }

        if (coinText == null) TryFindCoinText();
        UpdateCoinUI();

        GlobalUIOverlay overlay = GlobalUIOverlay.Instance;
    }

    public void AddCoin(int amount)
    {
        if (amount < 0) return;

        long nextTotal = (long)totalCoin + amount;
        totalCoin = nextTotal > int.MaxValue ? int.MaxValue : (int)nextTotal;
        UpdateCoinUI();
        SaveCoin();
        GameLog.Info($"Coin +{amount}, total: {totalCoin}");
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && totalCoin >= amount;
    }

    public bool SpendCoin(int amount)
    {
        if (amount < 0 || !CanAfford(amount)) return false;
        totalCoin -= amount;
        UpdateCoinUI();
        SaveCoin();
        GameLog.Info($"Coin -{amount}, total: {totalCoin}");
        return true;
    }

    public void SetTotalCoin(int amount)
    {
        totalCoin = Mathf.Max(0, amount);
        UpdateCoinUI();
        SaveCoin();
    }

    public int GetTotalCoin() => totalCoin;

    // Method yang dipanggil oleh StarterGameplayUI
    public void BindCoinText(TextMeshProUGUI text)
    {
        coinText = text;
        UpdateCoinUI();
    }

    private void UpdateCoinUI()
    {
        if (coinText == null && !coinTextSearched) TryFindCoinText();
        if (coinText != null)
            coinText.text = totalCoin.ToString();

        CoinsChanged?.Invoke(totalCoin);
    }

    private void TryFindCoinText()
    {
        coinTextSearched = true;
        TextMeshProUGUI[] allTexts = FindObjectsByType<TextMeshProUGUI>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        foreach (var txt in allTexts)
        {
            if (txt.gameObject.name == "CoinText")
            {
                coinText = txt;
                break;
            }
        }
    }

    private void SaveCoin()
    {
        if (usePlayerPrefs)
        {
            PlayerPrefs.SetInt(GameConstants.Persistence.TotalCoinKey, totalCoin);
            PlayerPrefs.Save();
        }
    }

    private static int LoadSavedCoin()
    {
        if (PlayerPrefs.HasKey(GameConstants.Persistence.TotalCoinKey))
            return PlayerPrefs.GetInt(GameConstants.Persistence.TotalCoinKey, 0);

        if (PlayerPrefs.HasKey(GameConstants.Persistence.LegacyTotalCoinKey))
            return PlayerPrefs.GetInt(GameConstants.Persistence.LegacyTotalCoinKey, 0);

        return GameConstants.Economy.StartingCoin;
    }
}
