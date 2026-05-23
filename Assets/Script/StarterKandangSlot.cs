using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class StarterKandangSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Chicken Visual")]
    [SerializeField] private GameObject chickenVisual;
    [SerializeField] private Transform chickenParent;
    [SerializeField] private bool startsOccupied;
    [SerializeField] private Vector2 chickenVisualSize = new Vector2(108f, 118f);
    [SerializeField] private Vector2 chickenVisualOffset = new Vector2(0f, -8f);

    [Header("Bubble Event")]
    [SerializeField] private GameObject bubbleVisual;
    [SerializeField] private Image bubbleImage;
    [SerializeField] private Sprite[] careBubbleSprites;
    [SerializeField] private Sprite harvestBubbleSprite;
    [SerializeField] private TextMeshProUGUI bubbleLabel;
    [SerializeField] private string feedBubbleText = "MAKAN";
    [SerializeField] private string coolingBubbleText = "KIPAS";
    [SerializeField] private string heatingBubbleText = "HEATER";
    [SerializeField] private string sellBubbleText = "JUAL";
    [SerializeField] private Vector2 bubbleSize = new Vector2(130f, 56f);
    [SerializeField] private Vector2 bubbleOffset = new Vector2(0f, 68f);
    [SerializeField] private Color feedBubbleColor = new Color(0.95f, 0.78f, 0.22f, 1f);
    [SerializeField] private Color coolingBubbleColor = new Color(0.25f, 0.65f, 0.95f, 1f);
    [SerializeField] private Color heatingBubbleColor = new Color(0.95f, 0.45f, 0.22f, 1f);
    [SerializeField] private Color sellBubbleColor = new Color(0.31f, 0.78f, 0.32f, 1f);
    [SerializeField] private float needInterval = 5f;
    [SerializeField] private float notificationDelay = 1f;
    [SerializeField] private int baseSellReward = 20;
    [SerializeField] private int careBonus = 10;

    private GameObject spawnedChicken;
    private Coroutine eventCoroutine;
    private bool occupied;
    private bool feedSatisfied;
    private bool coolingSatisfied;
    private bool heatingSatisfied;
    private int sellReward;
    private ChickenNeed currentNeed;
    private SlotState currentState;

    private enum ChickenNeed
    {
        Feed,
        Cooling,
        Heating
    }

    private enum SlotState
    {
        Empty,
        WaitingForCareEvent,
        WaitingForCareClick,
        WaitingForSellClick
    }

    public bool IsEmpty => currentState == SlotState.Empty;

    private void Awake()
    {
        PrepareSlotHitbox();
        EnsureBubbleVisual();
        ResetChickenProgress();
        SetOccupied(startsOccupied);

        if (chickenVisual != null)
        {
            PositionChickenVisual(chickenVisual.transform);
            chickenVisual.SetActive(occupied);
        }

        HideBubble();

        if (occupied)
            StartNeedTimer();
    }

    public bool TryPlaceChicken(GameObject chickenPrefab)
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning($"{name}: Slot kandang tidak aktif, ayam tidak bisa ditempatkan.");
            return false;
        }

        if (!IsEmpty)
            return false;

        if (chickenPrefab != null)
        {
            Transform parent = chickenParent != null ? chickenParent : transform;
            spawnedChicken = Instantiate(chickenPrefab, parent);
            PositionChickenVisual(spawnedChicken.transform);
        }
        else if (chickenVisual != null)
        {
            PositionChickenVisual(chickenVisual.transform);
            chickenVisual.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"{name}: Tidak ada prefab atau visual ayam untuk ditampilkan.");
        }

        ResetChickenProgress();
        SetOccupied(true);
        StartNeedTimer();
        return true;
    }

    public void ClearChicken()
    {
        StopEventTimer();

        if (spawnedChicken != null)
        {
            Destroy(spawnedChicken);
            spawnedChicken = null;
        }

        if (chickenVisual != null)
            chickenVisual.SetActive(false);

        HideBubble();
        ResetChickenProgress();
        SetOccupied(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
            return;

        if (currentState == SlotState.WaitingForCareClick)
        {
            CompleteCurrentNeed();
            return;
        }

        if (currentState == SlotState.WaitingForSellClick)
        {
            if (CoinManager.Instance != null)
                CoinManager.Instance.AddCoin(sellReward);

            Debug.Log($"{name}: Ayam dijual, +{sellReward} coin.");
            ClearChicken();
        }
    }

    private void SetOccupied(bool value)
    {
        occupied = value;
        currentState = occupied ? SlotState.WaitingForCareEvent : SlotState.Empty;
    }

    private void StartNeedTimer()
    {
        StopEventTimer();
        currentState = SlotState.WaitingForCareEvent;
        eventCoroutine = StartCoroutine(NeedEventDelay());
    }

    private void StopEventTimer()
    {
        if (eventCoroutine == null)
            return;

        StopCoroutine(eventCoroutine);
        eventCoroutine = null;
    }

    private System.Collections.IEnumerator NeedEventDelay()
    {
        yield return new WaitForSeconds(needInterval + notificationDelay);

        if (currentState == SlotState.WaitingForCareEvent)
            ShowNextNeedBubble();
    }

    private void ShowNextNeedBubble()
    {
        Sprite bubbleSprite = null;
        if (careBubbleSprites != null && careBubbleSprites.Length > 0)
            bubbleSprite = careBubbleSprites[Random.Range(0, careBubbleSprites.Length)];

        currentNeed = GetNextNeed();
        ShowBubble(bubbleSprite, GetNeedText(currentNeed), GetNeedColor(currentNeed));
        currentState = SlotState.WaitingForCareClick;
        Debug.Log($"{name}: Notifikasi {GetNeedText(currentNeed)} muncul.");
    }

    private void CompleteCurrentNeed()
    {
        switch (currentNeed)
        {
            case ChickenNeed.Feed:
                feedSatisfied = true;
                break;
            case ChickenNeed.Cooling:
                coolingSatisfied = true;
                break;
            case ChickenNeed.Heating:
                heatingSatisfied = true;
                break;
        }

        sellReward += careBonus;

        if (IsReadyToSell())
        {
            ShowSellBubble();
            return;
        }

        HideBubble();
        StartNeedTimer();
        Debug.Log($"{name}: Kebutuhan {GetNeedText(currentNeed)} terpenuhi.");
    }

    private void ShowSellBubble()
    {
        ShowBubble(harvestBubbleSprite, sellBubbleText, sellBubbleColor);
        currentState = SlotState.WaitingForSellClick;
        Debug.Log($"{name}: Semua kebutuhan terpenuhi, ayam siap dijual.");
    }

    private ChickenNeed GetNextNeed()
    {
        if (!feedSatisfied)
            return ChickenNeed.Feed;

        if (!coolingSatisfied)
            return ChickenNeed.Cooling;

        return ChickenNeed.Heating;
    }

    private string GetNeedText(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Feed:
                return feedBubbleText;
            case ChickenNeed.Cooling:
                return coolingBubbleText;
            case ChickenNeed.Heating:
                return heatingBubbleText;
            default:
                return feedBubbleText;
        }
    }

    private Color GetNeedColor(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Feed:
                return feedBubbleColor;
            case ChickenNeed.Cooling:
                return coolingBubbleColor;
            case ChickenNeed.Heating:
                return heatingBubbleColor;
            default:
                return feedBubbleColor;
        }
    }

    private bool IsReadyToSell()
    {
        return feedSatisfied && coolingSatisfied && heatingSatisfied;
    }

    private void ResetChickenProgress()
    {
        feedSatisfied = false;
        coolingSatisfied = false;
        heatingSatisfied = false;
        sellReward = baseSellReward;
    }

    private void ShowBubble(Sprite sprite, string label, Color fallbackColor)
    {
        EnsureBubbleVisual();

        if (bubbleVisual != null)
            bubbleVisual.SetActive(true);

        if (bubbleImage != null)
        {
            bubbleImage.sprite = sprite;
            bubbleImage.color = sprite != null ? Color.white : fallbackColor;
            bubbleImage.enabled = true;
        }

        if (bubbleLabel != null)
        {
            bubbleLabel.text = label;
            bubbleLabel.enabled = true;
        }
    }

    private void HideBubble()
    {
        if (bubbleVisual != null)
            bubbleVisual.SetActive(false);

        if (bubbleImage != null)
            bubbleImage.enabled = false;

        if (bubbleLabel != null)
            bubbleLabel.enabled = false;
    }

    private void EnsureBubbleVisual()
    {
        if (bubbleVisual == null)
            CreateFallbackBubble();

        if (bubbleVisual == null)
            return;

        if (bubbleImage == null)
            bubbleImage = bubbleVisual.GetComponent<Image>();

        if (bubbleLabel == null)
            bubbleLabel = bubbleVisual.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void CreateFallbackBubble()
    {
        GameObject bubbleObject = new GameObject("AutoBubble", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bubbleObject.transform.SetParent(transform, false);

        RectTransform bubbleRect = bubbleObject.GetComponent<RectTransform>();
        bubbleRect.anchorMin = new Vector2(0.5f, 0.5f);
        bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
        bubbleRect.pivot = new Vector2(0.5f, 0.5f);
        bubbleRect.sizeDelta = bubbleSize;
        bubbleRect.anchoredPosition = bubbleOffset;

        bubbleVisual = bubbleObject;
        bubbleImage = bubbleObject.GetComponent<Image>();
        bubbleImage.raycastTarget = false;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(bubbleObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        bubbleLabel = labelObject.GetComponent<TextMeshProUGUI>();
        bubbleLabel.alignment = TextAlignmentOptions.Center;
        bubbleLabel.fontSize = 22f;
        bubbleLabel.fontStyle = FontStyles.Bold;
        bubbleLabel.color = new Color(0.12f, 0.16f, 0.10f, 1f);
        bubbleLabel.raycastTarget = false;
    }

    private void PrepareSlotHitbox()
    {
        Image slotImage = GetComponent<Image>();
        if (slotImage == null)
            return;

        slotImage.color = new Color(1f, 1f, 1f, 0f);
        slotImage.raycastTarget = true;
    }

    private void PositionChickenVisual(Transform visual)
    {
        if (visual == null)
            return;

        visual.SetAsLastSibling();
        visual.localRotation = Quaternion.identity;
        visual.localScale = Vector3.one;

        RectTransform rect = visual as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = chickenVisualSize;
            rect.anchoredPosition = chickenVisualOffset;
        }
        else
        {
            visual.localPosition = new Vector3(chickenVisualOffset.x, chickenVisualOffset.y, 0f);
        }

        Image[] images = visual.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
            image.raycastTarget = false;

        TextMeshProUGUI[] labels = visual.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
            label.raycastTarget = false;
    }
}
