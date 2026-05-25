using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KoleksiIoTController : MonoBehaviour
{
    [System.Serializable]
    public class IoTProduct
    {
        public string productKey = "SmartSilo";
        public string productName = "Smart Silo";
        public int productPrice = 100;
        public Texture productImage;
    }

    [Header("Product Settings")]
    [SerializeField] private IoTProduct product = new IoTProduct();

    [Header("UI References")]
    [SerializeField] private RawImage productImage;
    [SerializeField] private TextMeshProUGUI productNameText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button buyButton;
    [SerializeField] private GameObject ownedContainer;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged += OnCoinsChanged;
    }

    private void Start()
    {
        if (productImage != null && product.productImage != null)
            productImage.texture = product.productImage;

        if (productNameText != null)
            productNameText.text = product.productName;

        if (CoinManager.Instance != null)
        {
            TextMeshProUGUI coinDisplay = GameObject.Find("CoinText")?.GetComponent<TextMeshProUGUI>();
            if (coinDisplay != null)
                CoinManager.Instance.BindCoinText(coinDisplay);
        }

        if (buyButton != null)
            ButtonHelper.AddListenerOnce(buyButton, BuyProduct);

        if (backButton != null)
            ButtonHelper.AddListenerOnce(backButton, GoBack);

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int totalCoin)
    {
        UpdateBuyButtonState();
    }

    private void UpdateBuyButtonState()
    {
        if (buyButton == null || !buyButton.gameObject.activeSelf)
            return;

        bool canAfford = CoinManager.Instance != null && CoinManager.Instance.CanAfford(product.productPrice);
        buyButton.interactable = canAfford;
    }

    private bool IsPurchased()
    {
        return PlayerPrefs.GetInt(GameConstants.Persistence.KoleksiIoTPurchasedPrefix + product.productKey, 0) == 1;
    }

    public void BuyProduct()
    {
        if (CoinManager.Instance == null || !CoinManager.Instance.CanAfford(product.productPrice))
            return;

        if (!CoinManager.Instance.SpendCoin(product.productPrice))
            return;

        PlayerPrefs.SetInt(GameConstants.Persistence.KoleksiIoTPurchasedPrefix + product.productKey, 1);
        PlayerPrefs.Save();
        RefreshUI();
    }

    private void RefreshUI()
    {
        bool purchased = IsPurchased();

        if (statusText != null)
            statusText.text = purchased ? "Sudah Dibeli" : "Harga: " + product.productPrice + " Koin";

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!purchased);
            if (!purchased)
                UpdateBuyButtonState();
        }

        if (ownedContainer != null)
            ownedContainer.SetActive(purchased);
    }

    public void GoBack()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.GoToMainMenu();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
