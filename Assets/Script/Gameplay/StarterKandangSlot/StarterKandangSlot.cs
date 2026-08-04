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
    [SerializeField] private Sprite humidityUpBubbleSprite;
    [SerializeField] private Sprite humidityDownBubbleSprite;
    [SerializeField] private Sprite addDryHuskBubbleSprite;
    [SerializeField] private Sprite reduceFeedBubbleSprite;
    [SerializeField] private Sprite sellBubbleSprite;

    [Header("Bubble Visual")]
    [SerializeField] private GameObject bubbleVisual;
    [SerializeField] private Image bubbleImage;
    [SerializeField] private TextMeshProUGUI bubbleLabel;
    [SerializeField] private string feedBubbleText = "MAKAN";
    [SerializeField] private string coolingBubbleText = "KIPAS";
    [SerializeField] private string heatingBubbleText = "HEATER";
    [SerializeField] private string humidityUpBubbleText = "LEMBAB";
    [SerializeField] private string humidityDownBubbleText = "KERING";
    [SerializeField] private string addDryHuskBubbleText = "SEKAM";
    [SerializeField] private string reduceFeedBubbleText = "KURANGI";
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

    [Header("Wiring Minigame Config")]
    [SerializeField] private int wiringPairCount = 4;
    [SerializeField] private float wiringTimeLimit = 30f;

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

    private List<ChickenNeed> needsQueue;
    private bool[] needSatisfied;
    private bool[] needFailed;
    private int currentNeedIndex;
    private int sellReward;
    private SlotState currentState;

    public ChickenNeed CurrentNeed => currentNeedIndex >= 0 && currentNeedIndex < needsQueue?.Count
        ? needsQueue[currentNeedIndex] : ChickenNeed.Feed;
    public int TotalNeedCount => needsQueue?.Count ?? 0;
    public int CompletedCareCount => completedCareCount;

    private int completedCareCount
    {
        get
        {
            if (needSatisfied == null || needFailed == null) return 0;
            int len = Mathf.Min(needSatisfied.Length, needFailed.Length);
            int count = 0;
            for (int i = 0; i < len; i++)
                if (needSatisfied[i] || needFailed[i]) count++;
            return count;
        }
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
        SetBubbleExpiryByLevel();
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

    private void SetBubbleExpiryByLevel()
    {
        if (GameManager.Instance == null)
        {
            GameLog.Warn($"{name}: GameManager.Instance is null in SetBubbleExpiryByLevel, defaulting to Starter (no expiry).");
            bubbleExpiryDuration = GameConstants.StarterSlot.BubbleExpiryDurationStarter;
            return;
        }

        switch (GameManager.Instance.currentLevelIndex)
        {
            case 0:
                bubbleExpiryDuration = GameConstants.StarterSlot.BubbleExpiryDurationStarter;
                break;
            case 1:
                bubbleExpiryDuration = GameConstants.StarterSlot.BubbleExpiryDurationBeginner;
                break;
            case 2:
                bubbleExpiryDuration = GameConstants.StarterSlot.BubbleExpiryDurationIntermediate;
                break;
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
        catch (System.Exception e)
        {
            GameLog.Error($"{name}: Exception in TryPlaceChicken: {e.Message}\n{e.StackTrace}");
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
        StopAllEffects();

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
            ChickenNeed need = CurrentNeed;

            if (need == ChickenNeed.Feed && (FeedManager.Instance == null || !FeedManager.Instance.UseFeed(1)))
            {
                if (UIAlertPanel.Instance != null)
                {
                    UIAlertPanel.Instance.Show(UIAlertPanel.NotificationType.FoodOut);
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
            PlaySellEffect();
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

    private int GetCurrentNeedIndex()
    {
        if (needsQueue == null) return -1;
        for (int i = 0; i < needsQueue.Count; i++)
            if (!needSatisfied[i] && !needFailed[i])
                return i;
        return -1;
    }

    private ChickenNeed GetNeedAt(int index)
    {
        if (needsQueue == null || index < 0 || index >= needsQueue.Count)
            return ChickenNeed.Feed;
        return needsQueue[index];
    }

    private void FailCurrentNeedAndAdvance(string reason)
    {
        StopBubbleExpiryTimer();
        ResetAnimationToNormal();

        int idx = currentNeedIndex;
        if (idx >= 0 && idx < needFailed.Length)
            needFailed[idx] = true;

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
        GameLog.Info($"{name}: Kebutuhan {GetNeedText(GetNeedAt(idx))} gagal ({reason}), lanjut kebutuhan berikutnya.");
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
        currentNeedIndex = GetCurrentNeedIndex();
        if (currentNeedIndex < 0)
        {
            ShowSellBubble();
            return;
        }

        ChickenNeed need = needsQueue[currentNeedIndex];
        Sprite needSprite = GetNeedSprite(need);
        ShowBubble(needSprite, GetNeedText(need));
        currentState = SlotState.WaitingForCareClick;
        NotifyStateChanged();
        UpdateAnimationByNeed(need);
        StartBubbleExpiryTimer();
        SaveManager.SaveAll();
        GameLog.Info($"{name}: Notifikasi {GetNeedText(need)} muncul.");
    }

    private void StartBubbleExpiryTimer()
    {
        StopBubbleExpiryTimer();
        if (bubbleExpiryDuration <= 0f)
            return;
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
        if (WiringMinigameController.Instance != null && WiringMinigameController.Instance.IsPlaying)
            return true;
        if (HumidityToggleController.Instance != null && HumidityToggleController.Instance.IsPlaying)
            return true;
        if (PipelinePuzzleController.Instance != null && PipelinePuzzleController.Instance.IsPlaying)
            return true;
        if (DragDropSackController.Instance != null && DragDropSackController.Instance.IsPlaying)
            return true;
        if (HoldSwipeController.Instance != null && HoldSwipeController.Instance.IsPlaying)
            return true;
        return false;
    }

    private bool TryCompleteCurrentNeedByActiveIoT()
    {
        if (StarterIoTController.Instance == null)
            return false;

        string iotKey = GetIoTKeyForNeed(CurrentNeed);
        if (string.IsNullOrEmpty(iotKey))
            return false;

        if (StarterIoTController.Instance.IsActiveForNeed(iotKey))
        {
            GameLog.Info($"{name}: IoT {iotKey} aktif, kebutuhan {GetNeedText(CurrentNeed)} selesai dengan tap tanpa minigame.");
            UpdateAnimationByNeed(CurrentNeed);
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
                return GameConstants.IoT.ProductKeyHeater;
            case ChickenNeed.Heating:
                return GameConstants.IoT.ProductKeyFan;
            default:
                return null;
        }
    }

    private void CompleteCurrentNeed()
    {
        StopBubbleExpiryTimer();
        ResetAnimationToNormal();

        int idx = currentNeedIndex;
        if (idx >= 0 && idx < needSatisfied.Length)
            needSatisfied[idx] = true;

        RecalculateSellReward();
        SaveManager.SaveAll();

        UIAlertPanel.Instance?.Show(UIAlertPanel.NotificationType.NeedFulfilled);

        if (IsReadyToSell())
        {
            ShowSellBubble();
            return;
        }

        HideBubble();
        StartNeedTimer();
        GameLog.Info($"{name}: Kebutuhan {GetNeedText(GetNeedAt(idx))} terpenuhi.");
    }

    private void ShowSellBubble()
    {
        ShowBubble(sellBubbleSprite, sellBubbleText);
        currentState = SlotState.WaitingForSellClick;
        NotifyStateChanged();
        UIAlertPanel.Instance?.Show(UIAlertPanel.NotificationType.ReadyToSell);
        GameLog.Info($"{name}: Semua kebutuhan terpenuhi, ayam siap dijual.");
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this);
    }

    private void RecalculateSellReward()
    {
        int failCount = 0;
        if (needFailed != null)
        {
            for (int i = 0; i < needFailed.Length; i++)
                if (needFailed[i]) failCount++;
        }
        sellReward = Mathf.Max(20, GameConstants.Economy.BaseSellPrice - failCount * GameConstants.Economy.FailPenalty);
    }

    private Sprite GetNeedSprite(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Feed: return feedBubbleSprite;
            case ChickenNeed.Cooling: return coolingBubbleSprite;
            case ChickenNeed.Heating: return heatingBubbleSprite;
            case ChickenNeed.HumidityUp: return humidityUpBubbleSprite;
            case ChickenNeed.HumidityDown: return humidityDownBubbleSprite;
            case ChickenNeed.AddDryHusk: return addDryHuskBubbleSprite;
            case ChickenNeed.ReduceFeed: return reduceFeedBubbleSprite;
            default: return feedBubbleSprite;
        }
    }

    private string GetNeedText(ChickenNeed need)
    {
        switch (need)
        {
            case ChickenNeed.Feed: return feedBubbleText;
            case ChickenNeed.Cooling: return coolingBubbleText;
            case ChickenNeed.Heating: return heatingBubbleText;
            case ChickenNeed.HumidityUp: return humidityUpBubbleText;
            case ChickenNeed.HumidityDown: return humidityDownBubbleText;
            case ChickenNeed.AddDryHusk: return addDryHuskBubbleText;
            case ChickenNeed.ReduceFeed: return reduceFeedBubbleText;
            default: return feedBubbleText;
        }
    }

    private bool IsReadyToSell()
    {
        return completedCareCount >= (needsQueue?.Count ?? 0);
    }

    private void ResetChickenProgress()
    {
        needsQueue = GenerateNeedsQueue();
        int count = needsQueue.Count;
        needSatisfied = new bool[count];
        needFailed = new bool[count];
        currentNeedIndex = 0;
        RecalculateSellReward();
        ResetAnimationToNormal();
    }

    private List<ChickenNeed> GenerateNeedsQueue()
    {
        int level = GameManager.Instance != null ? GameManager.Instance.currentLevelIndex : 0;

        // Starter: fixed order [Feed, Cooling, Heating]
        if (level == 0)
        {
            return new List<ChickenNeed> { ChickenNeed.Feed, ChickenNeed.Cooling, ChickenNeed.Heating };
        }

        // Beginner/Intermediate: Feed (wajib) + random subset from pool
        var queue = new List<ChickenNeed>();
        queue.Add(ChickenNeed.Feed);

        var pool = GetNeedPool();
        pool.Remove(ChickenNeed.Feed);

        int totalNeeds = GetTotalNeedsCount();
        int remaining = totalNeeds - 1;

        for (int i = 0; i < remaining && pool.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, pool.Count);
            queue.Add(pool[randomIndex]);
            pool.RemoveAt(randomIndex);
        }

        return queue;
    }

    private List<ChickenNeed> GetNeedPool()
    {
        var pool = new List<ChickenNeed>();
        int level = GameManager.Instance != null ? GameManager.Instance.currentLevelIndex : 0;

        switch (level)
        {
            case 0:
                pool.AddRange(new[] { ChickenNeed.Feed, ChickenNeed.Cooling, ChickenNeed.Heating });
                break;
            case 1:
                pool.AddRange(new[] { ChickenNeed.Feed, ChickenNeed.Cooling, ChickenNeed.Heating,
                                      ChickenNeed.HumidityUp, ChickenNeed.HumidityDown });
                break;
            case 2:
                pool.AddRange(new[] { ChickenNeed.Feed, ChickenNeed.Cooling, ChickenNeed.Heating,
                                      ChickenNeed.HumidityUp, ChickenNeed.HumidityDown,
                                      ChickenNeed.AddDryHusk, ChickenNeed.ReduceFeed });
                break;
        }

        return pool;
    }

    private int GetTotalNeedsCount()
    {
        int level = GameManager.Instance != null ? GameManager.Instance.currentLevelIndex : 0;
        switch (level)
        {
            case 0: return GameConstants.Difficulty.StarterSteps;
            case 1: return GameConstants.Difficulty.BeginnerSteps;
            case 2: return GameConstants.Difficulty.IntermediateSteps;
            default: return GameConstants.Difficulty.StarterSteps;
        }
    }

    public SaveManager.SlotSaveData GetSaveData()
    {
        bool hasActiveCareBubble = occupied
            && (currentState == SlotState.WaitingForCareClick || currentState == SlotState.WaitingForHealthMinigame);

        int[] needQueueArr = needsQueue?.ConvertAll(n => (int)n).ToArray();
        int activeNeed = hasActiveCareBubble && currentNeedIndex >= 0 ? (int)needsQueue[currentNeedIndex] : -1;

        return new SaveManager.SlotSaveData
        {
            slotId = SlotId,
            occupied = occupied,
            prefabName = placedPrefabName,
            needQueue = needQueueArr,
            needSatisfied = needSatisfied,
            needFailed = needFailed,
            currentNeedIndex = currentNeedIndex,
            completedCareCount = completedCareCount,
            sellReward = sellReward,
            hasActiveBubble = hasActiveCareBubble,
            activeBubbleNeed = activeNeed
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

        if (prefab == null)
        {
            Debug.LogWarning($"{name}: RestoreFromSave gagal — prefab '{data.prefabName}' tidak ditemukan dan chickenVisual tidak di-assign di Inspector.");
            return;
        }

        int visualCount = Mathf.Max(1, chickensPerPurchase);
        for (int i = 0; i < visualCount; i++)
        {
            GameObject visual = CreateChickenVisual(prefab);
            if (visual != null)
                spawnedChickens.Add(visual);
        }

        placedPrefabName = data.prefabName;
        occupied = true;

        if (data.needQueue != null && data.needQueue.Length > 0)
        {
            needsQueue = new List<ChickenNeed>(System.Array.ConvertAll(data.needQueue, n => (ChickenNeed)n));
            needSatisfied = data.needSatisfied ?? new bool[needsQueue.Count];
            needFailed = data.needFailed ?? new bool[needsQueue.Count];
            currentNeedIndex = data.currentNeedIndex;
        }
        else
        {
            ResetChickenProgress();
        }

        sellReward = data.sellReward;
        currentState = SlotState.WaitingForCareEvent;

        PositionAllChickenVisuals();

        if (IsReadyToSell())
        {
            ShowSellBubble();
        }
        else if (data.hasActiveBubble)
        {
            int idx = -1;
            for (int i = 0; i < needsQueue.Count; i++)
                if (!needSatisfied[i] && !needFailed[i]) { idx = i; break; }

            if (idx >= 0)
            {
                currentNeedIndex = idx;
                ChickenNeed need = needsQueue[idx];
                Sprite needSprite = GetNeedSprite(need);
                ShowBubble(needSprite, GetNeedText(need));
                currentState = SlotState.WaitingForCareClick;
                NotifyStateChanged();
                UpdateAnimationByNeed(need);
                StartBubbleExpiryTimer();
            }
            else
            {
                isWanderingPaused = false;
                StartNeedTimer();
            }
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
