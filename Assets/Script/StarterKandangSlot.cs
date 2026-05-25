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

    [Header("Jigsaw Puzzle Textures")]
    [SerializeField] private Texture jigsawFeedTexture;
    [SerializeField] private Texture jigsawCoolingTexture;
    [SerializeField] private Texture jigsawHeatingTexture;

    [Header("Animation")]
    [SerializeField] private Animator chickenAnimator;
    [SerializeField] private string idleAnimParam = "";
    [SerializeField] private string heatAnimParam = "isbakar";
    [SerializeField] private string coldAnimParam = "isdingin";

    [Header("Chicken Wander")]
    [SerializeField] private bool enableWander = true;
    [SerializeField] private Vector2 wanderRadius = new Vector2(50f, 20f);
    [SerializeField] private float wanderSpeed = 80f;
    [SerializeField] private float wanderPauseMin = 1.5f;
    [SerializeField] private float wanderPauseMax = 3.5f;

    private GameObject spawnedChicken;
    private Coroutine eventCoroutine;
    private Coroutine wanderCoroutine;
    private bool isWanderingPaused;
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
        {
            StartNeedTimer();
            if (enableWander) StartWander();
        }
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
        if (enableWander) StartWander();
        return true;
    }

    public void ClearChicken()
    {
        StopEventTimer();
        StopWander();

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
        isWanderingPaused = false;
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

    private void StartWander()
    {
        StopWander();
        isWanderingPaused = false;
        wanderCoroutine = StartCoroutine(WanderRoutine());
    }

    private void StopWander()
    {
        CoroutineHelper.StopSafe(this, ref wanderCoroutine);
    }

    private void PauseWander()
    {
        isWanderingPaused = true;
    }

    private IEnumerator WanderRoutine()
    {
        RectTransform visualRect = GetActiveVisualRect();
        if (visualRect == null)
            yield break;

        Vector2 startPos = chickenVisualOffset;
        Vector2 target = startPos;

        while (true)
        {
            if (!isWanderingPaused && occupied)
            {
                float speedMult = 1f;
                float radiusMult = 1f;
                float pauseMin = wanderPauseMin;
                float pauseMax = wanderPauseMax;
                bool canMove = true;

                if (currentState == SlotState.WaitingForCareClick)
                {
                    if (currentNeed == ChickenNeed.Feed)
                        canMove = false;
                    else
                    {
                        speedMult = 3f;
                        radiusMult = 2f;
                        pauseMin = 0.1f;
                        pauseMax = 0.4f;
                    }
                }
                else if (currentState == SlotState.WaitingForSellClick)
                {
                    canMove = false;
                }

                if (!canMove)
                {
                    yield return null;
                    continue;
                }

                float dist = Vector2.Distance(visualRect.anchoredPosition, target);

                if (dist < 2f)
                {
                    target = new Vector2(
                        startPos.x + Random.Range(-wanderRadius.x * radiusMult, wanderRadius.x * radiusMult),
                        startPos.y + Random.Range(-wanderRadius.y * radiusMult, wanderRadius.y * radiusMult)
                    );

                    float pause = Random.Range(pauseMin, pauseMax);
                    yield return new WaitForSeconds(pause);
                }
                else
                {
                    Vector2 oldPos = visualRect.anchoredPosition;
                    visualRect.anchoredPosition = Vector2.MoveTowards(
                        oldPos, target, wanderSpeed * speedMult * Time.deltaTime
                    );
                    UpdateChickenFacing(oldPos, visualRect.anchoredPosition);
                }
            }

            yield return null;
        }
    }

    private void UpdateChickenFacing(Vector2 from, Vector2 to)
    {
        RectTransform visualRect = GetActiveVisualRect();
        if (visualRect == null) return;

        float dirX = to.x - from.x;
        if (Mathf.Abs(dirX) < 0.01f) return;

        Vector3 scale = visualRect.localScale;
        bool facingRight = scale.x < 0;
        bool movingRight = dirX > 0;

        if (movingRight && !facingRight)
            scale.x = -Mathf.Abs(scale.x);
        else if (!movingRight && facingRight)
            scale.x = Mathf.Abs(scale.x);

        visualRect.localScale = scale;
    }

    private RectTransform GetActiveVisualRect()
    {
        Transform visual = spawnedChicken != null ? spawnedChicken.transform : (chickenVisual != null ? chickenVisual.transform : null);
        return visual != null ? visual as RectTransform : null;
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
        if (chickenAnimator == null || !occupied)
            return;

        TrySetAnimationBool(heatAnimParam, false);
        TrySetAnimationBool(coldAnimParam, false);
        TrySetAnimationBool(idleAnimParam, true);
    }

    private bool TryStartHealthMinigame()
    {
        if (!useHealthMinigame)
            return false;

        if (JigsawMinigameController.Instance != null && JigsawMinigameController.Instance.IsPlaying)
            return true;

        JigsawMinigameController jigsawController = JigsawMinigameController.GetOrCreateInstance();
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
        if (chickenAnimator == null || string.IsNullOrWhiteSpace(paramName))
            return;

        if (!HasAnimatorParameter(paramName))
        {
            Debug.LogWarning($"{name}: Animator parameter '{paramName}' tidak ditemukan.");
            return;
        }

        chickenAnimator.SetBool(paramName, value);
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
