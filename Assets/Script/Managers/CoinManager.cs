using UnityEngine;
using System;

public class CoinManager : Singleton<CoinManager>
{
    public event Action<int> CoinsChanged;

    [Header("Settings")]
    [SerializeField] private bool resetCoinOnStart = false;
    [SerializeField] private bool usePlayerPrefs = true;

    private int totalCoin = 0;
    private bool hasInitialized;

    protected override void Awake()
    {
        base.Awake();
        if (resetCoinOnStart)
            SetTotalCoin(GameConstants.Economy.StartingCoin);
    }

    void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (hasInitialized)
            return;

        if (usePlayerPrefs && !resetCoinOnStart)
            totalCoin = LoadSavedCoin();
        else if (resetCoinOnStart)
            totalCoin = Mathf.Max(0, GameConstants.Economy.StartingCoin);
        else
            totalCoin = 0;

        hasInitialized = true;
        SaveCoin();
        CoinsChanged?.Invoke(totalCoin);
    }

    public void AddCoin(int amount)
    {
        Initialize();
        if (amount < 0) return;

        long nextTotal = (long)totalCoin + amount;
        totalCoin = nextTotal > int.MaxValue ? int.MaxValue : (int)nextTotal;
        CoinsChanged?.Invoke(totalCoin);
        SaveCoin();
        GameLog.Info($"Coin +{amount}, total: {totalCoin}");
    }

    public bool CanAfford(int amount)
    {
        Initialize();
        return amount >= 0 && totalCoin >= amount;
    }

    public bool SpendCoin(int amount)
    {
        Initialize();
        if (amount < 0 || !CanAfford(amount)) return false;
        totalCoin -= amount;
        CoinsChanged?.Invoke(totalCoin);
        SaveCoin();
        GameLog.Info($"Coin -{amount}, total: {totalCoin}");
        return true;
    }

    public void SetTotalCoin(int amount)
    {
        Initialize();
        totalCoin = Mathf.Max(0, amount);
        CoinsChanged?.Invoke(totalCoin);
        SaveCoin();
    }

    public int GetTotalCoin()
    {
        Initialize();
        return totalCoin;
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
