using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using SlotStateChanged = System.Action<StarterKandangSlot>;

public partial class StarterKandangSlot : MonoBehaviour, IPointerClickHandler, IHealthCheckListener
{
    public event SlotStateChanged StateChanged;

    [Header("Chicken Visual")]
    [SerializeField] private GameObject chickenVisual;
    [SerializeField] private Transform chickenParent;
    [SerializeField] private bool startsOccupied;
    [SerializeField] private int chickensPerPurchase = 8;
    [SerializeField] private Vector2 chickenVisualSize = new Vector2(108f, 118f);
    [SerializeField] private Vector2 packedChickenVisualSize = new Vector2(42f, 50f);
    [SerializeField] private Vector2 chickenVisualOffset = new Vector2(0f, -8f);
    [SerializeField] private Vector2 packedChickenSpacing = new Vector2(44f, 34f);

    [Header("Slot Label")]
    [SerializeField] private TextMeshProUGUI slotLabel;
    [SerializeField] private string slotLabelFormat = "KANDANG {0}";
    [SerializeField] private bool autoNumberSlotLabel = true;

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
    [SerializeField] private float notificationDelay = GameConstants.StarterSlot.NotificationDelay;
    [SerializeField] private float needIntervalMin = GameConstants.StarterSlot.NeedIntervalMin;
    [SerializeField] private float needIntervalMax = GameConstants.StarterSlot.NeedIntervalMax;

    [Header("Optional Health Minigame")]
    [SerializeField] private bool useHealthMinigame;
    [SerializeField] private bool clearChickenOnHealthFail;

    [Header("Jigsaw Puzzle Textures")]
    [SerializeField] private Texture jigsawFeedTexture;
    [SerializeField] private Texture jigsawCoolingTexture;
    [SerializeField] private Texture jigsawHeatingTexture;

    [Header("Animation")]
    [SerializeField] private Animator chickenAnimator;
    [SerializeField] private string idleAnimParam = "";
    [SerializeField] private string heatAnimParam = "isbakar";
    [SerializeField] private string coldAnimParam = "isdingin";

    private readonly List<GameObject> spawnedChickens = new List<GameObject>();
    private Coroutine eventCoroutine;
    private bool occupied;
    private bool feedSatisfied;
    private bool coolingSatisfied;
    private bool heatingSatisfied;
    private bool feedFailed;
    private bool coolingFailed;
    private bool heatingFailed;
    private int completedCareCount;
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
    public bool CanAcceptChicken => IsEmpty;
    public int CurrentChickenCount => GetActiveChickenVisuals().Count;

    private void Awake()
    {
        RefreshSlotLabel();
        PrepareSlotHitbox();
        EnsureBubbleVisual();
        SetOccupied(startsOccupied);

        if (chickenVisual != null)
        {
            PositionChickenVisual(chickenVisual.transform, chickenVisualOffset, chickenVisualSize);
            chickenVisual.SetActive(occupied);
            FindAnimator();
        }

        ResetChickenProgress();
        PositionAllChickenVisuals();
        HideBubble();

        if (occupied)
        {
            StartNeedTimer();
            UpdateWanderState();
        }
    }

    private void OnTransformParentChanged()
    {
        RefreshSlotLabel();
    }

    public bool TryPlaceChicken(GameObject chickenPrefab)
    {
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning($"{name}: Slot kandang tidak aktif, ayam tidak bisa ditempatkan.");
            return false;
        }

        if (!CanAcceptChicken)
            return false;

        int visualCount = Mathf.Max(1, chickensPerPurchase);
        int placedCount = 0;
        for (int i = 0; i < visualCount; i++)
        {
            if (CreateChickenVisual(chickenPrefab) != null)
                placedCount++;
        }

        if (placedCount == 0)
        {
            Debug.LogWarning($"{name}: Tidak ada prefab atau visual ayam untuk ditampilkan.");
            return false;
        }

        PositionAllChickenVisuals();
        ResetChickenProgress();
        SetOccupied(true);
        StartNeedTimer();

