using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using SlotStateChanged = System.Action<StarterKandangSlot>;

public class StarterKandangSlot : MonoBehaviour, IPointerClickHandler, IHealthCheckListener
{
    public event SlotStateChanged StateChanged;

    [Header("Chicken Visual")]
    [SerializeField] private GameObject chickenVisual;
    [SerializeField] private Transform chickenParent;
    [SerializeField] private bool startsOccupied;
    [SerializeField] private Vector2 chickenVisualSize = new Vector2(108f, 118f);
    [SerializeField] private Vector2 chickenVisualOffset = new Vector2(0f, -8f);

    [Header("Bubble Sprites (Images)")]
    [SerializeField] private Sprite feedBubbleSprite;
    [SerializeField] private Sprite coolingBubbleSprite;
    [SerializeField] private Sprite heatingBubbleSprite;
    [SerializeField] private Sprite sellBubbleSprite;

    [Header("Bubble Visual")]
    [SerializeField] private GameObject bubbleVisual;
    [SerializeField] private Image bubbleImage;
    [SerializeField] private TextMeshProUGUI bubbleLabel;
    [SerializeField] private string feedBubbleText = "MAKAN";
    [SerializeField] private string coolingBubbleText = "KIPAS";
    [SerializeField] private string heatingBubbleText = "HEATER";
    [SerializeField] private string sellBubbleText = "JUAL";
    [SerializeField] private Vector2 bubbleSize = new Vector2(130f, 56f);
    [SerializeField] private Vector2 bubbleOffset = new Vector2(0f, 68f);

    [Header("Timing & Rewards")]
    [SerializeField] private float needInterval = GameConstants.StarterSlot.NeedInterval;
    [SerializeField] private float notificationDelay = GameConstants.StarterSlot.NotificationDelay;
    [SerializeField] private int baseSellReward = GameConstants.StarterSlot.BaseSellReward;
    [SerializeField] private int careBonus = GameConstants.StarterSlot.CareBonus;

    [Header("Optional Health Minigame")]
    [SerializeField] private bool useHealthMinigame;
    [SerializeField] private bool clearChickenOnHealthFail;

