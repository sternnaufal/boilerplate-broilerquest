using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
    [SerializeField] private GameObject productCardPrefab;
    [SerializeField] private Button backButton;

    [Header("Card Prefab Fallback Colors")]
    [SerializeField] private Color ownedColor = new Color(0.2f, 0.6f, 0.3f, 1f);
    [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private readonly List<GameObject> cardInstances = new List<GameObject>();

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

        BuildProductCards();
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

    private void BuildProductCards()
    {
        ClearCards();

        if (products == null || productContainer == null)
            return;

        foreach (IoTProduct product in products)
        {
            GameObject card;

            if (productCardPrefab != null)
            {
                card = Instantiate(productCardPrefab, productContainer);
            }
            else
            {
                card = CreateFallbackCard(product);
                card.transform.SetParent(productContainer, false);
            }

            SetupCard(card, product);
            cardInstances.Add(card);
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

        IoTProduct capturedProduct = product;

        if (buyButton != null)
        {
            ButtonHelper.AddListenerOnce(buyButton, () => BuyProduct(capturedProduct));
        }

        RefreshCard(card, key, price, buyButton, priceText, ownedBadge, cardBg);
    }

    private void RefreshAllCards()
    {
        if (products == null || cardInstances == null)
            return;

        for (int i = 0; i < cardInstances.Count && i < products.Length; i++)
        {
            GameObject card = cardInstances[i];
            IoTProduct product = products[i];
            if (card == null || product == null)
                continue;

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
        return PlayerPrefs.GetInt(GameConstants.Persistence.KoleksiIoTPurchasedPrefix + productKey, 0) == 1;
    }

    private GameObject CreateFallbackCard(IoTProduct product)
    {
        GameObject card = new GameObject("ProductCard_" + product.productKey, typeof(RectTransform), typeof(Image));
        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(280f, 320f);

        GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(card.transform, false);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -10f);
        nameRect.sizeDelta = new Vector2(260f, 40f);
        TextMeshProUGUI nameText = nameObj.GetComponent<TextMeshProUGUI>();
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.fontSize = 22f;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = Color.white;
        nameObj.name = "NameText";

        GameObject priceObj = new GameObject("PriceText", typeof(RectTransform), typeof(TextMeshProUGUI));
        priceObj.transform.SetParent(card.transform, false);
        RectTransform priceRect = priceObj.GetComponent<RectTransform>();
        priceRect.anchorMin = new Vector2(0.5f, 0.5f);
        priceRect.anchorMax = new Vector2(0.5f, 0.5f);
        priceRect.pivot = new Vector2(0.5f, 0.5f);
        priceRect.anchoredPosition = new Vector2(0f, 0f);
        priceRect.sizeDelta = new Vector2(200f, 40f);
        TextMeshProUGUI priceText = priceObj.GetComponent<TextMeshProUGUI>();
        priceText.alignment = TextAlignmentOptions.Center;
        priceText.fontSize = 20f;
        priceText.color = Color.white;
        priceObj.name = "PriceText";

        GameObject btnObj = new GameObject("BuyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(card.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.anchoredPosition = new Vector2(0f, 15f);
        btnRect.sizeDelta = new Vector2(200f, 50f);
        Image btnImage = btnObj.GetComponent<Image>();
        btnImage.color = new Color(0.95f, 0.72f, 0.22f, 1f);
        btnObj.name = "BuyButton";

        GameObject btnLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnLabel.transform.SetParent(btnObj.transform, false);
        RectTransform labelRect = btnLabel.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI labelText = btnLabel.GetComponent<TextMeshProUGUI>();
        labelText.text = "BELI";
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = 24f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = new Color(0.12f, 0.15f, 0.08f, 1f);

        GameObject badgeObj = new GameObject("OwnedBadge", typeof(RectTransform), typeof(Image));
        badgeObj.transform.SetParent(card.transform, false);
        RectTransform badgeRect = badgeObj.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 0f);
        badgeRect.anchorMax = new Vector2(0.5f, 0f);
        badgeRect.pivot = new Vector2(0.5f, 0f);
        badgeRect.anchoredPosition = new Vector2(0f, 15f);
        badgeRect.sizeDelta = new Vector2(200f, 50f);
        Image badgeImage = badgeObj.GetComponent<Image>();
        badgeImage.color = new Color(0.2f, 0.7f, 0.3f, 1f);
        badgeObj.name = "OwnedBadge";
        badgeObj.SetActive(false);

        GameObject badgeLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeLabel.transform.SetParent(badgeObj.transform, false);
        RectTransform badgeLabelRect = badgeLabel.GetComponent<RectTransform>();
        badgeLabelRect.anchorMin = Vector2.zero;
        badgeLabelRect.anchorMax = Vector2.one;
        badgeLabelRect.offsetMin = Vector2.zero;
        badgeLabelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI badgeLabelText = badgeLabel.GetComponent<TextMeshProUGUI>();
        badgeLabelText.text = "DIMILIKI";
        badgeLabelText.alignment = TextAlignmentOptions.Center;
        badgeLabelText.fontSize = 20f;
        badgeLabelText.fontStyle = FontStyles.Bold;
        badgeLabelText.color = Color.white;

        return card;
    }

    private void ClearCards()
    {
        foreach (GameObject card in cardInstances)
        {
            if (card != null)
                Destroy(card);
        }
        cardInstances.Clear();
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
