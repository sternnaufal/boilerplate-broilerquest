using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KoleksiIoTController : MonoBehaviour
{
    [System.Serializable]
    public class IoTProduct
    {
        public string productKey;
        public string productName;
        public int productPrice;
        public Texture productImage;
    }

    [Header("Products")]
    [SerializeField] private IoTProduct[] products;

    [Header("UI References")]
    [SerializeField] private Transform productContainer;
    [SerializeField] private Button backButton;

    [Header("Card Colors")]
    [SerializeField] private Color ownedColor = new Color(0.2f, 0.6f, 0.3f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Awake()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged += OnCoinsChanged;
    }

    private void Start()
    {
        EnsureDefaultProducts();

        if (backButton != null)
            ButtonHelper.AddListenerOnce(backButton, GoBack);

        if (CoinManager.Instance != null)
        {
            TextMeshProUGUI coinDisplay = GameObject.Find("CoinText")?.GetComponent<TextMeshProUGUI>();
            if (coinDisplay != null)
                CoinManager.Instance.BindCoinText(coinDisplay);
        }

        SetupAllCards();
    }

    private void EnsureDefaultProducts()
    {
        if (products != null && products.Length > 0)
            return;

        products = new IoTProduct[]
        {
            new IoTProduct
            {
                productKey = GameConstants.IoT.ProductKeyFeeder,
                productName = GameConstants.IoT.ProductNameFeeder,
                productPrice = GameConstants.Economy.AutoFeederCost
            },
            new IoTProduct
            {
                productKey = GameConstants.IoT.ProductKeyFan,
                productName = GameConstants.IoT.ProductNameFan,
                productPrice = GameConstants.Economy.AutoFanCost
            },
            new IoTProduct
            {
                productKey = GameConstants.IoT.ProductKeyHeater,
                productName = GameConstants.IoT.ProductNameHeater,
                productPrice = GameConstants.Economy.AutoHeaterCost
            }
        };
    }

    private void OnDestroy()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged -= OnCoinsChanged;
    }

    private void OnCoinsChanged(int totalCoin)
    {
        RefreshAllCards();
    }

    private void SetupAllCards()
    {
        if (products == null || productContainer == null)
            return;

        foreach (IoTProduct product in products)
        {
            Transform cardTransform = productContainer.Find(product.productKey);
            if (cardTransform == null)
                continue;

            SetupCard(cardTransform.gameObject, product);
        }

        RefreshAllCards();
    }

    private void SetupCard(GameObject card, IoTProduct product)
    {
        string key = product.productKey;
        int price = product.productPrice;

        RawImage image = card.GetComponentInChildren<RawImage>(true);
        if (image != null && product.productImage != null)
            image.texture = product.productImage;

        TextMeshProUGUI nameText = FindTextInChildren(card, "NameText");
        if (nameText != null)
            nameText.text = product.productName;

        TextMeshProUGUI priceText = FindTextInChildren(card, "PriceText");
        Button buyButton = FindButtonInChildren(card, "BuyButton");
        GameObject ownedBadge = FindChildByName(card, "OwnedBadge");
        Image cardBg = card.GetComponent<Image>();

        if (buyButton != null)
            ButtonHelper.AddListenerOnce(buyButton, () => BuyProduct(product));

        RefreshCard(card, key, price, buyButton, priceText, ownedBadge, cardBg);
    }

    private void RefreshAllCards()
    {
        if (products == null || productContainer == null)
            return;

        foreach (IoTProduct product in products)
        {
            Transform cardTransform = productContainer.Find(product.productKey);
            if (cardTransform == null)
                continue;

            GameObject card = cardTransform.gameObject;
            string key = product.productKey;
            int price = product.productPrice;

            Button buyButton = FindButtonInChildren(card, "BuyButton");
            TextMeshProUGUI priceText = FindTextInChildren(card, "PriceText");
            GameObject ownedBadge = FindChildByName(card, "OwnedBadge");
            Image cardBg = card.GetComponent<Image>();

            RefreshCard(card, key, price, buyButton, priceText, ownedBadge, cardBg);
        }
    }

    private void RefreshCard(GameObject card, string key, int price, Button buyButton, TextMeshProUGUI priceText, GameObject ownedBadge, Image cardBg)
    {
        bool purchased = IsPurchased(key);

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(!purchased);
            if (!purchased)
            {
                bool canAfford = CoinManager.Instance != null && CoinManager.Instance.CanAfford(price);
                buyButton.interactable = canAfford;
            }
        }

        if (priceText != null)
            priceText.text = purchased ? "" : price + " Koin";

        if (ownedBadge != null)
            ownedBadge.SetActive(purchased);

        if (cardBg != null)
            cardBg.color = purchased ? ownedColor : lockedColor;
    }

    private void BuyProduct(IoTProduct product)
    {
        if (CoinManager.Instance == null || !CoinManager.Instance.CanAfford(product.productPrice))
            return;

        if (!CoinManager.Instance.SpendCoin(product.productPrice))
            return;

        PlayerPrefs.SetInt(GameConstants.Persistence.KoleksiIoTPurchasedPrefix + product.productKey, 1);
        PlayerPrefs.Save();
        RefreshAllCards();
    }

    private bool IsPurchased(string productKey)
    {
        return StarterIoTController.CheckPurchased(productKey);
    }

    private static TextMeshProUGUI FindTextInChildren(GameObject parent, string name)
    {
        TextMeshProUGUI[] texts = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.gameObject.name == name)
                return text;
        }
        return null;
    }

    private static Button FindButtonInChildren(GameObject parent, string name)
    {
        Button[] buttons = parent.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.gameObject.name == name)
                return button;
        }
        return null;
    }

    private static GameObject FindChildByName(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    public void GoBack()
    {
        if (SceneController.Instance != null)
            SceneController.Instance.GoToMainMenu();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
