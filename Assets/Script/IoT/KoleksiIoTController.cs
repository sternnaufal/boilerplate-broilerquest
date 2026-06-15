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

    [Header("Locked Placeholder")]
    [SerializeField] private string lockedPlaceholderText = "?????";
    [SerializeField] private Color lockedImageColor = new Color(0.2f, 0.2f, 0.2f, 1f);

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
        RawImage image = card.GetComponentInChildren<RawImage>(true);
        if (image != null && product.productImage != null)
            image.texture = product.productImage;

        TextMeshProUGUI nameText = FindTextInChildren(card, "NameText");
        TextMeshProUGUI priceText = FindTextInChildren(card, "PriceText");
        Button buyButton = FindButtonInChildren(card, "BuyButton");
        GameObject ownedBadge = FindChildByName(card, "OwnedBadge");
        Image cardBg = card.GetComponent<Image>();

        RefreshCard(product, buyButton, nameText, priceText, image, ownedBadge, cardBg);
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

            Button buyButton = FindButtonInChildren(card, "BuyButton");
            TextMeshProUGUI nameText = FindTextInChildren(card, "NameText");
            TextMeshProUGUI priceText = FindTextInChildren(card, "PriceText");
            RawImage image = card.GetComponentInChildren<RawImage>(true);
            GameObject ownedBadge = FindChildByName(card, "OwnedBadge");
            Image cardBg = card.GetComponent<Image>();

            RefreshCard(product, buyButton, nameText, priceText, image, ownedBadge, cardBg);
        }
    }

    private void RefreshCard(IoTProduct product, Button buyButton, TextMeshProUGUI nameText, TextMeshProUGUI priceText, RawImage image, GameObject ownedBadge, Image cardBg)
    {
        if (product == null)
            return;

        bool purchased = IsPurchased(product.productKey);

        if (buyButton != null)
            buyButton.gameObject.SetActive(false);

        if (nameText != null)
            nameText.text = purchased ? product.productName : lockedPlaceholderText;

        if (priceText != null)
            priceText.text = purchased ? "" : lockedPlaceholderText;

        if (image != null)
        {
            if (purchased && product.productImage != null)
                image.texture = product.productImage;

            image.color = purchased ? Color.white : lockedImageColor;
        }

        if (ownedBadge != null)
            ownedBadge.SetActive(purchased);

        if (cardBg != null)
            cardBg.color = purchased ? ownedColor : lockedColor;
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
            if (SceneTransition.Instance != null)
                SceneTransition.Instance.LoadScene("MainMenu");
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
