using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class StarterIoTController : MonoBehaviour
{
    [System.Serializable]
    public class IoTDeviceDef
    {
        public string productKey;
        public string displayName;
        public int productPrice;
        public Color activeColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    }

    [System.Serializable]
    public class IoTDeviceUI
    {
        public string productKey;                     // harus cocok dengan productKey di devices
        public Button toggleButton;                   // tombol untuk toggle ON/OFF
        public TextMeshProUGUI statusText;            // teks status (BELI/ON/OFF)
        //public Image backgroundImage;                 // background untuk warna status
    }

    [Header("Device Definitions")]
    public IoTDeviceDef[] devices;

    [Header("Manual UI References")]
    public IoTDeviceUI[] deviceUIs;                   // assign manual di Inspector

    [Header("SFX")]
    [SerializeField] private AudioClip toggleOnSfx;
    [SerializeField] private AudioClip toggleOffSfx;

    [Header("Warna (opsional)")]
    public Color ownedColor = new Color(0.2f, 0.8f, 0.3f, 1f);

    private readonly Dictionary<string, bool> activeStates = new Dictionary<string, bool>();
    private readonly Dictionary<string, IoTDeviceDef> deviceDefMap = new Dictionary<string, IoTDeviceDef>();
    private static StarterIoTController instance;

    public static StarterIoTController Instance => instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        BuildDeviceMap();
    }

    private void OnEnable()
    {
        RegisterUIListeners();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnregisterUIListeners();
    }

    private void BuildDeviceMap()
    {
        deviceDefMap.Clear();
        if (devices == null) return;
        foreach (IoTDeviceDef def in devices)
        {
            if (def == null || string.IsNullOrEmpty(def.productKey))
                continue;

            if (def.productPrice <= 0)
                def.productPrice = GetDefaultPrice(def.productKey);

            deviceDefMap[def.productKey] = def;
        }
    }

    // ========== Save/Load active states ==========
    public Dictionary<string, bool> GetActiveStates()
    {
        return new Dictionary<string, bool>(activeStates);
    }

    public void SetActiveStates(Dictionary<string, bool> states)
    {
        foreach (var kvp in states)
        {
            if (IsPurchased(kvp.Key))
                activeStates[kvp.Key] = kvp.Value;
        }

        RefreshAll();
    }

    // ========== Public API untuk status ==========
    public static bool CheckPurchased(string productKey)
    {
        return PlayerPrefs.GetInt(GameConstants.Persistence.KoleksiIoTPurchasedPrefix + productKey, 0) == 1;
    }

    public bool IsPurchased(string productKey)
    {
        return CheckPurchased(productKey);
    }

    public bool IsActiveForNeed(string productKey)
    {
        return IsActive(productKey);
    }

    public bool IsActive(string productKey)
    {
        return IsPurchased(productKey) && activeStates.ContainsKey(productKey) && activeStates[productKey];
    }

    public void ToggleDevice(string productKey)
    {
        if (!IsPurchased(productKey)) return;
        bool current = activeStates.ContainsKey(productKey) && activeStates[productKey];
        activeStates[productKey] = !current;
        if (SFXManager.Instance != null)
            SFXManager.Instance.PlaySFX(activeStates[productKey] ? toggleOnSfx : toggleOffSfx);
        SaveManager.SaveIotStates(activeStates);
        RefreshAll();
    }

    public void SetDeviceActive(string productKey, bool active)
    {
        if (!IsPurchased(productKey)) return;
        activeStates[productKey] = active;
        RefreshAll();
    }

    // ========== Untuk pembelian (dipanggil dari toko IoT) ==========
    public bool PurchaseDevice(string productKey)
    {
        if (string.IsNullOrEmpty(productKey)) return false;
        if (IsPurchased(productKey)) return true;

        int price = GetDevicePrice(productKey);
        if (CoinManager.Instance == null || !CoinManager.Instance.SpendCoin(price))
            return false;

        PlayerPrefs.SetInt(GameConstants.Persistence.KoleksiIoTPurchasedPrefix + productKey, 1);
        PlayerPrefs.Save();
        // Set default active false
        if (!activeStates.ContainsKey(productKey))
            activeStates[productKey] = false;
        SaveManager.SaveIotStates(activeStates);
        RefreshAll();
        return true;
    }

    // ========== Manual UI ==========
    private void RegisterUIListeners()
    {
        if (deviceUIs == null) return;
        foreach (var ui in deviceUIs)
        {
            if (ui == null || ui.toggleButton == null) continue;
            string key = ui.productKey;
            ui.toggleButton.onClick.RemoveAllListeners();
            ui.toggleButton.onClick.AddListener(() => HandleDeviceButton(key));
        }
    }

    private void UnregisterUIListeners()
    {
        if (deviceUIs == null) return;
        foreach (var ui in deviceUIs)
        {
            if (ui?.toggleButton != null)
                ui.toggleButton.onClick.RemoveAllListeners();
        }
    }

    public void RefreshAll()
    {
        if (deviceUIs == null) return;

        foreach (var ui in deviceUIs)
        {
            if (ui == null || string.IsNullOrEmpty(ui.productKey)) continue;

            bool purchased = IsPurchased(ui.productKey);
            bool active = IsActive(ui.productKey);
            IoTDeviceDef def = GetDeviceDef(ui.productKey);

            if (ui.toggleButton != null)
            {
                ui.toggleButton.interactable = true;

                Image buttonImage = ui.toggleButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    Color targetColor;
                    if (!purchased)
                        targetColor = def?.lockedColor ?? Color.gray;
                    else if (active)
                        targetColor = def?.activeColor ?? ownedColor;
                    else
                        targetColor = def?.inactiveColor ?? Color.gray;

                    targetColor.a = 1f;  // paksa alpha penuh
                    buttonImage.color = targetColor;
                }
            }

            if (ui.statusText != null)
            {
                if (!purchased) ui.statusText.text = GetDevicePrice(ui.productKey) + " Koin";
                else if (active) ui.statusText.text = "ON";
                else ui.statusText.text = "OFF";
            }
        }
    }

    private void HandleDeviceButton(string productKey)
    {
        if (IsPurchased(productKey))
        {
            ToggleDevice(productKey);
            return;
        }

        PurchaseDevice(productKey);
    }

    private IoTDeviceDef GetDeviceDef(string productKey)
    {
        deviceDefMap.TryGetValue(productKey, out var def);
        return def;
    }

    private int GetDevicePrice(string productKey)
    {
        IoTDeviceDef def = GetDeviceDef(productKey);
        if (def != null && def.productPrice > 0)
            return def.productPrice;

        return GetDefaultPrice(productKey);
    }

    private static int GetDefaultPrice(string productKey)
    {
        switch (productKey)
        {
            case GameConstants.IoT.ProductKeyFeeder:
                return GameConstants.Economy.AutoFeederCost;
            case GameConstants.IoT.ProductKeyFan:
                return GameConstants.Economy.AutoFanCost;
            case GameConstants.IoT.ProductKeyHeater:
                return GameConstants.Economy.AutoHeaterCost;
            default:
                return 0;
        }
    }
}
