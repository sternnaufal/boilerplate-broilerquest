using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StarterTutorialController : MonoBehaviour
{
    public enum TutorialStep
    {
        Welcome,
        OpenShop,
        BuyChicken,
        FirstCare,
        FirstMinigame,
        SecondCare,
        SecondMinigame,
        SellBubble,
        WaitingForSell,
        Done
    }

    [Header("UI References (Drag dari Editor)")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button nextButton;

    [Header("Target Elements (Drag per-step)")]
    [SerializeField] private GameObject targetHPButton;
    [SerializeField] private GameObject targetShopAPK;
    [SerializeField] private GameObject targetKandangArea;

    [Header("Blocking")]
    [SerializeField] private GameObject blockerPanel;

    [Header("Text Content")]
    [SerializeField] private string welcomeTitle = "Selamat Datang!";
    [SerializeField] private string welcomeBody = "Mari mulai beternak ayam. Ikuti langkah-langkah berikut.";
    [SerializeField] private string openShopTitle = "Buka Shop APK";
    [SerializeField] private string openShopBody = "Tekan tombol HP untuk membuka Shop APK.";
    [SerializeField] private string buyTitle = "Beli Ayam";
    [SerializeField] private string buyBody = "Pilih ayam untuk dibeli. Perhatikan coinmu!";
    [SerializeField] private string firstCareTitle = "Rawat Ayam (Pakan)";
    [SerializeField] private string firstCareBody = "Tekan bubble kebutuhan pada kandang untuk memberi pakan.";
    [SerializeField] private string firstMinigameTitle = "Selesaikan Mini-Game";
    [SerializeField] private string firstMinigameBody = "Selesaikan puzzle pakan untuk memberi makan ayam.";
    [SerializeField] private string secondCareTitle = "Rawat Ayam (Dingin)";
    [SerializeField] private string secondCareBody = "Tekan bubble kebutuhan kedua untuk mendinginkan ayam.";
    [SerializeField] private string secondMinigameTitle = "Selesaikan Mini-Game";
    [SerializeField] private string secondMinigameBody = "Selesaikan puzzle untuk mendinginkan ayam.";
    [SerializeField] private string sellTitle = "Jual Ayam!";
    [SerializeField] private string sellBody = "Tekan bubble ayam siap dijual. Kamu akan mendapatkan uang!";

    [Header("Timing")]
    [SerializeField] private float fadeDuration = 0.3f;

    private TutorialStep currentStep = TutorialStep.Welcome;
    private StarterChickenShop chickenShop;
    private StarterKandangSlot[] kandangSlots;
    private int initialCoin;
    private bool subscribedToSlots;
    private CanvasGroup panelCanvasGroup;
    private CanvasGroup blockerCanvasGroup;

    private Transform originalParent;
    private int originalSiblingIndex;

    public bool IsTutorialActive { get; private set; }
    public bool IsTutorialDone { get; private set; }

    public event Action OnTutorialCompleted;
    public event Action OnChickenBought;

    private void Awake()
    {
        panelCanvasGroup = EnsureCanvasGroup(tutorialPanel);
        blockerCanvasGroup = EnsureCanvasGroup(blockerPanel);

        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);
        if (blockerPanel != null)
            blockerPanel.SetActive(false);
    }

    public void StartTutorial(StarterChickenShop shop, StarterKandangSlot[] slots)
    {
        chickenShop = shop;
        kandangSlots = slots;

        if (ShouldSkipTutorial())
        {
            IsTutorialDone = true;
            NotifyDone();
            return;
        }

        IsTutorialActive = true;
        IsTutorialDone = false;
        initialCoin = CoinManager.Instance != null ? CoinManager.Instance.GetTotalCoin() : 0;

        SubscribeToSlots();
        SetStep(TutorialStep.Welcome);
    }

    private bool ShouldSkipTutorial()
    {
        if (DemoModeConfig.IsDemoMode)
            return false;
        return PlayerPrefs.GetInt(GameConstants.Persistence.StarterTutorialDoneKey, 0) == 1;
    }

    public void MarkTutorialDone()
    {
        if (!DemoModeConfig.IsDemoMode)
        {
            PlayerPrefs.SetInt(GameConstants.Persistence.StarterTutorialDoneKey, 1);
            PlayerPrefs.Save();
        }

        IsTutorialDone = true;
        IsTutorialActive = false;
        UnsubscribeFromSlots();
        RestoreTarget();
        HideBlocker();
        HidePanel();
        NotifyDone();
    }

    private void SetStep(TutorialStep step)
    {
        currentStep = step;

        switch (step)
        {
            case TutorialStep.Welcome:
                ShowPanel(welcomeTitle, welcomeBody);
                ShowTarget(null);
                ShowBlocker();
                SetupNextButton(() => SetStep(TutorialStep.OpenShop));
                break;

            case TutorialStep.OpenShop:
                ShowPanel(openShopTitle, openShopBody);
                ShowTarget(targetHPButton);
                ShowBlocker();
                if (nextButton != null) nextButton.gameObject.SetActive(false);
                break;

            case TutorialStep.BuyChicken:
                ShowPanel(buyTitle, buyBody);
                ShowTarget(targetShopAPK);
                ShowBlocker();
                if (nextButton != null) nextButton.gameObject.SetActive(false);
                break;

            case TutorialStep.FirstCare:
                ShowPanel(firstCareTitle, firstCareBody);
                ShowTarget(targetKandangArea);
                ShowBlocker();
                if (nextButton != null) nextButton.gameObject.SetActive(false);
                break;

            case TutorialStep.FirstMinigame:
                ShowPanel(firstMinigameTitle, firstMinigameBody);
                ShowTarget(null);
                HideBlocker();
                if (nextButton != null) nextButton.gameObject.SetActive(false);
                break;

            case TutorialStep.SecondCare:
                ShowPanel(secondCareTitle, secondCareBody);
                ShowTarget(targetKandangArea);
                ShowBlocker();
                if (nextButton != null) nextButton.gameObject.SetActive(false);
                break;

            case TutorialStep.SecondMinigame:
                ShowPanel(secondMinigameTitle, secondMinigameBody);
                ShowTarget(null);
                HideBlocker();
                if (nextButton != null) nextButton.gameObject.SetActive(false);
                break;

            case TutorialStep.SellBubble:
                ShowPanel(sellTitle, sellBody);
                ShowTarget(targetKandangArea);
                ShowBlocker();
                if (nextButton != null) nextButton.gameObject.SetActive(false);
                break;

            case TutorialStep.WaitingForSell:
                HidePanel();
                ShowTarget(null);
                HideBlocker();
                break;

            case TutorialStep.Done:
                MarkTutorialDone();
                break;
        }
    }

    private void Update()
    {
        if (!IsTutorialActive) return;

        switch (currentStep)
        {
            case TutorialStep.OpenShop:
                if (targetShopAPK != null && targetShopAPK.activeSelf)
                    SetStep(TutorialStep.BuyChicken);
                break;

            case TutorialStep.BuyChicken:
                int buyCoin = CoinManager.Instance != null ? CoinManager.Instance.GetTotalCoin() : 0;
                if (buyCoin < initialCoin)
                {
                    OnChickenBought?.Invoke();
                    SetStep(TutorialStep.FirstCare);
                }
                break;

            case TutorialStep.WaitingForSell:
                int sellCoin = CoinManager.Instance != null ? CoinManager.Instance.GetTotalCoin() : 0;
                if (sellCoin > initialCoin)
                    SetStep(TutorialStep.Done);
                break;
        }
    }

    private void OnSlotStateChanged(StarterKandangSlot slot)
    {
        if (!IsTutorialActive) return;

        switch (currentStep)
        {
            case TutorialStep.FirstCare:
                if (slot.CurrentState == StarterKandangSlot.SlotState.WaitingForHealthMinigame)
                {
                    SetStep(TutorialStep.FirstMinigame);
                    StartCoroutine(WaitForMinigameThenAdvance(TutorialStep.SecondCare, slot));
                }
                break;

            case TutorialStep.SecondCare:
                if (slot.CurrentState == StarterKandangSlot.SlotState.WaitingForHealthMinigame)
                {
                    SetStep(TutorialStep.SecondMinigame);
                    StartCoroutine(WaitForMinigameThenAutoComplete(slot));
                }
                break;

            case TutorialStep.SellBubble:
                if (slot.CurrentState == StarterKandangSlot.SlotState.Empty)
                {
                    initialCoin = CoinManager.Instance != null ? CoinManager.Instance.GetTotalCoin() : 0;
                    SetStep(TutorialStep.WaitingForSell);
                }
                break;
        }
    }

    private IEnumerator WaitForMinigameThenAdvance(TutorialStep nextStep, StarterKandangSlot slot)
    {
        yield return new WaitUntil(() =>
            currentStep == TutorialStep.FirstMinigame
            && slot != null
            && slot.CurrentState != StarterKandangSlot.SlotState.WaitingForHealthMinigame);

        if (currentStep == TutorialStep.FirstMinigame)
            SetStep(nextStep);
    }

    private IEnumerator WaitForMinigameThenAutoComplete(StarterKandangSlot slot)
    {
        yield return new WaitUntil(() =>
            currentStep == TutorialStep.SecondMinigame
            && slot != null
            && (slot.CurrentState == StarterKandangSlot.SlotState.WaitingForCareClick
                || slot.CurrentState == StarterKandangSlot.SlotState.WaitingForSellClick));

        if (currentStep != TutorialStep.SecondMinigame) yield break;

        if (slot.CurrentState == StarterKandangSlot.SlotState.WaitingForSellClick)
        {
            SetStep(TutorialStep.SellBubble);
            yield break;
        }

        slot.AutoCompleteNeed();

        yield return null;

        if (currentStep != TutorialStep.SellBubble)
            SetStep(TutorialStep.SellBubble);
    }

    private void SubscribeToSlots()
    {
        if (subscribedToSlots || kandangSlots == null) return;
        foreach (var slot in kandangSlots)
        {
            if (slot != null)
                slot.StateChanged += OnSlotStateChanged;
        }
        subscribedToSlots = true;
    }

    private void UnsubscribeFromSlots()
    {
        if (!subscribedToSlots || kandangSlots == null) return;
        foreach (var slot in kandangSlots)
        {
            if (slot != null)
                slot.StateChanged -= OnSlotStateChanged;
        }
        subscribedToSlots = false;
    }

    private void ShowPanel(string title, string body)
    {
        if (tutorialPanel == null) return;
        if (!tutorialPanel.activeSelf)
            tutorialPanel.SetActive(true);
        if (titleText != null) titleText.text = title;
        if (bodyText != null) bodyText.text = body;
        if (panelCanvasGroup != null)
            StartCoroutine(Fade(panelCanvasGroup, 0f, 1f));
    }

    private void HidePanel()
    {
        if (tutorialPanel == null) return;
        if (panelCanvasGroup != null)
            StartCoroutine(Fade(panelCanvasGroup, 1f, 0f, () => tutorialPanel.SetActive(false)));
        else
            tutorialPanel.SetActive(false);
    }

    private void ShowBlocker()
    {
        if (blockerPanel == null) return;
        if (!blockerPanel.activeSelf)
            blockerPanel.SetActive(true);
        if (blockerCanvasGroup != null)
        {
            blockerCanvasGroup.alpha = 0f;
            blockerCanvasGroup.blocksRaycasts = true;
            blockerCanvasGroup.interactable = false;
        }
    }

    private void HideBlocker()
    {
        if (blockerPanel == null) return;
        if (blockerCanvasGroup != null)
            blockerCanvasGroup.blocksRaycasts = false;
        blockerPanel.SetActive(false);
    }

    private void ShowTarget(GameObject target)
    {
        RestoreTarget();

        if (target == null) return;

        Canvas targetCanvas = target.GetComponentInParent<Canvas>();
        if (targetCanvas != null && tutorialPanel != null)
        {
            Canvas tutorialCanvas = tutorialPanel.GetComponentInParent<Canvas>();
            if (tutorialCanvas != null && targetCanvas != tutorialCanvas)
            {
                originalParent = target.transform.parent;
                originalSiblingIndex = target.transform.GetSiblingIndex();
                target.transform.SetParent(tutorialPanel.transform);
                target.transform.SetAsLastSibling();
            }
        }
    }

    private void RestoreTarget()
    {
        if (originalParent != null)
        {
            Transform lastChild = tutorialPanel != null && tutorialPanel.transform.childCount > 0
                ? tutorialPanel.transform.GetChild(tutorialPanel.transform.childCount - 1)
                : null;

            if (lastChild != null)
            {
                lastChild.SetParent(originalParent);
                lastChild.SetSiblingIndex(originalSiblingIndex);
            }

            originalParent = null;
            originalSiblingIndex = 0;
        }
    }

    private void SetupNextButton(Action callback)
    {
        if (nextButton == null) return;
        nextButton.gameObject.SetActive(true);
        nextButton.onClick.RemoveAllListeners();
        if (callback != null)
            nextButton.onClick.AddListener(() => callback.Invoke());
    }

    private IEnumerator Fade(CanvasGroup cg, float from, float to, Action onComplete = null)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        cg.alpha = to;
        onComplete?.Invoke();
    }

    private void NotifyDone()
    {
        OnTutorialCompleted?.Invoke();
    }

    private static CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    private void OnDestroy()
    {
        UnsubscribeFromSlots();
        RestoreTarget();
    }
}
