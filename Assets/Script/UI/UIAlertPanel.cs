using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIAlertPanel : MonoBehaviour
{
    public static UIAlertPanel Instance { get; private set; }

    [Header("Notification Panels (Drag from Hierarchy)")]
    [SerializeField] private GameObject foodOutPanel;      // PakanHabis
    [SerializeField] private GameObject coinOutPanel;      // DuidHabis
    [SerializeField] private GameObject timeOutPanel;      // WaktuHabis
    [SerializeField] private GameObject mainMenuConfirmPanel; // MainMenu

    [Header("Auto-hide Duration")]
    [SerializeField] private float autoHideDelay = 3f;

    private Coroutine autoHideCoroutine;

    public enum NotificationType
    {
        FoodOut,
        CoinOut,
        TimeOut,
        MainMenuConfirm
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
            Instance = null;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Nonaktifkan semua panel di awal
        SetAllPanelsActive(false);
    }

    private void SetAllPanelsActive(bool active)
    {
        if (foodOutPanel != null) foodOutPanel.SetActive(active);
        if (coinOutPanel != null) coinOutPanel.SetActive(active);
        if (timeOutPanel != null) timeOutPanel.SetActive(active);
        if (mainMenuConfirmPanel != null) mainMenuConfirmPanel.SetActive(active);
    }

    public void Show(NotificationType type, System.Action onConfirm = null)
    {
        // Hentikan auto-hide yang sedang berjalan
        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);
        SetAllPanelsActive(false);

        switch (type)
        {
            case NotificationType.FoodOut:
                if (foodOutPanel != null)
                {
                    foodOutPanel.SetActive(true);
                    autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(foodOutPanel));
                }
                break;
            case NotificationType.CoinOut:
                if (coinOutPanel != null)
                {
                    coinOutPanel.SetActive(true);
                    autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(coinOutPanel));
                }
                break;
            case NotificationType.TimeOut:
                if (timeOutPanel != null)
                {
                    timeOutPanel.SetActive(true);
                    autoHideCoroutine = StartCoroutine(AutoHideAfterDelay(timeOutPanel));
                }
                break;
            case NotificationType.MainMenuConfirm:
                if (mainMenuConfirmPanel != null)
                {
                    mainMenuConfirmPanel.SetActive(true);
                    SetupMainMenuButtons(onConfirm);
                }
                break;
        }
    }

    private IEnumerator AutoHideAfterDelay(GameObject panel)
    {
        yield return new WaitForSeconds(autoHideDelay);
        if (panel != null) panel.SetActive(false);
        autoHideCoroutine = null;
    }

    private void SetupMainMenuButtons(System.Action onConfirm)
    {
        if (mainMenuConfirmPanel == null) return;

        Button kembali = FindButtonInChildren(mainMenuConfirmPanel.transform, "KembaliBut");
        Button lanjutkan = FindButtonInChildren(mainMenuConfirmPanel.transform, "LanjutkanBut");

        if (kembali != null)
        {
            kembali.onClick.RemoveAllListeners();
            kembali.onClick.AddListener(() => HideMainMenuConfirm());
        }
        if (lanjutkan != null)
        {
            lanjutkan.onClick.RemoveAllListeners();
            lanjutkan.onClick.AddListener(() =>
            {
                HideMainMenuConfirm();
                onConfirm?.Invoke();
            });
        }
    }

    private Button FindButtonInChildren(Transform parent, string buttonName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == buttonName)
            {
                Button btn = child.GetComponent<Button>();
                if (btn != null) return btn;
            }
            Button found = FindButtonInChildren(child, buttonName);
            if (found != null) return found;
        }
        return null;
    }

    private void HideMainMenuConfirm()
    {
        if (mainMenuConfirmPanel != null)
            mainMenuConfirmPanel.SetActive(false);
    }

    public void HideAll()
    {
        if (autoHideCoroutine != null) StopCoroutine(autoHideCoroutine);
        SetAllPanelsActive(false);
    }
}