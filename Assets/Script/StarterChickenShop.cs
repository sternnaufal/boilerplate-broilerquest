using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class StarterChickenOption
{
    public string displayName = "Ayam";
    public int price = 25;
    public GameObject chickenPrefab;
    public Sprite icon;
    public Button buyButton;
    public TextMeshProUGUI labelText;
}

public class StarterChickenShop : MonoBehaviour
{
    [Header("Shop Options")]
    [SerializeField] private StarterChickenOption[] options;

    [Header("Kandang Slots")]
    [SerializeField] private StarterKandangSlot[] kandangSlots;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private string startupMessage = "Beli ayam. Ayam akan masuk ke kandang kosong pertama.";
    [SerializeField] private string noCoinMessage = "Coin belum cukup.";
    [SerializeField] private string noSlotMessage = "Semua kandang sudah terisi.";
    [SerializeField] private string boughtMessage = "Ayam berhasil dibeli.";

    private bool listenersRegistered;

    private void OnEnable()
    {
        RegisterButtonListeners();
        UpdateOptionLabels();
        PolishShopButtons();
        RefreshShopState();

        if (messageText != null && string.IsNullOrWhiteSpace(messageText.text))
            ShowMessage(startupMessage);
    }

    private void Update()
    {
        RefreshShopState();
    }

    private void RegisterButtonListeners()
    {
        if (listenersRegistered || options == null)
            return;

        for (int i = 0; i < options.Length; i++)
        {
            int optionIndex = i;
            Button button = options[i] != null ? options[i].buyButton : null;
            RegisterButton(button, () => TryBuyChicken(optionIndex));
        }

        listenersRegistered = true;
    }

    private static void RegisterButton(Button button, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    public void BuyOption0()
    {
        TryBuyChicken(0);
    }

    public void BuyOption1()
    {
        TryBuyChicken(1);
    }

    public void BuyOption2()
    {
        TryBuyChicken(2);
    }

    public bool TryBuyChicken(int optionIndex)
    {
        StarterChickenOption option = GetOption(optionIndex);
        if (option == null)
            return false;

        StarterKandangSlot emptySlot = FindEmptyKandang();
        if (emptySlot == null)
        {
            ShowMessage(noSlotMessage);
            return false;
        }

        if (CoinManager.Instance == null || !CoinManager.Instance.SpendCoin(option.price))
        {
            ShowMessage(noCoinMessage);
            RefreshShopState();
            return false;
        }

        if (!emptySlot.TryPlaceChicken(option.chickenPrefab))
        {
            CoinManager.Instance.AddCoin(option.price);
            ShowMessage(noSlotMessage);
            RefreshShopState();
            return false;
        }

        ShowMessage($"{option.displayName}: {boughtMessage}. Slot kosong: {GetEmptySlotCount()}.");
        RefreshShopState();
        return true;
    }

    public void RefreshShopState()
    {
        if (options == null)
            return;

        bool hasEmptySlot = FindEmptyKandang() != null;

        for (int i = 0; i < options.Length; i++)
        {
            StarterChickenOption option = options[i];
            if (option == null)
                continue;

            bool canAfford = CoinManager.Instance != null && CoinManager.Instance.CanAfford(option.price);

            if (option.buyButton != null)
            {
                option.buyButton.interactable = hasEmptySlot && canAfford;
                StyleButtonState(option.buyButton, hasEmptySlot && canAfford);
            }
        }
    }

    private void UpdateOptionLabels()
    {
        if (options == null)
            return;

        for (int i = 0; i < options.Length; i++)
        {
            StarterChickenOption option = options[i];
            if (option != null && option.labelText != null)
                option.labelText.text = $"{option.displayName} - {option.price}";
        }
    }

    private void PolishShopButtons()
    {
        if (options == null)
            return;

        for (int i = 0; i < options.Length; i++)
        {
            StarterChickenOption option = options[i];
            if (option == null)
                continue;

            if (option.buyButton != null)
            {
                StyleButtonState(option.buyButton, true);
                EnsureOptionIcon(option);
            }

            if (option.labelText != null)
            {
                option.labelText.color = new Color(0.12f, 0.15f, 0.08f, 1f);
                option.labelText.fontSize = Mathf.Max(option.labelText.fontSize, 24f);
                option.labelText.fontStyle = FontStyles.Bold;
                option.labelText.alignment = TextAlignmentOptions.MidlineLeft;
                RectTransform labelRect = option.labelText.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(86f, 8f);
                labelRect.offsetMax = new Vector2(-12f, -8f);
            }
        }

        if (messageText != null)
        {
            messageText.color = new Color(1f, 0.96f, 0.78f, 1f);
            messageText.fontSize = Mathf.Max(messageText.fontSize, 24f);
            messageText.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void StyleButtonState(Button button, bool interactable)
    {
        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        image.color = interactable
            ? new Color(0.95f, 0.72f, 0.22f, 1f)
            : new Color(0.48f, 0.42f, 0.28f, 0.82f);
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

    private StarterKandangSlot FindEmptyKandang()
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

    private int GetEmptySlotCount()
    {
        if (kandangSlots == null)
            return 0;

        int emptyCount = 0;
        foreach (StarterKandangSlot slot in kandangSlots)
        {
            if (slot != null && slot.gameObject.activeInHierarchy && slot.IsEmpty)
                emptyCount++;
        }

        return emptyCount;
    }

    private void EnsureOptionIcon(StarterChickenOption option)
    {
        if (option == null || option.buyButton == null)
            return;

        Sprite sprite = option.icon != null ? option.icon : FindFallbackIcon(option);
        if (sprite == null)
            return;

        Transform buttonTransform = option.buyButton.transform;
        Transform existing = buttonTransform.Find("ItemIcon");
        Image iconImage;

        if (existing != null)
        {
            iconImage = existing.GetComponent<Image>();
        }
        else
        {
            GameObject iconObject = new GameObject("ItemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(buttonTransform, false);
            iconObject.transform.SetAsFirstSibling();
            iconImage = iconObject.GetComponent<Image>();
        }

        RectTransform rect = iconImage.rectTransform;
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(44f, 0f);
        rect.sizeDelta = new Vector2(62f, 62f);

        iconImage.sprite = sprite;
        iconImage.preserveAspect = true;
        iconImage.color = Color.white;
        iconImage.raycastTarget = false;
    }

    private Sprite FindFallbackIcon(StarterChickenOption option)
    {
        if (option != null && option.chickenPrefab != null)
        {
            Image prefabImage = option.chickenPrefab.GetComponentInChildren<Image>(true);
            if (prefabImage != null && prefabImage.sprite != null)
                return prefabImage.sprite;
        }

        if (kandangSlots == null)
            return null;

        foreach (StarterKandangSlot slot in kandangSlots)
        {
            if (slot == null)
                continue;

            Image[] slotImages = slot.GetComponentsInChildren<Image>(true);
            foreach (Image slotImage in slotImages)
            {
                if (slotImage != null && slotImage.sprite != null)
                    return slotImage.sprite;
            }
        }

        return null;
    }

    private void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.Log(message);
    }
}
