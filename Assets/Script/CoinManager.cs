using UnityEngine;
using TMPro; // jika pakai TextMeshPro. Jika pakai legacy Text, ganti dengan using UnityEngine.UI;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("UI Reference")]
    public TextMeshProUGUI coinText; // drag CoinText dari Canvas ke sini

    [Header("Starting Coin")]
    [SerializeField] private bool resetCoinOnStart;
    [SerializeField] private int startingCoin;

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
        if (resetCoinOnStart)
            totalCoin = Mathf.Max(0, startingCoin);

        UpdateCoinUI();
    }

    public void AddCoin(int amount)
    {
        totalCoin += amount;
        UpdateCoinUI();
        Debug.Log($"Coin +{amount}, total: {totalCoin}");
    }

    public bool CanAfford(int amount)
    {
        return amount >= 0 && totalCoin >= amount;
    }

    public bool SpendCoin(int amount)
    {
        if (amount < 0)
        {
            Debug.LogWarning("Jumlah coin yang dipakai tidak boleh negatif.");
            return false;
        }

        if (!CanAfford(amount))
            return false;

        totalCoin -= amount;
        UpdateCoinUI();
        Debug.Log($"Coin -{amount}, total: {totalCoin}");
        return true;
    }

    public void SetTotalCoin(int amount)
    {
        totalCoin = Mathf.Max(0, amount);
        UpdateCoinUI();
    }

    public void BindCoinText(TextMeshProUGUI text)
    {
        coinText = text;
        UpdateCoinUI();
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
