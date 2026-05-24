using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KoleksiIoTController : MonoBehaviour
{
    [Header("Product Settings")]
    [SerializeField] private string productKey = "SmartSilo";
    [SerializeField] private string productName = "Smart Silo";
    [SerializeField] private int productPrice = 100;

    private TextMeshProUGUI statusText;
    private Button buyButton;
    private GameObject ownedContainer;
    private Button backButton;

    private void Start()
    {
        statusText = GameObject.Find("StatusText")?.GetComponent<TextMeshProUGUI>();
        ownedContainer = GameObject.Find("OwnedContainer");
        buyButton = GameObject.Find("BuyButton")?.GetComponent<Button>();
        backButton = GameObject.Find("BackButton")?.GetComponent<Button>();

        if (CoinManager.Instance != null)
        {
            TextMeshProUGUI coinDisplay = GameObject.Find("CoinText")?.GetComponent<TextMeshProUGUI>();
            if (coinDisplay != null)
                CoinManager.Instance.BindCoinText(coinDisplay);
        }

        if (buyButton != null)
            buyButton.onClick.AddListener(BuyProduct);

        if (backButton != null)
            backButton.onClick.AddListener(GoBack);

        RefreshUI();
    }

    private void Update()
    {
        if (buyButton != null && buyButton.gameObject.activeSelf)
        {
            bool canAfford = CoinManager.Instance != null && CoinManager.Instance.CanAfford(productPrice);
            buyButton.interactable = canAfford;
        }
    }

    private bool IsPurchased()
    {
        return PlayerPrefs.GetInt("KoleksiIoT.Purchased." + productKey, 0) == 1;
    }

    private void BuyProduct()
    {
        if (CoinManager.Instance != null && CoinManager.Instance.CanAfford(productPrice))
        {
            CoinManager.Instance.SpendCoin(productPrice);
            PlayerPrefs.SetInt("KoleksiIoT.Purchased." + productKey, 1);
            PlayerPrefs.Save();
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        bool purchased = IsPurchased();

        if (statusText != null)
            statusText.text = purchased ? "Sudah Dibeli" : "Harga: " + productPrice + " Koin";

        if (buyButton != null)
            buyButton.gameObject.SetActive(!purchased);

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