        UpdateWanderState();
        return true;
    }

    public void ClearChicken()
    {
        StopEventTimer();
        StopWander();

        foreach (GameObject spawnedChicken in spawnedChickens)
        {
            if (spawnedChicken != null)
                Destroy(spawnedChicken);
        }
        spawnedChickens.Clear();

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
            if (currentNeed == ChickenNeed.Feed && (FeedManager.Instance == null || !FeedManager.Instance.CanUseFeed(1)))
            {
                GameLog.Info($"{name}: Pakan tidak cukup! Beli pakan dulu.");
                return;
            }

            if (currentNeed == ChickenNeed.Feed)
                FeedManager.Instance.UseFeed(1);

            if (TryStartHealthMinigame())
                return;

            CompleteCurrentNeed();
            return;
        }

        if (currentState == SlotState.WaitingForSellClick)
        {
            int finalReward = sellReward;
            if (CoinManager.Instance != null)
                CoinManager.Instance.AddCoin(sellReward);

            GameLog.Info($"{name}: {CurrentChickenCount} ayam dijual, +{finalReward} coin.");
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
        MarkCurrentNeedFailed();
        completedCareCount++;
        RecalculateSellReward();

        if (IsReadyToSell())
        {
            ShowSellBubble();
            return;
        }

        HideBubble();
        isWanderingPaused = false;
        StartNeedTimer();
        GameLog.Info($"{name}: Kebutuhan {GetNeedText(currentNeed)} gagal, lanjut kebutuhan berikutnya.");
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
        float randomDelay = Random.Range(needIntervalMin, needIntervalMax) + notificationDelay;
        yield return new WaitForSeconds(randomDelay);

        if (currentState == SlotState.WaitingForCareEvent)
            ShowNextNeedBubble();
    }

    private void ShowNextNeedBubble()
    {
        currentNeed = GetNextNeed();

        if (TryAutoCompleteByIoT())
            return;

        Sprite needSprite = GetNeedSprite(currentNeed);
        ShowBubble(needSprite, GetNeedText(currentNeed));
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        UpdateAnimationByNeed(currentNeed);
        GameLog.Info($"{name}: Notifikasi {GetNeedText(currentNeed)} muncul.");
    }

    private bool TryAutoCompleteByIoT()
    {
        if (StarterIoTController.Instance == null)
            return false;

        string iotKey = GetIoTKeyForNeed(currentNeed);
        if (string.IsNullOrEmpty(iotKey))
            return false;

        if (StarterIoTController.Instance.IsActiveForNeed(iotKey))
        {
            GameLog.Info($"{name}: IoT {iotKey} aktif, kebutuhan {GetNeedText(currentNeed)} otomatis terpenuhi.");
            UpdateAnimationByNeed(currentNeed);
            CompleteCurrentNeed();
            return true;
        }

        return false;
    }

    private static string GetIoTKeyForNeed(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Feed:
                return GameConstants.IoT.ProductKeyFeeder;
            case ChickenNeed.Cooling:
                return GameConstants.IoT.ProductKeyFan;
            case ChickenNeed.Heating:
                return GameConstants.IoT.ProductKeyHeater;
            default:
                return null;
        }
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

        completedCareCount++;
        RecalculateSellReward();

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

    private GameObject CreateChickenVisual(GameObject chickenPrefab)
    {
        Transform parent = chickenParent != null ? chickenParent : transform;

        GameObject prefabToInstantiate = chickenPrefab != null ? chickenPrefab : chickenVisual;
        if (prefabToInstantiate == null)
            return null;

        GameObject visual = Instantiate(prefabToInstantiate, parent);
        visual.SetActive(true);
        spawnedChickens.Add(visual);

        AssignChickenAnimator(visual);
        return visual;
    }

    private List<GameObject> GetActiveChickenVisuals()
    {
        spawnedChickens.RemoveAll(chicken => chicken == null);

        List<GameObject> visuals = new List<GameObject>();
        foreach (GameObject spawnedChicken in spawnedChickens)
        {
            if (spawnedChicken != null && spawnedChicken.activeSelf)
                visuals.Add(spawnedChicken);
        }

        return visuals;
    }

    private void PositionAllChickenVisuals()
    {
        List<GameObject> visuals = GetActiveChickenVisuals();
        int count = visuals.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 size = count > 1 ? packedChickenVisualSize : chickenVisualSize;
            Vector2 offset = count > 1 ? GetPackedChickenOffset(i, count) : chickenVisualOffset;
            PositionChickenVisual(visuals[i].transform, offset, size);
        }
    }

    private Vector2 GetPackedChickenOffset(int index, int count)
    {
        int columnCount = Mathf.Min(2, Mathf.Max(1, count));
        int rowCount = Mathf.CeilToInt(count / (float)columnCount);
        int row = index / columnCount;
        int column = index % columnCount;

        float x = (column - (columnCount - 1) * 0.5f) * packedChickenSpacing.x;
        float y = ((rowCount - 1) * 0.5f - row) * packedChickenSpacing.y;
        return chickenVisualOffset + new Vector2(x, y - 10f);
    }

    private void RecalculateSellReward()
    {
        int failCount = (feedFailed ? 1 : 0) + (coolingFailed ? 1 : 0) + (heatingFailed ? 1 : 0);
        sellReward = Mathf.Max(0, GameConstants.Economy.BaseSellPrice - failCount * GameConstants.Economy.FailPenalty);
    }

    private void RefreshSlotLabel()
    {
        if (!autoNumberSlotLabel)
            return;

        if (slotLabel == null)
            slotLabel = FindSlotLabel();

        if (slotLabel == null)
            return;

        int slotNumber = GetSlotIndexInParent() + 1;
        slotLabel.text = string.Format(slotLabelFormat, slotNumber);
    }

    private TextMeshProUGUI FindSlotLabel()
    {
        TextMeshProUGUI[] labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI label in labels)
        {
            if (label != null && label.gameObject.name == "Kandang")
                return label;
        }

        return labels.Length > 0 ? labels[0] : null;
    }

    private int GetSlotIndexInParent()
    {
        if (transform.parent == null)
            return transform.GetSiblingIndex();

        int slotIndex = 0;
        for (int i = 0; i < transform.parent.childCount; i++)
        {
            Transform sibling = transform.parent.GetChild(i);
            StarterKandangSlot siblingSlot = sibling.GetComponent<StarterKandangSlot>();
            if (siblingSlot == null)
                continue;

            if (siblingSlot == this)
                return slotIndex;

            slotIndex++;
        }

        return transform.GetSiblingIndex();
    }

    private ChickenNeed GetNextNeed()
    {
        bool feedDone = feedSatisfied || feedFailed;
        bool coolingDone = coolingSatisfied || coolingFailed;
        bool heatingDone = heatingSatisfied || heatingFailed;

        // Feed always first
        if (!feedDone)
            return ChickenNeed.Feed;

        // If both cooling and heating are still not done, pick randomly
        if (!coolingDone && !heatingDone)
        {
            return Random.Range(0, 2) == 0 ? ChickenNeed.Cooling : ChickenNeed.Heating;
        }

        // Otherwise return the only not-done need
        if (!coolingDone)
            return ChickenNeed.Cooling;
        if (!heatingDone)
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
        bool feedDone = feedSatisfied || feedFailed;
        bool coolingDone = coolingSatisfied || coolingFailed;
        bool heatingDone = heatingSatisfied || heatingFailed;
        return feedDone && coolingDone && heatingDone;
    }

    private void MarkCurrentNeedFailed()
    {
        switch (currentNeed)
        {
            case ChickenNeed.Feed:
                feedFailed = true;
                break;
            case ChickenNeed.Cooling:
                coolingFailed = true;
                break;
            case ChickenNeed.Heating:
                heatingFailed = true;
                break;
        }
    }

    private void ResetChickenProgress()
    {
        feedSatisfied = false;
        coolingSatisfied = false;
        heatingSatisfied = false;
        feedFailed = false;
        coolingFailed = false;
        heatingFailed = false;
        completedCareCount = 0;
        RecalculateSellReward();
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
            if (label == feedBubbleText || label == coolingBubbleText || label == heatingBubbleText)
            {
                bubbleLabel.text = "";
                bubbleLabel.enabled = false;
            }
            else
            {
                bubbleLabel.text = label;
                bubbleLabel.enabled = !string.IsNullOrEmpty(label);
            }
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

    private void PositionChickenVisual(Transform visual, Vector2 anchoredOffset, Vector2 visualSize)
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
            rect.sizeDelta = visualSize;
            rect.anchoredPosition = anchoredOffset;
        }
        else
        {
            visual.localPosition = new Vector3(anchoredOffset.x, anchoredOffset.y, 0f);
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
                TrySetAnimationBool(heatAnimParam, true);
                break;
            case ChickenNeed.Cooling:
                TrySetAnimationBool(coldAnimParam, true);
                break;
            case ChickenNeed.Feed:
                ResetAnimationToNormal();
                break;
        }
    }

    private void ResetAnimationToNormal()
    {
        if (!occupied)
            return;

        TrySetAnimationBool(heatAnimParam, false);
        TrySetAnimationBool(coldAnimParam, false);
    }

    private bool TryStartHealthMinigame()
    {
        if (!useHealthMinigame)
            return false;

        if (JigsawMinigameController.Instance != null && JigsawMinigameController.Instance.IsPlaying)
            return true;

        JigsawMinigameController jigsawController = JigsawMinigameController.Instance;
        if (jigsawController != null)
        {
            Texture puzzleTexture = GetNeedPuzzleTexture(currentNeed);
            if (puzzleTexture != null)
            {
                currentState = SlotState.WaitingForHealthMinigame;
                NotifyStateChanged();

                if (jigsawController.ShowJigsaw(this, puzzleTexture, GetNeedTitle(currentNeed)))
                    return true;

                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
            }
        }

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

    private Texture GetNeedPuzzleTexture(ChickenNeed need)
    {
        Texture configuredTexture = null;
        Sprite fallbackSprite = null;

        switch (need)
        {
            case ChickenNeed.Feed:
                configuredTexture = jigsawFeedTexture;
                fallbackSprite = feedBubbleSprite;
                break;
            case ChickenNeed.Cooling:
                configuredTexture = jigsawCoolingTexture;
                fallbackSprite = coolingBubbleSprite;
                break;
            case ChickenNeed.Heating:
                configuredTexture = jigsawHeatingTexture;
                fallbackSprite = heatingBubbleSprite;
                break;
        }

        if (configuredTexture != null)
            return configuredTexture;

        return fallbackSprite != null ? fallbackSprite.texture : null;
    }

    private string GetNeedTitle(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Feed:
                return "Susun Puzzle Pakan";
            case ChickenNeed.Cooling:
                return "Susun Puzzle Dingin";
            case ChickenNeed.Heating:
                return "Susun Puzzle Panas";
            default:
                return "Susun Puzzle";
        }
    }

    private void TrySetAnimationBool(string paramName, bool value)
    {
        if (string.IsNullOrWhiteSpace(paramName))
            return;

        List<Animator> animators = GetActiveChickenAnimators();
        if (animators.Count == 0)
            return;

        bool anyAnimatorHandled = false;
        foreach (Animator animator in animators)
        {
            if (!HasAnimatorParameter(animator, paramName))
                continue;

            animator.SetBool(paramName, value);
            anyAnimatorHandled = true;
        }

        if (!anyAnimatorHandled)
            Debug.LogWarning($"{name}: Animator parameter '{paramName}' tidak ditemukan.");
    }

    private List<Animator> GetActiveChickenAnimators()
    {
        List<Animator> animators = new List<Animator>();
        foreach (GameObject visual in GetActiveChickenVisuals())
        {
            Animator animator = visual != null ? visual.GetComponentInChildren<Animator>(true) : null;
            if (animator != null)
                animators.Add(animator);
        }

        if (animators.Count == 0 && chickenAnimator != null)
            animators.Add(chickenAnimator);

        return animators;
    }

    private bool HasAnimatorParameter(Animator animator, string parameterName)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
