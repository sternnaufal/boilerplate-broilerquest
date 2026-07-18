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
        public GameObject onObject;
        public GameObject offObject;
        public GameObject buyObject;
        public Animator deviceAnimator; 
        public Animator[] deviceAnimators; 
        public string animatorParam = "isOn";
        public GameObject deviceVisual;
    }

    [Header("Device Definitions")]
    public IoTDeviceDef[] devices;

    [Header("Manual UI References")]
    public IoTDeviceUI[] deviceUIs;                   // assign manual di Inspector

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
            SFXManager.Instance.PlayIotToggle(activeStates[productKey]);
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
        if (CoinManager.Instance == null || !CoinManager.Instance.CanAfford(price))
            return false;

        int coinsAfter = CoinManager.Instance.GetTotalCoin() - price;
        int reserve = GetSoftlockReserve();
        if (coinsAfter < reserve)
        {
            GameLog.Info($"IoT: Tidak cukup coin — butuh {price + reserve} coin (harga {price} + cadangan {reserve}).");
            return false;
        }

        CoinManager.Instance.SpendCoin(price);

        PlayerPrefs.SetInt(GameConstants.Persistence.KoleksiIoTPurchasedPrefix + productKey, 1);
        PlayerPrefs.Save();
        // Set default active false
        if (!activeStates.ContainsKey(productKey))
            activeStates[productKey] = false;
        SaveManager.SaveIotStates(activeStates);
        RefreshAll();
        return true;
    }

    private static int GetSoftlockReserve()
    {
        bool hasChickens = false;
        bool hasFeed = FeedManager.Instance != null && FeedManager.Instance.GetFeedCount() > 0;

        var slots = GameObject.FindObjectsByType<StarterKandangSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var slot in slots)
        {
            if (slot != null && !slot.IsEmpty) { hasChickens = true; break; }
        }

        if (!hasChickens) return GameConstants.Economy.ChickenPrice + GameConstants.Economy.FeedCost;
        if (!hasFeed) return GameConstants.Economy.FeedCost;
        return 0;
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

            // Update tombol
            if (ui.toggleButton != null)
            {
                bool canBuy = purchased || (CoinManager.Instance != null
                    && CoinManager.Instance.GetTotalCoin() - GetDevicePrice(ui.productKey) >= GetSoftlockReserve());
                ui.toggleButton.interactable = purchased || canBuy;
                Image buttonImage = ui.toggleButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    Color targetColor = purchased
                        ? (active ? (def?.activeColor ?? ownedColor) : (def?.inactiveColor ?? Color.gray))
                        : (def?.lockedColor ?? Color.gray);
                    buttonImage.color = targetColor;
                }
            }

            if (ui.onObject != null) ui.onObject.SetActive(false);
            if (ui.offObject != null) ui.offObject.SetActive(false);
            if (ui.buyObject != null) ui.buyObject.SetActive(false);

            GameObject target = null;
            if (!purchased)
                target = ui.buyObject;
            else if (active)
                target = ui.onObject;
            else
                target = ui.offObject;

            if (target != null)
            {
                target.SetActive(true);
                StartCoroutine(AnimateIconIn(target.transform));
            }

            if (!purchased)
            {
                if (ui.statusText != null)
                    ui.statusText.text = GetDevicePrice(ui.productKey) + "";
            }
            else
            {
                if (ui.statusText != null) ui.statusText.text = "";
            }

            // ⭐ Update animators (baru)
            if (ui.deviceAnimators != null)
            {
                bool isActive = purchased && active;
                foreach (Animator anim in ui.deviceAnimators)
                {
                    if (anim != null)
                        anim.SetBool(ui.animatorParam, isActive);
                }
            }

            if (ui.deviceVisual != null)
            {
                ui.deviceVisual.SetActive(purchased);
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

        int price = GetDevicePrice(productKey);
        Vector2 screenPos = GetButtonScreenPos(productKey);

        if (CoinManager.Instance == null || !CoinManager.Instance.CanAfford(price))
        {
            FloatingFeedback.ShowText($"Butuh {price} coin!", screenPos, new Color(1f, 0.3f, 0.3f));
            UIAlertPanel.Instance?.Show(UIAlertPanel.NotificationType.CoinOut);
            return;
        }

        int reserve = GetSoftlockReserve();
        int coinsAfter = CoinManager.Instance.GetTotalCoin() - price;
        if (coinsAfter < reserve)
        {
            string reason = reserve == GameConstants.Economy.ChickenPrice + GameConstants.Economy.FeedCost
                ? $"Sisakan {reserve} coin\nuntuk ayam & pakan!"
                : $"Sisakan {reserve} coin\nuntuk beli pakan!";
            FloatingFeedback.ShowText(reason, screenPos, new Color(1f, 0.7f, 0.2f));
            return;
        }

        PurchaseDevice(productKey);
    }

    private Vector2 GetButtonScreenPos(string productKey)
    {
        if (deviceUIs == null) return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        foreach (var ui in deviceUIs)
        {
            if (ui?.productKey == productKey && ui.toggleButton != null)
                return RectTransformUtility.WorldToScreenPoint(null, ui.toggleButton.transform.position);
        }
        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
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

    private System.Collections.IEnumerator AnimateIconIn(Transform target)
    {
        target.localScale = Vector3.zero;
        float half = 0.1f;
        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 1.12f, t * t * (3f - 2f * t));
            yield return null;
        }
        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / half);
            target.localScale = Vector3.Lerp(Vector3.one * 1.12f, Vector3.one, t * t * (3f - 2f * t));
            yield return null;
        }
        target.localScale = Vector3.one;
    }
}
