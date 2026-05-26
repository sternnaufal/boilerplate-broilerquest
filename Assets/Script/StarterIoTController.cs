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
        public Color activeColor = new Color(0.2f, 0.8f, 0.3f, 1f);
        public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    }

    [Header("Device Definitions")]
    public IoTDeviceDef[] devices;

    [Header("UI Layout")]
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private GameObject deviceRowPrefab;
    [SerializeField] private Color ownedColor = new Color(0.2f, 0.8f, 0.3f, 1f);

    private readonly Dictionary<string, bool> activeStates = new Dictionary<string, bool>();
    private readonly List<GameObject> rowInstances = new List<GameObject>();
    private static StarterIoTController instance;

    public static StarterIoTController Instance => instance;

    private readonly Dictionary<string, IoTDeviceDef> deviceDefMap = new Dictionary<string, IoTDeviceDef>();

    private void Awake()
    {
        instance = this;
        BuildDeviceMap();
    }

    private void OnEnable()
    {
        BuildUI();
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void BuildDeviceMap()
    {
        deviceDefMap.Clear();
        if (devices == null)
            return;

        foreach (IoTDeviceDef def in devices)
        {
            if (def != null && !string.IsNullOrEmpty(def.productKey))
                deviceDefMap[def.productKey] = def;
        }
    }

    public bool IsPurchased(string productKey)
    {
        return PlayerPrefs.GetInt(GameConstants.Persistence.KoleksiIoTPurchasedPrefix + productKey, 0) == 1;
    }

    public bool IsActive(string productKey)
    {
        return IsPurchased(productKey) && activeStates.ContainsKey(productKey) && activeStates[productKey];
    }

    public void ToggleDevice(string productKey)
    {
        if (!IsPurchased(productKey))
            return;

        bool current = activeStates.ContainsKey(productKey) && activeStates[productKey];
        activeStates[productKey] = !current;
        RefreshAll();
    }

    public void SetDeviceActive(string productKey, bool active)
    {
        if (!IsPurchased(productKey))
            return;

        activeStates[productKey] = active;
        RefreshAll();
    }

    public bool IsActiveForNeed(string productKey)
    {
        return IsActive(productKey);
    }

    private void BuildUI()
    {
        ClearRows();
        if (!autoCreateUI || devices == null)
            return;

        Transform parent = transform;

        GameObject headerObj = CreateHeaderLabel(parent);
        rowInstances.Add(headerObj);

        foreach (IoTDeviceDef def in devices)
        {
            if (def == null || string.IsNullOrEmpty(def.productKey))
                continue;

            GameObject row = CreateDeviceRow(parent, def);
            rowInstances.Add(row);
        }
    }

    private GameObject CreateHeaderLabel(Transform parent)
    {
        GameObject header = new GameObject("IoTHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        header.transform.SetParent(parent, false);
        RectTransform rect = header.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -10f);
        rect.sizeDelta = new Vector2(0f, 36f);

        TextMeshProUGUI text = header.GetComponent<TextMeshProUGUI>();
        text.text = "--- Panel IoT ---";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.95f, 0.72f, 0.22f, 1f);
        return header;
    }

    private GameObject CreateDeviceRow(Transform parent, IoTDeviceDef def)
    {
        GameObject row = new GameObject("IoT_" + def.productKey, typeof(RectTransform), typeof(Image), typeof(Button));
        row.transform.SetParent(parent, false);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0f, 1f);
        rowRect.anchorMax = new Vector2(1f, 1f);
        rowRect.pivot = new Vector2(0.5f, 1f);
        rowRect.sizeDelta = new Vector2(0f, 52f);

        Button button = row.GetComponent<Button>();
        ButtonHelper.AddListenerOnce(button, () => ToggleDevice(def.productKey));

        string capturedKey = def.productKey;
        IoTDeviceDef capturedDef = def;

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(row.transform, false);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0.7f, 1f);
        labelRect.offsetMin = new Vector2(12f, 0f);
        labelRect.offsetMax = new Vector2(0f, 0f);
        TextMeshProUGUI labelText = labelObj.GetComponent<TextMeshProUGUI>();
        labelText.text = def.displayName;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.fontSize = 20f;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = Color.white;

        GameObject statusObj = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObj.transform.SetParent(row.transform, false);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.7f, 0f);
        statusRect.anchorMax = new Vector2(1f, 1f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = new Vector2(-12f, 0f);
        TextMeshProUGUI statusText = statusObj.GetComponent<TextMeshProUGUI>();
        statusText.alignment = TextAlignmentOptions.MidlineRight;
        statusText.fontSize = 18f;
        statusText.fontStyle = FontStyles.Bold;

        return row;
    }

    public void RefreshAll()
    {
        if (devices == null)
            return;

        foreach (IoTDeviceDef def in devices)
        {
            if (def == null || string.IsNullOrEmpty(def.productKey))
                continue;

            GameObject row = FindRow(def.productKey);
            if (row == null)
                continue;

            bool purchased = IsPurchased(def.productKey);
            bool active = IsActive(def.productKey);

            Image bg = row.GetComponent<Image>();
            Button button = row.GetComponent<Button>();

            if (!purchased)
            {
                if (bg != null) bg.color = def.lockedColor;
                if (button != null) button.interactable = false;
            }
            else if (active)
            {
                if (bg != null) bg.color = def.activeColor;
                if (button != null) button.interactable = true;
            }
            else
            {
                if (bg != null) bg.color = def.inactiveColor;
                if (button != null) button.interactable = true;
            }

            TextMeshProUGUI statusText = FindChildText(row, "Status");
            if (statusText != null)
            {
                if (!purchased)
                    statusText.text = "BELI";
                else if (active)
                    statusText.text = "ON";
                else
                    statusText.text = "OFF";
            }
        }
    }

    private GameObject FindRow(string productKey)
    {
        foreach (GameObject row in rowInstances)
        {
            if (row != null && row.name == "IoT_" + productKey)
                return row;
        }
        return null;
    }

    private void ClearRows()
    {
        foreach (GameObject row in rowInstances)
        {
            if (row != null)
                Destroy(row);
        }
        rowInstances.Clear();
    }

    private static TextMeshProUGUI FindChildText(GameObject parent, string childName)
    {
        TextMeshProUGUI[] texts = parent.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI t in texts)
        {
            if (t.gameObject.name == childName)
                return t;
        }
        return null;
    }
}
