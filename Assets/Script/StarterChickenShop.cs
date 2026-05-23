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
        if (button == null || button.onClick.GetPersistentEventCount() > 0)
            return;

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

        ShowMessage($"{option.displayName}: {boughtMessage}. Tunggu notif di kandang.");
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
                StyleButtonState(option.buyButton, true);

            if (option.labelText != null)
            {
                option.labelText.color = new Color(0.12f, 0.15f, 0.08f, 1f);
                option.labelText.fontSize = Mathf.Max(option.labelText.fontSize, 24f);
                option.labelText.fontStyle = FontStyles.Bold;
                option.labelText.alignment = TextAlignmentOptions.Center;
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

    private void ShowMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;

        Debug.Log(message);
    }
}
