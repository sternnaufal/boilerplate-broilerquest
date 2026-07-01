using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StarterChickenOption
{
    public string displayName = "Ayam";
    [NonSerialized] public int price = GameConstants.Economy.ChickenPrice;
    public GameObject chickenPrefab;
    public Sprite icon;
    public Button buyButton;
    public TextMeshProUGUI labelText;
}

[System.Serializable]
public class ShopButtonStyleConfig
{
    public Color interactableColor = new Color(0.95f, 0.72f, 0.22f, 1f);
    public Color disabledColor = new Color(0.48f, 0.42f, 0.28f, 0.82f);
    public Color labelColor = new Color(0.12f, 0.15f, 0.08f, 1f);
    public float labelFontSize = 24f;
    public Color messageColor = new Color(1f, 0.96f, 0.78f, 1f);
}

public class StarterChickenShop : MonoBehaviour
{
    [Header("Shop Options")]
    [SerializeField] private StarterChickenOption[] options;

    [Header("Kandang Slots")]
    [SerializeField] private StarterKandangSlot[] kandangSlots;

    [Header("Button Styles")]
    [SerializeField] private ShopButtonStyleConfig buttonStyle = new ShopButtonStyleConfig();

    [Header("Feed Purchase")]
    [SerializeField] private Button feedBuyButton;
    [SerializeField] private TextMeshProUGUI feedBuyLabel;
    [SerializeField] private string feedBuyButtonText = "Beli Pakan";
    [SerializeField] private string feedBoughtMessage = "Pakan berhasil dibeli!";
    [SerializeField] private string noCoinFeedMessage = "Duitmu tidak cukup!";

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string startupMessage = "Beli ayam. Satu pembelian mengisi satu kandang dengan beberapa ayam.";
    [SerializeField] private string noCoinMessage = "Coin belum cukup.";
    [SerializeField] private string noSlotMessage = "Semua kandang sudah terisi.";
    [SerializeField] private string boughtMessage = "Ayam berhasil dibeli.";

    private bool listenersRegistered;
    private bool isSubscribedToSlots;
    private bool kandangSlotsResolved;

    private void Awake()
    {
        ResolveKandangSlots();
        kandangSlotsResolved = true;
    }

    private void OnEnable()
    {
        if (!kandangSlotsResolved)
        {
            ResolveKandangSlots();
            kandangSlotsResolved = true;
        }
        SubscribeToStateChanges();
        RegisterButtonListeners();
        RegisterFeedButton();
        OverridePrices();
        UpdateOptionLabels();
        RefreshShopState();

        if (messageText != null && string.IsNullOrWhiteSpace(messageText.text))
            ShowMessage(startupMessage);
    }

    private void OnDisable()
    {
        UnsubscribeFromStateChanges();
    }

    private void RegisterButtonListeners()
    {
        if (listenersRegistered || options == null)
            return;

        for (int i = 0; i < options.Length; i++)
        {
            int optionIndex = i;
            Button button = options[i] != null ? options[i].buyButton : null;
            ButtonHelper.AddListenerOnce(button, () => TryBuyChicken(optionIndex));
        }

        listenersRegistered = true;
    }

    private void RegisterFeedButton()
    {
        EnsureFeedButton();
        if (feedBuyButton != null)
            ButtonHelper.AddListenerOnce(feedBuyButton, TryBuyFeed);
    }

    private void EnsureFeedButton()
    {
        if (feedBuyButton != null)
            return;

        Debug.LogWarning($"{name}: Feed buy button is not assigned. Assign ShopAPK/Pakan and ShopAPK/Pakan/Label in the Inspector.");
    }

    public void TryBuyFeed()
    {
        int cost = GameConstants.Economy.FeedCost;
        int increment = GameConstants.Economy.FeedIncrement;

        if (CoinManager.Instance == null || !CoinManager.Instance.CanAfford(cost))
        {
            ShowMessage(noCoinFeedMessage);
            UIAlertPanel.Instance?.Show(UIAlertPanel.NotificationType.CoinOut);
            if (SFXManager.Instance != null) SFXManager.Instance.PlayBuyFail();
            return;
        }

        if (HasNoChickens())
        {
            ShowMessage("Beli ayam dulu sebelum beli pakan!");
            if (SFXManager.Instance != null) SFXManager.Instance.PlayBuyFail();
            return;
        }

        CoinManager.Instance.SpendCoin(cost);
        FeedManager.Instance.AddFeed(increment);
        ShowMessage(feedBoughtMessage);
        if (SFXManager.Instance != null) SFXManager.Instance.PlayFeedBuy();
        RefreshShopState();
    }

