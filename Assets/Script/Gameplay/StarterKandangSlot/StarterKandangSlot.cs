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
    [SerializeField] private float bubbleExpiryDuration = GameConstants.StarterSlot.BubbleExpiryDurationStarter;

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
    private string placedPrefabName;
    private Coroutine eventCoroutine;
    private Coroutine bubbleExpiryCoroutine;
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
    public string SlotId => gameObject.name;

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

    private void OnDisable()
    {
        StopBubbleExpiryTimer();
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
        List<GameObject> newVisuals = new List<GameObject>(visualCount);

        try
        {
            for (int i = 0; i < visualCount; i++)
            {
                GameObject visual = CreateChickenVisual(chickenPrefab);
                if (visual != null)
                    newVisuals.Add(visual);
            }

            if (newVisuals.Count == 0)
            {
                Debug.LogWarning($"{name}: Tidak ada prefab atau visual ayam untuk ditampilkan.");
                return false;
            }

            spawnedChickens.AddRange(newVisuals);
            placedPrefabName = chickenPrefab != null ? chickenPrefab.name : "";
            PositionAllChickenVisuals();
            ResetChickenProgress();
            SetOccupied(true);
            StartNeedTimer();
            UpdateWanderState();
            SaveManager.SaveAll();
            return true;
        }
        catch
        {
            foreach (GameObject visual in newVisuals)
            {
                spawnedChickens.Remove(visual);
                if (visual != null)
                    Destroy(visual);
            }
            return false;
        }
    }

    public void ClearChicken(bool save = true)
    {
        StopBubbleExpiryTimer();
        StopEventTimer();
        StopWander();
        RegisterUntrackedChickenVisuals();

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
        if (save) SaveManager.SaveAll();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive())
            return;

        if (currentState == SlotState.WaitingForCareClick)
        {
            if (currentNeed == ChickenNeed.Feed && (FeedManager.Instance == null || !FeedManager.Instance.TryConsumeFeed(1)))
            {
                if (UIAlertPanel.Instance != null)
                {
                    UIAlertPanel.Instance.Show("Pakan habis, beli di shop terlebih dahulu.");
                }
                else
                {
                    GameLog.Info($"{name}: Pakan tidak cukup! Beli pakan dulu.");
                }
                return;
            }

            if (TryCompleteCurrentNeedByActiveIoT())
                return;

            if (TryStartHealthMinigame())
                return;

            CompleteCurrentNeed();
            if (SFXManager.Instance != null) SFXManager.Instance.PlayCareComplete();
            return;
        }

        if (currentState == SlotState.WaitingForSellClick)
        {
            int finalReward = sellReward;
            if (CoinManager.Instance != null)
                CoinManager.Instance.AddCoin(sellReward);

            GameLog.Info($"{name}: {CurrentChickenCount} ayam dijual, +{finalReward} coin.");
            if (SFXManager.Instance != null) SFXManager.Instance.PlaySellComplete();
            ClearChicken();
        }
    }

    public void OnHealthCheckSuccess()
    {
        if (currentState != SlotState.WaitingForHealthMinigame)
            return;

        if (SFXManager.Instance != null) SFXManager.Instance.PlayHealthSuccess();
        CompleteCurrentNeed();
    }

    public void OnHealthCheckFailure()
    {
        if (currentState != SlotState.WaitingForHealthMinigame)
            return;

        if (SFXManager.Instance != null) SFXManager.Instance.PlayHealthFail();
        FailCurrentNeedAndAdvance("puzzle gagal");
    }

    private void FailCurrentNeedAndAdvance(string reason)
    {
        StopBubbleExpiryTimer();
        ResetAnimationToNormal();
        MarkCurrentNeedFailed();
        completedCareCount++;
        RecalculateSellReward();
        SaveManager.SaveAll();

        if (IsReadyToSell())
        {
            ShowSellBubble();
            return;
        }

        HideBubble();
        isWanderingPaused = false;
        StartNeedTimer();
        GameLog.Info($"{name}: Kebutuhan {GetNeedText(currentNeed)} gagal ({reason}), lanjut kebutuhan berikutnya.");
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

        Sprite needSprite = GetNeedSprite(currentNeed);
        ShowBubble(needSprite, GetNeedText(currentNeed));
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        UpdateAnimationByNeed(currentNeed);
        StartBubbleExpiryTimer();
        SaveManager.SaveAll();
        GameLog.Info($"{name}: Notifikasi {GetNeedText(currentNeed)} muncul.");
    }

    private void StartBubbleExpiryTimer()
    {
        StopBubbleExpiryTimer();
        if (bubbleExpiryDuration <= 0f)
            return; // no expiry for this level
        CoroutineHelper.StopAndStart(this, ref bubbleExpiryCoroutine, BubbleExpiryRoutine());
    }

    private void StopBubbleExpiryTimer()
    {
        CoroutineHelper.StopSafe(this, ref bubbleExpiryCoroutine);
    }

    private IEnumerator BubbleExpiryRoutine()
    {
        float remaining = bubbleExpiryDuration;

        while (remaining > 0f)
        {
            bool shouldTick = currentState == SlotState.WaitingForCareClick
                && IsGameplayActive()
                && !IsPuzzleActive();

            if (shouldTick)
                remaining -= Time.deltaTime;

            yield return null;
        }

        if (currentState == SlotState.WaitingForCareClick)
            FailCurrentNeedAndAdvance("bubble expired");
    }

    private bool IsGameplayActive()
    {
        if (GameManager.Instance != null)
            return GameManager.Instance.IsGameActive();

        if (GameStateManager.Instance != null)
            return GameStateManager.Instance.CurrentState == GameState.Playing;

        return true;
    }

    private bool IsPuzzleActive()
    {
        if (JigsawMinigameController.Instance != null && JigsawMinigameController.Instance.IsPlaying)
            return true;
        if (MemoryMatchController.Instance != null && MemoryMatchController.Instance.IsPlaying)
            return true;
        return false;
    }

    private bool TryCompleteCurrentNeedByActiveIoT()
    {
        if (StarterIoTController.Instance == null)
            return false;

        string iotKey = GetIoTKeyForNeed(currentNeed);
        if (string.IsNullOrEmpty(iotKey))
            return false;

        if (StarterIoTController.Instance.IsActiveForNeed(iotKey))
        {
            GameLog.Info($"{name}: IoT {iotKey} aktif, kebutuhan {GetNeedText(currentNeed)} selesai dengan tap tanpa minigame.");
            UpdateAnimationByNeed(currentNeed);
            CompleteCurrentNeed();
            if (SFXManager.Instance != null) SFXManager.Instance.PlayCareComplete();
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
        StopBubbleExpiryTimer();
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
        SaveManager.SaveAll();

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

    private void RecalculateSellReward()
    {
        int failCount = (feedFailed ? 1 : 0) + (coolingFailed ? 1 : 0) + (heatingFailed ? 1 : 0);
        sellReward = Mathf.Max(0, GameConstants.Economy.BaseSellPrice - failCount * GameConstants.Economy.FailPenalty);
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

    public SaveManager.SlotSaveData GetSaveData()
    {
        bool hasActiveCareBubble = occupied
            && (currentState == SlotState.WaitingForCareClick || currentState == SlotState.WaitingForHealthMinigame);

        return new SaveManager.SlotSaveData
        {
            slotId = SlotId,
            occupied = occupied,
            prefabName = placedPrefabName,
            feedSatisfied = feedSatisfied,
            coolingSatisfied = coolingSatisfied,
            heatingSatisfied = heatingSatisfied,
            feedFailed = feedFailed,
            coolingFailed = coolingFailed,
            heatingFailed = heatingFailed,
            completedCareCount = completedCareCount,
            sellReward = sellReward,
            hasActiveBubble = hasActiveCareBubble,
            activeBubbleNeed = hasActiveCareBubble ? (int)currentNeed : -1
        };
    }

    public void RestoreFromSave(SaveManager.SlotSaveData data, System.Func<string, GameObject> prefabLookup)
    {
        ClearChicken(false);
        if (!data.occupied) return;

        GameObject prefab = null;
        if (!string.IsNullOrEmpty(data.prefabName) && prefabLookup != null)
            prefab = prefabLookup(data.prefabName);
        if (prefab == null)
            prefab = chickenVisual;

        if (prefab == null) return;

        int visualCount = Mathf.Max(1, chickensPerPurchase);
        for (int i = 0; i < visualCount; i++)
        {
            GameObject visual = CreateChickenVisual(prefab);
            if (visual != null)
                spawnedChickens.Add(visual);
        }

        placedPrefabName = data.prefabName;
        occupied = true;
        feedSatisfied = data.feedSatisfied;
        coolingSatisfied = data.coolingSatisfied;
        heatingSatisfied = data.heatingSatisfied;
        feedFailed = data.feedFailed;
        coolingFailed = data.coolingFailed;
        heatingFailed = data.heatingFailed;
        completedCareCount = data.completedCareCount;
        sellReward = data.sellReward;
        currentState = SlotState.WaitingForCareEvent;

        PositionAllChickenVisuals();

        if (IsReadyToSell())
        {
            ShowSellBubble();
        }
        else if (data.hasActiveBubble && data.activeBubbleNeed >= 0 && data.activeBubbleNeed <= 2)
        {
            currentNeed = (ChickenNeed)data.activeBubbleNeed;
            Sprite needSprite = GetNeedSprite(currentNeed);
            ShowBubble(needSprite, GetNeedText(currentNeed));
            currentState = SlotState.WaitingForCareClick;
            NotifyStateChanged();
            UpdateAnimationByNeed(currentNeed);
            StartBubbleExpiryTimer();
        }
        else
        {
            isWanderingPaused = false;
            StartNeedTimer();
        }

        UpdateWanderState();
        NotifyStateChanged();
    }

}