    [Header("Animation")]
    [SerializeField] private Animator chickenAnimator;
    [SerializeField] private string idleAnimParam = "";
    [SerializeField] private string heatAnimParam = "isbakar";
    [SerializeField] private string coldAnimParam = "isdingin";

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
        WaitingForHealthMinigame,
        WaitingForSellClick
    }

    public bool IsEmpty => currentState == SlotState.Empty;

    private void Awake()
    {
        PrepareSlotHitbox();
        EnsureBubbleVisual();
        SetOccupied(startsOccupied);

        if (chickenVisual != null)
        {
            PositionChickenVisual(chickenVisual.transform);
            chickenVisual.SetActive(occupied);
            FindAnimator();
        }

        ResetChickenProgress();
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
            AssignChickenAnimator(spawnedChicken);
        }
        else if (chickenVisual != null)
        {
            PositionChickenVisual(chickenVisual.transform);
            chickenVisual.SetActive(true);
            AssignChickenAnimator(chickenVisual);
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

        chickenAnimator = null;
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
            if (TryStartHealthMinigame())
                return;

            CompleteCurrentNeed();
            return;
        }

        if (currentState == SlotState.WaitingForSellClick)
        {
            if (CoinManager.Instance != null)
                CoinManager.Instance.AddCoin(sellReward);

            GameLog.Info($"{name}: Ayam dijual, +{sellReward} coin.");
            ClearChicken();
        }
    }

    public void OnHealthCheckSuccess()
    {
        if (currentState != SlotState.WaitingForHealthMinigame)
            return;

        CompleteCurrentNeed();
    }

    public void OnHealthCheckFailure()
    {
        if (currentState != SlotState.WaitingForHealthMinigame)
            return;

        ResetAnimationToNormal();

        if (clearChickenOnHealthFail)
        {
            ClearChicken();
            return;
        }

        HideBubble();
        StartNeedTimer();
        GameLog.Info($"{name}: Minigame kesehatan gagal, kebutuhan akan muncul lagi.");
    }

    private void SetOccupied(bool value)
    {
        occupied = value;
        currentState = occupied ? SlotState.WaitingForCareEvent : SlotState.Empty;
        NotifyStateChanged();
    }

    private void StartNeedTimer()
    {
        currentState = SlotState.WaitingForCareEvent;
        NotifyStateChanged();
        CoroutineHelper.StopAndStart(this, ref eventCoroutine, NeedEventDelay());
    }

    private void StopEventTimer()
    {
        CoroutineHelper.StopSafe(this, ref eventCoroutine);
    }

    private IEnumerator NeedEventDelay()
    {
        yield return new WaitForSeconds(needInterval + notificationDelay);

        if (currentState == SlotState.WaitingForCareEvent)
            ShowNextNeedBubble();
    }

    private void ShowNextNeedBubble()
    {
        currentNeed = GetNextNeed();
        Sprite needSprite = GetNeedSprite(currentNeed);
        ShowBubble(needSprite, GetNeedText(currentNeed));
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        UpdateAnimationByNeed(currentNeed);
        GameLog.Info($"{name}: Notifikasi {GetNeedText(currentNeed)} muncul.");
    }

    private void CompleteCurrentNeed()
    {
        ResetAnimationToNormal();

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
        GameLog.Info($"{name}: Kebutuhan {GetNeedText(currentNeed)} terpenuhi.");
    }

    private void ShowSellBubble()
    {
        ShowBubble(sellBubbleSprite, sellBubbleText);
        currentState = SlotState.WaitingForSellClick;
        NotifyStateChanged();
        GameLog.Info($"{name}: Semua kebutuhan terpenuhi, ayam siap dijual.");
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this);
    }

    private ChickenNeed GetNextNeed()
    {
        // Feed always first
        if (!feedSatisfied)
            return ChickenNeed.Feed;

        // If both cooling and heating are still unsatisfied, pick randomly
        if (!coolingSatisfied && !heatingSatisfied)
        {
            return Random.Range(0, 2) == 0 ? ChickenNeed.Cooling : ChickenNeed.Heating;
        }

        // Otherwise return the only unsatisfied need
        if (!coolingSatisfied)
            return ChickenNeed.Cooling;
        if (!heatingSatisfied)
            return ChickenNeed.Heating;

        // Should never reach here (sell bubble will be shown instead)
        return ChickenNeed.Feed;
    }

    private Sprite GetNeedSprite(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Feed:
                return feedBubbleSprite;
            case ChickenNeed.Cooling:
                return coolingBubbleSprite;
            case ChickenNeed.Heating:
                return heatingBubbleSprite;
            default:
                return feedBubbleSprite;
        }
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
        ResetAnimationToNormal();
    }

    private void ShowBubble(Sprite sprite, string label)
    {
        EnsureBubbleVisual();

        if (bubbleVisual != null)
            bubbleVisual.SetActive(true);

        if (bubbleImage != null)
        {
            bubbleImage.sprite = sprite;
            bubbleImage.color = Color.white; // Use sprite's original colors
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
        bubbleLabel.fontSize = GameConstants.UI.BubbleLabelFontSize;
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

    private void FindAnimator()
    {
        if (chickenAnimator == null && chickenVisual != null)
            chickenAnimator = chickenVisual.GetComponentInChildren<Animator>(true);
    }

    private void AssignChickenAnimator(GameObject source)
    {
        chickenAnimator = source != null ? source.GetComponentInChildren<Animator>(true) : null;
    }

    private void UpdateAnimationByNeed(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Heating:
                TrySetAnimationTrigger(heatAnimParam);
                break;
            case ChickenNeed.Cooling:
                TrySetAnimationTrigger(coldAnimParam);
                break;
            case ChickenNeed.Feed:
                ResetAnimationToNormal();
                break;
        }
    }

    private void ResetAnimationToNormal()
    {
        if (chickenAnimator == null || !occupied)
            return;

        ResetAnimationTrigger(heatAnimParam);
        ResetAnimationTrigger(coldAnimParam);
        TrySetAnimationTrigger(idleAnimParam);
    }

    private bool TryStartHealthMinigame()
    {
        if (!useHealthMinigame)
            return false;

        if (PopupKesehatan.Instance == null)
        {
            Debug.LogWarning($"{name}: PopupKesehatan belum tersedia, kebutuhan diselesaikan langsung.");
            return false;
        }

        currentState = SlotState.WaitingForHealthMinigame;
        NotifyStateChanged();
        PopupKesehatan.Instance.ShowHealthCheck(this);
        return true;
    }

    private void TrySetAnimationTrigger(string triggerName)
    {
        if (chickenAnimator == null || string.IsNullOrWhiteSpace(triggerName))
            return;

        if (!HasAnimatorParameter(triggerName))
        {
            Debug.LogWarning($"{name}: Animator parameter '{triggerName}' tidak ditemukan.");
            return;
        }

        chickenAnimator.SetTrigger(triggerName);
    }

    private void ResetAnimationTrigger(string triggerName)
    {
        if (chickenAnimator == null || string.IsNullOrWhiteSpace(triggerName) || !HasAnimatorParameter(triggerName))
            return;

        chickenAnimator.ResetTrigger(triggerName);
    }

    private bool HasAnimatorParameter(string parameterName)
    {
        foreach (AnimatorControllerParameter parameter in chickenAnimator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