    public bool TryBuyChicken(int optionIndex)
    {
        StarterChickenOption option = GetOption(optionIndex);
        if (option == null)
            return false;

        StarterKandangSlot availableSlot = FindAvailableKandang();
        if (availableSlot == null)
        {
            ShowMessage(noSlotMessage);
            if (SFXManager.Instance != null) SFXManager.Instance.PlayBuyFail();
            return false;
        }

        if (CoinManager.Instance == null || !CoinManager.Instance.CanAfford(option.price))
        {
            ShowMessage(noCoinMessage);
            UIAlertPanel.Instance?.Show(UIAlertPanel.NotificationType.CoinOut);
            if (SFXManager.Instance != null) SFXManager.Instance.PlayBuyFail();
            RefreshShopState();
            return false;
        }

        int feedCount = FeedManager.Instance != null ? FeedManager.Instance.GetFeedCount() : 0;
        int coinsAfterBuy = CoinManager.Instance.GetTotalCoin() - option.price;
        if (!HasNoChickens() && feedCount == 0 && coinsAfterBuy < GameConstants.Economy.FeedCost)
        {
            ShowMessage("Beli pakan dulu sebelum tambah ayam!");
            if (SFXManager.Instance != null) SFXManager.Instance.PlayBuyFail();
            return false;
        }

        CoinManager.Instance.SpendCoin(option.price);

        if (!availableSlot.TryPlaceChicken(option.chickenPrefab))
        {
            CoinManager.Instance.AddCoin(option.price);
            ShowMessage("Gagal menaruh ayam: Prefab/Visual ayam tidak di-assign di Inspector.");
            if (SFXManager.Instance != null) SFXManager.Instance.PlayBuyFail();
            RefreshShopState();
            return false;
        }

        ShowMessage($"{option.displayName}: {boughtMessage}. Kandang kosong: {GetAvailableKandangCount()}.");
        if (SFXManager.Instance != null) SFXManager.Instance.PlayBuySuccess();
        RefreshShopState();
        return true;
    }

    public void RefreshShopState()
    {
        if (!kandangSlotsResolved)
            ResolveKandangSlots();

        SubscribeToStateChanges();

        if (options == null)
            return;

        bool hasAvailableSlot = FindAvailableKandang() != null;

        bool hasNoChickens = true;
        if (kandangSlots != null)
        {
            foreach (var slot in kandangSlots)
            {
                if (slot != null && !slot.IsEmpty)
                {
                    hasNoChickens = false;
                    break;
                }
            }
        }

        int feedCount = FeedManager.Instance != null ? FeedManager.Instance.GetFeedCount() : 0;
        int currentCoins = CoinManager.Instance != null ? CoinManager.Instance.GetTotalCoin() : 0;

        for (int i = 0; i < options.Length; i++)
        {
            StarterChickenOption option = options[i];
            if (option == null)
                continue;

            bool canAfford = CoinManager.Instance != null && CoinManager.Instance.CanAfford(option.price);
            bool feedSafe = hasNoChickens || feedCount > 0 || (currentCoins - option.price >= GameConstants.Economy.FeedCost);

            if (option.buyButton != null)
                option.buyButton.interactable = hasAvailableSlot && canAfford && feedSafe;
        }

        if (feedBuyButton != null)
        {
            bool canAffordFeed = !hasNoChickens
                && CoinManager.Instance != null
                && CoinManager.Instance.CanAfford(GameConstants.Economy.FeedCost);
            feedBuyButton.interactable = canAffordFeed;
        }

        if (feedBuyLabel != null)
        {
            int currentFeedCount = FeedManager.Instance != null ? FeedManager.Instance.GetFeedCount() : 0;
            feedBuyLabel.text = $"{feedBuyButtonText} - {GameConstants.Economy.FeedCost} ({currentFeedCount})";
        }
    }

    public void SetKandangSlots(StarterKandangSlot[] slots)
    {
        UnsubscribeFromStateChanges();
        kandangSlots = slots;
        kandangSlotsResolved = true;
        SubscribeToStateChanges();
        RefreshShopState();
    }

