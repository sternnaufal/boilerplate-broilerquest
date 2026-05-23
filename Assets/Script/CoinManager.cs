using UnityEngine;
using TMPro;
using System;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }
    public event Action<int> CoinsChanged;

    [Header("UI Reference")]
    public TextMeshProUGUI coinText;

    [Header("Settings")]
    [SerializeField] private bool resetCoinOnStart = false;
    [SerializeField] private int startingCoin = 0;
    [SerializeField] private bool usePlayerPrefs = true;

    private int totalCoin = 0;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            if (resetCoinOnStart)
                Instance.SetTotalCoin(startingCoin);

            if (coinText != null)
                Instance.BindCoinText(coinText);

            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (usePlayerPrefs && !resetCoinOnStart)
            totalCoin = PlayerPrefs.GetInt("TotalCoin", 0);
        else if (resetCoinOnStart)
            totalCoin = Mathf.Max(0, startingCoin);
        else
            totalCoin = 0;

        if (coinText == null) TryFindCoinText();
        UpdateCoinUI();
        SaveCoin();
    }

    public void AddCoin(int amount)
    {
        if (amount < 0) return;

        long nextTotal = (long)totalCoin + amount;
        totalCoin = nextTotal > int.MaxValue ? int.MaxValue : (int)nextTotal;
        UpdateCoinUI();
        SaveCoin();
        Debug.Log($"Coin +{amount}, total: {totalCoin}");
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
        Debug.Log($"Coin -{amount}, total: {totalCoin}");
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
        if (coinText == null) TryFindCoinText();
        if (coinText != null)
            coinText.text = totalCoin.ToString();

        CoinsChanged?.Invoke(totalCoin);
    }

    private void TryFindCoinText()
    {
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
            PlayerPrefs.SetInt("TotalCoin", totalCoin);
            PlayerPrefs.Save();
        }
    }
}
