using UnityEngine;
using TMPro; // jika pakai TextMeshPro. Jika pakai legacy Text, ganti dengan using UnityEngine.UI;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("UI Reference")]
    public TextMeshProUGUI coinText; // drag CoinText dari Canvas ke sini

    private int totalCoin = 0;

    void Awake()
    {
        // Singleton pattern biar mudah diakses dari script lain
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // biar tidak hilang saat ganti scene (opsional)
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        UpdateCoinUI();
    }

    public void AddCoin(int amount)
    {
        totalCoin += amount;
        UpdateCoinUI();
        Debug.Log($"Coin +{amount}, total: {totalCoin}");
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = totalCoin.ToString();
    }

    public int GetTotalCoin()
    {
        return totalCoin;
    }
}