    private void UpdateOptionLabels()
    {
        if (options == null)
            return;

        for (int i = 0; i < options.Length; i++)
        {
            StarterChickenOption option = options[i];
            if (option != null && option.labelText != null)
                option.labelText.text = $"{option.price}";
        }
    }

    private void StyleButtonState(Button button, bool interactable)
    {
        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        image.color = interactable
            ? buttonStyle.interactableColor
            : buttonStyle.disabledColor;
    }

    private StarterChickenOption GetOption(int optionIndex)
    {
        if (options == null || optionIndex < 0 || optionIndex >= options.Length)
        {
            Debug.LogWarning($"Opsi ayam index {optionIndex} tidak tersedia.");
            return null;
        }

        return options[optionIndex];
    }

    private StarterKandangSlot FindAvailableKandang()
    {
        if (kandangSlots == null)
            return null;

        foreach (StarterKandangSlot slot in kandangSlots)
        {
            if (slot != null && slot.gameObject.activeInHierarchy && slot.IsEmpty)
                return slot;
        }

        return null;
    }

    private void ResolveKandangSlots()
    {
        StarterKandangSlot[] discoveredSlots = FindObjectsByType<StarterKandangSlot>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.InstanceID);

        if (discoveredSlots.Length == 0)
            return;

        if (HasCompleteConfiguredSlots(discoveredSlots.Length))
            return;

        kandangSlots = discoveredSlots;
        kandangSlotsResolved = true;
    }

    private bool HasCompleteConfiguredSlots(int discoveredSlotCount)
    {
        if (kandangSlots == null || kandangSlots.Length == 0)
            return false;

        int activeConfiguredCount = 0;
        foreach (StarterKandangSlot slot in kandangSlots)
        {
            if (slot != null && slot.gameObject.activeInHierarchy)
                activeConfiguredCount++;
        }

        return activeConfiguredCount == kandangSlots.Length && activeConfiguredCount >= discoveredSlotCount;
    }

    private void SubscribeToStateChanges()
    {
        if (isSubscribedToSlots)
            UnsubscribeFromStateChanges();

        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged += HandleCoinsChanged;

        if (kandangSlots != null)
        {
            foreach (StarterKandangSlot slot in kandangSlots)
            {
                if (slot != null)
                    slot.StateChanged += HandleSlotStateChanged;
            }
        }

        isSubscribedToSlots = true;
    }

    private void UnsubscribeFromStateChanges()
    {
        if (!isSubscribedToSlots)
            return;

        if (CoinManager.Instance != null)
            CoinManager.Instance.CoinsChanged -= HandleCoinsChanged;

        if (kandangSlots != null)
        {
            foreach (StarterKandangSlot slot in kandangSlots)
            {
                if (slot != null)
                    slot.StateChanged -= HandleSlotStateChanged;
            }
        }

        isSubscribedToSlots = false;
    }

    private void HandleCoinsChanged(int _)
    {
        RefreshShopState();
    }

    private void HandleSlotStateChanged(StarterKandangSlot _)
    {
        RefreshShopState();
    }

    private void OverridePrices()
    {
        if (options == null)
            return;

        foreach (StarterChickenOption option in options)
        {
            if (option != null)
                option.price = GameConstants.Economy.ChickenPrice;
        }
    }

    private bool HasNoChickens()
    {
        if (kandangSlots == null)
            return true;
        foreach (var slot in kandangSlots)
        {
            if (slot != null && !slot.IsEmpty)
                return false;
        }
        return true;
    }

    private int GetAvailableKandangCount()
    {
        if (kandangSlots == null)
            return 0;

        int availableCount = 0;
        foreach (StarterKandangSlot slot in kandangSlots)
        {
            if (slot != null && slot.gameObject.activeInHierarchy && slot.IsEmpty)
                availableCount++;
        }

        return availableCount;
    }

    private void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        GameLog.Info(message);
    }

    public GameObject GetChickenPrefabByName(string prefabName)
    {
        if (options == null || string.IsNullOrEmpty(prefabName)) return null;
        foreach (StarterChickenOption option in options)
        {
            if (option != null && option.chickenPrefab != null && option.chickenPrefab.name == prefabName)
                return option.chickenPrefab;
        }
        return null;
    }
}
