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

    [Header("Bubble Event")]
    [SerializeField] private GameObject bubbleVisual;
    [SerializeField] private Image bubbleImage;
    [SerializeField] private Sprite[] careBubbleSprites;
    [SerializeField] private Sprite harvestBubbleSprite;
    [SerializeField] private TextMeshProUGUI bubbleLabel;
    [SerializeField] private string careBubbleText = "RAWAT";
    [SerializeField] private string harvestBubbleText = "PANEN";
    [SerializeField] private Vector2 bubbleSize = new Vector2(130f, 56f);
    [SerializeField] private Vector2 bubbleOffset = new Vector2(0f, 68f);
    [SerializeField] private Color careBubbleColor = new Color(0.95f, 0.78f, 0.22f, 1f);
    [SerializeField] private Color harvestBubbleColor = new Color(0.31f, 0.78f, 0.32f, 1f);
    [SerializeField] private float minEventDelay = 3f;
    [SerializeField] private float maxEventDelay = 8f;
    [SerializeField] private int harvestReward = 10;

    private GameObject spawnedChicken;
    private Coroutine eventCoroutine;
    private bool occupied;
    private SlotState currentState;

    private enum SlotState
    {
        Empty,
        WaitingForCareEvent,
        WaitingForCareClick,
        WaitingForHarvestClick
    }

    public bool IsEmpty => currentState == SlotState.Empty;

    private void Awake()
    {
        EnsureBubbleVisual();
        SetOccupied(startsOccupied);

        if (chickenVisual != null)
            chickenVisual.SetActive(occupied);

        HideBubble();

        if (occupied)
            StartCareEventTimer();
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
            spawnedChicken.transform.localPosition = Vector3.zero;
            spawnedChicken.transform.localRotation = Quaternion.identity;
            spawnedChicken.transform.localScale = Vector3.one;
        }
        else if (chickenVisual != null)
        {
            chickenVisual.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"{name}: Tidak ada prefab atau visual ayam untuk ditampilkan.");
        }

        SetOccupied(true);
        StartCareEventTimer();
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
        SetOccupied(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
            return;

        if (currentState == SlotState.WaitingForCareClick)
        {
            ShowHarvestBubble();
            return;
        }

        if (currentState == SlotState.WaitingForHarvestClick)
        {
            if (CoinManager.Instance != null)
                CoinManager.Instance.AddCoin(harvestReward);

            Debug.Log($"{name}: Ayam dipanen, +{harvestReward} coin.");
            ClearChicken();
        }
    }

    private void SetOccupied(bool value)
    {
        occupied = value;
        currentState = occupied ? SlotState.WaitingForCareEvent : SlotState.Empty;
    }

    private void StartCareEventTimer()
    {
        StopEventTimer();
        currentState = SlotState.WaitingForCareEvent;
        eventCoroutine = StartCoroutine(CareEventDelay());
    }

    private void StopEventTimer()
    {
        if (eventCoroutine == null)
            return;

        StopCoroutine(eventCoroutine);
        eventCoroutine = null;
    }

    private System.Collections.IEnumerator CareEventDelay()
    {
        float delay = Random.Range(minEventDelay, maxEventDelay);
        yield return new WaitForSeconds(delay);

        if (currentState == SlotState.WaitingForCareEvent)
            ShowCareBubble();
    }

    private void ShowCareBubble()
    {
        Sprite bubbleSprite = null;
        if (careBubbleSprites != null && careBubbleSprites.Length > 0)
            bubbleSprite = careBubbleSprites[Random.Range(0, careBubbleSprites.Length)];

        ShowBubble(bubbleSprite, careBubbleText, careBubbleColor);
        currentState = SlotState.WaitingForCareClick;
        Debug.Log($"{name}: Bubble perawatan muncul.");
    }

    private void ShowHarvestBubble()
    {
        ShowBubble(harvestBubbleSprite, harvestBubbleText, harvestBubbleColor);
        currentState = SlotState.WaitingForHarvestClick;
        Debug.Log($"{name}: Perawatan selesai, bubble coin muncul.");
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
}
